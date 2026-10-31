using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Shared builders for enchanted armor. Repeated armor effects live here so
    /// each armor family can select the same bonuses without reimplementing them.
    /// </summary>
    internal static class ArmorEnchantmentFactory
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";
        private static readonly HashSet<string> MissingSpellWarningsLogged = new HashSet<string>();

        internal static bool RegisterWizardGarbVariants(Database<ItemDefinition> itemDb)
        {
            bool success = true;

            success &= RegisterSpellVariant(itemDb, "WizardsAdventuringGarbPlus1OfSpeed",
                "Wizard's Adventuring Garb +1 of Speed", 1, "Rare", "Haste", 3);
            success &= RegisterPassiveVariant(itemDb, "WizardsAdventuringGarbPlus1OfProtection",
                "Wizard's Adventuring Garb +1 of Protection", 1, "Uncommon",
                new List<FeatureDefinition> { CreateSavingThrowBonus("WizardsGarbSaveBonus1", 1) });
            success &= RegisterPassiveVariant(itemDb, "WizardsAdventuringGarbPlus2OfProtection",
                "Wizard's Adventuring Garb +2 of Protection", 2, "Rare",
                new List<FeatureDefinition> { CreateSavingThrowBonus("WizardsGarbSaveBonus2", 2) });
            success &= RegisterSpellVariant(itemDb, "WizardsAdventuringGarbPlus1OfFeatherFall",
                "Wizard's Adventuring Garb +1 of Feather Fall", 1, "Uncommon", "FeatherFall", 3);
            success &= RegisterSpellVariant(itemDb, "WizardsAdventuringGarbPlus1OfFreeMovement",
                "Wizard's Adventuring Garb +1 of Free Movement", 1, "Rare", "FreedomOfMovement", 3);
            success &= RegisterSpellVariant(itemDb, "WizardsAdventuringGarbPlus2OfMistWalking",
                "Wizard's Adventuring Garb +2 of Mist Walking", 2, "Rare", "MistyStep", 0);

            FeatureDefinitionDamageAffinity fire = CreateDamageResistance("WizardsGarbFireResistance", "DamageFire");
            success &= RegisterSpellVariant(itemDb, "WizardsAdventuringGarbPlus1OfFireResistance",
                "Wizard's Adventuring Garb +1 of Fire Resistance", 1, "Rare", "AganazzarScorcher", 0, fire);

            FeatureDefinitionDamageAffinity cold = CreateDamageResistance("WizardsGarbColdResistance", "DamageCold");
            FeatureDefinitionConditionAffinity chilled = CreateConditionImmunity("WizardsGarbChilledImmunity", "ConditionChilled");
            FeatureDefinitionConditionAffinity coldCondition = CreateConditionImmunity("WizardsGarbFrozenConditionImmunity", "ConditionFrozen");
            success &= RegisterPassiveVariant(itemDb, "WizardsAdventuringGarbPlus1OfColdResistance",
                "Wizard's Adventuring Garb +1 of Cold Resistance", 1, "Rare",
                new List<FeatureDefinition> { cold, chilled, coldCondition });

            FeatureDefinitionDamageAffinity lightning = CreateDamageResistance("WizardsGarbLightningResistance", "DamageLightning");
            success &= RegisterPassiveVariant(itemDb, "WizardsAdventuringGarbPlus1OfLightningResistance",
                "Wizard's Adventuring Garb +1 of Lightning Resistance", 1, "Rare",
                new List<FeatureDefinition> { lightning });

            FeatureDefinition intelligence = CreateAttributeModifier(
                "WizardsGarbIntelligenceBonus", "Intelligence", "Additive", 1);
            success &= RegisterSpellVariant(itemDb, "WizardsAdventuringGarbPlus1OfBrilliance",
                "Wizard's Adventuring Garb +1 of Brilliance", 1, "Rare", "Sunbeam", 0, intelligence);

            success &= RegisterPassiveVariant(itemDb, "WizardsAdventuringGarbPlus1OfDeepPockets",
                "Wizard's Adventuring Garb +1 of Deep Pockets", 1, "Uncommon",
                new List<FeatureDefinition> { CreateCarryingCapacityBonus("WizardsGarbCarryDouble", 2f) });
            success &= RegisterPassiveVariant(itemDb, "WizardsAdventuringGarbPlus2OfDeepestPockets",
                "Wizard's Adventuring Garb +2 of Deepest Pockets", 2, "Rare",
                new List<FeatureDefinition> { CreateCarryingCapacityBonus("WizardsGarbCarryTriple", 3f) });
            success &= RegisterPassiveVariant(itemDb, "WizardsAdventuringGarbPlus1OfPlenty",
                "Wizard's Adventuring Garb +1 of Plenty", 1, "Uncommon", new List<FeatureDefinition>());

            success &= RegisterSlotVariant(itemDb, "WizardsAdventuringGarbPlus1OfWizardry",
                "Wizard's Adventuring Garb +1 of Wizardry", 1, "Rare",
                new[] { 1, 2 }, new[] { 2, 1 });
            success &= RegisterSlotVariant(itemDb, "WizardsAdventuringGarbPlus2OfWizardry",
                "Wizard's Adventuring Garb +2 of Wizardry", 2, "VeryRare",
                new[] { 1, 2, 3 }, new[] { 2, 2, 1 });

            Debug.Log(LogPrefix + "Wizard's Garb enchantment variants registration: " + success + ".");
            return success;
        }

        internal static bool RegisterPassiveVariant(
            Database<ItemDefinition> itemDb,
            string itemId,
            string title,
            int tier,
            string rarity,
            List<FeatureDefinition> features,
            string armorSourceId = "ClothesWizard",
            string appearanceSourceId = "ClothesWizard",
            int armorClassBase = 12,
            bool requiresAttunement = true)
        {
            return RegisterVariant(itemDb, itemId, title, tier, rarity,
                GetPassiveDescription(itemId, title), features, null, null, 0,
                armorSourceId, appearanceSourceId, armorClassBase,
                null, false, requiresAttunement);
        }

        private static bool RegisterSpellVariant(
            Database<ItemDefinition> itemDb,
            string itemId,
            string title,
            int tier,
            string rarity,
            string spellId,
            int durationMinutes,
            params FeatureDefinition[] features)
        {
            return RegisterSpellVariantForSources(itemDb, itemId, title, tier, rarity, spellId,
                durationMinutes, "ClothesWizard", "ClothesWizard", features);
        }

        internal static bool RegisterSharedSpellVariant(
            Database<ItemDefinition> itemDb,
            string itemId,
            string title,
            int tier,
            string rarity,
            string spellId,
            int durationMinutes,
            string armorSourceId,
            string appearanceSourceId,
            int armorClassBase = 12,
            params FeatureDefinition[] features)
        {
            return RegisterSpellVariantForSources(itemDb, itemId, title, tier, rarity, spellId,
                durationMinutes, armorSourceId, appearanceSourceId, features, armorClassBase);
        }

        private static bool RegisterSpellVariantForSources(
            Database<ItemDefinition> itemDb,
            string itemId,
            string title,
            int tier,
            string rarity,
            string spellId,
            int durationMinutes,
            string armorSourceId,
            string appearanceSourceId,
            FeatureDefinition[] features,
            int armorClassBase = 12)
        {
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            SpellDefinition spell = spellDb == null ? null : durationMinutes > 0
                ? RingItems.GetTimedSpell(spellDb, spellId, itemId + "Spell", durationMinutes)
                : spellDb.GetElement(spellId, true);

            if (spell == null)
            {
                if (MissingSpellWarningsLogged.Add(spellId))
                    Debug.LogWarning(LogPrefix + (spellDb == null
                        ? "Spell database unavailable"
                        : "Could not find spell '" + spellId + "'") + " for " + title + ". Registering the armor now and retrying spell setup later.");
            }

            List<FeatureDefinition> featureList = new List<FeatureDefinition>();
            if (features != null)
                featureList.AddRange(features);

            const int charges = 3;
            string description = title + ". Cast " +
                (spell == null || spell.GuiPresentation == null ? spellId : spell.GuiPresentation.Title) +
                " 3 times per day; the item regains its uses at dawn.";
            if (durationMinutes > 0)
                description = title + ". Cast " +
                    (spell == null || spell.GuiPresentation == null ? spellId : spell.GuiPresentation.Title) +
                    " 3 times per day, with each casting lasting " + durationMinutes + " minutes. It regains its uses at dawn.";

            string passiveDescription = GetPassiveDescription(itemId, null);
            if (!string.IsNullOrEmpty(passiveDescription))
                description += " " + passiveDescription;

            bool registered = RegisterVariant(itemDb, itemId, title, tier, rarity, description,
                featureList, spell, durationMinutes > 0 ? "Minute" : "Instantaneous", charges,
                armorSourceId, appearanceSourceId, armorClassBase, null, true);
            return registered && spell != null;
        }

        internal static bool RegisterSlotVariant(
            Database<ItemDefinition> itemDb,
            string itemId,
            string title,
            int tier,
            string rarity,
            int[] levels,
            int[] counts,
            string armorSourceId = "ClothesWizard",
            string appearanceSourceId = "ClothesWizard")
        {
            FeatureDefinitionMagicAffinity affinity = CreateSlotAffinity(itemId, levels, counts);
            return RegisterPassiveVariant(itemDb, itemId, title, tier, rarity,
                new List<FeatureDefinition> { affinity }, armorSourceId, appearanceSourceId);
        }

        internal static bool RegisterVariant(
            Database<ItemDefinition> itemDb,
            string itemId,
            string title,
            int tier,
            string rarity,
            string description,
            List<FeatureDefinition> features,
            SpellDefinition spell,
            string duration,
            int charges,
            string armorSourceId = "ClothesWizard",
            string appearanceSourceId = "ClothesWizard",
            int armorClassBase = 12,
            UsableDeviceDescription deviceDescription = null,
            bool expectsSpellFunction = false,
            bool requiresAttunement = true)
        {
            // Unfinished Business strips XML tags from definition descriptions during
            // startup documentation generation. Never let a custom armor definition
            // enter the item database with a null or blank GUI description.
            string safeDescription = string.IsNullOrWhiteSpace(description)
                ? (string.IsNullOrWhiteSpace(title) ? itemId : title) + "."
                : description;

            try
            {
                if (itemDb == null)
                    return false;

                ItemDefinition existing = itemDb.GetElement(itemId, true);
                if (existing != null)
                {
                    // Repair a definition left behind by an earlier registration pass.
                    // This also covers a null GuiPresentation, not only a null Description.
                    if (existing.GuiPresentation == null ||
                        string.IsNullOrWhiteSpace(existing.GuiPresentation.Description))
                    {
                        GuiPresentation repairedPresentation;
                        if (existing.GuiPresentation != null)
                        {
                            repairedPresentation = new GuiPresentation(existing.GuiPresentation);
                        }
                        else
                        {
                            ItemDefinition repairAppearance = itemDb.GetElement(appearanceSourceId, true);
                            if (repairAppearance == null || repairAppearance.GuiPresentation == null)
                            {
                                Debug.LogError(LogPrefix + "Could not repair the missing GUI presentation for " + title + ".");
                                return false;
                            }

                            repairedPresentation = new GuiPresentation(repairAppearance.GuiPresentation);
                        }

                        repairedPresentation.Title = title;
                        repairedPresentation.Description = safeDescription;
                        if (!SetMember(existing, "guiPresentation", repairedPresentation))
                        {
                            Debug.LogError(LogPrefix + "Could not assign a safe GUI description to existing armor " + title + ".");
                            return false;
                        }

                        Debug.Log(LogPrefix + "Repaired missing GUI description for existing armor " + title + ".");
                    }

                    if (!SetMember(existing, "requiresAttunement", requiresAttunement))
                        return false;

                    if ((string.Equals(itemId, "WarlocksArmorPlus1OfDeepPockets", StringComparison.Ordinal) ||
                         string.Equals(itemId, "WarlocksArmorPlus2OfDeepestPockets", StringComparison.Ordinal)) &&
                        !SetMember(existing, "staticProperties", new List<ItemPropertyDescription>()))
                        return false;

                    if (deviceDescription != null && !existing.IsUsableDevice)
                    {
                        if (!SetMember(existing, "usableDeviceDescription", deviceDescription) ||
                            !SetMember(existing, "isUsableDevice", true))
                            return false;
                    }

                    if (deviceDescription != null)
                        return existing.IsUsableDevice;

                    if (spell == null)
                        return !expectsSpellFunction;

                    if (!existing.IsUsableDevice || existing.UsableDeviceDescription == null ||
                        existing.UsableDeviceDescription.DeviceFunctions == null ||
                        existing.UsableDeviceDescription.DeviceFunctions.Count == 0)
                    {
                        ItemDefinition existingDeviceTemplate = RingItems.FindWandTemplate(itemDb);
                        if (existingDeviceTemplate == null ||
                            !RingItems.AttachSpellFunction(existing, existingDeviceTemplate, spell, duration, charges))
                        {
                            Debug.LogError(LogPrefix + "Could not finish configuring delayed spell casting for " + title + ".");
                            return false;
                        }
                    }

                    return true;
                }

                ItemDefinition source = itemDb.GetElement(armorSourceId, true);
                ItemDefinition appearanceSource = itemDb.GetElement(appearanceSourceId, true);
                if (source == null || source.ArmorDescription == null || source.GuiPresentation == null ||
                    appearanceSource == null || appearanceSource.GuiPresentation == null || appearanceSource.ItemPresentation == null)
                {
                    Debug.LogError(LogPrefix + "Armor template or appearance is unavailable for " + title +
                        " (template=" + armorSourceId + ", appearance=" + appearanceSourceId + ").");
                    return false;
                }

                ItemDefinition item = UnityEngine.Object.Instantiate(source);
                if (item == null)
                    return false;

                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                GuiPresentation presentation = new GuiPresentation(appearanceSource.GuiPresentation);
                presentation.Title = title;
                presentation.Description = safeDescription;
                SetMember(item, "guiPresentation", presentation);

                ItemPresentation itemPresentation = new ItemPresentation(appearanceSource.ItemPresentation);
                if (appearanceSourceId == "ClothesWizard" ||
                    appearanceSourceId == "LeatherDruid" ||
                    appearanceSourceId == "Warlock_Armor" ||
                    appearanceSourceId == "SorcererArmor")
                {
                    SetMember(itemPresentation, "useArmorAddressableName", true);
                    SetMember(itemPresentation, "armorAddressableName", appearanceSourceId);
                    SetMember(itemPresentation, "isArmorAddressableNameGenderSpecific", false);
                }
                else if (string.Equals(source.ArmorDescription.ArmorType, "PlateType",
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(source.ArmorDescription.ArmorType, "PlateArmorType",
                             StringComparison.OrdinalIgnoreCase) ||
                         (source.Name != null && source.Name.IndexOf("Plate", StringComparison.OrdinalIgnoreCase) >= 0) ||
                         string.Equals(source.ArmorDescription.ArmorType, "HalfPlateType",
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(source.ArmorDescription.ArmorType, "BreastPlateType",
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(source.ArmorDescription.ArmorType, "ChainMailType",
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(source.ArmorDescription.ArmorType, "ChainShirtType",
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(source.ArmorDescription.ArmorType, "ScaleMailType",
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(source.ArmorDescription.ArmorType, "HideArmorType",
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(source.ArmorDescription.ArmorType, "StuddedLeatherType",
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(source.ArmorDescription.ArmorType, "LeatherType",
                             StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(source.ArmorDescription.ArmorType, "PaddedLeatherType",
                             StringComparison.OrdinalIgnoreCase))
                {
                    // Custom armor definitions have new item IDs, so the character
                    // equipment renderer cannot infer the vanilla model from the enchanted
                    // item's name. Preserve an explicit mapping from the base definition;
                    // only synthesize one when the base definition relies on its own ID.
                    bool hasGenderSpecificAddressables =
                        itemPresentation.IsArmorAddressableNameGenderSpecific &&
                        (!string.IsNullOrEmpty(itemPresentation.MaleArmorAddressableName) ||
                         !string.IsNullOrEmpty(itemPresentation.FemaleArmorAddressableName));
                    bool hasSharedAddressable = !itemPresentation.IsArmorAddressableNameGenderSpecific &&
                        !string.IsNullOrEmpty(itemPresentation.ArmorAddressableName);
                    if (!itemPresentation.UseArmorAddressableName ||
                        (!hasSharedAddressable && !hasGenderSpecificAddressables))
                    {
                        SetMember(itemPresentation, "useArmorAddressableName", true);
                        SetMember(itemPresentation, "armorAddressableName", appearanceSource.Name);
                        SetMember(itemPresentation, "isArmorAddressableNameGenderSpecific", false);
                    }
                }
                SetMember(item, "itemPresentation", itemPresentation);

                ArmorDescription armor = new ArmorDescription(source.ArmorDescription);
                SetMember(armor, "armorClassValue", armorClassBase + tier);
                SetMember(item, "armorDefinition", armor);
                SetMember(item, "magical", true);
                SetMember(item, "requiresAttunement", requiresAttunement);
                SetEnumMember(item, "itemRarity", rarity);

                ItemPropertyDescription propertyTemplate = FindPropertyTemplate(itemDb);
                if ((features != null && features.Count > 0) && propertyTemplate == null)
                {
                    Debug.LogError(LogPrefix + "No item-property template available for " + title + ".");
                    return false;
                }

                List<ItemPropertyDescription> properties = new List<ItemPropertyDescription>();
                if (features != null)
                {
                    foreach (FeatureDefinition feature in features)
                    {
                        if (feature == null)
                            return false;

                        ItemPropertyDescription property = new ItemPropertyDescription(propertyTemplate);
                        if (!SetMember(property, "appliesOnItemOnly", false) ||
                            !SetEnumMember(property, "type", "Feature") ||
                            !SetMember(property, "featureDefinition", feature) ||
                            !SetMember(property, "conditionDefinition", null) ||
                            !SetEnumMember(property, "knowledgeAffinity", "ActiveAndVisible"))
                        {
                            Debug.LogError(LogPrefix + "Could not attach an enchantment feature to " + title + ".");
                            return false;
                        }
                        properties.Add(property);
                    }
                }
                SetMember(item, "staticProperties", properties);

                if (deviceDescription != null)
                {
                    if (!SetMember(item, "usableDeviceDescription", deviceDescription) ||
                        !SetMember(item, "isUsableDevice", true))
                    {
                        Debug.LogError(LogPrefix + "Could not attach independent spell uses to " + title + ".");
                        return false;
                    }
                }
                else if (spell != null)
                {
                    ItemDefinition deviceTemplate = RingItems.FindWandTemplate(itemDb);
                    if (deviceTemplate == null || !RingItems.AttachSpellFunction(item, deviceTemplate, spell, duration, charges))
                    {
                        Debug.LogError(LogPrefix + "Could not configure daily spell casting for " + title + ".");
                        return false;
                    }
                }
                else
                {
                    SetMember(item, "isUsableDevice", false);
                    SetMember(item, "usableDeviceDescription", null);
                }

                itemDb.Add(item);
                bool registered = itemDb.GetElement(itemId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered armor enchantment " + title + ".");
                else
                    Debug.LogError(LogPrefix + "Registration verification failed for " + title + ".");
                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + title + ": " + ex);
                return false;
            }
        }

        internal static FeatureDefinitionAttributeModifier CreateAttributeModifier(
            string id, string attribute, string operation, int value)
        {
            FeatureDefinitionAttributeModifier feature = CreateFeature<FeatureDefinitionAttributeModifier>(id);
            if (feature == null)
                return null;
            SetMember(feature, "modifiedAttribute", attribute);
            SetEnumMember(feature, "modifierOperation", operation);
            SetMember(feature, "modifierValue", value);
            return feature;
        }

        internal static FeatureDefinitionDamageAffinity CreateDamageResistance(string id, string damageType)
        {
            FeatureDefinitionDamageAffinity feature = CreateFeature<FeatureDefinitionDamageAffinity>(id);
            if (feature == null)
                return null;
            SetMember(feature, "damageType", damageType);
            SetEnumMember(feature, "damageAffinityType", "Resistance");
            return feature;
        }

        internal static FeatureDefinitionDamageAffinity CreateFlatDamageReduction(
            string id, string damageType, int reduction)
        {
            FeatureDefinitionDamageAffinity feature = CreateFeature<FeatureDefinitionDamageAffinity>(id);
            if (feature == null)
                return null;

            // Flat reduction is independent of resistance/vulnerability. Verify every
            // backing member so a missing or renamed game API member cannot silently
            // produce an enchantment that registers but does nothing.
            bool configured =
                SetMember(feature, "damageType", damageType) &&
                SetEnumMember(feature, "damageAffinityType", "Reduction") &&
                SetMember(feature, "flatDamageReduction", reduction) &&
                SetMember(feature, "flatDamageReductionOnlyAppliesToFirstDamageForm", false);

            if (!configured)
            {
                Debug.LogError(LogPrefix + "Could not configure flat damage reduction feature " +
                    id + " (damage type=" + damageType + ", reduction=" + reduction + ").");
                return null;
            }

            Debug.Log(LogPrefix + "Configured flat damage reduction " + id +
                " | DamageType=" + damageType + " | Reduction=" + reduction + ".");
            return feature;
        }

        internal static FeatureDefinitionConditionAffinity CreateConditionImmunity(string id, string conditionType)
        {
            FeatureDefinitionConditionAffinity feature = CreateFeature<FeatureDefinitionConditionAffinity>(id);
            if (feature == null)
                return null;
            SetMember(feature, "conditionType", conditionType);
            SetEnumMember(feature, "conditionAffinityType", "Immunity");
            return feature;
        }

        internal static FeatureDefinitionEquipmentAffinity CreateCarryingCapacityBonus(string id, float multiplier)
        {
            FeatureDefinitionEquipmentAffinity feature = CreateFeature<FeatureDefinitionEquipmentAffinity>(id);
            if (feature == null)
                return null;
            if (!SetMember(feature, "carryingCapacityMultiplier", multiplier) ||
                Math.Abs(feature.CarryingCapacityMultiplier - multiplier) > 0.001f)
            {
                Debug.LogError(LogPrefix + "Could not set carrying-capacity multiplier " + multiplier +
                    " on " + id + ".");
                return null;
            }
            return feature;
        }

        internal static FeatureDefinitionSavingThrowAffinity CreateSavingThrowBonus(string id, int bonus)
        {
            FeatureDefinitionSavingThrowAffinity feature = CreateFeature<FeatureDefinitionSavingThrowAffinity>(id);
            if (feature == null)
                return null;

            List<FeatureDefinitionSavingThrowAffinity.SavingThrowAffinityGroup> groups =
                new List<FeatureDefinitionSavingThrowAffinity.SavingThrowAffinityGroup>();
            string[] abilities = { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
            foreach (string ability in abilities)
            {
                FeatureDefinitionSavingThrowAffinity.SavingThrowAffinityGroup group =
                    new FeatureDefinitionSavingThrowAffinity.SavingThrowAffinityGroup();
                group.abilityScoreName = ability;
                group.savingThrowModifierType = FeatureDefinitionSavingThrowAffinity.ModifierType.AddDice;
                group.savingThrowModifierDiceNumber = bonus;
                group.savingThrowModifierDieType = RuleDefinitions.DieType.D1;
                SetEnumMember(group, "savingThrowContext", "None");
                groups.Add(group);
            }
            SetMember(feature, "affinityGroups", groups);
            return feature;
        }

        private static FeatureDefinitionMagicAffinity CreateSlotAffinity(string id, int[] levels, int[] counts)
        {
            if (levels == null || counts == null || levels.Length != counts.Length)
                return null;
            FeatureDefinitionMagicAffinity feature = CreateFeature<FeatureDefinitionMagicAffinity>(id + "MagicAffinity");
            if (feature == null)
                return null;
            List<AdditionalSlotsDuplet> slots = new List<AdditionalSlotsDuplet>();
            for (int i = 0; i < levels.Length; i++)
            {
                AdditionalSlotsDuplet slot = (AdditionalSlotsDuplet)Activator.CreateInstance(typeof(AdditionalSlotsDuplet));
                SetMember(slot, "slotLevel", levels[i]);
                SetMember(slot, "slotsNumber", counts[i]);
                slots.Add(slot);
            }
            SetMember(feature, "additionalSlots", slots);
            return feature;
        }

        private static string GetPassiveDescription(string itemId, string title)
        {
            if (itemId.IndexOf("OfArchery", StringComparison.Ordinal) >= 0)
                return "While worn, grants proficiency with longbows and shortbows.";
            if (itemId.IndexOf("OfProtection", StringComparison.Ordinal) >= 0)
                return "While worn, grants its enhancement bonus to armor class and all saving throws.";
            if (itemId.IndexOf("OfStrength", StringComparison.Ordinal) >= 0)
                return "While worn, sets Strength to 19.";
            if (itemId.IndexOf("OfPiercingProtection", StringComparison.Ordinal) >= 0)
                return "While worn, grants its enhancement bonus to armor class and reduces piercing damage by the same amount.";
            if (itemId.IndexOf("OfSlashingProtection", StringComparison.Ordinal) >= 0)
                return "While worn, grants its enhancement bonus to armor class and reduces slashing damage by the same amount.";
            if (itemId.IndexOf("OfBludgeoningProtection", StringComparison.Ordinal) >= 0)
                return "While worn, grants its enhancement bonus to armor class and reduces bludgeoning damage by the same amount.";
            if (itemId.IndexOf("OfColdResistance", StringComparison.Ordinal) >= 0)
                return "While worn, halves cold damage and grants immunity to chilled and frozen conditions.";
            if (itemId.IndexOf("OfFireResistance", StringComparison.Ordinal) >= 0)
                return "While worn, halves fire damage.";
            if (itemId.IndexOf("OfLightningResistance", StringComparison.Ordinal) >= 0)
                return "While worn, halves lightning damage and prevents shove effects.";
            if (itemId.IndexOf("OfDeepPockets", StringComparison.Ordinal) >= 0)
                return "Doubles the wearer's carrying capacity.";
            if (itemId.IndexOf("OfDeepestPockets", StringComparison.Ordinal) >= 0)
                return "Triples the wearer's carrying capacity.";
            if (itemId.IndexOf("OfPlenty", StringComparison.Ordinal) >= 0)
                return "After each long rest, grants 20 random crafting components and 3 food rations.";
            if (itemId.IndexOf("OfWizardry", StringComparison.Ordinal) >= 0)
                return itemId.IndexOf("Plus1", StringComparison.Ordinal) >= 0
                    ? "While worn, grants 2 additional 1st-level spell slots and 1 additional 2nd-level spell slot to Wizards."
                    : "While worn, grants 2 additional 1st-level, 2 additional 2nd-level, and 1 additional 3rd-level spell slot to Wizards.";
            if (itemId.IndexOf("OfSorcery", StringComparison.Ordinal) >= 0)
                return itemId.IndexOf("Plus1", StringComparison.Ordinal) >= 0
                    ? "While worn, grants 2 additional 1st-level spell slots and 1 additional 2nd-level spell slot to Sorcerers."
                    : "While worn, grants 2 additional 1st-level, 2 additional 2nd-level, and 1 additional 3rd-level spell slot to Sorcerers.";
            if (itemId.IndexOf("OfInfernal", StringComparison.Ordinal) >= 0)
                return itemId.IndexOf("Plus1", StringComparison.Ordinal) >= 0
                    ? "While worn, grants 2 additional 1st-level spell slots and 1 additional 2nd-level spell slot to Warlocks."
                    : "While worn, grants 2 additional 1st-level, 2 additional 2nd-level, and 1 additional 3rd-level spell slot to Warlocks.";
            if (itemId.IndexOf("OfTheDruid", StringComparison.Ordinal) >= 0)
                return itemId.IndexOf("Plus1", StringComparison.Ordinal) >= 0
                    ? "While worn, grants 2 additional 1st-level spell slots and 1 additional 2nd-level spell slot to Druids."
                    : "While worn, grants 2 additional 1st-level, 2 additional 2nd-level, and 1 additional 3rd-level spell slot to Druids.";
            if (itemId.IndexOf("OfBrilliance", StringComparison.Ordinal) >= 0)
            {
                if (itemId.StartsWith("WarlocksArmor", StringComparison.Ordinal))
                    return "While worn, increases Charisma by 1.";
                if (itemId.StartsWith("DruidsArmor", StringComparison.Ordinal))
                    return "While worn, increases Wisdom by 1.";
                return "While worn, increases Intelligence by 1.";
            }
            if (itemId.IndexOf("DexterityAndStrength", StringComparison.Ordinal) >= 0)
                return "While worn, increases Dexterity by 3 and Strength by 1.";
            if (itemId.StartsWith("LeatherPlus", StringComparison.Ordinal) &&
                itemId.IndexOf("Dexterity", StringComparison.Ordinal) >= 0)
                return "While worn, increases Dexterity by the enhancement bonus shown in the item name.";
            if (title != null && title.IndexOf("Protection", StringComparison.Ordinal) >= 0)
                return "While worn, grants its listed defensive bonus.";
            return null;
        }

        private static T CreateFeature<T>(string id) where T : FeatureDefinition
        {
            Database<T> database = DatabaseRepository.GetDatabase<T>();
            if (database == null)
            {
                Debug.LogError(LogPrefix + "Feature database unavailable for " + id + ".");
                return null;
            }

            T feature = database.GetElement(id, true);
            if (feature != null)
            {
                // Repair an existing custom feature if an earlier registration pass
                // left it without the GUI text that Unfinished Business expects.
                return EnsureFeaturePresentation(feature, database, id) ? feature : null;
            }

            feature = ScriptableObject.CreateInstance<T>();
            if (feature == null)
                return null;

            feature.ForceName(id);
            feature.SetUserContentGUID(Guid.NewGuid().ToString());
            feature.IsUserContent = true;

            // Feature definitions are visible to Unfinished Business as soon as they
            // enter their database. Give them a valid presentation BEFORE adding them.
            if (!EnsureFeaturePresentation(feature, database, id))
            {
                UnityEngine.Object.Destroy(feature);
                return null;
            }

            database.Add(feature);
            T registered = database.GetElement(id, true);
            return registered ?? feature;
        }

        private static bool EnsureFeaturePresentation<T>(T feature, Database<T> database, string id)
            where T : FeatureDefinition
        {
            if (feature == null || database == null)
                return false;

            GuiPresentation current = feature.GuiPresentation;
            if (current != null && !string.IsNullOrEmpty(current.Description))
                return true;

            T template = null;
            foreach (T candidate in database.GetAllElements())
            {
                if (candidate == null || object.ReferenceEquals(candidate, feature) || candidate.GuiPresentation == null ||
                    string.IsNullOrEmpty(candidate.GuiPresentation.Description))
                    continue;

                template = candidate;
                break;
            }

            if (template == null)
            {
                Debug.LogError(LogPrefix + "Could not create a GUI presentation for feature " + id +
                    ": no existing feature of type " + typeof(T).Name + " has a non-empty description.");
                return false;
            }

            GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
            presentation.Title = id;
            presentation.Description = "Custom equipment feature used by Solasta Expanded Equipment: " + id + ".";

            if (!SetMember(feature, "guiPresentation", presentation))
            {
                Debug.LogError(LogPrefix + "Could not assign the GUI presentation to feature " + id + ".");
                return false;
            }

            Debug.Log(LogPrefix + "Created feature " + id + " with a non-empty GUI description.");
            return true;
        }

        private static ItemPropertyDescription FindPropertyTemplate(Database<ItemDefinition> itemDb)
        {
            foreach (ItemDefinition item in itemDb.GetAllElements())
                if (item != null && item.StaticProperties != null)
                    foreach (ItemPropertyDescription property in item.StaticProperties)
                        if (property != null)
                            return property;
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
                    if (field.IsInitOnly) return false;
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
            Type memberType = GetMemberType(target == null ? null : target.GetType(), name);
            if (memberType == null || !memberType.IsEnum)
                return false;
            try { return SetMember(target, name, Enum.Parse(memberType, value, true)); }
            catch { return false; }
        }

        private static Type GetMemberType(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.FieldType;
                PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0) return property.PropertyType;
            }
            return null;
        }
    }
}
