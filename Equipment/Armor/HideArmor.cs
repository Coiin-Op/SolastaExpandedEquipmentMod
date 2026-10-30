using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class HideArmor
    {
        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition armor = itemDb == null ? null : itemDb.GetElement("HideArmor", true);
            if (armor == null || armor.ArmorDescription == null ||
                armor.GuiPresentation == null || armor.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Hide armor template.");
                return false;
            }

            string sourceId = armor.Name;
            int baseArmorClass = armor.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 2);

            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 3);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 2);

            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "HidePlus1OfFreeMovement", "Hide Armor +1 of Free Movement",
                1, "Rare", "FreedomOfMovement", 3);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "HidePlus1OfPassWithoutTrace", "Druid's Hide +1 of Pass without Trace",
                1, "Uncommon", "PassWithoutTrace", 0);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "HidePlus1OfSpikeGrowth", "Druid's Hide +1 of Thorns",
                1, "Rare", "SpikeGrowth", 0);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "HidePlus1OfWallOfThorns", "Druid's Hide +1 of Wall of Thorns",
                1, "VeryRare", "WallOfThorns", 0);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "HidePlus2OfBladeBarrier", "Druid's Hide +2 of Defense",
                2, "VeryRare", "BladeBarrier", 0);

            return success;
        }

        private static bool RegisterStrength(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "HidePlus" + tier + "OfStrength";
            string title = "Hide Armor +" + tier + " of Strength";
            FeatureDefinitionAttributeModifier strength =
                ArmorEnchantmentFactory.CreateAttributeModifier(id + "StrengthBonus", "Strength", "Set", 19);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier, tier == 1 ? "Rare" : "VeryRare",
                new List<FeatureDefinition> { strength }, sourceId, sourceId, baseArmorClass);
        }

        private static bool RegisterDamageProtection(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass,
            string damageName, string damageType, int tier)
        {
            string id = "HidePlus" + tier + "Of" + damageName + "Protection";
            string title = "Hide Armor +" + tier + " vs. " + damageName + " Damage";
            FeatureDefinitionDamageAffinity reduction =
                ArmorEnchantmentFactory.CreateFlatDamageReduction(id + "DamageReduction", damageType, tier);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier,
                tier == 1 ? "Uncommon" : tier == 2 ? "Rare" : "VeryRare",
                new List<FeatureDefinition> { reduction }, sourceId, sourceId, baseArmorClass);
        }

        private static bool RegisterSpell(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass,
            string id, string title, int tier, string rarity, string spellId, int durationMinutes)
        {
            return ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                itemDb, id, title, tier, rarity, spellId, durationMinutes,
                sourceId, sourceId, baseArmorClass);
        }
    }
}
