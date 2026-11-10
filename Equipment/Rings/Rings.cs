using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Registers the custom rings separately from weapons and armor.
    /// Passive ring effects reuse the game's own feature definitions.
    /// Active rings use the game's spell-device system and recharge at dawn.
    /// </summary>
    public static class RingItems
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";

        public static bool Register()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            if (itemDb == null)
            {
                Debug.LogError(LogPrefix + "Ring registration failed: item database unavailable.");
                return false;
            }

            FreeMovementEquipDiagnostic.Initialize();
            bool freeMovement = RegisterFreeMovement(itemDb);
            bool knock = RegisterSpellRing(itemDb,
                "RingOfKnock", "Ring of Knock",
                "Knock", null,
                "A magical ring that lets its wearer cast Knock three times. It recharges at dawn.",
                "Uncommon", false, "Instantaneous");

            bool invisibility = RegisterSpellRing(itemDb,
                "RingOfInvisibility", "Ring of Invisibility",
                "Invisibility", "RingOfInvisibilitySpell3Minutes",
                "A magical ring that lets its wearer cast Invisibility three times. Each casting lasts 3 minutes. It recharges at dawn.",
                "VeryRare", true, "Minute");

            bool slotRings = RingSlotItems.Register(itemDb);
            bool additionalRings = RegisterAdditionalRings(itemDb);

            Debug.Log(LogPrefix + "Ring registration results: Free Movement=" + freeMovement +
                ", Knock=" + knock + ", Invisibility=" + invisibility +
                ", Spell-slot rings=" + slotRings +
                ", Additional rings=" + additionalRings + ".");

            return freeMovement && knock && invisibility && slotRings && additionalRings;
        }

        private static bool RegisterAdditionalRings(Database<ItemDefinition> itemDb)
        {
            bool success = true;
            success &= RegisterDailySpellRing(itemDb, "RingOfJump", "Ring of Jump", "Uncommon",
                new[] { "Jump" }, "Jump", 3);
            success &= RegisterDailySpellRing(itemDb, "RingOfDarkness", "Ring of Darkness", "Rare",
                new[] { "Darkness", "Darkvision" }, "Darkness and Darkvision", 3);
            success &= RegisterDailySpellRing(itemDb, "RingOfSilence", "Ring of Silence", "Uncommon",
                new[] { "Silence" }, "Silence", 3);
            success &= RegisterDailySpellRing(itemDb, "RingOfSpiderClimbing", "Ring of Spider Climbing", "Rare",
                new[] { "SpiderClimb" }, "Spider Climb", 3);
            success &= RegisterDailySpellRing(itemDb, "RingOfDispelMagic", "Ring of Dispel Magic", "Rare",
                new[] { "DispelMagic" }, "Dispel Magic", 3);
            success &= RegisterSpellResistanceRing(itemDb);

            success &= RegisterElementalRing(itemDb, "RingOfTheFireElemental", "Ring of the Fire Elemental",
                "Fire", new[] { "SummonFireElemental", "ConjureFireElemental", "ConjureElementalFire" });
            success &= RegisterElementalRing(itemDb, "RingOfTheAirElemental", "Ring of the Air Elemental",
                "Air", new[] { "SummonAirElemental", "ConjureAirElemental", "ConjureElementalAir" });
            success &= RegisterElementalRing(itemDb, "RingOfTheEarthElemental", "Ring of the Earth Elemental",
                "Earth", new[] { "SummonEarthElemental", "ConjureEarthElemental", "ConjureElementalEarth" });
            return success;
        }

        private static bool RegisterDailySpellRing(
            Database<ItemDefinition> itemDb, string itemId, string title, string rarity,
            string[] spellIds, string spellNames, int uses)
        {
            if (itemDb.GetElement(itemId, true) != null)
                return true;

            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            if (spellDb == null)
                return false;

            List<KeyValuePair<SpellDefinition, int>> spellUses = new List<KeyValuePair<SpellDefinition, int>>();
            foreach (string spellId in spellIds)
            {
                SpellDefinition spell = FindSpell(spellDb, spellId, null);
                if (spell == null)
                {
                    Debug.LogError(LogPrefix + "Could not find spell '" + spellId + "' for " + title + ".");
                    return false;
                }
                spellUses.Add(new KeyValuePair<SpellDefinition, int>(spell, uses));
            }

            return RegisterDailySpellRing(itemDb, itemId, title, rarity,
                "A magical ring that lets its wearer cast " + spellNames + " " + uses + " times per day. It recharges at dawn.",
                spellUses);
        }

        private static bool RegisterElementalRing(
            Database<ItemDefinition> itemDb, string itemId, string title, string element, string[] spellIds)
        {
            if (itemDb.GetElement(itemId, true) != null)
                return true;

            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            SpellDefinition spell = null;
            foreach (string spellId in spellIds)
            {
                spell = FindSpell(spellDb, spellId, null);
                if (spell != null)
                    break;
            }
            if (spell == null)
                spell = FindElementalSummoningSpell(spellDb, element);

            if (spell == null)
            {
                Debug.LogError(LogPrefix + "Could not find a " + element + " elemental summoning spell for " + title + ".");
                return false;
            }

            return RegisterDailySpellRing(itemDb, itemId, title, "VeryRare",
                "Summons a " + element.ToLowerInvariant() + " elemental twice per day. Recharges at dawn.",
                new List<KeyValuePair<SpellDefinition, int>>
                {
                    new KeyValuePair<SpellDefinition, int>(spell, 2)
                });
        }

        private static SpellDefinition FindSpell(
            Database<SpellDefinition> spellDb, string spellId, string titleFragment)
        {
            if (spellDb == null)
                return null;

            SpellDefinition exact = spellDb.GetElement(spellId, true);
            if (exact != null)
                return exact;

            if (string.IsNullOrEmpty(titleFragment))
                return null;

            foreach (SpellDefinition candidate in spellDb.GetAllElements())
            {
                string id = candidate == null ? string.Empty : candidate.Name ?? string.Empty;
                string title = candidate == null || candidate.GuiPresentation == null
                    ? string.Empty : candidate.GuiPresentation.Title ?? string.Empty;
                if (id.IndexOf(titleFragment, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    title.IndexOf(titleFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return candidate;
            }

            return null;
        }

        private static SpellDefinition FindElementalSummoningSpell(
            Database<SpellDefinition> spellDb, string element)
        {
            if (spellDb == null)
                return null;

            foreach (SpellDefinition candidate in spellDb.GetAllElements())
            {
                if (candidate == null || candidate.GuiPresentation == null)
                    continue;

                string id = candidate.Name ?? string.Empty;
                string title = candidate.GuiPresentation.Title ?? string.Empty;
                string combined = id + " " + title;
                bool matchingElement = combined.IndexOf(element, StringComparison.OrdinalIgnoreCase) >= 0;
                bool elementalSummon = combined.IndexOf("elemental", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (combined.IndexOf("summon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     combined.IndexOf("conjure", StringComparison.OrdinalIgnoreCase) >= 0);
                if (matchingElement && elementalSummon)
                    return candidate;
            }

            return null;
        }

        private static bool RegisterDailySpellRing(
            Database<ItemDefinition> itemDb, string itemId, string title, string rarity,
            string description, List<KeyValuePair<SpellDefinition, int>> spellUses)
        {
            try
            {
                if (itemDb.GetElement(itemId, true) != null)
                    return true;

                ItemDefinition template = FindItemTemplate(itemDb, "RingOfTheLightbringers", "FingerSlot", true);
                UsableDeviceDescription device = CreateSpellFunctionsByRecharge(itemDb, spellUses);
                if (template == null || device == null)
                    return false;

                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null)
                    return false;
                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;
                SetPresentation(item, template, title, description);
                SetMember(item, "magical", true);
                SetMember(item, "requiresAttunement", true);
                SetEnumMember(item, "itemRarity", rarity);
                SetMember(item, "staticProperties", new List<ItemPropertyDescription>());
                SetMember(item, "usableDeviceDescription", device);
                SetMember(item, "isUsableDevice", true);
                itemDb.Add(item);
                return itemDb.GetElement(itemId, true) != null;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + title + ": " + ex);
                return false;
            }
        }

        private static bool RegisterSpellResistanceRing(Database<ItemDefinition> itemDb)
        {
            const string itemId = "RingOfSpellResistance";
            const string title = "Ring of Spell Resistance";
            if (itemDb.GetElement(itemId, true) != null)
                return true;

            ItemDefinition mantle = itemDb.GetElement("MantleOfSpellResistance", true);
            if (mantle == null)
            {
                foreach (ItemDefinition candidate in itemDb.GetAllElements())
                {
                    string candidateTitle = candidate == null || candidate.GuiPresentation == null
                        ? string.Empty : candidate.GuiPresentation.Title ?? string.Empty;
                    if (candidateTitle.IndexOf("Mantle of Spell Resistance", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mantle = candidate;
                        break;
                    }
                }
            }

            ItemDefinition template = FindItemTemplate(itemDb, "RingDarkvision", "FingerSlot", false);
            if (mantle == null || mantle.StaticProperties == null || mantle.StaticProperties.Count == 0 || template == null)
            {
                Debug.LogError(LogPrefix + "Could not copy the spell-resistance feature to " + title + ".");
                return false;
            }

            ItemDefinition item = UnityEngine.Object.Instantiate(template);
            if (item == null)
                return false;
            item.ForceName(itemId);
            item.SetUserContentGUID(Guid.NewGuid().ToString());
            item.IsUserContent = true;
            SetPresentation(item, template, title,
                "Grants the same protection from spells as the Mantle of Spell Resistance.");
            SetMember(item, "magical", true);
            SetMember(item, "requiresAttunement", true);
            SetEnumMember(item, "itemRarity", "VeryRare");
            List<ItemPropertyDescription> properties = new List<ItemPropertyDescription>();
            foreach (ItemPropertyDescription sourceProperty in mantle.StaticProperties)
                if (sourceProperty != null)
                    properties.Add(new ItemPropertyDescription(sourceProperty));
            SetMember(item, "staticProperties", properties);
            SetMember(item, "isUsableDevice", false);
            SetMember(item, "usableDeviceDescription", null);
            itemDb.Add(item);
            return itemDb.GetElement(itemId, true) != null;
        }

        private static bool RegisterFreeMovement(Database<ItemDefinition> itemDb)
        {
            const string itemId = "RingOfFreeMovement";
            const string displayName = "Ring of Free Movement";

            try
            {
                if (itemDb.GetElement(itemId, true) != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition baseRing = FindItemTemplate(itemDb, "RingDarkvision", "FingerSlot", false);
                if (baseRing == null)
                {
                    Debug.LogError(LogPrefix + "Could not find a ring item template for " + displayName + ".");
                    return false;
                }

                Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
                ConditionDefinition freedomCondition = conditionDb == null
                    ? null
                    : conditionDb.GetElement("ConditionFreedomOfMovement", true);

                if (freedomCondition == null)
                {
                    Debug.LogError(LogPrefix + "Could not find the game's ConditionFreedomOfMovement definition.");
                    return false;
                }

                // TEMPORARY EXPANDED DIAGNOSTIC: inspect the condition, its features,
                // movement-affinity settings, and comparable terrain-related definitions.
                RunFreedomMovementDiagnostic(freedomCondition);

                ItemDefinition item = UnityEngine.Object.Instantiate(baseRing);
                if (item == null)
                    return false;

                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                if (!SetPresentation(item, baseRing, displayName,
                    "Grants the effects of Freedom of Movement while worn: ignores difficult terrain and prevents the Paralyzed and Restrained conditions."))
                    Debug.LogWarning(LogPrefix + "Could not completely set the presentation for " + displayName + ".");

                SetMember(item, "magical", true);
                SetMember(item, "requiresAttunement", true);
                SetEnumMember(item, "itemRarity", "Rare");
                SetMember(item, "isUsableDevice", false);
                SetMember(item, "usableDeviceDescription", null);

                List<ItemPropertyDescription> properties = new List<ItemPropertyDescription>();
                ItemPropertyDescription propertyTemplate = FindPropertyTemplate(itemDb);
                if (propertyTemplate == null)
                {
                    Debug.LogError(LogPrefix + "Could not find an item-property template for " + displayName + ".");
                    return false;
                }

                int added = AddConditionFeaturesAsProperties(freedomCondition, properties, propertyTemplate);
                if (added < 1)
                {
                    Debug.LogError(LogPrefix + "Could not attach Freedom of Movement features to " + displayName + ".");
                    return false;
                }

                if (!SetMember(item, "staticProperties", properties))
                {
                    Debug.LogError(LogPrefix + "Could not set passive features for " + displayName + ".");
                    return false;
                }

                // Confirm the ring definition itself contains all three original
                // Freedom of Movement features before it enters the item database.
                LogFreeMovementStaticProperties(item);

                itemDb.Add(item);
                bool registered = itemDb.GetElement(itemId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered " + displayName + " with " + added + " permanent Freedom of Movement features.");
                else
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");

                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + displayName + ": " + ex);
                return false;
            }
        }

        private static bool RegisterSpellRing(
            Database<ItemDefinition> itemDb,
            string itemId,
            string displayName,
            string baseSpellId,
            string customSpellId,
            string description,
            string rarity,
            bool requiresAttunement,
            string functionDuration)
        {
            try
            {
                if (itemDb.GetElement(itemId, true) != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition baseRing = FindItemTemplate(itemDb, "RingOfTheLightbringers", "FingerSlot", true);
                if (baseRing == null)
                {
                    Debug.LogError(LogPrefix + "Could not find a usable ring template for " + displayName + ".");
                    return false;
                }

                Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
                if (spellDb == null)
                {
                    Debug.LogError(LogPrefix + "Spell database unavailable for " + displayName + ".");
                    return false;
                }

                SpellDefinition spell = string.IsNullOrEmpty(customSpellId)
                    ? spellDb.GetElement(baseSpellId, true)
                    : GetTimedSpell(spellDb, baseSpellId, customSpellId, 3);

                if (spell == null)
                {
                    Debug.LogError(LogPrefix + "Could not find spell '" + baseSpellId + "' for " + displayName + ".");
                    return false;
                }

                ItemDefinition item = UnityEngine.Object.Instantiate(baseRing);
                if (item == null)
                    return false;

                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                if (!SetPresentation(item, baseRing, displayName, description))
                    Debug.LogWarning(LogPrefix + "Could not completely set the presentation for " + displayName + ".");

                SetMember(item, "magical", true);
                SetMember(item, "requiresAttunement", requiresAttunement);
                SetEnumMember(item, "itemRarity", rarity);
                SetMember(item, "staticProperties", new List<ItemPropertyDescription>());

                if (!AttachSpellFunction(item, baseRing, spell, functionDuration, 3))
                {
                    Debug.LogError(LogPrefix + "Could not configure the spell function for " + displayName + ".");
                    return false;
                }

                itemDb.Add(item);
                bool registered = itemDb.GetElement(itemId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered " + displayName + " with 3 charges, recharging at dawn.");
                else
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");

                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + displayName + ": " + ex);
                return false;
            }
        }
        internal static ItemDefinition FindWandTemplate(Database<ItemDefinition> itemDb)
        {
            ItemDefinition preferred = itemDb.GetElement("WandOfLightningBolts", true);
            if (IsSuitableTemplate(preferred, "UtilitySlot", true))
                return preferred;

            foreach (ItemDefinition item in itemDb.GetAllElements())
            {
                if (!IsSuitableTemplate(item, "UtilitySlot", true))
                    continue;

                string name = item.Name ?? string.Empty;
                string title = item.GuiPresentation == null ? string.Empty : (item.GuiPresentation.Title ?? string.Empty);
                if (name.IndexOf("Wand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    title.IndexOf("Wand", StringComparison.OrdinalIgnoreCase) >= 0)
                    return item;
            }

            return null;
        }

        internal static UsableDeviceDescription CreateSpellFunctionsByRecharge(
            Database<ItemDefinition> itemDb,
            List<KeyValuePair<SpellDefinition, int>> spellUses)
        {
            if (spellUses == null || spellUses.Count == 0)
                return null;

            ItemDefinition template = FindWandTemplate(itemDb);
            UsableDeviceDescription baseDescription = template == null ? null : template.UsableDeviceDescription;
            if (baseDescription == null || baseDescription.DeviceFunctions == null || baseDescription.DeviceFunctions.Count == 0)
            {
                Debug.LogError(LogPrefix + "Could not find a usable wand function template for a multi-spell device.");
                return null;
            }

            UsableDeviceDescription device = new UsableDeviceDescription(baseDescription);
            device.DeviceFunctions.Clear();
            bool usageSet = SetEnumMember(device, "usage", "ByFunction");
            if (!usageSet)
                return null;

            DeviceFunctionDescription functionTemplate = baseDescription.DeviceFunctions[0];
            foreach (KeyValuePair<SpellDefinition, int> spellUse in spellUses)
            {
                if (spellUse.Key == null || spellUse.Value < 1)
                    return null;

                DeviceFunctionDescription function = new DeviceFunctionDescription(functionTemplate);
                bool typeSet = SetEnumMember(function, "type", "Spell");
                bool spellSet = SetMember(function, "spellDefinition", spellUse.Key);
                bool powerCleared = SetMember(function, "featureDefinitionPower", null);
                bool parentUsageSet = SetEnumMember(function, "parentUsage", "ByFunction");
                bool usesSet = SetMember(function, "useAmount", spellUse.Value);
                bool affinitySet = SetEnumMember(function, "useAffinity", "IterationPerRecharge");
                bool rechargeSet = SetEnumMember(function, "rechargeRate", "Dawn");
                bool durationSet = SetMember(function, "durationType", spellUse.Key.EffectDescription.DurationType);
                bool overchargeSet = SetMember(function, "canOverchargeSpell", false);

                if (!typeSet || !spellSet || !powerCleared || !parentUsageSet || !usesSet ||
                    !affinitySet || !rechargeSet || !durationSet || !overchargeSet)
                {
                    Debug.LogError(LogPrefix + "Could not configure the daily spell function for " + spellUse.Key.Name + ".");
                    return null;
                }

                device.DeviceFunctions.Add(function);
            }

            return device;
        }

        internal static UsableDeviceDescription CreateSpellFunctionsByCharges(
            Database<ItemDefinition> itemDb,
            List<KeyValuePair<SpellDefinition, int>> spellCosts,
            int chargePool)
        {
            if (spellCosts == null || spellCosts.Count == 0 || chargePool < 1)
                return null;

            ItemDefinition template = FindWandTemplate(itemDb);
            UsableDeviceDescription baseDescription = template == null ? null : template.UsableDeviceDescription;
            if (baseDescription == null || baseDescription.DeviceFunctions == null || baseDescription.DeviceFunctions.Count == 0)
                return null;

            UsableDeviceDescription device = new UsableDeviceDescription(baseDescription);
            device.DeviceFunctions.Clear();
            DeviceFunctionDescription functionTemplate = baseDescription.DeviceFunctions[0];

            foreach (KeyValuePair<SpellDefinition, int> spellCost in spellCosts)
            {
                if (spellCost.Key == null || spellCost.Value < 1 || spellCost.Value > chargePool)
                    return null;

                DeviceFunctionDescription function = new DeviceFunctionDescription(functionTemplate);
                bool typeSet = SetEnumMember(function, "type", "Spell");
                bool spellSet = SetMember(function, "spellDefinition", spellCost.Key);
                bool powerCleared = SetMember(function, "featureDefinitionPower", null);
                bool parentUsageSet = SetEnumMember(function, "parentUsage", "Charges");
                bool usesSet = SetMember(function, "useAmount", spellCost.Value);
                bool affinitySet = SetEnumMember(function, "useAffinity", "ChargeCost");
                bool rechargeSet = SetEnumMember(function, "rechargeRate", "Dawn");
                bool durationSet = SetMember(function, "durationType", spellCost.Key.EffectDescription.DurationType);
                bool overchargeSet = SetMember(function, "canOverchargeSpell", false);
                if (!typeSet || !spellSet || !powerCleared || !parentUsageSet || !usesSet ||
                    !affinitySet || !rechargeSet || !durationSet || !overchargeSet)
                    return null;

                device.DeviceFunctions.Add(function);
            }

            bool usageSet = SetEnumMember(device, "usage", "Charges");
            bool capitalSet = SetEnumMember(device, "chargesCapital", "Fixed");
            bool capitalNumberSet = SetMember(device, "chargesCapitalNumber", chargePool);
            bool capitalDieSet = SetEnumMember(device, "chargesCapitalDie", "D1");
            bool capitalBonusSet = SetMember(device, "chargesCapitalBonus", 0);
            bool rechargeSetDevice = SetEnumMember(device, "rechargeRate", "Dawn");
            bool rechargeNumberSet = SetMember(device, "rechargeNumber", chargePool);
            bool rechargeDieSet = SetEnumMember(device, "rechargeDie", "D1");
            bool rechargeBonusSet = SetMember(device, "rechargeBonus", 0);
            bool outOfChargesSet = SetEnumMember(device, "outOfChargesConsequence", "Persist");
            if (!usageSet || !capitalSet || !capitalNumberSet || !capitalDieSet || !capitalBonusSet ||
                !rechargeSetDevice || !rechargeNumberSet || !rechargeDieSet || !rechargeBonusSet || !outOfChargesSet)
                return null;

            return device;
        }

        internal static bool AttachSpellFunction(
            ItemDefinition item,
            ItemDefinition deviceTemplate,
            SpellDefinition spell,
            string duration,
            int charges)
        {
            try
            {
                UsableDeviceDescription baseDescription = deviceTemplate.UsableDeviceDescription;
                if (baseDescription == null || baseDescription.DeviceFunctions == null || baseDescription.DeviceFunctions.Count == 0)
                {
                    Debug.LogError(LogPrefix + "The ring template has no usable-device function to copy.");
                    return false;
                }

                UsableDeviceDescription device = new UsableDeviceDescription(baseDescription);
                device.DeviceFunctions.Clear();

                DeviceFunctionDescription templateFunction = baseDescription.DeviceFunctions[0];
                DeviceFunctionDescription function = new DeviceFunctionDescription(templateFunction);

                // Each cast costs one charge; the device recharges the configured amount at dawn.
                bool typeSet = SetEnumMember(function, "type", "Spell");
                bool spellSet = SetMember(function, "spellDefinition", spell);
                bool powerCleared = SetMember(function, "featureDefinitionPower", null);
                bool parentUsageSet = SetEnumMember(function, "parentUsage", "Charges");
                bool usesSet = SetMember(function, "useAmount", 1);
                bool affinitySet = SetEnumMember(function, "useAffinity", "ChargeCost");
                bool functionRechargeSet = SetEnumMember(function, "rechargeRate", "Dawn");
                bool durationSet = SetEnumMember(function, "durationType", duration);
                bool overchargeSet = SetMember(function, "canOverchargeSpell", false);

                bool usageSet = SetEnumMember(device, "usage", "Charges");
                bool capitalSet = SetEnumMember(device, "chargesCapital", "Fixed");
                bool capitalNumberSet = SetMember(device, "chargesCapitalNumber", charges);
                bool capitalDieSet = SetEnumMember(device, "chargesCapitalDie", "D1");
                bool capitalBonusSet = SetMember(device, "chargesCapitalBonus", 0);
                bool rechargeSet = SetEnumMember(device, "rechargeRate", "Dawn");
                bool rechargeNumberSet = SetMember(device, "rechargeNumber", charges);
                bool rechargeDieSet = SetEnumMember(device, "rechargeDie", "D1");
                bool rechargeBonusSet = SetMember(device, "rechargeBonus", 0);
                bool outOfChargesSet = SetEnumMember(device, "outOfChargesConsequence", "Persist");

                if (!typeSet || !spellSet || !powerCleared || !parentUsageSet || !usesSet ||
                    !affinitySet || !functionRechargeSet || !durationSet || !overchargeSet ||
                    !usageSet || !capitalSet || !capitalNumberSet || !capitalDieSet ||
                    !capitalBonusSet || !rechargeSet || !rechargeNumberSet || !rechargeDieSet ||
                    !rechargeBonusSet || !outOfChargesSet)
                {
                    Debug.LogError(LogPrefix + "Could not configure a three-charge spell ring. " +
                        "Set flags: type=" + typeSet + ", spell=" + spellSet +
                        ", parentUsage=" + parentUsageSet + ", affinity=" + affinitySet +
                        ", usage=" + usageSet + ", charges=" + capitalNumberSet +
                        ", recharge=" + rechargeSet + ", refill=" + rechargeBonusSet + ".");
                    return false;
                }

                device.DeviceFunctions.Add(function);

                bool deviceSet = SetMember(item, "usableDeviceDescription", device);
                bool usableSet = SetMember(item, "isUsableDevice", true);
                if (!deviceSet || !usableSet)
                {
                    Debug.LogError(LogPrefix + "Could not attach the configured charge-based spell device to " + item.Name + ".");
                    return false;
                }

                Debug.Log(LogPrefix + item.Name + " configured for " + charges + " charges, 1 charge per cast, recharging at dawn.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + "Spell-device setup failed: " + ex);
                return false;
            }
        }

        internal static SpellDefinition GetTimedSpell(
            Database<SpellDefinition> spellDb,
            string baseSpellId,
            string customSpellId,
            int minutes)
        {
            SpellDefinition existing = spellDb.GetElement(customSpellId, true);
            if (existing != null)
            {
                SetSpellDuration(existing, minutes);
                return existing;
            }

            SpellDefinition baseSpell = spellDb.GetElement(baseSpellId, true);
            if (baseSpell == null)
                return null;

            SpellDefinition spell = UnityEngine.Object.Instantiate(baseSpell);
            if (spell == null)
                return null;

            spell.ForceName(customSpellId);
            spell.SetUserContentGUID(Guid.NewGuid().ToString());
            spell.IsUserContent = true;

            if (!SetSpellDuration(spell, minutes))
            {
                Debug.LogError(LogPrefix + "Could not set the custom Invisibility spell to " + minutes + " minutes.");
                return null;
            }

            spellDb.Add(spell);
            SpellDefinition registered = spellDb.GetElement(customSpellId, true);
            return registered ?? spell;
        }

        private static bool SetSpellDuration(SpellDefinition spell, int minutes)
        {
            object effect = ReadMember(spell, "EffectDescription", "effectDescription");
            if (effect == null)
                return false;

            return SetEnumMember(effect, "DurationType", "Minute") &&
                SetMember(effect, "DurationParameter", minutes);
        }

        private static int AddConditionFeaturesAsProperties(
            ConditionDefinition condition,
            List<ItemPropertyDescription> properties,
            ItemPropertyDescription propertyTemplate)
        {
            object featureCollection = ReadMember(condition, "Features", "features");
            IEnumerable enumerable = featureCollection as IEnumerable;
            if (enumerable == null)
                return 0;

            int added = 0;
            foreach (object feature in enumerable)
            {
                if (feature == null)
                    continue;

                ItemPropertyDescription property = new ItemPropertyDescription(propertyTemplate);
                bool appliesSet = SetMember(property, "appliesOnItemOnly", false);
                bool typeSet = SetEnumMember(property, "type", "Feature");
                bool featureSet = SetMember(property, "featureDefinition", feature);
                bool conditionSet = SetMember(property, "conditionDefinition", null);
                bool knowledgeSet = SetEnumMember(property, "knowledgeAffinity", "ActiveAndVisible");

                if (appliesSet && typeSet && featureSet && conditionSet && knowledgeSet)
                {
                    properties.Add(property);
                    added++;
                }
            }

            return added;
        }

        private static void RunFreedomMovementDiagnostic(ConditionDefinition condition)
        {
            Debug.Log(LogPrefix + "=== FREEDOM MOVEMENT EXPANDED DIAGNOSTIC START ===");

            DumpDiagnosticMembers("CONDITION", condition);

            object featureCollection = ReadMember(condition, "Features", "features");
            IEnumerable features = featureCollection as IEnumerable;
            int featureIndex = 0;

            if (features != null)
            {
                foreach (object feature in features)
                {
                    if (feature == null)
                        continue;

                    object featureName = ReadMember(feature, "Name", "name");
                    string name = Convert.ToString(featureName);
                    string label = "CONDITION FEATURE " + featureIndex + " [" + name + "]";

                    Debug.Log(LogPrefix + label + " TYPE=" + feature.GetType().FullName);
                    DumpDiagnosticMembers(label, feature);

                    if (feature.GetType().Name.IndexOf("MovementAffinity", StringComparison.OrdinalIgnoreCase) >= 0)
                        DumpDiagnosticMembers("MOVEMENT AFFINITY DETAIL", feature);

                    featureIndex++;
                }
            }
            else
            {
                Debug.LogWarning(LogPrefix + "Condition feature collection could not be read.");
            }

            Debug.Log(LogPrefix + "CONDITION FEATURE COUNT=" + featureIndex);

            // Compare Freedom of Movement with other movement-affinity definitions.
            // This helps identify the exact terrain-related field or enum used by the game.
            try
            {
                Database<FeatureDefinitionMovementAffinity> movementDb =
                    DatabaseRepository.GetDatabase<FeatureDefinitionMovementAffinity>();

                if (movementDb == null)
                {
                    Debug.LogWarning(LogPrefix + "Movement-affinity database unavailable.");
                }
                else
                {
                    int matches = 0;
                    foreach (FeatureDefinitionMovementAffinity candidate in movementDb.GetAllElements())
                    {
                        if (candidate == null)
                            continue;

                        string candidateName = candidate.Name ?? string.Empty;
                        string searchable = candidateName + " " +
                            Convert.ToString(ReadMember(candidate, "GuiPresentation", "guiPresentation"));

                        bool relevant =
                            candidateName.IndexOf("Freedom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            candidateName.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            candidateName.IndexOf("Movement", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            searchable.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (!relevant)
                            continue;

                        matches++;
                        string label = "RELATED MOVEMENT DEFINITION [" + candidateName + "]";
                        Debug.Log(LogPrefix + label + " TYPE=" + candidate.GetType().FullName);
                        DumpDiagnosticMembers(label, candidate);
                    }

                    Debug.Log(LogPrefix + "RELATED MOVEMENT DEFINITION COUNT=" + matches);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + "Movement-affinity comparison scan failed: " + ex);
            }

            Debug.Log(LogPrefix + "=== FREEDOM MOVEMENT EXPANDED DIAGNOSTIC END ===");
        }

        private static void DumpDiagnosticMembers(string label, object target)
        {
            if (target == null)
            {
                Debug.LogWarning(LogPrefix + label + " is NULL.");
                return;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            HashSet<string> logged = new HashSet<string>();
            Type targetType = target.GetType();

            Debug.Log(LogPrefix + label + " OBJECT TYPE=" + targetType.FullName);

            for (Type type = targetType; type != null; type = type.BaseType)
            {
                FieldInfo[] fields = type.GetFields(flags | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    if (field == null || !logged.Add("F:" + field.Name))
                        continue;

                    try
                    {
                        object value = field.GetValue(target);
                        Debug.Log(LogPrefix + label + " FIELD " + type.Name + "." +
                            field.Name + " (" + field.FieldType.FullName + ") = " +
                            FormatDiagnosticValue(value));
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(LogPrefix + label + " FIELD " + field.Name +
                            " <read failed: " + ex.GetType().Name + ">");
                    }
                }

                PropertyInfo[] properties = type.GetProperties(flags | BindingFlags.DeclaredOnly);
                foreach (PropertyInfo property in properties)
                {
                    if (property == null || property.GetIndexParameters().Length != 0 ||
                        property.GetGetMethod(true) == null || !logged.Add("P:" + property.Name))
                        continue;

                    try
                    {
                        object value = property.GetValue(target, null);
                        Debug.Log(LogPrefix + label + " PROPERTY " + type.Name + "." +
                            property.Name + " (" + property.PropertyType.FullName + ") = " +
                            FormatDiagnosticValue(value));
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(LogPrefix + label + " PROPERTY " + property.Name +
                            " <read failed: " + ex.GetType().Name + ">");
                    }
                }
            }
        }

        private static string FormatDiagnosticValue(object value)
        {
            if (value == null)
                return "<null>";

            Type valueType = value.GetType();

            if (value is string || valueType.IsPrimitive || valueType.IsEnum ||
                value is decimal || value is Guid)
                return Convert.ToString(value);

            UnityEngine.Object unityObject = value as UnityEngine.Object;
            if (unityObject != null)
                return unityObject.name + " [" + valueType.FullName + "]";

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null && !(value is string))
            {
                List<string> entries = new List<string>();
                int count = 0;

                foreach (object entry in enumerable)
                {
                    if (count >= 20)
                    {
                        entries.Add("...");
                        break;
                    }

                    if (entry == null)
                        entries.Add("<null>");
                    else
                    {
                        object entryName = ReadMember(entry, "Name", "name");
                        string name = Convert.ToString(entryName);
                        entries.Add(string.IsNullOrEmpty(name)
                            ? entry.GetType().Name + ":" + Convert.ToString(entry)
                            : name + " [" + entry.GetType().Name + "]");
                    }

                    count++;
                }

                return "[" + string.Join(", ", entries.ToArray()) + "]";
            }

            object namedValue = ReadMember(value, "Name", "name");
            string namedText = Convert.ToString(namedValue);
            if (!string.IsNullOrEmpty(namedText))
                return namedText + " [" + valueType.FullName + "]";

            return valueType.FullName + " [" + Convert.ToString(value) + "]";
        }

        private static ItemPropertyDescription FindPropertyTemplate(Database<ItemDefinition> itemDb)
        {
            foreach (ItemDefinition item in itemDb.GetAllElements())
            {
                if (item == null || item.StaticProperties == null)
                    continue;

                foreach (ItemPropertyDescription property in item.StaticProperties)
                {
                    if (property != null)
                        return property;
                }
            }

            return null;
        }

        private static ItemDefinition FindItemTemplate(
            Database<ItemDefinition> itemDb,
            string preferredId,
            string requiredSlot,
            bool requireUsableDevice)
        {
            ItemDefinition preferred = itemDb.GetElement(preferredId, true);
            if (IsSuitableTemplate(preferred, requiredSlot, requireUsableDevice))
                return preferred;

            foreach (ItemDefinition item in itemDb.GetAllElements())
            {
                if (IsSuitableTemplate(item, requiredSlot, requireUsableDevice))
                    return item;
            }

            return null;
        }

        private static bool IsSuitableTemplate(ItemDefinition item, string requiredSlot, bool requireUsableDevice)
        {
            if (item == null || !HasSlot(item, requiredSlot))
                return false;

            if (requireUsableDevice)
            {
                object usable = ReadMember(item, "IsUsableDevice", "isUsableDevice");
                if (!(usable is bool) || !(bool)usable)
                    return false;

                UsableDeviceDescription device = item.UsableDeviceDescription;
                if (device == null || device.DeviceFunctions == null || device.DeviceFunctions.Count == 0)
                    return false;
            }

            return true;
        }

        private static bool HasSlot(ItemDefinition item, string slotName)
        {
            object slots = ReadMember(item, "SlotTypes", "slotTypes");
            IEnumerable enumerable = slots as IEnumerable;
            if (enumerable == null || slots is string)
                return false;

            foreach (object slot in enumerable)
            {
                if (slot == null)
                    continue;

                string name = slot as string;
                if (name == null)
                    name = Convert.ToString(ReadMember(slot, "Name", "name")) ?? Convert.ToString(slot);

                if (string.Equals(name, slotName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        internal static bool SetPresentation(ItemDefinition item, ItemDefinition template, string title, string description)
        {
            if (item == null || template == null || template.GuiPresentation == null)
                return false;

            GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
            presentation.Title = title;
            presentation.Description = description;
            return SetMember(item, "guiPresentation", presentation);
        }

        private static object ReadMember(object target, params string[] names)
        {
            if (target == null)
                return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                foreach (string name in names)
                {
                    FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                    if (field != null)
                        return field.GetValue(target);

                    PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                    if (property != null && property.GetIndexParameters().Length == 0 && property.GetGetMethod(true) != null)
                        return property.GetValue(target, null);
                }
            }

            return null;
        }

        internal static bool SetMember(object target, string name, object value)
        {
            if (target == null)
                return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    if (field.IsInitOnly)
                        return false;
                    field.SetValue(target, value);
                    return true;
                }

                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 && property.GetSetMethod(true) != null)
                {
                    property.SetValue(target, value, null);
                    return true;
                }
            }

            return false;
        }

        internal static bool SetEnumMember(object target, string name, string enumValue)
        {
            if (target == null)
                return false;

            Type memberType = GetMemberType(target.GetType(), name);
            if (memberType == null || !memberType.IsEnum)
                return false;

            try
            {
                object parsed = Enum.Parse(memberType, enumValue, true);
                return SetMember(target, name, parsed);
            }
            catch
            {
                return false;
            }
        }

        private static Type GetMemberType(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.FieldType;

                PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.PropertyType;
            }

            return null;
        }

        private static void LogFreeMovementStaticProperties(ItemDefinition item)
        {
            try
            {
                int paralyzed = 0;
                int restrained = 0;
                int movement = 0;

                if (item == null || item.StaticProperties == null)
                {
                    Debug.LogError(LogPrefix + "PRE-EQUIP CHECK: Ring staticProperties is NULL.");
                    return;
                }

                foreach (ItemPropertyDescription property in item.StaticProperties)
                {
                    if (property == null)
                        continue;

                    FeatureDefinition feature = property.FeatureDefinition;
                    string featureName = feature == null ? "<null>" : feature.Name;
                    bool difficultTerrain = false;

                    FeatureDefinitionMovementAffinity movementAffinity =
                        feature as FeatureDefinitionMovementAffinity;
                    if (movementAffinity != null)
                        difficultTerrain = movementAffinity.ImmuneDifficultTerrain;

                    if (featureName.IndexOf("Paralyzed", StringComparison.OrdinalIgnoreCase) >= 0)
                        paralyzed++;
                    if (featureName.IndexOf("Restrained", StringComparison.OrdinalIgnoreCase) >= 0)
                        restrained++;
                    if (movementAffinity != null)
                        movement++;

                    Debug.Log(LogPrefix + "PRE-EQUIP CHECK: property type=" +
                        Convert.ToString(ReadMember(property, "Type", "type")) +
                        ", feature=" + featureName +
                        ", featureType=" + (feature == null ? "<null>" : feature.GetType().Name) +
                        ", ImmuneDifficultTerrain=" + difficultTerrain);
                }

                Debug.Log(LogPrefix + "PRE-EQUIP SUMMARY: ParalyzedAffinity=" + (paralyzed > 0) +
                    ", RestrainedAffinity=" + (restrained > 0) +
                    ", MovementAffinity=" + (movement > 0) +
                    ", DifficultTerrainImmunity=" + HasDifficultTerrainImmunity(item) +
                    ", StaticPropertyCount=" + item.StaticProperties.Count);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + "PRE-EQUIP check failed: " + ex);
            }
        }

        private static bool HasDifficultTerrainImmunity(ItemDefinition item)
        {
            if (item == null || item.StaticProperties == null)
                return false;

            foreach (ItemPropertyDescription property in item.StaticProperties)
            {
                FeatureDefinitionMovementAffinity movement =
                    property == null ? null : property.FeatureDefinition as FeatureDefinitionMovementAffinity;
                if (movement != null && movement.ImmuneDifficultTerrain)
                    return true;
            }

            return false;
        }
    }
}

/// <summary>
/// Creates the class-specific spell-slot rings. Their magic-affinity features use
/// Solasta's native AdditionalSlots mechanism; the patch below limits each ring
/// to its matching class repertoire.
/// </summary>
internal static class RingSlotItems
{
    private const string LogPrefix = "[Solasta Expanded Equipment] ";
    private const string FeatureSuffix = "SpellSlotAffinity";

    private sealed class RingSpec
    {
        public string Id;
        public string Title;
        public string CasterClass;
        public string Rarity;
        public int[] Levels;
        public int[] Counts;
    }

    private static readonly RingSpec[] Rings =
    {
        Make("RingOfWizardry", "Ring of Wizardry", "Wizard", "Uncommon", new[] { 1, 2, 3 }, new[] { 3, 3, 2 }),
        Make("GreaterRingOfWizardry", "Greater Ring of Wizardry", "Wizard", "Rare", new[] { 3, 4, 5 }, new[] { 3, 2, 1 }),
        Make("RingOfArchWizardry", "Ring of Arch Wizardry", "Wizard", "VeryRare", new[] { 4, 5, 6, 7 }, new[] { 3, 3, 1, 1 }),
        Make("RingOfTheDruid", "Ring of the Druid", "Druid", "Uncommon", new[] { 1, 2, 3 }, new[] { 3, 3, 2 }),
        Make("RingOfTheGrandDruid", "Ring of the Grand Druid", "Druid", "Rare", new[] { 3, 4, 5 }, new[] { 3, 2, 1 }),
        Make("RingOfTheArchDruid", "Ring of the Arch Druid", "Druid", "VeryRare", new[] { 4, 5, 6, 7 }, new[] { 3, 3, 1, 1 }),
        Make("RingOfSorcery", "Ring of Sorcery", "Sorcerer", "Uncommon", new[] { 1, 2, 3 }, new[] { 3, 3, 2 }),
        Make("GreaterRingOfSorcery", "Greater Ring of Sorcery", "Sorcerer", "Rare", new[] { 3, 4, 5 }, new[] { 3, 2, 1 }),
        Make("RingOfArchSorcery", "Ring of Arch Sorcery", "Sorcerer", "VeryRare", new[] { 4, 5, 6, 7 }, new[] { 3, 3, 1, 1 }),
        Make("RingOfTheWarlock", "Ring of the Warlock", "Warlock", "Uncommon", new[] { 1, 2, 3 }, new[] { 2, 2, 2 }),
        Make("GreaterRingOfWarlock", "Greater Ring of Warlock", "Warlock", "Rare", new[] { 3, 4, 5 }, new[] { 2, 1, 1 }),
        Make("RingOfArchWarlock", "Ring of Arch Warlock", "Warlock", "VeryRare", new[] { 4, 5, 6, 7 }, new[] { 1, 1, 1, 1 })
    };

    private static RingSpec Make(string id, string title, string casterClass, string rarity, int[] levels, int[] counts)
    {
        return new RingSpec
        {
            Id = id,
            Title = title,
            CasterClass = casterClass,
            Rarity = rarity,
            Levels = levels,
            Counts = counts
        };
    }

    public static bool Register(Database<ItemDefinition> itemDb)
    {
        bool success = true;
        foreach (RingSpec ring in Rings)
        {
            success = RegisterOne(itemDb, ring) && success;
        }

        return success;
    }

    internal static int GetEquippedWarlockBonusSlots(object hero)
    {
        object inventory = ReadMember(hero, "CharacterInventory", "characterInventory");
        IEnumerable slots = ReadMember(inventory, "InventorySlotsByName", "inventorySlotsByName") as IEnumerable;
        if (slots == null)
            return 0;

        int bonus = 0;
        foreach (object entry in slots)
        {
            object slot = ReadMember(entry, "Value", "value");
            object slotType = ReadMember(slot, "SlotTypeDefinition", "slotTypeDefinition");
            string slotTypeName = Convert.ToString(ReadMember(slotType, "Name", "name"));
            if (!string.Equals(slotTypeName, "FingerSlot", StringComparison.OrdinalIgnoreCase))
                continue;

            object equippedItem = ReadMember(slot, "EquipedItem", "equipedItem");
            object definition = ReadMember(equippedItem, "ItemDefinition", "itemDefinition");
            string itemId = Convert.ToString(ReadMember(definition, "Name", "name"));

            foreach (RingSpec ring in Rings)
            {
                if (ring.CasterClass != "Warlock" || ring.Id != itemId)
                    continue;

                foreach (int count in ring.Counts)
                    bonus += count;
                break;
            }
        }

        return bonus;
    }

    private static bool RegisterOne(Database<ItemDefinition> itemDb, RingSpec ring)
    {
        try
        {
            // Validate or repair the feature definition even when the ring item
            // already exists in the database. This avoids silently reusing an
            // older feature with missing GUI text.
            FeatureDefinitionMagicAffinity affinity = CreateSlotAffinity(ring);
            if (affinity == null)
                return false;

            ItemDefinition existing = itemDb.GetElement(ring.Id, true);
            if (existing != null)
            {
                GuiPresentation existingPresentation = existing.GuiPresentation != null
                    ? new GuiPresentation(existing.GuiPresentation)
                    : new GuiPresentation();
                existingPresentation.Title = ring.Title;
                existingPresentation.Description = BuildDescription(ring);

                if (!SetMember(existing, "guiPresentation", existingPresentation) ||
                    existing.GuiPresentation == null ||
                    string.IsNullOrEmpty(existing.GuiPresentation.Title) ||
                    string.IsNullOrEmpty(existing.GuiPresentation.Description))
                {
                    Debug.LogError(LogPrefix + ring.Title + " exists but its GUI presentation could not be repaired.");
                    return false;
                }

                Debug.Log(LogPrefix + ring.Title + " already exists; its feature and GUI presentation were verified.");
                return true;
            }

            ItemDefinition template = FindRingTemplate(itemDb);
            if (template == null || template.GuiPresentation == null)
            {
                Debug.LogError(LogPrefix + "Could not find a ring template with a valid GUI presentation for " + ring.Title + ".");
                return false;
            }

            ItemPropertyDescription propertyTemplate = FindPropertyTemplate(itemDb);
            if (propertyTemplate == null)
            {
                Debug.LogError(LogPrefix + "Could not find an item-property template for " + ring.Title + ".");
                return false;
            }

            ItemDefinition item = UnityEngine.Object.Instantiate(template);
            if (item == null)
                return false;

            item.ForceName(ring.Id);
            item.SetUserContentGUID(Guid.NewGuid().ToString());
            item.IsUserContent = true;

            string description = BuildDescription(ring);
            GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
            presentation.Title = ring.Title;
            presentation.Description = description;

            bool presentationSet = SetMember(item, "guiPresentation", presentation);
            bool magicalSet = SetMember(item, "magical", true);
            bool attunementSet = SetMember(item, "requiresAttunement", true);
            bool raritySet = SetEnumMember(item, "itemRarity", ring.Rarity);
            bool deviceFlagSet = SetMember(item, "isUsableDevice", false);
            bool deviceDescriptionSet = SetMember(item, "usableDeviceDescription", null);

            ItemPropertyDescription property = new ItemPropertyDescription(propertyTemplate);
            bool appliesSet = SetMember(property, "appliesOnItemOnly", false);
            bool propertyTypeSet = SetEnumMember(property, "type", "Feature");
            bool featureSet = SetMember(property, "featureDefinition", affinity);
            bool conditionSet = SetMember(property, "conditionDefinition", null);
            bool knowledgeSet = SetEnumMember(property, "knowledgeAffinity", "ActiveAndVisible");

            bool propertiesSet = SetMember(item, "staticProperties", new List<ItemPropertyDescription> { property });

            if (!presentationSet || !magicalSet || !attunementSet || !raritySet ||
                !deviceFlagSet || !deviceDescriptionSet || !appliesSet || !propertyTypeSet ||
                !featureSet || !conditionSet || !knowledgeSet || !propertiesSet)
            {
                Debug.LogError(LogPrefix + "Could not fully configure " + ring.Title + ".");
                return false;
            }

            itemDb.Add(item);
            bool registered = itemDb.GetElement(ring.Id, true) != null;
            if (registered)
                Debug.Log(LogPrefix + "Registered " + ring.Title + ": " + description);
            else
                Debug.LogError(LogPrefix + "Registration verification failed for " + ring.Title + ".");

            return registered;
        }
        catch (Exception ex)
        {
            Debug.LogError(LogPrefix + "Failed to register " + ring.Title + ": " + ex);
            return false;
        }
    }

    private static FeatureDefinitionMagicAffinity CreateSlotAffinity(RingSpec ring)
    {
        Database<FeatureDefinitionMagicAffinity> featureDb =
            DatabaseRepository.GetDatabase<FeatureDefinitionMagicAffinity>();
        if (featureDb == null)
        {
            Debug.LogError(LogPrefix + "Magic-affinity database unavailable for " + ring.Title + ".");
            return null;
        }

        string featureName = ring.Id + FeatureSuffix;
        FeatureDefinitionMagicAffinity feature = featureDb.GetElement(featureName, true);
        bool isNew = feature == null;
        if (isNew)
        {
            feature = ScriptableObject.CreateInstance<FeatureDefinitionMagicAffinity>();
            if (feature == null)
                return null;

            feature.ForceName(featureName);
            feature.SetUserContentGUID(Guid.NewGuid().ToString());
            feature.IsUserContent = true;
        }

        // Unfinished Business scans feature definitions while building its
        // documentation. It expects every registered feature to have usable GUI
        // text, so repair or create that presentation before the feature enters
        // the database.
        if (!EnsureSlotAffinityPresentation(feature, featureDb, ring))
        {
            Debug.LogError(LogPrefix + "Could not create a valid GUI presentation for " + featureName + ".");
            if (isNew)
                UnityEngine.Object.Destroy(feature);
            return null;
        }

        List<AdditionalSlotsDuplet> slots = new List<AdditionalSlotsDuplet>();
        for (int i = 0; i < ring.Levels.Length; i++)
        {
            object slot = Activator.CreateInstance(typeof(AdditionalSlotsDuplet));
            if (slot == null ||
                !SetMember(slot, "slotLevel", ring.Levels[i]) ||
                !SetMember(slot, "slotsNumber", ring.Counts[i]))
            {
                Debug.LogError(LogPrefix + "Could not configure spell-slot entry for " + ring.Title + ".");
                if (isNew)
                    UnityEngine.Object.Destroy(feature);
                return null;
            }

            slots.Add((AdditionalSlotsDuplet)slot);
        }

        if (!SetMember(feature, "additionalSlots", slots))
        {
            Debug.LogError(LogPrefix + "Could not set spell-slot bonuses for " + ring.Title + ".");
            if (isNew)
                UnityEngine.Object.Destroy(feature);
            return null;
        }

        if (isNew)
        {
            featureDb.Add(feature);
            feature = featureDb.GetElement(featureName, true);
        }

        if (feature == null || feature.GuiPresentation == null ||
            string.IsNullOrEmpty(feature.GuiPresentation.Title) ||
            string.IsNullOrEmpty(feature.GuiPresentation.Description))
        {
            Debug.LogError(LogPrefix + "Spell-slot feature " + featureName +
                " failed its post-registration GUI-presentation check.");
            return null;
        }

        return feature;
    }

    private static bool EnsureSlotAffinityPresentation(
        FeatureDefinitionMagicAffinity feature,
        Database<FeatureDefinitionMagicAffinity> featureDb,
        RingSpec ring)
    {
        if (feature == null || featureDb == null || ring == null)
            return false;

        GuiPresentation sourcePresentation = feature.GuiPresentation;

        if (sourcePresentation == null || string.IsNullOrEmpty(sourcePresentation.Description))
        {
            sourcePresentation = null;

            foreach (FeatureDefinitionMagicAffinity candidate in featureDb.GetAllElements())
            {
                if (candidate == null || object.ReferenceEquals(candidate, feature) ||
                    candidate.GuiPresentation == null ||
                    string.IsNullOrEmpty(candidate.GuiPresentation.Description))
                    continue;

                sourcePresentation = candidate.GuiPresentation;
                break;
            }
        }

        // A blank presentation is a safe fallback when the source feature has no
        // usable GUI text. The title and description are assigned immediately below.
        GuiPresentation presentation = sourcePresentation != null
            ? new GuiPresentation(sourcePresentation)
            : new GuiPresentation();
        presentation.Title = ring.Title + " Spell Slot Affinity";
        presentation.Description = BuildDescription(ring);

        if (string.IsNullOrEmpty(presentation.Title) || string.IsNullOrEmpty(presentation.Description) ||
            !SetMember(feature, "guiPresentation", presentation))
            return false;

        GuiPresentation assigned = feature.GuiPresentation;
        return assigned != null &&
            !string.IsNullOrEmpty(assigned.Title) &&
            !string.IsNullOrEmpty(assigned.Description);
    }

    private static string BuildDescription(RingSpec ring)
    {
        if (ring.CasterClass == "Warlock")
        {
            int total = 0;
            foreach (int count in ring.Counts)
                total += count;

            return "While worn, grants " + total + " additional Warlock Pact Magic slots at the wearer's current slot level. " +
                "These slots are available to the Warlock spell repertoire and return on a long rest.";
        }

        List<string> bonuses = new List<string>();
        for (int i = 0; i < ring.Levels.Length; i++)
            bonuses.Add("+" + ring.Counts[i] + " " + Ordinal(ring.Levels[i]));

        return "While worn by a " + ring.CasterClass + ", grants " + string.Join(", ", bonuses.ToArray()) +
            " level spell slots. The extra slots return on a long rest.";
    }

    private static string Ordinal(int level)
    {
        if (level % 100 >= 11 && level % 100 <= 13)
            return level + "th";

        switch (level % 10)
        {
            case 1: return level + "st";
            case 2: return level + "nd";
            case 3: return level + "rd";
            default: return level + "th";
        }
    }

    private static ItemDefinition FindRingTemplate(Database<ItemDefinition> itemDb)
    {
        ItemDefinition preferred = itemDb.GetElement("RingDarkvision", true);
        if (HasFingerSlot(preferred))
            return preferred;

        foreach (ItemDefinition item in itemDb.GetAllElements())
        {
            if (HasFingerSlot(item))
                return item;
        }

        return null;
    }

    private static bool HasFingerSlot(ItemDefinition item)
    {
        if (item == null || item.SlotTypes == null)
            return false;

        foreach (string slot in item.SlotTypes)
        {
            if (string.Equals(slot, "FingerSlot", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static ItemPropertyDescription FindPropertyTemplate(Database<ItemDefinition> itemDb)
    {
        foreach (ItemDefinition item in itemDb.GetAllElements())
        {
            if (item == null || item.StaticProperties == null)
                continue;

            foreach (ItemPropertyDescription property in item.StaticProperties)
            {
                if (property != null)
                    return property;
            }
        }

        return null;
    }

    private static object ReadMember(object target, params string[] names)
    {
        if (target == null)
            return null;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(target);

                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 && property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }
        }

        return null;
    }

    private static bool SetMember(object target, string name, object value)
    {
        if (target == null)
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
            if (field != null)
            {
                if (field.IsInitOnly)
                    return false;
                field.SetValue(target, value);
                return true;
            }

            PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
            if (property != null && property.GetIndexParameters().Length == 0 && property.GetSetMethod(true) != null)
            {
                property.SetValue(target, value, null);
                return true;
            }
        }

        return false;
    }

    private static bool SetEnumMember(object target, string name, string value)
    {
        if (target == null)
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
            if (field != null && field.FieldType.IsEnum)
                return SetMember(target, name, Enum.Parse(field.FieldType, value, true));

            PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
            if (property != null && property.PropertyType.IsEnum)
                return SetMember(target, name, Enum.Parse(property.PropertyType, value, true));
        }

        return false;
    }
}

/// <summary>Filters slot affinities to the class named by each ring.</summary>
/// <summary>
/// Logs the Free Movement ring's actual equipped features after the game's
/// inventory refresh has completed.
/// </summary>
internal static class FreeMovementEquipDiagnostic
{
    private const string LogPrefix = "[Solasta Expanded Equipment] ";
    private const string RingId = "RingOfFreeMovement";
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized)
            return;

        try
        {
            MethodInfo target = AccessTools.Method(
                typeof(RulesetCharacterHero),
                "ItemEquiped",
                new[] { typeof(RulesetInventory), typeof(RulesetInventorySlot), typeof(RulesetItem) });

            if (target == null)
            {
                Debug.LogError(LogPrefix + "EQUIP CHECK: Could not find RulesetCharacterHero.ItemEquiped.");
                return;
            }

            Harmony harmony = new Harmony("SolastaExpandedEquipmentMod.FreeMovementEquipDiagnostic");
            HarmonyMethod postfix = new HarmonyMethod(
                typeof(FreeMovementEquipDiagnostic).GetMethod(
                    nameof(AfterItemEquiped), BindingFlags.Static | BindingFlags.NonPublic));

            harmony.Patch(target, postfix: postfix);
            initialized = true;
            Debug.Log(LogPrefix + "Free Movement equip diagnostic patch active.");
        }
        catch (Exception ex)
        {
            Debug.LogError(LogPrefix + "EQUIP CHECK patch initialization failed: " + ex);
        }
    }

    private static void AfterItemEquiped(
        RulesetCharacterHero __instance,
        RulesetInventorySlot __1,
        RulesetItem __2)
    {
        try
        {
            if (__instance == null || __2 == null || __2.ItemDefinition == null)
                return;

            ItemDefinition item = __2.ItemDefinition;
            if (!string.Equals(item.Name, RingId, StringComparison.Ordinal))
                return;

            string characterName = Convert.ToString(ReadMember(__instance, "Name", "name"));
            string characterGuid = Convert.ToString(ReadMember(__instance, "Guid", "guid"));
            string slotName = Convert.ToString(ReadMember(
                ReadMember(__1, "SlotTypeDefinition", "slotTypeDefinition"), "Name", "name"));

            Debug.Log(LogPrefix + "EQUIP CHECK: Ring equipped. Character=" + characterName +
                ", GUID=" + characterGuid + ", Slot=" + slotName +
                ", Item=" + item.Name + ".");

            int paralyzed = 0;
            int restrained = 0;
            int movement = 0;
            int difficultTerrain = 0;

            object activeFeaturesObject = ReadMember(__instance, "activeItemFeatures");
            IDictionary activeFeatures = activeFeaturesObject as IDictionary;
            object itemFeaturesObject = null;

            if (activeFeatures != null && activeFeatures.Contains(item))
                itemFeaturesObject = activeFeatures[item];

            IEnumerable activeFeaturesForRing = itemFeaturesObject as IEnumerable;
            if (activeFeaturesForRing != null)
            {
                foreach (object featureObject in activeFeaturesForRing)
                {
                    FeatureDefinition feature = featureObject as FeatureDefinition;
                    if (feature == null)
                        continue;

                    string featureName = feature.Name ?? "<unnamed>";
                    FeatureDefinitionMovementAffinity movementAffinity =
                        feature as FeatureDefinitionMovementAffinity;
                    bool ignoresDifficultTerrain = movementAffinity != null &&
                        movementAffinity.ImmuneDifficultTerrain;

                    if (featureName.IndexOf("Paralyzed", StringComparison.OrdinalIgnoreCase) >= 0)
                        paralyzed++;
                    if (featureName.IndexOf("Restrained", StringComparison.OrdinalIgnoreCase) >= 0)
                        restrained++;
                    if (movementAffinity != null)
                    {
                        movement++;
                        if (ignoresDifficultTerrain)
                            difficultTerrain++;
                    }

                    Debug.Log(LogPrefix + "EQUIP CHECK ACTIVE FEATURE: " + featureName +
                        ", Type=" + feature.GetType().Name +
                        ", ImmuneDifficultTerrain=" + ignoresDifficultTerrain);
                }
            }
            else
            {
                Debug.LogError(LogPrefix + "EQUIP CHECK: Could not read activeItemFeatures entry for Ring of Free Movement. " +
                    "DictionaryFound=" + (activeFeatures != null) +
                    ", RingEntryFound=" + (itemFeaturesObject != null) + ".");
            }

            Debug.Log(LogPrefix + "EQUIP CHECK SUMMARY: Character=" + characterName +
                ", ActiveParalyzedAffinity=" + (paralyzed > 0) +
                ", ActiveRestrainedAffinity=" + (restrained > 0) +
                ", ActiveMovementAffinity=" + (movement > 0) +
                ", ActiveDifficultTerrainImmunity=" + (difficultTerrain > 0) +
                ", ActiveFeatureCount=" + (activeFeaturesForRing == null ? 0 : CountFeatures(activeFeaturesForRing)) + ".");
        }
        catch (Exception ex)
        {
            Debug.LogError(LogPrefix + "EQUIP CHECK failed: " + ex);
        }
    }

    private static int CountFeatures(IEnumerable features)
    {
        int count = 0;
        foreach (object feature in features)
            if (feature != null)
                count++;
        return count;
    }

    private static object ReadMember(object target, params string[] names)
    {
        if (target == null)
            return null;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(target);

                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }
        }

        return null;
    }
}

internal static class SpellSlotRingPatch
{
    private const string LogPrefix = "[Solasta Expanded Equipment] ";
    private const string FeatureSuffix = "SpellSlotAffinity";
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized)
            return;

        try
        {
            MethodInfo target = AccessTools.Method(typeof(RulesetSpellRepertoire), "ComputeSpellSlots");
            if (target == null)
            {
                Debug.LogError(LogPrefix + "Could not find RulesetSpellRepertoire.ComputeSpellSlots.");
                return;
            }

            Harmony harmony = new Harmony("SolastaExpandedEquipmentMod.SpellSlotRings");
            HarmonyMethod prefix = new HarmonyMethod(typeof(SpellSlotRingPatch).GetMethod(
                nameof(FilterClassSpecificSlotBonuses), BindingFlags.Static | BindingFlags.NonPublic));
            harmony.Patch(target, prefix: prefix);
            initialized = true;
            Debug.Log(LogPrefix + "Class-specific spell-slot ring patch active.");

            PatchUnfinishedBusinessWarlockPool(harmony);
        }
        catch (Exception ex)
        {
            Debug.LogError(LogPrefix + "Spell-slot ring patch initialization failed: " + ex);
        }
    }

    private static void PatchUnfinishedBusinessWarlockPool(Harmony harmony)
    {
        Type sharedSpellsContext = AccessTools.TypeByName("SolastaUnfinishedBusiness.Models.SharedSpellsContext");
        MethodInfo target = sharedSpellsContext == null
            ? null
            : AccessTools.Method(sharedSpellsContext, "GetWarlockMaxSlots");

        if (target == null)
        {
            Debug.Log(LogPrefix + "UB Warlock pact-slot helper not found; native Warlock slot handling will be used.");
            return;
        }

        HarmonyMethod postfix = new HarmonyMethod(typeof(SpellSlotRingPatch).GetMethod(
            nameof(AddWarlockPactSlots), BindingFlags.Static | BindingFlags.NonPublic));
        harmony.Patch(target, postfix: postfix);
        Debug.Log(LogPrefix + "UB Warlock pact-slot ring support active.");
    }

    private static void AddWarlockPactSlots(object __0, ref int __result)
    {
        try
        {
            __result += RingSlotItems.GetEquippedWarlockBonusSlots(__0);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(LogPrefix + "Could not apply Warlock ring bonuses to the UB Pact Magic pool: " + ex.Message);
        }
    }

    private static void FilterClassSpecificSlotBonuses(
        RulesetSpellRepertoire __instance,
        ref List<FeatureDefinition> spellCastingAffinities)
    {
        if (spellCastingAffinities == null || __instance == null || __instance.SpellCastingClass == null)
            return;

        string casterClass = __instance.SpellCastingClass.Name;
        List<FeatureDefinition> filtered = null;

        for (int i = 0; i < spellCastingAffinities.Count; i++)
        {
            FeatureDefinition feature = spellCastingAffinities[i];
            string ringClass = GetRingClass(feature == null ? null : feature.Name);
            if (ringClass == null || string.Equals(ringClass, casterClass, StringComparison.OrdinalIgnoreCase))
                continue;

            if (filtered == null)
                filtered = new List<FeatureDefinition>(spellCastingAffinities);
            filtered.Remove(feature);
        }

        if (filtered != null)
            spellCastingAffinities = filtered;
    }

    private static string GetRingClass(string featureName)
    {
        if (string.IsNullOrEmpty(featureName))
            return null;

        string itemId;
        if (featureName.EndsWith(FeatureSuffix, StringComparison.Ordinal))
            itemId = featureName.Substring(0, featureName.Length - FeatureSuffix.Length);
        else if (featureName.EndsWith("MagicAffinity", StringComparison.Ordinal))
            itemId = featureName.Substring(0, featureName.Length - "MagicAffinity".Length);
        else
            return null;

        if (itemId == "RingOfWizardry" || itemId == "GreaterRingOfWizardry" || itemId == "RingOfArchWizardry")
            return "Wizard";
        if (itemId == "RingOfTheDruid" || itemId == "RingOfTheGrandDruid" || itemId == "RingOfTheArchDruid")
            return "Druid";
        if (itemId == "RingOfSorcery" || itemId == "GreaterRingOfSorcery" || itemId == "RingOfArchSorcery")
            return "Sorcerer";
        if (itemId == "RingOfTheWarlock" || itemId == "GreaterRingOfWarlock" || itemId == "RingOfArchWarlock")
            return "Warlock";
        if (itemId.StartsWith("WizardsAdventuringGarb", StringComparison.Ordinal))
            return "Wizard";
        if (itemId.StartsWith("SorcerersArmor", StringComparison.Ordinal))
            return "Sorcerer";
        if (itemId.StartsWith("WarlocksArmor", StringComparison.Ordinal))
            return "Warlock";
        if (itemId.StartsWith("DruidsArmor", StringComparison.Ordinal))
            return "Druid";

        return null;
    }
}
