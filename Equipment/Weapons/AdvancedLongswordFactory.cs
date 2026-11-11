using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    internal static class AdvancedLongswordFactory
    {
        private const string Prefix = "[Solasta Expanded Equipment] ";

        private sealed class SlayerSpec
        {
            internal readonly string Id;
            internal readonly string Title;
            internal readonly int BaseBonus;
            internal readonly string[] TargetTokens;
            internal readonly int[] TargetBonuses;

            internal SlayerSpec(string id, string title, int baseBonus,
                string[] targetTokens, int[] targetBonuses)
            {
                Id = id;
                Title = title;
                BaseBonus = baseBonus;
                TargetTokens = targetTokens;
                TargetBonuses = targetBonuses;
            }
        }

        internal static bool Register()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null)
                return false;

            ItemDefinition longsword = itemDb.GetElement("Longsword", true);
            if (longsword == null || longsword.GuiPresentation == null)
                return false;

            bool success = true;
            success &= RegisterSpellSword(itemDb, spellDb, longsword,
                "LongswordPlus2OfSlow", "Slow Longsword +2", "Slow", 2, null, 0);
            success &= RegisterQuicknessSword(itemDb, longsword, "Longsword");
            success &= RegisterConditionSword(itemDb, longsword, conditionDb,
                "LongswordPlus2OfPetrification", "Longsword +2 of Petrification",
                "ConditionPetrified", "Constitution", 11);
            success &= RegisterDemonicSword(itemDb, spellDb, longsword, conditionDb,
                "LongswordPlus2OfDemonicInfluence", "Longsword +2 of Demonic Influence");
            success &= RegisterSpellSword(itemDb, spellDb, longsword,
                "LongswordPlus3OfMindNumbing", "Longsword +3 of Mind Numbing",
                "Feeblemind", 3, "Wisdom", 14);

            SlayerSpec[] slayers =
            {
                new SlayerSpec("LongswordPlus2VsDragons", "Longsword +2 vs Dragons", 1,
                    new[] { "Dragon" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus2VsSorrAkkathPlus3", "Longsword +2 +3 vs Sorr-Akkath", 2,
                    new[] { "SorrAkkath" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus2VsGoblinoidsPlus3", "Longsword +2 +3 vs Goblinoids", 2,
                    new[] { "Goblinoid", "Goblin" }, new[] { 1, 1 }),
                new SlayerSpec("LongswordPlus2VsGiantsPlus3FeyPlus4", "Longsword +2 +3 vs Giants, +4 vs Fey", 2,
                    new[] { "Giant", "Fey" }, new[] { 1, 2 }),
                new SlayerSpec("LongswordPlus2VsDwarvesPlus3", "Longsword +2 +3 vs Dwarves", 2,
                    new[] { "Dwarf" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus2VsElvesPlus3", "Longsword +2 +3 vs Elves", 2,
                    new[] { "Elf" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus2VsHalflingsPlus3", "Longsword +2 +3 vs Halflings", 2,
                    new[] { "Halfling" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus2VsBeastsPlus3", "Longsword +2 +3 vs Beasts", 2,
                    new[] { "Beast" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus2VsHumansPlus3", "Longsword +2 +3 vs Humans", 2,
                    new[] { "Human" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus2VsUndeadPlus3", "Longsword +2 +3 vs Undead", 2,
                    new[] { "Undead" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus2VsAbominationsPlus3", "Longsword +2 +3 vs Abominations", 2,
                    new[] { "Abomination" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus2VsDragonsPlus3", "Longsword +2 +3 vs Dragons", 2,
                    new[] { "Dragon" }, new[] { 1 }),
                new SlayerSpec("LongswordPlus3VsSorrAkkath", "Longsword +3 vs Sorr-Akkath", 0,
                    new[] { "Sorr-Akkath" }, new[] { 3 }),
                new SlayerSpec("LongswordPlus3VsDragons", "Longsword +3 vs Dragons", 0,
                    new[] { "Dragons" }, new[] { 3 }),
                new SlayerSpec("LongswordPlus3VsUndead", "Longsword +3 vs Undead", 0,
                    new[] { "Undead" }, new[] { 3 })
            };

            foreach (SlayerSpec spec in slayers)
                success &= RegisterSlayer(itemDb, longsword, spec);

            Debug.Log(Prefix + "Advanced longsword registration finished: " + success + ".");
            return success;
        }

        internal static bool RegisterShortswords()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null)
                return false;
            ItemDefinition shortsword = itemDb.GetElement("Shortsword", true);
            if (shortsword == null || shortsword.GuiPresentation == null)
                return false;

            bool success = true;
            success &= RegisterSpellSword(itemDb, spellDb, shortsword,
                "ShortswordPlus2OfSlow", "Slow Shortsword +2", "Slow", 2, null, 0);
            success &= RegisterQuicknessSword(itemDb, shortsword, "Shortsword");
            success &= RegisterConditionSword(itemDb, shortsword, conditionDb,
                "ShortswordPlus2OfPetrification", "Shortsword +2 of Petrification",
                "ConditionPetrified", "Constitution", 11);
            success &= RegisterDemonicSword(itemDb, spellDb, shortsword, conditionDb,
                "ShortswordPlus2OfDemonicInfluence", "Shortsword +2 of Demonic Influence");
            success &= RegisterSpellSword(itemDb, spellDb, shortsword,
                "ShortswordPlus3OfMindNumbing", "Shortsword +3 of Mind Numbing",
                "Feeblemind", 3, "Wisdom", 14);

            SlayerSpec[] slayers =
            {
                new SlayerSpec("ShortswordPlus2VsGoblinoidsPlus3", "Shortsword +2 +3 vs Goblinoids", 2,
                    new[] { "Goblinoid", "Goblin" }, new[] { 1, 1 }),
                new SlayerSpec("ShortswordPlus2VsGiantsPlus3FeyPlus4", "Shortsword +2 +3 vs Giants, +4 vs Fey", 2,
                    new[] { "Giant", "Fey" }, new[] { 1, 2 }),
                new SlayerSpec("ShortswordPlus2VsDwarvesPlus3", "Shortsword +2 +3 vs Dwarves", 2,
                    new[] { "Dwarf" }, new[] { 1 }),
                new SlayerSpec("ShortswordPlus2VsElvesPlus3", "Shortsword +2 +3 vs Elves", 2,
                    new[] { "Elf" }, new[] { 1 }),
                new SlayerSpec("ShortswordPlus2VsHalflingsPlus3", "Shortsword +2 +3 vs Halflings", 2,
                    new[] { "Halfling" }, new[] { 1 }),
                new SlayerSpec("ShortswordPlus2VsBeastsPlus3", "Shortsword +2 +3 vs Beasts", 2,
                    new[] { "Beast" }, new[] { 1 }),
                new SlayerSpec("ShortswordPlus2VsHumansPlus3", "Shortsword +2 +3 vs Humans", 2,
                    new[] { "Human" }, new[] { 1 }),
                new SlayerSpec("ShortswordPlus2VsUndeadPlus3", "Shortsword +2 +3 vs Undead", 2,
                    new[] { "Undead" }, new[] { 1 }),
                new SlayerSpec("ShortswordPlus2VsAbominationsPlus3", "Shortsword +2 +3 vs Abominations", 2,
                    new[] { "Abomination" }, new[] { 1 }),
                new SlayerSpec("ShortswordPlus3VsDragons", "Shortsword +3 vs Dragons", 0,
                    new[] { "Dragons" }, new[] { 3 })
            };
            foreach (SlayerSpec spec in slayers)
                success &= RegisterSlayer(itemDb, shortsword, spec);
            Debug.Log(Prefix + "Advanced shortsword registration finished: " + success + ".");
            return success;
        }

        private static bool RegisterSpellSword(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            string id, string title, string spellId, int weaponBonus,
            string saveAbility, int fixedDc)
        {
            if (itemDb.GetElement(id, true) != null)
                return true;
            SpellDefinition spell = FindSpell(spellDb, spellId);
            if (spell == null || spell.EffectDescription == null ||
                spell.EffectDescription.EffectForms == null || spell.EffectDescription.EffectForms.Count == 0)
            {
                Debug.LogError(Prefix + "Could not find usable spell " + spellId + " for " + title + ".");
                return false;
            }

            return RegisterEffectSword(itemDb, template, id, title, weaponBonus,
                spell.EffectDescription, null, saveAbility, fixedDc);
        }

        private static bool RegisterConditionSword(Database<ItemDefinition> itemDb,
            ItemDefinition template, Database<ConditionDefinition> conditionDb,
            string id, string title, string conditionId, string saveAbility, int fixedDc)
        {
            ConditionDefinition condition = FindCondition(conditionDb, conditionId);
            if (condition == null)
            {
                Debug.LogError(Prefix + "Could not find condition " + conditionId + " for " + title + ".");
                return false;
            }
            return RegisterEffectSword(itemDb, template, id, title, 2,
                null, condition, saveAbility, fixedDc);
        }

        private static bool RegisterDemonicSword(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            Database<ConditionDefinition> conditionDb, string id, string title)
        {
            if (itemDb.GetElement(id, true) != null)
                return true;

            ConditionDefinition condition = FindCondition(conditionDb, "DemonicInfluence");
            if (condition != null)
                return RegisterEffectSword(itemDb, template, id, title, 2,
                    null, condition, "Constitution", 11);

            SpellDefinition spell = FindSpell(spellDb, "DemonicInfluence");
            if (spell != null && spell.EffectDescription != null)
                return RegisterEffectSword(itemDb, template, id, title, 2,
                    spell.EffectDescription, null, "Constitution", 11);

            Debug.LogError(Prefix + "Could not resolve Demonic Influence spell or condition.");
            return false;
        }

        private static bool RegisterEffectSword(Database<ItemDefinition> itemDb,
            ItemDefinition template, string id, string title, int bonus,
            EffectDescription source, ConditionDefinition condition,
            string saveAbility, int fixedDc)
        {
            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null) return false;
                item.ForceName(id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;
                if (!SetPresentation(item, template, title,
                    "A magical +" + bonus + " longsword. On a hit, " +
                    (condition == null ? title.Replace("Longsword +2 of ", "") : condition.GuiPresentation.Title) +
                    " affects the target. Its magical effect triggers when the weapon hits."))
                    return false;

                FeatureDefinitionAttackModifier modifier =
                    WeaponFactory.CreateAttackModifier(id, title, bonus, template.GuiPresentation);
                if (modifier == null || !AttachFeature(itemDb, item, template, modifier))
                    return false;

                object weaponDescription = Read(item, "WeaponDescription");
                object effect = Read(weaponDescription, "EffectDescription");
                IList forms = Read(effect, "EffectForms") as IList;
                if (forms == null) return false;
                forms.Clear();

                if (condition != null)
                {
                    EffectForm conditionForm = WeaponFactory.CreateMagicalLongswordConditionForm(condition, true);
                    if (conditionForm == null) return false;
                    forms.Add(conditionForm);
                    ConfigureEffect(effect, true, saveAbility, fixedDc, "Hour", 1);
                }
                else
                {
                    foreach (EffectForm form in source.EffectForms)
                    {
                        EffectForm copy = EffectForm.GetCopy(form);
                        if (copy == null) return false;
                        forms.Add(copy);
                    }
                    CopyEffectMetadata(source, effect);
                    if (fixedDc > 0)
                        ConfigureSave(effect, true, saveAbility, fixedDc);
                }

                itemDb.Add(item);
                return itemDb.GetElement(id, true) != null;
            }
            catch (Exception ex)
            {
                Debug.LogError(Prefix + "Could not register " + title + ": " + ex);
                return false;
            }
        }

        private static bool RegisterQuicknessSword(Database<ItemDefinition> itemDb,
            ItemDefinition template, string weaponType)
        {
            string id = weaponType + "Plus2OfQuickness";
            string title = weaponType + " +2 of Quickness";
            if (itemDb.GetElement(id, true) != null) return true;
            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                item.ForceName(id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;
                if (!SetPresentation(item, template, title,
                    "A magical +2 " + weaponType.ToLowerInvariant() +
                    " that grants its wielder one additional main attack."))
                    return false;
                FeatureDefinitionAttackModifier modifier =
                    WeaponFactory.CreateAttackModifier(id, title, 2, template.GuiPresentation);
                FeatureDefinitionAdditionalAction extraAttack =
                    ScriptableObject.CreateInstance<FeatureDefinitionAdditionalAction>();
                extraAttack.ForceName(id + "AdditionalAttack");
                extraAttack.SetUserContentGUID(Guid.NewGuid().ToString());
                extraAttack.IsUserContent = true;
                GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
                presentation.Title = title + " Additional Attack";
                presentation.Description = "Grants one additional main attack while this weapon is equipped.";
                if (!WeaponFactory.SetField(extraAttack, "guiPresentation", presentation) ||
                    !WeaponFactory.SetField(extraAttack, "actionType", ActionDefinitions.ActionType.Main) ||
                    !WeaponFactory.SetField(extraAttack, "maxAttacksNumber", 1) ||
                    !WeaponFactory.SetField(extraAttack, "triggerCondition", RuleDefinitions.AdditionalActionTriggerCondition.None) ||
                    modifier == null || !AttachFeature(itemDb, item, template, modifier) ||
                    !AttachFeature(itemDb, item, template, extraAttack) ||
                    !WeaponFactory.SetField(item, "magical", true))
                    return false;
                Database<FeatureDefinitionAdditionalAction> featureDb =
                    DatabaseRepository.GetDatabase<FeatureDefinitionAdditionalAction>();
                if (featureDb != null && featureDb.GetElement(extraAttack.Name, true) == null)
                    featureDb.Add(extraAttack);
                itemDb.Add(item);
                return itemDb.GetElement(id, true) != null;
            }
            catch (Exception ex)
            {
                Debug.LogError(Prefix + "Could not register " + title + ": " + ex);
                return false;
            }
        }

        private static bool RegisterSlayer(Database<ItemDefinition> itemDb,
            ItemDefinition template, SlayerSpec spec)
        {
            if (itemDb.GetElement(spec.Id, true) != null) return true;
            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                item.ForceName(spec.Id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;
                string description = BuildSlayerDescription(spec);
                if (!SetPresentation(item, template, spec.Title, description)) return false;

                // A target-only +3 Slayer sword has no universal attack/damage modifier.
                // Its full bonus is supplied by BeastSlayerCombatPatch only on a matching target.
                if (spec.BaseBonus > 0)
                {
                    FeatureDefinitionAttackModifier modifier = WeaponFactory.CreateAttackModifier(
                        spec.Id, spec.Title, spec.BaseBonus, template.GuiPresentation);
                    if (modifier == null || !AttachFeature(itemDb, item, template, modifier))
                        return false;
                }

                if (!WeaponFactory.SetField(item, "magical", true)) return false;
                itemDb.Add(item);
                return itemDb.GetElement(spec.Id, true) != null;
            }
            catch (Exception ex)
            {
                Debug.LogError(Prefix + "Could not register " + spec.Title + ": " + ex);
                return false;
            }
        }

        private static string BuildSlayerDescription(SlayerSpec spec)
        {
            if (spec.BaseBonus <= 0 && spec.TargetTokens.Length == 1 &&
                spec.TargetBonuses.Length == 1)
            {
                return "A magical longsword that grants +" + spec.TargetBonuses[0] +
                    " to attack and damage against " + spec.TargetTokens[0] + ".";
            }

            string description = "A magical longsword";
            if (spec.BaseBonus > 0)
                description += " that grants +" + spec.BaseBonus +
                    " to attack and damage";
            else
                description += " with target-specific attack and damage bonuses";

            if (spec.TargetTokens.Length > 0)
            {
                for (int i = 0; i < spec.TargetTokens.Length; i++)
                {
                    int targetBonus = i < spec.TargetBonuses.Length
                        ? spec.TargetBonuses[i] : 0;
                    if (targetBonus <= 0)
                        continue;

                    description += i == 0 ? ", and an additional +" : ", plus +";
                    description += targetBonus + " to attack and damage against " +
                        spec.TargetTokens[i];
                }
            }

            return description + ".";
        }

        private static bool AttachFeature(Database<ItemDefinition> itemDb,
            ItemDefinition item, ItemDefinition template, FeatureDefinition feature)
        {
            if (item.StaticProperties == null)
                WeaponFactory.SetField(item, "staticProperties", new List<ItemPropertyDescription>());
            ItemPropertyDescription property = WeaponFactory.CreateFeatureProperty(itemDb, template, feature);
            if (property == null) return false;
            item.StaticProperties.Add(property);
            return true;
        }

        private static bool SetPresentation(ItemDefinition item, ItemDefinition template,
            string title, string description)
        {
            GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
            presentation.Title = title;
            presentation.Description = description;
            return WeaponFactory.SetField(item, "guiPresentation", presentation) &&
                   WeaponFactory.SetField(item, "magical", true);
        }

        private static SpellDefinition FindSpell(Database<SpellDefinition> db, string id)
        {
            SpellDefinition direct = db.GetElement(id, true);
            if (direct != null) return direct;
            foreach (SpellDefinition spell in db.GetAllElements())
                if (spell != null && Normalize(spell.Name) == Normalize(id)) return spell;
            return null;
        }

        private static ConditionDefinition FindCondition(Database<ConditionDefinition> db, string token)
        {
            ConditionDefinition direct = db.GetElement(token, true);
            if (direct != null) return direct;
            foreach (ConditionDefinition condition in db.GetAllElements())
                if (condition != null && Normalize(condition.Name).Contains(Normalize(token))) return condition;
            return null;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty :
                value.Replace("Condition", "").Replace("Spell", "").Replace("_", "").ToLowerInvariant();
        }

        private static void CopyEffectMetadata(EffectDescription source, object destination)
        {
            foreach (string member in new[] { "HasSavingThrow", "SavingThrowAbility",
                "DifficultyClassComputation", "SavingThrowDifficultyAbility",
                "FixedSavingThrowDifficultyClass", "DurationType", "DurationParameter",
                "EndOfEffect", "HasShoveRoll" })
                SetMember(destination, char.ToLowerInvariant(member[0]) + member.Substring(1), Read(source, member));
        }

        private static void ConfigureEffect(object effect, bool hasSave,
            string ability, int dc, string duration, int durationParameter)
        {
            SetMember(effect, "hasSavingThrow", hasSave);
            SetMember(effect, "savingThrowAbility", ability);
            SetEnum(effect, "difficultyClassComputation", "FixedValue");
            SetMember(effect, "fixedSavingThrowDifficultyClass", dc);
            SetEnum(effect, "durationType", duration);
            SetMember(effect, "durationParameter", durationParameter);
        }

        private static void ConfigureSave(object effect, bool hasSave, string ability, int dc)
        {
            SetMember(effect, "hasSavingThrow", hasSave);
            SetMember(effect, "savingThrowAbility", ability);
            SetEnum(effect, "difficultyClassComputation", "FixedValue");
            SetMember(effect, "fixedSavingThrowDifficultyClass", dc);
        }

        private static object Read(object target, string name)
        {
            if (target == null) return null;
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
                PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(target, null);
                type = type.BaseType;
            }
            return null;
        }

        private static bool SetMember(object target, string name, object value)
        {
            if (target == null) return false;
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null && !field.IsInitOnly) { field.SetValue(target, value); return true; }
                PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null && property.CanWrite) { property.SetValue(target, value, null); return true; }
                type = type.BaseType;
            }
            return false;
        }

        private static bool SetEnum(object target, string name, string value)
        {
            object current = Read(target, name);
            if (current == null) return false;
            return SetMember(target, name, Enum.Parse(current.GetType(), value));
        }
    }
}
