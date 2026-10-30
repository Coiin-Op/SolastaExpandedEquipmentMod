using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class PaddedArmor
    {
        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition armor = itemDb == null ? null : itemDb.GetElement("Padded", true);
            if (armor == null || armor.ArmorDescription == null ||
                armor.GuiPresentation == null || armor.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Padded armor template.");
                return false;
            }

            string sourceId = armor.Name;
            int baseArmorClass = armor.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 2);

            success &= RegisterDexterityVariant(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterDexterityVariant(itemDb, sourceId, baseArmorClass, 2);
            success &= RegisterDexterityVariant(itemDb, sourceId, baseArmorClass, 3);

            FeatureDefinitionAttributeModifier dexterityAndStrength =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    "PaddedPlus3DexterityStrengthBonus", "Strength", "Add", 1);
            success &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "PaddedPlus3DexterityAndStrength", "Padded +3 Dexterity, +1 Strength",
                3, "VeryRare", new List<FeatureDefinition> { dexterityAndStrength },
                sourceId, sourceId, baseArmorClass);

            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "PaddedPlus1OfFreeMovement", "Padded +1 of Free Movement",
                1, "Rare", "FreedomOfMovement", 3, sourceId, sourceId, baseArmorClass);

            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "PaddedPlus1OfKnock", "Thief's Padded Leather +1 of Knock",
                1, "Rare", "Knock", 0, sourceId, sourceId, baseArmorClass);
            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "PaddedPlus1OfFly", "Thief's Padded Leather +1 of Flight",
                1, "Rare", "Fly", 0, sourceId, sourceId, baseArmorClass);

            FeatureDefinitionAttributeModifier archeryDex1 =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    "PaddedPlus1OfArcheryDexterity", "Dexterity", "Add", 1);
            success &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "PaddedPlus1OfArchery", "Thief's Padded Leather +1 of Archery",
                1, "Rare", new List<FeatureDefinition> { archeryDex1 },
                sourceId, sourceId, baseArmorClass);

            FeatureDefinitionAttributeModifier archeryDex2 =
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    "PaddedPlus2OfArcheryDexterity", "Dexterity", "Add", 2);
            success &= ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, "PaddedPlus2OfArchery", "Thief's Padded Leather +2 of Archery",
                2, "VeryRare", new List<FeatureDefinition> { archeryDex2 },
                sourceId, sourceId, baseArmorClass);

            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "PaddedPlus1OfSpy", "Spy's Padded Leather +1",
                1, "Rare", "Fly", 0, sourceId, sourceId, baseArmorClass);
            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "PaddedPlus1OfSpyTongues", "Spy's Padded Leather +1 of Tongues",
                1, "Rare", "Tongues", 0, sourceId, sourceId, baseArmorClass);
            success &= ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, "PaddedPlus1OfSpyInvisibility", "Spy's Padded Leather +1 of Invisibility",
                1, "Rare", "Invisibility", 0, sourceId, sourceId, baseArmorClass);

            return success;
        }

        private static bool RegisterDexterityVariant(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "PaddedPlus" + tier + "Dexterity";
            string title = "Padded +" + tier + " Dexterity";
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
            string id = "PaddedPlus" + tier + "Of" + damageName + "Protection";
            string title = "Padded +" + tier + " vs. " + damageName + " Damage";
            FeatureDefinitionDamageAffinity reduction =
                ArmorEnchantmentFactory.CreateFlatDamageReduction(id + "DamageReduction", damageType, tier);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier, tier == 1 ? "Uncommon" : "Rare",
                new List<FeatureDefinition> { reduction }, sourceId, sourceId, baseArmorClass);
        }
    }
}
