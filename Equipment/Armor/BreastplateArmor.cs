using System;
using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class BreastplateArmor
    {
        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            if (itemDb == null)
                return false;

            ItemDefinition breastplate = itemDb.GetElement("Breastplate", true);
            if (breastplate == null || breastplate.ArmorDescription == null ||
                breastplate.GuiPresentation == null || breastplate.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning(
                        "[SolastaExpandedEquipmentMod] Could not find base armor template 'Breastplate'.");
                return false;
            }

            string sourceId = breastplate.Name;
            int baseArmorClass = breastplate.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 2);
            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 2);

            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "BreastplatePlus1OfFeatherFall", "Breastplate +1 of Feather Fall",
                1, "Uncommon", "FeatherFall", 3);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "BreastplatePlus1OfFreeMovement", "Breastplate +1 of Free Movement",
                1, "Rare", "FreedomOfMovement", 3);

            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Piercing", "DamagePiercing", 1);
            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Piercing", "DamagePiercing", 2);
            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Slashing", "DamageSlashing", 1);
            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Slashing", "DamageSlashing", 2);
            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Slashing", "DamageSlashing", 3);
            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Bludgeoning", "DamageBludgeoning", 1);
            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Bludgeoning", "DamageBludgeoning", 2);

            return success;
        }

        private static bool RegisterStrength(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "BreastplatePlus" + tier + "OfStrength";
            string title = "Breastplate +" + tier + " of Strength";
            FeatureDefinitionAttributeModifier strength =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    id + "StrengthBonus", "Strength", "Set", 19);

            return RegisterPassive(itemDb, id, title, tier,
                tier == 1 ? "Rare" : "VeryRare",
                new List<FeatureDefinition> { strength }, sourceId, baseArmorClass);
        }

        private static bool RegisterProtection(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "BreastplatePlus" + tier + "OfProtection";
            string title = "Breastplate +" + tier + " of Protection";
            FeatureDefinitionSavingThrowAffinity saves =
                ArmorEnchantmentFactory.CreateSavingThrowBonus(id + "SaveBonus", tier);

            return RegisterPassive(itemDb, id, title, tier,
                tier == 1 ? "Uncommon" : "Rare",
                new List<FeatureDefinition> { saves }, sourceId, baseArmorClass);
        }

        private static bool RegisterArmorBonus(
            Database<ItemDefinition> itemDb,
            string sourceId,
            int baseArmorClass,
            string damageName,
            string damageType,
            int tier)
        {
            string id = "BreastplatePlus" + tier + "Of" + damageName + "Protection";
            string title = "Breastplate +" + tier + " vs. " + damageName + " Damage";
            FeatureDefinitionDamageAffinity reduction =
                ArmorEnchantmentFactory.CreateFlatDamageReduction(
                    id + "DamageReduction", damageType, tier);

            return RegisterPassive(itemDb, id, title, tier,
                tier == 1 ? "Uncommon" : tier == 2 ? "Rare" : "VeryRare",
                new List<FeatureDefinition> { reduction }, sourceId, baseArmorClass);
        }

        private static bool RegisterSpell(
            Database<ItemDefinition> itemDb,
            string sourceId,
            int baseArmorClass,
            string id,
            string title,
            int tier,
            string rarity,
            string spellId,
            int durationMinutes)
        {
            return ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, id, title, tier, rarity, spellId, durationMinutes,
                sourceId, sourceId, baseArmorClass);
        }

        private static bool RegisterPassive(
            Database<ItemDefinition> itemDb,
            string id,
            string title,
            int tier,
            string rarity,
            List<FeatureDefinition> features,
            string sourceId,
            int baseArmorClass)
        {
            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier, rarity, features,
                sourceId, sourceId, baseArmorClass);
        }
    }
}
