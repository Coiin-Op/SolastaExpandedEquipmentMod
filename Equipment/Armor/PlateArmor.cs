using System;
using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class PlateArmor
    {
        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition plate = FindPlateTemplate(itemDb);
            if (plate == null || plate.ArmorDescription == null ||
                plate.GuiPresentation == null || plate.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Plate armor template.");
                return false;
            }

            string sourceId = plate.Name;
            int baseArmorClass = plate.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 2);
            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 2);

            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "PlateArmorPlus2OfMistWalking", "Plate Armor +2 of Mist Walking",
                2, "Rare", "MistyStep", 3);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "PlateArmorPlus1OfFeatherFall", "Plate Armor +1 of Feather Fall",
                1, "Uncommon", "FeatherFall", 3);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "PlateArmorPlus1OfFreeMovement", "Plate Armor +1 of Free Movement",
                1, "Rare", "FreedomOfMovement", 3);

            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Piercing", "DamagePiercing", 1);
            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Piercing", "DamagePiercing", 2);
            success &= RegisterArmorBonus(itemDb, sourceId, baseArmorClass,
                "Piercing", "DamagePiercing", 3);
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
            string id = "PlateArmorPlus" + tier + "OfStrength";
            string title = "Plate Armor +" + tier + " of Strength";
            FeatureDefinitionAttributeModifier strength = ArmorEnchantmentFactory.CreateAttributeModifier(
                id + "StrengthBonus", "Strength", "Set", 19);
            return RegisterPassive(itemDb, id, title, tier, tier == 1 ? "Rare" : "VeryRare",
                new List<FeatureDefinition> { strength }, sourceId, baseArmorClass);
        }

        private static bool RegisterProtection(
            Database<ItemDefinition> itemDb, string sourceId, int baseArmorClass, int tier)
        {
            string id = "PlateArmorPlus" + tier + "OfProtection";
            string title = "Plate Armor +" + tier + " of Protection";
            FeatureDefinitionSavingThrowAffinity saves =
                ArmorEnchantmentFactory.CreateSavingThrowBonus(id + "SaveBonus", tier);
            return RegisterPassive(itemDb, id, title, tier, tier == 1 ? "Uncommon" : "Rare",
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
            string id = "PlateArmorPlus" + tier + "Of" + damageName + "Protection";
            string title = "Plate Armor +" + tier + " vs. " + damageName + " Damage";
            FeatureDefinitionDamageAffinity reduction = ArmorEnchantmentFactory.CreateFlatDamageReduction(
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
                itemDb, id, title, tier, rarity, features, sourceId, sourceId, baseArmorClass);
        }

        internal static ItemDefinition FindPlateTemplate(Database<ItemDefinition> itemDb)
        {
            if (itemDb == null)
                return null;

            string[] knownIds = { "PlateArmor", "ArmorPlate", "PlateMail", "Plate" };
            foreach (string id in knownIds)
            {
                ItemDefinition candidate = itemDb.GetElement(id, true);
                if (IsPlate(candidate))
                    return candidate;
            }

            foreach (ItemDefinition candidate in itemDb.GetAllElements())
                if (IsPlate(candidate))
                    return candidate;

            return null;
        }

        private static bool IsPlate(ItemDefinition item)
        {
            if (item == null || item.ArmorDescription == null || item.ItemPresentation == null ||
                item.GuiPresentation == null)
                return false;

            string id = item.Name ?? string.Empty;
            string armorType = item.ArmorDescription.ArmorType ?? string.Empty;
            string title = item.GuiPresentation.Title ?? string.Empty;
            if (id.IndexOf("Plus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Of", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return id.IndexOf("Plate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                armorType.IndexOf("Plate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                title.IndexOf("Plate", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
