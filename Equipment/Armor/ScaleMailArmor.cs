using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class ScaleMailArmor
    {
        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition armor = itemDb == null ? null : itemDb.GetElement("ScaleMail", true);
            if (armor == null || armor.ArmorDescription == null ||
                armor.GuiPresentation == null || armor.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Scale Mail armor template.");
                return false;
            }

            string sourceId = armor.Name;
            int baseArmorClass = armor.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 2);
            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 2);

            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 2);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 3);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 1);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 2);

            // Separate spell variants use the existing shared-spell helper.
            // The wish list's differing daily uses will need a later helper
            // enhancement; this helper currently assigns three uses per day.
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ScaleMailPlus1OfHealingWord", "Cleric's Scale Mail +1 of Healing Word",
                1, "Rare", "HealingWord");
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ScaleMailPlus1OfPrayerOfHealing", "Cleric's Scale Mail +1 of Prayer of Healing",
                1, "Rare", "PrayerOfHealing");
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "ScaleMailPlus1OfLesserRestoration", "Cleric's Scale Mail +1 of Lesser Restoration",
                1, "Rare", "LesserRestoration");

            return success;
        }

        private static bool RegisterProtection(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "ScaleMailPlus" + tier + "OfProtection";
            string title = "Scale Mail +" + tier + " of Protection";
            FeatureDefinitionSavingThrowAffinity saves =
                ArmorEnchantmentFactory.CreateSavingThrowBonus(id + "SaveBonus", tier);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier, tier == 1 ? "Uncommon" : "Rare",
                new List<FeatureDefinition> { saves }, sourceId, sourceId, baseArmorClass);
        }

        private static bool RegisterStrength(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "ScaleMailPlus" + tier + "OfStrength";
            string title = "Scale Mail +" + tier + " of Strength";
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
            string id = "ScaleMailPlus" + tier + "Of" + damageName + "Protection";
            string title = "Scale Mail +" + tier + " vs. " + damageName + " Damage";
            FeatureDefinitionDamageAffinity reduction =
                ArmorEnchantmentFactory.CreateFlatDamageReduction(id + "DamageReduction", damageType, tier);

            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                itemDb, id, title, tier,
                tier == 1 ? "Uncommon" : tier == 2 ? "Rare" : "VeryRare",
                new List<FeatureDefinition> { reduction }, sourceId, sourceId, baseArmorClass);
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
