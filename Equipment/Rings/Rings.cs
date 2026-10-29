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
            bool spellWands = RegisterSpellWands(itemDb);

            Debug.Log(LogPrefix + "Ring registration results: Free Movement=" + freeMovement +
                ", Knock=" + knock + ", Invisibility=" + invisibility +
                ", Spell-slot rings=" + slotRings + ", Wands=" + spellWands + ".");

            return freeMovement && knock && invisibility && slotRings && spellWands;
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

        private static bool RegisterSpellWands(Database<ItemDefinition> itemDb)
        {
            bool success = true;
            success = RegisterSpellWand(itemDb, "WandOfDeathsDoor", "Wand of Death's Door",
                "Revivify", 5, "VeryRare") && success;
            success = RegisterSpellWand(itemDb, "WandOfConeOfCold", "Wand of Cone of Cold",
                "ConeOfCold", 3, "VeryRare") && success;
            success = RegisterSpellWand(itemDb, "WandOfRayOfEnfeeblement", "Wand of Ray of Enfeeblement",
                "RayOfEnfeeblement", 5, "Uncommon") && success;
            success = RegisterSpellWand(itemDb, "WandOfChromaticOrb", "Wand of Chromatic Orb",
                "ChromaticOrb", 5, "Uncommon") && success;
            success = RegisterSpellWand(itemDb, "WandOfAcidArrow", "Wand of Acid Arrow",
                "AcidArrow", 5, "Uncommon") && success;

            return success;
        }

        private static bool RegisterSpellWand(
            Database<ItemDefinition> itemDb,
            string itemId,
            string displayName,
            string spellId,
            int charges,
            string rarity)
        {
            try
            {
                if (itemDb.GetElement(itemId, true) != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition baseWand = FindWandTemplate(itemDb);
                if (baseWand == null)
                {
                    Debug.LogError(LogPrefix + "Could not find a usable wand template for " + displayName + ".");
                    return false;
                }

                Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
                if (spellDb == null)
                {
                    Debug.LogError(LogPrefix + "Spell database unavailable for " + displayName + ".");
                    return false;
                }

                SpellDefinition spell = spellDb.GetElement(spellId, true);
                if (spell == null)
                {
                    Debug.LogError(LogPrefix + "Could not find spell '" + spellId + "' for " + displayName + ".");
                    return false;
                }

                ItemDefinition item = UnityEngine.Object.Instantiate(baseWand);
                if (item == null)
                    return false;

                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                string spellName = spell.GuiPresentation == null ? spellId : (spell.GuiPresentation.Title ?? spellId);
                string description = "A magical wand with " + charges + " charges. Expend 1 charge to cast " +
                    spellName + ". It regains all expended charges at dawn.";
                if (!SetPresentation(item, baseWand, displayName, description))
                    Debug.LogWarning(LogPrefix + "Could not completely set the presentation for " + displayName + ".");

                bool magicalSet = SetMember(item, "magical", true);
                bool attunementSet = SetMember(item, "requiresAttunement", true);
                bool raritySet = SetEnumMember(item, "itemRarity", rarity);
                bool deviceFlagSet = SetMember(item, "isUsableDevice", true);

                if (!magicalSet || !attunementSet || !raritySet || !deviceFlagSet)
                {
                    Debug.LogError(LogPrefix + "Could not set all wand item fields for " + displayName + ".");
                    return false;
                }

                if (!AttachSpellFunction(item, baseWand, spell, "Instantaneous", charges))
                {
                    Debug.LogError(LogPrefix + "Could not configure the spell function for " + displayName + ".");
                    return false;
                }

                itemDb.Add(item);
                bool registered = itemDb.GetElement(itemId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered " + displayName + " with " + charges + " charges, recharging at dawn.");
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

        private static bool SetPresentation(ItemDefinition item, ItemDefinition template, string title, string description)
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

        private static bool SetEnumMember(object target, string name, string enumValue)
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
            if (itemDb.GetElement(ring.Id, true) != null)
            {
                Debug.Log(LogPrefix + ring.Title + " already exists; skipping.");
                return true;
            }

            ItemDefinition template = FindRingTemplate(itemDb);
            if (template == null)
            {
                Debug.LogError(LogPrefix + "Could not find a ring template for " + ring.Title + ".");
                return false;
            }

            ItemPropertyDescription propertyTemplate = FindPropertyTemplate(itemDb);
            if (propertyTemplate == null)
            {
                Debug.LogError(LogPrefix + "Could not find an item-property template for " + ring.Title + ".");
                return false;
            }

            FeatureDefinitionMagicAffinity affinity = CreateSlotAffinity(ring);
            if (affinity == null)
                return false;

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

        List<AdditionalSlotsDuplet> slots = new List<AdditionalSlotsDuplet>();
        for (int i = 0; i < ring.Levels.Length; i++)
        {
            object slot = Activator.CreateInstance(typeof(AdditionalSlotsDuplet));
            if (slot == null ||
                !SetMember(slot, "slotLevel", ring.Levels[i]) ||
                !SetMember(slot, "slotsNumber", ring.Counts[i]))
            {
                Debug.LogError(LogPrefix + "Could not configure spell-slot entry for " + ring.Title + ".");
                return null;
            }

            slots.Add((AdditionalSlotsDuplet)slot);
        }

        if (!SetMember(feature, "additionalSlots", slots))
        {
            Debug.LogError(LogPrefix + "Could not set spell-slot bonuses for " + ring.Title + ".");
            return null;
        }

        if (isNew)
        {
            featureDb.Add(feature);
            feature = featureDb.GetElement(featureName, true);
        }

        return feature;
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
