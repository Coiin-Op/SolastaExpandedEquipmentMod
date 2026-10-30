using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class DruidArmor
    {
        public static bool Register(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod)
        {
            bool baseArmorsRegistered = ArmorFactory.RegisterArmorFamily(
                itemDb,
                mod,
                "DruidsArmor",
                "Druid's Armor",
                "LeatherDruid");

            ItemDefinition source = itemDb == null
                ? null
                : itemDb.GetElement("LeatherDruid", true);

            if (source == null || source.ArmorDescription == null)
                return false;

            int baseArmorClass = source.ArmorDescription.ArmorClassValue;
            const string sourceId = "LeatherDruid";
            bool enchantmentsRegistered = true;

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "DruidsArmorPlus1OfSpeed",
                "Druid's Armor +1 of Speed", 1, "Rare",
                "Haste", 3, sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "DruidsArmorPlus1OfProtection",
                "Druid's Armor +1 of Protection", 1, "Uncommon",
                new List<FeatureDefinition>
                {
                    ArmorEnchantmentFactory.CreateSavingThrowBonus(
                        "DruidsArmorSaveBonus1", 1)
                },
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "DruidsArmorPlus2OfProtection",
                "Druid's Armor +2 of Protection", 2, "Rare",
                new List<FeatureDefinition>
                {
                    ArmorEnchantmentFactory.CreateSavingThrowBonus(
                        "DruidsArmorSaveBonus2", 2)
                },
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "DruidsArmorPlus1OfFeatherFall",
                "Druid's Armor +1 of Feather Fall", 1, "Uncommon",
                "FeatherFall", 3, sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "DruidsArmorPlus1OfFreeMovement",
                "Druid's Armor +1 of Free Movement", 1, "Rare",
                "FreedomOfMovement", 3, sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "DruidsArmorPlus2OfMistWalking",
                "Druid's Armor +2 of Mist Walking", 2, "Rare",
                "MistyStep", 0, sourceId, sourceId, baseArmorClass);

            FeatureDefinitionDamageAffinity fire =
                ArmorEnchantmentFactory.CreateDamageResistance(
                    "DruidsArmorFireResistance", "DamageFire");
            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "DruidsArmorPlus1OfFireResistance",
                "Druid's Armor +1 of Fire Resistance", 1, "Rare",
                "AganazzarScorcher", 0, sourceId, sourceId, baseArmorClass, fire);

            FeatureDefinitionDamageAffinity cold =
                ArmorEnchantmentFactory.CreateDamageResistance(
                    "DruidsArmorColdResistance", "DamageCold");
            FeatureDefinitionConditionAffinity chilled =
                ArmorEnchantmentFactory.CreateConditionImmunity(
                    "DruidsArmorChilledImmunity", "ConditionChilled");
            FeatureDefinitionConditionAffinity frozen =
                ArmorEnchantmentFactory.CreateConditionImmunity(
                    "DruidsArmorFrozenImmunity", "ConditionFrozen");
            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "DruidsArmorPlus1OfColdResistance",
                "Druid's Armor +1 of Cold Resistance", 1, "Rare",
                new List<FeatureDefinition> { cold, chilled, frozen },
                sourceId, sourceId, baseArmorClass);

            FeatureDefinitionDamageAffinity lightning =
                ArmorEnchantmentFactory.CreateDamageResistance(
                    "DruidsArmorLightningResistance", "DamageLightning");
            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "DruidsArmorPlus1OfLightningResistance",
                "Druid's Armor +1 of Lightning Resistance", 1, "Rare",
                new List<FeatureDefinition> { lightning },
                sourceId, sourceId, baseArmorClass);

            FeatureDefinition intelligence =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    "DruidsArmorIntelligenceBonus",
                    "Intelligence", "Additive", 1);
            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "DruidsArmorPlus1OfBrilliance",
                "Druid's Armor +1 of Brilliance", 1, "Rare",
                "Sunbeam", 0, sourceId, sourceId, baseArmorClass, intelligence);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "DruidsArmorPlus1OfDeepPockets",
                "Druid's Armor +1 of Deep Pockets", 1, "Uncommon",
                new List<FeatureDefinition>
                {
                    ArmorEnchantmentFactory.CreateCarryingCapacityBonus(
                        "DruidsArmorCarryDouble", 2f)
                },
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "DruidsArmorPlus2OfDeepestPockets",
                "Druid's Armor +2 of Deepest Pockets", 2, "Rare",
                new List<FeatureDefinition>
                {
                    ArmorEnchantmentFactory.CreateCarryingCapacityBonus(
                        "DruidsArmorCarryTriple", 3f)
                },
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "DruidsArmorPlus1OfPlenty",
                "Druid's Armor +1 of Plenty", 1, "Uncommon",
                new List<FeatureDefinition>(),
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSlotVariant(
                itemDb, "DruidsArmorPlus1OfSorcery",
                "Druid's Armor +1 of Sorcery", 1, "Rare",
                new[] { 1, 2 }, new[] { 2, 1 },
                sourceId, sourceId);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSlotVariant(
                itemDb, "DruidsArmorPlus2OfSorcery",
                "Druid's Armor +2 of Sorcery", 2, "VeryRare",
                new[] { 1, 2, 3 }, new[] { 2, 2, 1 },
                sourceId, sourceId);

            return baseArmorsRegistered && enchantmentsRegistered;
        }
    }
}
