using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class SorcererArmor
    {
        public static bool Register(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod)
        {
            bool baseArmorsRegistered = ArmorFactory.RegisterArmorFamily(
                itemDb,
                mod,
                "SorcerersArmor",
                "Sorcerer's Armor",
                "SorcererArmor");

            ItemDefinition source = itemDb == null
                ? null
                : itemDb.GetElement("SorcererArmor", true);

            if (source == null || source.ArmorDescription == null)
            {
                if (mod != null)
                    mod.Logger.Warning(
                        "[SolastaExpandedEquipmentMod] Sorcerer armor enchantments were not registered because the SorcererArmor template was unavailable.");

                return false;
            }

            int baseArmorClass = source.ArmorDescription.ArmorClassValue;
            const string sourceId = "SorcererArmor";
            bool enchantmentsRegistered = true;

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "SorcerersArmorPlus1OfSpeed",
                "Sorcerer's Armor +1 of Speed", 1, "Rare",
                "Haste", 3, sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "SorcerersArmorPlus1OfProtection",
                "Sorcerer's Armor +1 of Protection", 1, "Uncommon",
                new List<FeatureDefinition>
                {
                    ArmorEnchantmentFactory.CreateSavingThrowBonus(
                        "SorcerersArmorSaveBonus1", 1)
                },
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "SorcerersArmorPlus2OfProtection",
                "Sorcerer's Armor +2 of Protection", 2, "Rare",
                new List<FeatureDefinition>
                {
                    ArmorEnchantmentFactory.CreateSavingThrowBonus(
                        "SorcerersArmorSaveBonus2", 2)
                },
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "SorcerersArmorPlus1OfFeatherFall",
                "Sorcerer's Armor +1 of Feather Fall", 1, "Uncommon",
                "FeatherFall", 3, sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "SorcerersArmorPlus1OfFreeMovement",
                "Sorcerer's Armor +1 of Free Movement", 1, "Rare",
                "FreedomOfMovement", 3, sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "SorcerersArmorPlus2OfMistWalking",
                "Sorcerer's Armor +2 of Mist Walking", 2, "Rare",
                "MistyStep", 0, sourceId, sourceId, baseArmorClass);

            FeatureDefinitionDamageAffinity fire =
                ArmorEnchantmentFactory.CreateDamageResistance(
                    "SorcerersArmorFireResistance", "DamageFire");
            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "SorcerersArmorPlus1OfFireResistance",
                "Sorcerer's Armor +1 of Fire Resistance", 1, "Rare",
                "AganazzarScorcher", 0, sourceId, sourceId, baseArmorClass, fire);

            FeatureDefinitionDamageAffinity cold =
                ArmorEnchantmentFactory.CreateDamageResistance(
                    "SorcerersArmorColdResistance", "DamageCold");
            FeatureDefinitionConditionAffinity chilled =
                ArmorEnchantmentFactory.CreateConditionImmunity(
                    "SorcerersArmorChilledImmunity", "ConditionChilled");
            FeatureDefinitionConditionAffinity frozen =
                ArmorEnchantmentFactory.CreateConditionImmunity(
                    "SorcerersArmorFrozenImmunity", "ConditionFrozen");
            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "SorcerersArmorPlus1OfColdResistance",
                "Sorcerer's Armor +1 of Cold Resistance", 1, "Rare",
                new List<FeatureDefinition> { cold, chilled, frozen },
                sourceId, sourceId, baseArmorClass);

            FeatureDefinitionDamageAffinity lightning =
                ArmorEnchantmentFactory.CreateDamageResistance(
                    "SorcerersArmorLightningResistance", "DamageLightning");
            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "SorcerersArmorPlus1OfLightningResistance",
                "Sorcerer's Armor +1 of Lightning Resistance", 1, "Rare",
                new List<FeatureDefinition> { lightning },
                sourceId, sourceId, baseArmorClass);

            FeatureDefinition intelligence =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    "SorcerersArmorIntelligenceBonus",
                    "Intelligence", "Additive", 1);
            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "SorcerersArmorPlus1OfBrilliance",
                "Sorcerer's Armor +1 of Brilliance", 1, "Rare",
                "Sunbeam", 0, sourceId, sourceId, baseArmorClass, intelligence);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "SorcerersArmorPlus1OfDeepPockets",
                "Sorcerer's Armor +1 of Deep Pockets", 1, "Uncommon",
                new List<FeatureDefinition>
                {
                    ArmorEnchantmentFactory.CreateCarryingCapacityBonus(
                        "SorcerersArmorCarryDouble", 2f)
                },
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "SorcerersArmorPlus2OfDeepestPockets",
                "Sorcerer's Armor +2 of Deepest Pockets", 2, "Rare",
                new List<FeatureDefinition>
                {
                    ArmorEnchantmentFactory.CreateCarryingCapacityBonus(
                        "SorcerersArmorCarryTriple", 3f)
                },
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "SorcerersArmorPlus1OfPlenty",
                "Sorcerer's Armor +1 of Plenty", 1, "Uncommon",
                new List<FeatureDefinition>(),
                sourceId, sourceId, baseArmorClass);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSlotVariant(
                itemDb, "SorcerersArmorPlus1OfSorcery",
                "Sorcerer's Armor +1 of Sorcery", 1, "Rare",
                new[] { 1, 2 }, new[] { 2, 1 },
                sourceId, sourceId);

            enchantmentsRegistered &= ArmorEnchantmentFactory.RegisterSlotVariant(
                itemDb, "SorcerersArmorPlus2OfSorcery",
                "Sorcerer's Armor +2 of Sorcery", 2, "VeryRare",
                new[] { 1, 2, 3 }, new[] { 2, 2, 1 },
                sourceId, sourceId);

            return baseArmorsRegistered && enchantmentsRegistered;
        }
    }
}
