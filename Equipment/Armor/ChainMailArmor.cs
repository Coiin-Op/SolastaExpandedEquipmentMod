using System;
using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class ChainMailArmor
    {
        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition armor = itemDb == null ? null : itemDb.GetElement("ChainMail", true);
            if (armor == null || armor.ArmorDescription == null ||
                armor.GuiPresentation == null || armor.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Chain Mail armor template.");
                return false;
            }

            string sourceId = armor.Name;
            int baseArmorClass = armor.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 2);

            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ChainMailPlus1OfFeatherFall", "Chain Mail +1 of Feather Fall",
                1, "Uncommon", "FeatherFall", 3);

            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 3);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 2);

            // The original wish list calls for a healing-themed chain mail set.
            // These are separate items because the current shared-spell helper
            // attaches one spell per item.
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ChainMailPlus1OfHealingWord", "Cleric's Chain Mail +1 of Healing Word",
                1, "Rare", "HealingWord", 0);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ChainMailPlus1OfPrayerOfHealing", "Cleric's Chain Mail +1 of Prayer of Healing",
                1, "Rare", "PrayerOfHealing", 0);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ChainMailPlus1OfLesserRestoration", "Cleric's Chain Mail +1 of Lesser Restoration",
                1, "Rare", "LesserRestoration", 0);

            // Paladin-themed chain mail: Blessing of the Elements is represented
            // by the game's spell, if present in this database.
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ChainMailPlus1OfBlessingOfTheElements", "Paladin's Chain Mail +1 of Blessing of the Elements",
                1, "Rare", "BlessingOfTheElements", 0);

            return success;
        }

        private static bool RegisterStrength(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "ChainMailPlus" + tier + "OfStrength";
            string title = "Chain Mail +" + tier + " of Strength";
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
            string id = "ChainMailPlus" + tier + "Of" + damageName + "Protection";
            string title = "Chain Mail +" + tier + " vs. " + damageName + " Damage";
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
