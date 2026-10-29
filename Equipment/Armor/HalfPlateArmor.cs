using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    public static class HalfPlateArmor
    {
        private sealed class DailySpell
        {
            internal readonly string Id;
            internal readonly string Title;
            internal readonly int Uses;

            internal DailySpell(string id, string title, int uses)
            {
                Id = id;
                Title = title;
                Uses = uses;
            }
        }

        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition source = FindHalfPlateTemplate(itemDb);
            if (source == null || source.ArmorDescription == null ||
                source.GuiPresentation == null || source.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Half-Plate armor template.");
                return false;
            }

            string sourceId = source.Name;
            int baseArmorClass = source.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterStrength(itemDb, sourceId, baseArmorClass, 2);
            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 1);
            success &= RegisterProtection(itemDb, sourceId, baseArmorClass, 2);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "HalfPlatePlus1OfFeatherFall", "Half-Plate +1 of Feather Fall", 1, "Uncommon", "FeatherFall", 3);
            success &= RegisterSpell(itemDb, sourceId, baseArmorClass,
                "HalfPlatePlus1OfFreeMovement", "Half-Plate +1 of Free Movement", 1, "Rare", "FreedomOfMovement", 3);

            success &= RegisterPhysicalDefense(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 1);
            success &= RegisterPhysicalDefense(itemDb, sourceId, baseArmorClass, "Piercing", "DamagePiercing", 2);
            success &= RegisterPhysicalDefense(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 1);
            success &= RegisterPhysicalDefense(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 2);
            success &= RegisterPhysicalDefense(itemDb, sourceId, baseArmorClass, "Slashing", "DamageSlashing", 3);
            success &= RegisterPhysicalDefense(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 1);
            success &= RegisterPhysicalDefense(itemDb, sourceId, baseArmorClass, "Bludgeoning", "DamageBludgeoning", 2);

            success &= RegisterDailySpellArmor(itemDb, sourceId, baseArmorClass,
                "ClericsHalfPlatePlus1OfHealing", "Cleric's Half-Plate +1 of Healing", 1, "Rare",
                new[]
                {
                    new DailySpell("HealingWord", "Healing Word", 3),
                    new DailySpell("PrayerOfHealing", "Prayer of Healing", 2),
                    new DailySpell("LesserRestoration", "Lesser Restoration", 1)
                });
            success &= RegisterDailySpellArmor(itemDb, sourceId, baseArmorClass,
                "ClericsHalfPlatePlus2OfHealing", "Cleric's Half-Plate +2 of Healing", 2, "VeryRare",
                new[]
                {
                    new DailySpell("HealingWord", "Healing Word", 3),
                    new DailySpell("PrayerOfHealing", "Prayer of Healing", 3),
                    new DailySpell("LesserRestoration", "Lesser Restoration", 2),
                    new DailySpell("Heal", "Heal", 1)
                });
            success &= RegisterDailySpellArmor(itemDb, sourceId, baseArmorClass,
                "ClericsHalfPlatePlus1OfRestoration", "Cleric's Half-Plate +1 of Restoration", 1, "Rare",
                new[] { new DailySpell("LesserRestoration", "Lesser Restoration", 3) });

            success &= RegisterPaladinStrength(itemDb, sourceId, baseArmorClass, 1, 20);
            success &= RegisterPaladinStrength(itemDb, sourceId, baseArmorClass, 2, 21);
            success &= RegisterPaladinStrength(itemDb, sourceId, baseArmorClass, 3, 22);
            success &= RegisterDailySpellArmor(itemDb, sourceId, baseArmorClass,
                "PaladinsHalfPlatePlus1OfPrayers", "Paladin's Half-Plate +1 of Prayers", 1, "Rare",
                new[] { new DailySpell("PrayerOfHealing", "Prayer of Healing", 3) });
            success &= RegisterElementalBlessing(itemDb, sourceId, baseArmorClass);

            return success;
        }

        internal static ItemDefinition FindHalfPlateTemplate(Database<ItemDefinition> itemDb)
        {
            if (itemDb == null)
                return null;

            string[] knownIds = { "HalfPlate", "HalfPlateArmor", "ArmorHalfPlate", "HalfPlateMail" };
            foreach (string id in knownIds)
            {
                ItemDefinition candidate = itemDb.GetElement(id, true);
                if (IsHalfPlate(candidate))
                    return candidate;
            }

            foreach (ItemDefinition candidate in itemDb.GetAllElements())
                if (IsHalfPlate(candidate))
                    return candidate;

            return null;
        }

        private static bool IsHalfPlate(ItemDefinition item)
        {
            if (item == null || item.ArmorDescription == null || item.ItemPresentation == null ||
                item.GuiPresentation == null)
                return false;

            string id = item.Name ?? string.Empty;
            string title = item.GuiPresentation.Title ?? string.Empty;
            if (id.IndexOf("Primed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Plus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Of", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("ScaleMail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                title.IndexOf("Dwarven", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return string.Equals(item.ArmorDescription.ArmorType, "HalfPlateType", StringComparison.OrdinalIgnoreCase) &&
                (id.IndexOf("HalfPlate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 title.IndexOf("Halfplate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 title.IndexOf("Half-Plate", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool RegisterStrength(Database<ItemDefinition> db, string source, int baseAc, int tier)
        {
            string id = "HalfPlatePlus" + tier + "OfStrength";
            string title = "Half-Plate +" + tier + " of Strength";
            FeatureDefinitionAttributeModifier strength = ArmorEnchantmentFactory.CreateAttributeModifier(
                id + "StrengthBonus", "Strength", "Set", 19);
            return RegisterPassive(db, source, baseAc, id, title, tier,
                tier == 1 ? "Rare" : "VeryRare", new List<FeatureDefinition> { strength });
        }

        private static bool RegisterProtection(Database<ItemDefinition> db, string source, int baseAc, int tier)
        {
            string id = "HalfPlatePlus" + tier + "OfProtection";
            string title = "Half-Plate +" + tier + " of Protection";
            FeatureDefinitionSavingThrowAffinity saves =
                ArmorEnchantmentFactory.CreateSavingThrowBonus(id + "SaveBonus", tier);
            return RegisterPassive(db, source, baseAc, id, title, tier,
                tier == 1 ? "Uncommon" : "Rare", new List<FeatureDefinition> { saves });
        }

        private static bool RegisterPhysicalDefense(
            Database<ItemDefinition> db, string source, int baseAc,
            string damageName, string damageType, int tier)
        {
            string id = "HalfPlatePlus" + tier + "Of" + damageName + "Protection";
            string title = "Half-Plate +" + tier + " vs. " + damageName + " Damage";
            FeatureDefinitionDamageAffinity reduction = ArmorEnchantmentFactory.CreateFlatDamageReduction(
                id + "DamageReduction", damageType, tier);
            string rarity = tier == 1 ? "Uncommon" : tier == 2 ? "Rare" : "VeryRare";
            return RegisterPassive(db, source, baseAc, id, title, tier, rarity,
                new List<FeatureDefinition> { reduction });
        }

        private static bool RegisterPaladinStrength(
            Database<ItemDefinition> db, string source, int baseAc, int tier, int maxStrength)
        {
            string id = "PaladinsHalfPlatePlus" + tier + "OfStrength";
            string title = "Paladin's Half-Plate +" + tier + " of Strength";
            FeatureDefinitionAttributeModifier strength = ArmorEnchantmentFactory.CreateAttributeModifier(
                id + "StrengthBonus", "Strength", "Additive", tier);
            string description = title + ". Grants +" + tier + " Strength and raises the Strength maximum to " +
                maxStrength + " while worn.";
            return ArmorEnchantmentFactory.RegisterVariant(db, id, title, tier,
                tier == 1 ? "Rare" : "VeryRare", description,
                new List<FeatureDefinition> { strength }, null, null, 0,
                source, source, baseAc);
        }

        private static bool RegisterElementalBlessing(Database<ItemDefinition> db, string source, int baseAc)
        {
            const string itemId = "PaladinsHalfPlatePlus1OfBlessingOfElements";
            const string title = "Paladin's Half-Plate +1 of Blessing of the Elements";
            ConditionDefinition chosenMarker = CreateCondition(
                itemId + "ChoiceUsed", "Elemental Blessing Used", "The elemental blessing is chosen until the next long rest.",
                new List<FeatureDefinition>());
            if (chosenMarker == null)
                return false;

            List<FeatureDefinition> powers = new List<FeatureDefinition>();
            string[] elementNames = { "Acid", "Cold", "Fire", "Lightning", "Poison", "Thunder" };
            string[] damageTypes = { "DamageAcid", "DamageCold", "DamageFire", "DamageLightning", "DamagePoison", "DamageThunder" };

            for (int i = 0; i < elementNames.Length; i++)
            {
                FeatureDefinitionDamageAffinity resistance = ArmorEnchantmentFactory.CreateDamageResistance(
                    itemId + elementNames[i] + "Resistance", damageTypes[i]);
                ConditionDefinition condition = CreateCondition(
                    itemId + elementNames[i] + "Condition",
                    elementNames[i] + " Resistance",
                    "Grants resistance to " + elementNames[i].ToLowerInvariant() + " damage until the next long rest.",
                    new List<FeatureDefinition> { resistance });
                if (resistance == null || condition == null)
                    return false;

                FeatureDefinitionPower power = CreateElementChoicePower(
                    itemId + "Choose" + elementNames[i],
                    "Choose " + elementNames[i] + " Resistance",
                    condition,
                    chosenMarker);
                if (power == null)
                    return false;
                powers.Add(power);
            }

            string description = title + ". Grants +1 AC. After each long rest, choose one damage type—acid, cold, fire, lightning, poison, or thunder—to resist until the next long rest.";
            return ArmorEnchantmentFactory.RegisterVariant(db, itemId, title, 1, "Rare",
                description, powers, null, null, 0, source, source, baseAc);
        }

        private static FeatureDefinitionPower CreateElementChoicePower(
            string id, string title, ConditionDefinition resistance, ConditionDefinition marker)
        {
            Database<FeatureDefinitionPower> powerDb = DatabaseRepository.GetDatabase<FeatureDefinitionPower>();
            if (powerDb == null)
                return null;

            FeatureDefinitionPower power = powerDb.GetElement(id, true);
            if (power != null)
                return EnsurePowerGuiDefinition(power) ? power : null;

            power = ScriptableObject.CreateInstance<FeatureDefinitionPower>();
            if (power == null)
                return null;
            power.ForceName(id);
            power.SetUserContentGUID(Guid.NewGuid().ToString());
            power.IsUserContent = true;

            ConditionForm resistanceForm = ConditionForm.Get();
            resistanceForm.ConditionDefinition = resistance;
            resistanceForm.Operation = ConditionForm.ConditionOperation.Add;
            ArmorFactory.SetField(resistanceForm, "forceOnSelf", true);

            ConditionForm markerForm = ConditionForm.Get();
            markerForm.ConditionDefinition = marker;
            markerForm.Operation = ConditionForm.ConditionOperation.Add;
            ArmorFactory.SetField(markerForm, "forceOnSelf", true);

            EffectForm resistanceEffect = EffectForm.Get();
            resistanceEffect.FormType = EffectForm.EffectFormType.Condition;
            resistanceEffect.ConditionForm = resistanceForm;
            EffectForm markerEffect = EffectForm.Get();
            markerEffect.FormType = EffectForm.EffectFormType.Condition;
            markerEffect.ConditionForm = markerForm;

            EffectDescription effect = new EffectDescription();
            effect.RangeType = RuleDefinitions.RangeType.Self;
            effect.TargetType = RuleDefinitions.TargetType.Self;
            effect.TargetSide = RuleDefinitions.Side.Ally;
            effect.DurationType = RuleDefinitions.DurationType.UntilLongRest;
            effect.EffectForms.Add(resistanceEffect);
            effect.EffectForms.Add(markerEffect);

            GuiPresentation presentation = new GuiPresentation();
            presentation.Title = title;
            presentation.Description = "Choose this elemental resistance until your next long rest. Only one elemental blessing can be chosen at a time.";

            if (!ArmorFactory.SetField(power, "effectDescription", effect) ||
                !ArmorFactory.SetField(power, "activationTime", RuleDefinitions.ActivationTime.Action) ||
                !ArmorFactory.SetField(power, "rechargeRate", RuleDefinitions.RechargeRate.Dawn) ||
                !ArmorFactory.SetField(power, "usesDetermination", RuleDefinitions.UsesDetermination.Fixed) ||
                !ArmorFactory.SetField(power, "fixedUsesPerRecharge", 1) ||
                !ArmorFactory.SetField(power, "disableIfConditionIsOwned", marker) ||
                !ArmorFactory.SetField(power, "guiPresentation", presentation))
                return null;

            powerDb.Add(power);
            FeatureDefinitionPower registered = powerDb.GetElement(id, true);
            return registered != null && EnsurePowerGuiDefinition(registered) ? registered : null;
        }

        private static bool EnsurePowerGuiDefinition(FeatureDefinitionPower power)
        {
            try
            {
                IGuiWrapperService guiService = ServiceRepository.GetService<IGuiWrapperService>();
                if (guiService == null || power == null)
                    return true;

                FieldInfo mapField = null;
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                for (Type type = guiService.GetType(); type != null && mapField == null; type = type.BaseType)
                    mapField = type.GetField("powerDefinitionsMap", flags | BindingFlags.DeclaredOnly);
                if (mapField == null)
                    return true;

                IDictionary map = mapField.GetValue(guiService) as IDictionary;
                if (map == null)
                    return true;

                if (map[power.Name] == null)
                    map[power.Name] = new GuiPowerDefinition(power);
                return map[power.Name] != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Solasta Expanded Equipment] Could not register the elemental blessing power UI: " + ex.Message);
                return false;
            }
        }

        private static ConditionDefinition CreateCondition(
            string id, string title, string description, List<FeatureDefinition> features)
        {
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (conditionDb == null)
                return null;

            ConditionDefinition condition = conditionDb.GetElement(id, true);
            if (condition != null)
                return condition;

            condition = ScriptableObject.CreateInstance<ConditionDefinition>();
            if (condition == null)
                return null;
            condition.ForceName(id);
            condition.SetUserContentGUID(Guid.NewGuid().ToString());
            condition.IsUserContent = true;

            GuiPresentation presentation = new GuiPresentation();
            presentation.Title = title;
            presentation.Description = description;
            if (!ArmorFactory.SetField(condition, "guiPresentation", presentation) ||
                !ArmorFactory.SetField(condition, "conditionType", RuleDefinitions.ConditionType.Beneficial) ||
                !ArmorFactory.SetField(condition, "features", features) ||
                !ArmorFactory.SetField(condition, "specialDuration", true) ||
                !ArmorFactory.SetField(condition, "durationType", RuleDefinitions.DurationType.UntilLongRest))
                return null;

            conditionDb.Add(condition);
            return conditionDb.GetElement(id, true);
        }

        private static bool RegisterSpell(
            Database<ItemDefinition> db, string source, int baseAc,
            string id, string title, int tier, string rarity, string spellId, int durationMinutes)
        {
            return ArmorEnchantmentFactory.RegisterSharedSpellVariant(
                db, id, title, tier, rarity, spellId, durationMinutes,
                source, source, baseAc);
        }

        private static bool RegisterDailySpellArmor(
            Database<ItemDefinition> db, string source, int baseAc,
            string id, string title, int tier, string rarity, DailySpell[] spells)
        {
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            if (spellDb == null)
                return false;

            List<KeyValuePair<SpellDefinition, int>> spellUses = new List<KeyValuePair<SpellDefinition, int>>();
            List<string> descriptionParts = new List<string>();
            foreach (DailySpell spell in spells)
            {
                SpellDefinition definition = spellDb.GetElement(spell.Id, true);
                if (definition == null)
                    return false;
                spellUses.Add(new KeyValuePair<SpellDefinition, int>(definition, spell.Uses));
                descriptionParts.Add(spell.Title + " " + spell.Uses + " times per day");
            }

            UsableDeviceDescription device = RingItems.CreateSpellFunctionsByRecharge(db, spellUses);
            if (device == null)
                return false;

            string description = title + ". Grants +" + tier + " AC. " +
                string.Join("; ", descriptionParts.ToArray()) + ". Uses recharge at dawn.";
            return ArmorEnchantmentFactory.RegisterVariant(
                db, id, title, tier, rarity, description, new List<FeatureDefinition>(),
                null, null, 0, source, source, baseAc, device);
        }

        private static bool RegisterPassive(
            Database<ItemDefinition> db, string source, int baseAc,
            string id, string title, int tier, string rarity, List<FeatureDefinition> features)
        {
            return ArmorEnchantmentFactory.RegisterPassiveVariant(
                db, id, title, tier, rarity, features, source, source, baseAc);
        }
    }

    [HarmonyPatch(typeof(RulesetActor), "RefreshAttributes")]
    internal static class PaladinHalfPlateStrengthCapPatch
    {
        [HarmonyPostfix]
        private static void Postfix(RulesetActor __instance)
        {
            RulesetCharacterHero hero = __instance as RulesetCharacterHero;
            if (hero == null || hero.CharacterInventory == null ||
                hero.CharacterInventory.InventorySlotsByName == null)
                return;

            RulesetInventorySlot torso;
            if (!hero.CharacterInventory.InventorySlotsByName.TryGetValue(
                    EquipmentDefinitions.SlotTypeTorso, out torso) || torso == null || torso.EquipedItem == null ||
                torso.EquipedItem.ItemDefinition == null)
                return;

            string id = torso.EquipedItem.ItemDefinition.Name ?? string.Empty;
            int maxStrength = id.EndsWith("PaladinsHalfPlatePlus1OfStrength", StringComparison.Ordinal) ? 20 :
                id.EndsWith("PaladinsHalfPlatePlus2OfStrength", StringComparison.Ordinal) ? 21 :
                id.EndsWith("PaladinsHalfPlatePlus3OfStrength", StringComparison.Ordinal) ? 22 : 0;
            if (maxStrength == 0)
                return;

            RulesetAttribute strength = hero.GetAttribute("Strength");
            if (strength == null)
                return;

            if (strength.MaxValue < maxStrength)
                strength.MaxValue = maxStrength;
            if (strength.MaxEditableValue < maxStrength)
                strength.MaxEditableValue = maxStrength;
            strength.Refresh();
        }
    }
}
