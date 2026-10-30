using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class LeatherArmor
    {
        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition armor = itemDb == null ? null : itemDb.GetElement("Leather", true);
            if (armor == null || armor.ArmorDescription == null ||
                armor.GuiPresentation == null || armor.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Leather armor template.");
                return false;
            }

            string sourceId = armor.Name;
            int baseArmorClass = armor.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 2);

            success &= RegisterDexterityVariant(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterDexterityVariant(itemDb, sourceId, baseArmorClass, 2);
            success &= RegisterDexterityVariant(itemDb, sourceId, baseArmorClass, 3);

            FeatureDefinitionAttributeModifier strength =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    "LeatherPlus3DexterityStrengthBonus", "Strength", "Add", 1);
            success &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "LeatherPlus3DexterityAndStrength", "Leather +3 Dexterity, +1 Strength",
                3, "VeryRare", new List<FeatureDefinition> { strength },
                sourceId, sourceId, baseArmorClass);

            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "LeatherPlus1OfFreeMovement", "Leather +1 of Free Movement",
                1, "Rare", "FreedomOfMovement", 3, sourceId, sourceId, baseArmorClass);

            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "LeatherPlus1OfKnock", "Thief's Leather +1 of Knock",
                1, "Rare", "Knock", 0, sourceId, sourceId, baseArmorClass);
            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "LeatherPlus1OfFly", "Thief's Leather +1 of Flight",
                1, "Rare", "Fly", 0, sourceId, sourceId, baseArmorClass);

            FeatureDefinitionAttributeModifier archeryDex1 =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    "LeatherPlus1OfArcheryDexterity", "Dexterity", "Add", 1);
            success &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "LeatherPlus1OfArchery", "Thief's Leather +1 of Archery",
                1, "Rare", new List<FeatureDefinition> { archeryDex1 },
                sourceId, sourceId, baseArmorClass);

            FeatureDefinitionAttributeModifier archeryDex2 =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    "LeatherPlus2OfArcheryDexterity", "Dexterity", "Add", 2);
            success &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "LeatherPlus2OfArchery", "Thief's Leather +2 of Archery",
                2, "VeryRare", new List<FeatureDefinition> { archeryDex2 },
                sourceId, sourceId, baseArmorClass);

            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "LeatherPlus1OfSpy", "Spy's Leather +1",
                1, "Rare", "Fly", 0, sourceId, sourceId, baseArmorClass);
            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "LeatherPlus1OfSpyTongues", "Spy's Leather +1 of Tongues",
                1, "Rare", "Tongues", 0, sourceId, sourceId, baseArmorClass);
            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "LeatherPlus1OfSpyInvisibility", "Spy's Leather +1 of Invisibility",
                1, "Rare", "Invisibility", 0, sourceId, sourceId, baseArmorClass);

            return success;
        }

        private static bool RegisterDexterityVariant(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "LeatherPlus" + tier + "Dexterity";
            string title = "Leather +" + tier + " Dexterity";
            FeatureDefinitionAttributeModifier dexterity =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    id + "Bonus", "Dexterity", "Add", tier);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier, tier == 1 ? "Uncommon" : tier == 2 ? "Rare" : "VeryRare",
                new List<FeatureDefinition> { dexterity }, sourceId, sourceId, baseArmorClass);
        }

        private static bool RegisterDamageProtection(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass,
            string damageName, string damageType, int tier)
        {
            string id = "LeatherPlus" + tier + "Of" + damageName + "Protection";
            string title = "Leather +" + tier + " vs. " + damageName + " Damage";
            FeatureDefinitionDamageAffinity reduction =
                ArmorEnchantmentFactory.CreateFlatDamageReduction(id + "DamageReduction", damageType, tier);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier, tier == 1 ? "Uncommon" : "Rare",
                new List<FeatureDefinition> { reduction }, sourceId, sourceId, baseArmorClass);
        }
    }
}
