using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class StuddedLeatherArmor
    {
        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition armor = itemDb == null ? null : itemDb.GetElement("StuddedLeather", true);
            if (armor == null || armor.ArmorDescription == null ||
                armor.GuiPresentation == null || armor.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Studded Leather armor template.");
                return false;
            }

            string sourceId = armor.Name;
            int baseArmorClass = armor.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 2);

            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 2);

            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "StuddedLeatherPlus1OfKnock", "Thief's Studded Leather +1 of Knock",
                1, "Rare", "Knock");
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "StuddedLeatherPlus1OfFly", "Thief's Studded Leather +1 of Flight",
                1, "Rare", "Fly");
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "StuddedLeatherPlus1OfTongues", "Spy's Studded Leather +1 of Tongues",
                1, "Rare", "Tongues");
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "StuddedLeatherPlus1OfInvisibility", "Spy's Studded Leather +1 of Invisibility",
                1, "Rare", "Invisibility");

            success &= RegisterArcheryVariant(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterArcheryVariant(itemDb, sourceId, baseArmorClass, 2);

            return success;
        }

        private static bool RegisterProtection(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "StuddedLeatherPlus" + tier + "OfProtection";
            string title = "Studded Leather +" + tier + " of Protection";
            FeatureDefinitionSavingThrowAffinity saves =
                ArmorEnchantmentFactory.CreateSavingThrowBonus(id + "SaveBonus", tier);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier, tier == 1 ? "Uncommon" : "Rare",
                new List<FeatureDefinition> { saves }, sourceId, sourceId, baseArmorClass);
        }

        private static bool RegisterDamageProtection(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass,
            string damageName, string damageType, int tier)
        {
            string id = "StuddedLeatherPlus" + tier + "Of" + damageName + "Protection";
            string title = "Studded Leather +" + tier + " vs. " + damageName + " Damage";
            FeatureDefinitionDamageAffinity reduction =
                ArmorEnchantmentFactory.CreateFlatDamageReduction(id + "DamageReduction", damageType, tier);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier,
                tier == 1 ? "Uncommon" : "Rare",
                new List<FeatureDefinition> { reduction }, sourceId, sourceId, baseArmorClass);
        }

        private static bool RegisterArcheryVariant(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "StuddedLeatherPlus" + tier + "OfArchery";
            string title = "Thief's Studded Leather +" + tier + " of Archery";
            FeatureDefinitionAttributeModifier dexterity =
                ArmorEnchantmentFactory.CreateAttributeModifier(id + "DexterityBonus", "Dexterity", "Add", tier);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier, tier == 1 ? "Rare" : "VeryRare",
                new List<FeatureDefinition> { dexterity }, sourceId, sourceId, baseArmorClass);
        }

        private static bool RegisterSpell(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass,
            string id, string title, int tier, string rarity, string spellId)
        {
            return ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, id, title, tier, rarity, spellId, 0,
                sourceId, sourceId, baseArmorClass);
        }
    }
}
