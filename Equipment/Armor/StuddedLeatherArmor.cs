using System;
using System.Collections.Generic;
using UnityEngine;
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
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 3);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 3);
            success &= RegisterDamageProtection(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 3);

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
                ArmorEnchantmentFactory.CreateAttributeModifier(id + "DexterityBonus", "Dexterity", "Additive", tier);
            List<FeatureDefinitionProficiency> bowProficiencies =
                CreateLongAndShortBowProficiencies(id + "BowProficiency", itemDb);

            if (dexterity == null || bowProficiencies == null || bowProficiencies.Count < 2)
                return false;

            string description = title + ". Grants proficiency with longbows and shortbows and increases Dexterity by " + tier + ".";
            List<FeatureDefinition> features = new List<FeatureDefinition> { dexterity };
            features.AddRange(bowProficiencies);

            return ArmorEnchantmentFactory.RegisterVariant(
                itemDb, id, title, tier, tier == 1 ? "Rare" : "VeryRare", description,
                features, null, null, 0, sourceId, sourceId, baseArmorClass);
        }

        internal static List<FeatureDefinitionProficiency> CreateLongAndShortBowProficiencies(
            string id, Database<ItemDefinition> itemDb)
        {
            Database<FeatureDefinitionProficiency> proficiencyDb =
                DatabaseRepository.GetDatabase<FeatureDefinitionProficiency>();
            if (proficiencyDb == null)
                return null;

            Database<WeaponTypeDefinition> weaponTypeDb =
                DatabaseRepository.GetDatabase<WeaponTypeDefinition>();
            string longbowType = weaponTypeDb != null && weaponTypeDb.GetElement("LongbowType", true) != null
                ? "LongbowType" : FindWeaponType(itemDb, "Longbow");
            string shortbowType = weaponTypeDb != null && weaponTypeDb.GetElement("ShortbowType", true) != null
                ? "ShortbowType" : FindWeaponType(itemDb, "Shortbow");
            if (string.IsNullOrEmpty(longbowType) || string.IsNullOrEmpty(shortbowType))
                return null;

            List<FeatureDefinitionProficiency> features = new List<FeatureDefinitionProficiency>();
            if (!AddWeaponProficiency(proficiencyDb, id + "Longbow", "Longbow", longbowType, features) ||
                !AddWeaponProficiency(proficiencyDb, id + "Shortbow", "Shortbow", shortbowType, features))
                return null;

            return features;
        }

        private static bool AddWeaponProficiency(
            Database<FeatureDefinitionProficiency> proficiencyDb,
            string id,
            string weaponName,
            string weaponType,
            List<FeatureDefinitionProficiency> features)
        {
            FeatureDefinitionProficiency feature = proficiencyDb.GetElement(id, true);
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FeatureDefinitionProficiency>();
                if (feature == null)
                    return false;
                feature.ForceName(id);
                feature.SetUserContentGUID(Guid.NewGuid().ToString());
                feature.IsUserContent = true;
                proficiencyDb.Add(feature);
                feature = proficiencyDb.GetElement(id, true);
            }

            if (feature == null ||
                !ArmorFactory.SetField(feature, "proficiencyType", RuleDefinitions.ProficiencyType.Weapon) ||
                !ArmorFactory.SetField(feature, "proficiencies", new List<string> { weaponType }) ||
                !ArmorFactory.SetField(feature, "guiPresentation", MakeBowProficiencyPresentation(weaponName)))
                return false;

            Debug.Log("[Solasta Expanded Equipment] Configured " + weaponName +
                " proficiency feature " + id + " for weapon type " + weaponType + ".");
            features.Add(feature);
            return true;
        }

        private static GuiPresentation MakeBowProficiencyPresentation(string weaponName)
        {
            GuiPresentation gui = new GuiPresentation();
            gui.Title = weaponName + " Proficiency";
            gui.Description = "Grants proficiency with " + weaponName.ToLowerInvariant() + "s.";
            return gui;
        }

        private static string FindWeaponType(Database<ItemDefinition> itemDb, string weaponName)
        {
            if (itemDb == null)
                return null;

            ItemDefinition item = itemDb.GetElement(weaponName, true);
            if (item != null && item.WeaponDescription != null &&
                !string.IsNullOrEmpty(item.WeaponDescription.WeaponType))
                return item.WeaponDescription.WeaponType;

            foreach (ItemDefinition candidate in itemDb.GetAllElements())
            {
                if (candidate == null || candidate.WeaponDescription == null ||
                    candidate.GuiPresentation == null)
                    continue;

                string candidateTitle = candidate.GuiPresentation.Title ?? string.Empty;
                if (candidateTitle.IndexOf(weaponName, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    !string.IsNullOrEmpty(candidate.WeaponDescription.WeaponType))
                    return candidate.WeaponDescription.WeaponType;
            }

            return null;
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
