using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class ChainShirtArmor
    {
        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition armor = itemDb == null ? null : itemDb.GetElement("ChainShirt", true);
            if (armor == null || armor.ArmorDescription == null ||
                armor.GuiPresentation == null || armor.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Chain Shirt armor template.");
                return false;
            }

            string sourceId = armor.Name;
            int baseArmorClass = armor.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 2);

            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 3);

            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 3);

            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 3);

            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ChainShirtPlus1OfFreeMovement", "Chain Shirt +1 of Free Movement",
                1, "Rare", "FreedomOfMovement", 3);

            // Separate spell-themed variants preserve the game's existing spell
            // definitions and use the shared helper's standard three charges.
            // The exact per-spell charge counts from the original wish list may
            // need a dedicated multi-spell implementation later.
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ChainShirtPlus1OfHealingWord", "Cleric's Chain Shirt +1 of Healing Word",
                1, "Rare", "HealingWord", 0);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ChainShirtPlus1OfPrayerOfHealing", "Cleric's Chain Shirt +1 of Prayer of Healing",
                1, "Rare", "PrayerOfHealing", 0);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ChainShirtPlus1OfLesserRestoration", "Cleric's Chain Shirt +1 of Lesser Restoration",
                1, "Rare", "LesserRestoration", 0);

            return success;
        }

        private static bool RegisterStrength(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "ChainShirtPlus" + tier + "OfStrength";
            string title = "Chain Shirt +" + tier + " of Strength";
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
            string id = "ChainShirtPlus" + tier + "Of" + damageName + "Protection";
            string title = "Chain Shirt +" + tier + " vs. " + damageName + " Damage";
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
