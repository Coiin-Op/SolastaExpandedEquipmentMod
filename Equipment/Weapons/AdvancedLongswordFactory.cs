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

        internal static bool RegisterScimitars()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null)
                return false;

            ItemDefinition scimitar = itemDb.GetElement("Scimitar", true);
            if (scimitar == null || scimitar.GuiPresentation == null)
            {
                Debug.LogError(Prefix + "Vanilla Scimitar template unavailable or incomplete.");
                return false;
            }

            bool success = true;
            success &= RegisterScimitarSpellWeapon(itemDb, spellDb, scimitar,
                "ScimitarPlus2OfSlow", "Slow Scimitar +2", 2, "Slow", null, 0);
            success &= RegisterScimitarQuickness(itemDb, scimitar);
            success &= RegisterScimitarConditionWeapon(itemDb, scimitar, conditionDb,
                "ScimitarPlus2OfPetrification", "Scimitar +2 of Petrification",
                "ConditionPetrified", "Constitution", 11);
            success &= RegisterScimitarDemonicInfluence(itemDb, spellDb, scimitar, conditionDb);
            success &= RegisterScimitarSpellWeapon(itemDb, spellDb, scimitar,
                "ScimitarPlus3OfMindNumbing", "Scimitar +3 of Mind Numbing", 3,
                "Feeblemind", "Wisdom", 14);

            ScimitarSlayerSpec[] slayers =
            {
                new ScimitarSlayerSpec("ScimitarPlus2VsGoblinoidsPlus3", "Scimitar +2 +3 vs Goblinoids", 2,
                    new[] { "Goblinoids" }),
                new ScimitarSlayerSpec("ScimitarPlus2VsGiantsPlus3FeyPlus4", "Scimitar +2 +3 vs Giants, +4 vs Fey", 2,
                    new[] { "Giants", "Fey" }),
                new ScimitarSlayerSpec("ScimitarPlus2VsDwarvesPlus3", "Scimitar +2 +3 vs Dwarves", 2,
                    new[] { "Dwarves" }),
                new ScimitarSlayerSpec("ScimitarPlus2VsElvesPlus3", "Scimitar +2 +3 vs Elves", 2,
                    new[] { "Elves" }),
                new ScimitarSlayerSpec("ScimitarPlus2VsHalflingsPlus3", "Scimitar +2 +3 vs Halflings", 2,
                    new[] { "Halflings" }),
                new ScimitarSlayerSpec("ScimitarPlus2VsBeastsPlus3", "Scimitar +2 +3 vs Beasts", 2,
                    new[] { "Beasts" }),
                new ScimitarSlayerSpec("ScimitarPlus2VsHumansPlus3", "Scimitar +2 +3 vs Humans", 2,
                    new[] { "Humans" }),
                new ScimitarSlayerSpec("ScimitarPlus2VsUndeadPlus3", "Scimitar +2 +3 vs Undead", 2,
                    new[] { "Undead" }),
                new ScimitarSlayerSpec("ScimitarPlus2VsAbominationsPlus3", "Scimitar +2 +3 vs Abominations", 2,
                    new[] { "Abominations" }),
                new ScimitarSlayerSpec("ScimitarPlus3VsDragons", "Scimitar +3 vs Dragons", 0,
                    new[] { "Dragons" })
            };

            int slayersRegistered = 0;
            foreach (ScimitarSlayerSpec spec in slayers)
                if (RegisterScimitarSlayer(itemDb, scimitar, spec))
                    slayersRegistered++;

            Debug.Log(Prefix + "Advanced scimitar slayers registered: " +
                slayersRegistered + "/" + slayers.Length + ".");
            success &= slayersRegistered == slayers.Length;
            Debug.Log(Prefix + "Advanced scimitar registration finished: " + success + ".");
            return success;
        }

        private sealed class ScimitarSlayerSpec
        {
            internal readonly string Id;
            internal readonly string Title;
            internal readonly int BaseBonus;
            internal readonly string[] TargetTokens;

            internal ScimitarSlayerSpec(string id, string title, int baseBonus, string[] targetTokens)
            {
                Id = id;
                Title = title;
                BaseBonus = baseBonus;
                TargetTokens = targetTokens;
            }
        }

        private static bool RegisterScimitarSpellWeapon(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            string id, string title, int bonus, string spellId,
            string saveAbility, int fixedDc)
        {
            if (itemDb.GetElement(id, true) != null) return true;
            SpellDefinition spell = FindSpell(spellDb, spellId);
            if (spell == null || spell.EffectDescription == null ||
                spell.EffectDescription.EffectForms == null || spell.EffectDescription.EffectForms.Count == 0)
            {
                Debug.LogError(Prefix + "Could not find usable spell " + spellId + " for " + title + ".");
                return false;
            }
            return RegisterScimitarEffectWeapon(itemDb, template, id, title, bonus,
                spell.EffectDescription, null, saveAbility, fixedDc);
        }

        private static bool RegisterScimitarConditionWeapon(Database<ItemDefinition> itemDb,
            ItemDefinition template, Database<ConditionDefinition> conditionDb,
            string id, string title, string conditionId, string saveAbility, int fixedDc)
        {
            ConditionDefinition condition = FindCondition(conditionDb, conditionId);
            if (condition == null)
            {
                Debug.LogError(Prefix + "Could not find condition " + conditionId + " for " + title + ".");
                return false;
            }
            return RegisterScimitarEffectWeapon(itemDb, template, id, title, 2,
                null, condition, saveAbility, fixedDc);
        }

        private static bool RegisterScimitarDemonicInfluence(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            Database<ConditionDefinition> conditionDb)
        {
            const string id = "ScimitarPlus2OfDemonicInfluence";
            const string title = "Scimitar +2 of Demonic Influence";
            if (itemDb.GetElement(id, true) != null) return true;

            ConditionDefinition condition = FindCondition(conditionDb, "DemonicInfluence");
            if (condition != null)
                return RegisterScimitarEffectWeapon(itemDb, template, id, title, 2,
                    null, condition, "Constitution", 11);

            SpellDefinition spell = FindSpell(spellDb, "DemonicInfluence");
            if (spell != null && spell.EffectDescription != null &&
                spell.EffectDescription.EffectForms != null && spell.EffectDescription.EffectForms.Count > 0)
                return RegisterScimitarEffectWeapon(itemDb, template, id, title, 2,
                    spell.EffectDescription, null, "Constitution", 11);

            Debug.LogError(Prefix + "Could not resolve Demonic Influence spell or condition for the Scimitar.");
            return false;
        }

        private static bool RegisterScimitarEffectWeapon(Database<ItemDefinition> itemDb,
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
                string effectName = condition != null && condition.GuiPresentation != null
                    ? condition.GuiPresentation.Title
                    : title.Replace("Scimitar +2 of ", "").Replace("Scimitar +3 of ", "");
                if (!SetPresentation(item, template, title,
                    "A magical +" + bonus + " scimitar. On a hit, " + effectName +
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

        private static bool RegisterScimitarQuickness(Database<ItemDefinition> itemDb, ItemDefinition template)
        {
            const string id = "ScimitarPlus2OfQuickness";
            const string title = "Scimitar +2 of Quickness";
            if (itemDb.GetElement(id, true) != null) return true;
            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null) return false;
                item.ForceName(id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;
                if (!SetPresentation(item, template, title,
                    "A magical +2 scimitar that grants its wielder one additional main attack."))
                    return false;

                FeatureDefinitionAttackModifier modifier =
                    WeaponFactory.CreateAttackModifier(id, title, 2, template.GuiPresentation);
                FeatureDefinitionAdditionalAction extraAttack =
                    ScriptableObject.CreateInstance<FeatureDefinitionAdditionalAction>();
                if (extraAttack == null) return false;
                extraAttack.ForceName(id + "AdditionalAttack");
                extraAttack.SetUserContentGUID(Guid.NewGuid().ToString());
                extraAttack.IsUserContent = true;
                GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
                presentation.Title = title + " Additional Attack";
                presentation.Description = "Grants one additional main attack while this scimitar is equipped.";
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

        private static bool RegisterScimitarSlayer(Database<ItemDefinition> itemDb,
            ItemDefinition template, ScimitarSlayerSpec spec)
        {
            if (itemDb.GetElement(spec.Id, true) != null) return true;
            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null) return false;
                item.ForceName(spec.Id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                string description = spec.BaseBonus > 0
                    ? "A magical scimitar that grants +" + spec.BaseBonus +
                      " to attack and damage, with an additional conditional bonus against " +
                      string.Join(" and ", spec.TargetTokens) + "."
                    : "A magical scimitar that grants +3 to attack and damage against Dragons only.";
                if (!SetPresentation(item, template, spec.Title, description)) return false;

                if (spec.BaseBonus > 0)
                {
                    FeatureDefinitionAttackModifier modifier = WeaponFactory.CreateAttackModifier(
                        spec.Id, spec.Title, spec.BaseBonus, template.GuiPresentation);
                    if (modifier == null || !AttachFeature(itemDb, item, template, modifier))
                        return false;
                }

                itemDb.Add(item);
                return itemDb.GetElement(spec.Id, true) != null;
            }
            catch (Exception ex)
            {
                Debug.LogError(Prefix + "Could not register " + spec.Title + ": " + ex);
                return false;
            }
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
            {
                Debug.LogError(Prefix + "Vanilla Shortsword template unavailable or incomplete.");
                return false;
            }

            bool success = true;
            success &= RegisterShortswordSpellWeapon(itemDb, spellDb, shortsword,
                "ShortswordPlus2OfSlow", "Slow Shortsword +2", 2, "Slow", null, 0);
            success &= RegisterShortswordQuickness(itemDb, shortsword);
            success &= RegisterShortswordConditionWeapon(itemDb, shortsword, conditionDb,
                "ShortswordPlus2OfPetrification", "Shortsword +2 of Petrification",
                "ConditionPetrified", "Constitution", 11);
            success &= RegisterShortswordDemonicInfluence(itemDb, spellDb, shortsword, conditionDb);
            success &= RegisterShortswordSpellWeapon(itemDb, spellDb, shortsword,
                "ShortswordPlus3OfMindNumbing", "Shortsword +3 of Mind Numbing", 3,
                "Feeblemind", "Wisdom", 14);

            ShortswordSlayerSpec[] slayers =
            {
                new ShortswordSlayerSpec("ShortswordPlus2VsGoblinoidsPlus3", "Shortsword +2 +3 vs Goblinoids", 2,
                    new[] { "Goblinoids" }),
                new ShortswordSlayerSpec("ShortswordPlus2VsGiantsPlus3FeyPlus4", "Shortsword +2 +3 vs Giants, +4 vs Fey", 2,
                    new[] { "Giants", "Fey" }),
                new ShortswordSlayerSpec("ShortswordPlus2VsDwarvesPlus3", "Shortsword +2 +3 vs Dwarves", 2,
                    new[] { "Dwarves" }),
                new ShortswordSlayerSpec("ShortswordPlus2VsElvesPlus3", "Shortsword +2 +3 vs Elves", 2,
                    new[] { "Elves" }),
                new ShortswordSlayerSpec("ShortswordPlus2VsHalflingsPlus3", "Shortsword +2 +3 vs Halflings", 2,
                    new[] { "Halflings" }),
                new ShortswordSlayerSpec("ShortswordPlus2VsBeastsPlus3", "Shortsword +2 +3 vs Beasts", 2,
                    new[] { "Beasts" }),
                new ShortswordSlayerSpec("ShortswordPlus2VsHumansPlus3", "Shortsword +2 +3 vs Humans", 2,
                    new[] { "Humans" }),
                new ShortswordSlayerSpec("ShortswordPlus2VsUndeadPlus3", "Shortsword +2 +3 vs Undead", 2,
                    new[] { "Undead" }),
                new ShortswordSlayerSpec("ShortswordPlus2VsAbominationsPlus3", "Shortsword +2 +3 vs Abominations", 2,
                    new[] { "Abominations" }),
                new ShortswordSlayerSpec("ShortswordPlus3VsDragons", "Shortsword +3 vs Dragons", 0,
                    new[] { "Dragons" })
            };

            int slayersRegistered = 0;
            foreach (ShortswordSlayerSpec spec in slayers)
                if (RegisterShortswordSlayer(itemDb, shortsword, spec))
                    slayersRegistered++;

            Debug.Log(Prefix + "Advanced shortsword slayers registered: " +
                slayersRegistered + "/" + slayers.Length + ".");
            success &= slayersRegistered == slayers.Length;
            Debug.Log(Prefix + "Advanced shortsword registration finished: " + success + ".");
            return success;
        }

        internal static bool RegisterRapiers()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null)
                return false;
            ItemDefinition rapier = itemDb.GetElement("Rapier", true);
            if (rapier == null || rapier.GuiPresentation == null)
                return false;

            bool success = true;
            success &= RegisterRapierSpellSword(itemDb, spellDb, rapier,
                "RapierPlus2OfSlow", "Slow Rapier +2", "Slow", 2, null, 0, "rapier");
            success &= RegisterQuicknessSword(itemDb, rapier, "Rapier");
            success &= RegisterRapierConditionSword(itemDb, rapier, conditionDb,
                "RapierPlus2OfPetrification", "Rapier +2 of Petrification",
                "ConditionPetrified", "Constitution", 11, "rapier");
            success &= RegisterRapierDemonicSword(itemDb, spellDb, rapier, conditionDb,
                "RapierPlus2OfDemonicInfluence", "Rapier +2 of Demonic Influence", "rapier");
            success &= RegisterRapierSpellSword(itemDb, spellDb, rapier,
                "RapierPlus3OfMindNumbing", "Rapier +3 of Mind Numbing",
                "Feeblemind", 3, "Wisdom", 11, "rapier");

            SlayerSpec[] slayers =
            {
                new SlayerSpec("RapierPlus2VsGoblinoidsPlus3FeyPlus4", "Rapier +2 +3 vs Goblinoids, +4 vs Fey", 2,
                    new[] { "Goblinoids", "Fey" }, new[] { 1, 2 }),
                new SlayerSpec("RapierPlus2VsDwarvesPlus3", "Rapier +2 +3 vs Dwarves", 2,
                    new[] { "Dwarves" }, new[] { 1 }),
                new SlayerSpec("RapierPlus2VsElvesPlus3", "Rapier +2 +3 vs Elves", 2,
                    new[] { "Elves" }, new[] { 1 }),
                new SlayerSpec("RapierPlus2VsHalflingsPlus3", "Rapier +2 +3 vs Halflings", 2,
                    new[] { "Halflings" }, new[] { 1 }),
                new SlayerSpec("RapierPlus2VsBeastsPlus3", "Rapier +2 +3 vs Beasts", 2,
                    new[] { "Beasts" }, new[] { 1 }),
                new SlayerSpec("RapierPlus2VsHumansPlus3", "Rapier +2 +3 vs Humans", 2,
                    new[] { "Humans" }, new[] { 1 }),
                new SlayerSpec("RapierPlus2VsUndeadPlus3", "Rapier +2 +3 vs Undead", 2,
                    new[] { "Undead" }, new[] { 1 }),
                new SlayerSpec("RapierPlus2VsAbominationsPlus3", "Rapier +2 +3 vs Abominations", 2,
                    new[] { "Abominations" }, new[] { 1 }),
                new SlayerSpec("RapierPlus3VsDragons", "Rapier +3 vs Dragons", 0,
                    new[] { "Dragons" }, new[] { 3 })
            };
            foreach (SlayerSpec spec in slayers)
                success &= RegisterSlayer(itemDb, rapier, spec, "rapier");
            Debug.Log(Prefix + "Advanced rapier registration finished: " + success + ".");
            return success;
        }

        internal static bool RegisterGreatswords()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null)
                return false;
            ItemDefinition greatsword = itemDb.GetElement("Greatsword", true);
            if (greatsword == null || greatsword.GuiPresentation == null)
                return false;

            bool success = true;
            success &= RegisterRapierSpellSword(itemDb, spellDb, greatsword,
                "GreatswordPlus2OfSlow", "Slow Greatsword +2", "Slow", 2, null, 0, "greatsword");
            success &= RegisterQuicknessSword(itemDb, greatsword, "Greatsword");
            success &= RegisterRapierConditionSword(itemDb, greatsword, conditionDb,
                "GreatswordPlus2OfPetrification", "Greatsword +2 of Petrification",
                "ConditionPetrified", "Constitution", 11, "greatsword");
            success &= RegisterRapierDemonicSword(itemDb, spellDb, greatsword, conditionDb,
                "GreatswordPlus2OfDemonicInfluence", "Greatsword +2 of Demonic Influence", "greatsword");
            success &= RegisterRapierSpellSword(itemDb, spellDb, greatsword,
                "GreatswordPlus3OfMindNumbing", "Greatsword +3 of Mind Numbing",
                "Feeblemind", 3, "Wisdom", 11, "greatsword");

            SlayerSpec[] slayers =
            {
                new SlayerSpec("GreatswordPlus2VsGoblinoidsPlus3", "Greatsword +2 +3 vs Goblinoids", 2,
                    new[] { "Goblinoids" }, new[] { 1 }),
                new SlayerSpec("GreatswordPlus2VsGiantsPlus3FeyPlus4", "Greatsword +2 +3 vs Giants, +4 vs Fey", 2,
                    new[] { "Giants", "Fey" }, new[] { 1, 2 }),
                new SlayerSpec("GreatswordPlus2VsDwarvesPlus3", "Greatsword +2 +3 vs Dwarves", 2,
                    new[] { "Dwarves" }, new[] { 1 }),
                new SlayerSpec("GreatswordPlus2VsElvesPlus3", "Greatsword +2 +3 vs Elves", 2,
                    new[] { "Elves" }, new[] { 1 }),
                new SlayerSpec("GreatswordPlus2VsHalflingsPlus3", "Greatsword +2 +3 vs Halflings", 2,
                    new[] { "Halflings" }, new[] { 1 }),
                new SlayerSpec("GreatswordPlus2VsBeastsPlus3", "Greatsword +2 +3 vs Beasts", 2,
                    new[] { "Beasts" }, new[] { 1 }),
                new SlayerSpec("GreatswordPlus2VsHumansPlus3", "Greatsword +2 +3 vs Humans", 2,
                    new[] { "Humans" }, new[] { 1 }),
                new SlayerSpec("GreatswordPlus2VsUndeadPlus3", "Greatsword +2 +3 vs Undead", 2,
                    new[] { "Undead" }, new[] { 1 }),
                new SlayerSpec("GreatswordPlus2VsAbominationsPlus3", "Greatsword +2 +3 vs Abominations", 2,
                    new[] { "Abominations" }, new[] { 1 }),
                new SlayerSpec("GreatswordPlus3VsDragons", "Greatsword +3 vs Dragons", 0,
                    new[] { "Dragons" }, new[] { 3 })
            };
            foreach (SlayerSpec spec in slayers)
                success &= RegisterSlayer(itemDb, greatsword, spec, "greatsword");
            Debug.Log(Prefix + "Advanced greatsword registration finished: " + success + ".");
            return success;
        }

        internal static bool RegisterKatanas()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null)
                return false;
            ItemDefinition katana = itemDb.GetElement("Katana", true);
            if (katana == null)
            {
                katana = itemDb.GetElement("Longsword", true);
                Debug.LogWarning(Prefix + "Katana template not found; using Longsword as the Katana base template.");
            }
            if (katana == null || katana.GuiPresentation == null)
                return false;

            bool success = true;
            success &= RegisterRapierSpellSword(itemDb, spellDb, katana,
                "KatanaPlus2OfSlow", "Slow Katana +2", "Slow", 2, null, 0, "katana");
            success &= RegisterQuicknessSword(itemDb, katana, "Katana");
            success &= RegisterRapierConditionSword(itemDb, katana, conditionDb,
                "KatanaPlus2OfPetrification", "Katana +2 of Petrification",
                "ConditionPetrified", "Constitution", 11, "katana");
            success &= RegisterRapierDemonicSword(itemDb, spellDb, katana, conditionDb,
                "KatanaPlus2OfDemonicInfluence", "Katana +2 of Demonic Influence", "katana");
            success &= RegisterRapierSpellSword(itemDb, spellDb, katana,
                "KatanaPlus3OfMindNumbing", "Katana +3 of Mind Numbing",
                "Feeblemind", 3, "Wisdom", 11, "katana");

            SlayerSpec[] slayers =
            {
                new SlayerSpec("KatanaPlus2VsGoblinoidsPlus3", "Katana +2 +3 vs Goblinoids", 2,
                    new[] { "Goblinoids" }, new[] { 1 }),
                new SlayerSpec("KatanaPlus2VsGiantsPlus3FeyPlus4", "Katana +2 +3 vs Giants, +4 vs Fey", 2,
                    new[] { "Giants", "Fey" }, new[] { 1, 2 }),
                new SlayerSpec("KatanaPlus2VsDwarvesPlus3", "Katana +2 +3 vs Dwarves", 2,
                    new[] { "Dwarves" }, new[] { 1 }),
                new SlayerSpec("KatanaPlus2VsElvesPlus3", "Katana +2 +3 vs Elves", 2,
                    new[] { "Elves" }, new[] { 1 }),
                new SlayerSpec("KatanaPlus2VsHalflingsPlus3", "Katana +2 +3 vs Halflings", 2,
                    new[] { "Halflings" }, new[] { 1 }),
                new SlayerSpec("KatanaPlus2VsBeastsPlus3", "Katana +2 +3 vs Beasts", 2,
                    new[] { "Beasts" }, new[] { 1 }),
                new SlayerSpec("KatanaPlus2VsHumansPlus3", "Katana +2 +3 vs Humans", 2,
                    new[] { "Humans" }, new[] { 1 }),
                new SlayerSpec("KatanaPlus2VsUndeadPlus3", "Katana +2 +3 vs Undead", 2,
                    new[] { "Undead" }, new[] { 1 }),
                new SlayerSpec("KatanaPlus2VsAbominationsPlus3", "Katana +2 +3 vs Abominations", 2,
                    new[] { "Abominations" }, new[] { 1 }),
                new SlayerSpec("KatanaPlus3VsDragons", "Katana +3 vs Dragons", 0,
                    new[] { "Dragons" }, new[] { 3 })
            };
            foreach (SlayerSpec spec in slayers)
                success &= RegisterSlayer(itemDb, katana, spec, "katana");
            Debug.Log(Prefix + "Advanced katana registration finished: " + success + ".");
            return success;
        }

        internal static bool RegisterLongbows()
        {
            return RegisterBows("Longbow", "longbow");
        }

        internal static bool RegisterShortbows()
        {
            return RegisterBows("Shortbow", "shortbow");
        }

        internal static bool RegisterWarhammers()
        {
            return RegisterBows("Warhammer", "warhammer");
        }
        internal static bool RegisterGeneratedWeaponSet(
            string title, string weaponType, int demonicDc, bool includeDemonicInfluence)
        {
            return RegisterBows(title, weaponType, demonicDc, includeDemonicInfluence);
        }
        internal static bool RegisterMaces()
        {
            return RegisterBows("Mace", "mace");
        }

        private static bool RegisterBows(string title, string weaponType, int demonicDc = 11, bool includeDemonicInfluence = true)
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null)
                return false;
            ItemDefinition weapon = WeaponFactory.ResolveWeaponTemplate(itemDb, title, weaponType);
            if (weapon == null || weapon.GuiPresentation == null)
                return false;

            bool success = true;
            string idPrefix = WeaponFactory.NormalizeItemId(title);
            success &= RegisterRapierSpellSword(itemDb, spellDb, weapon,
                idPrefix + "Plus2OfSlow", "Slow " + title + " +2", "Slow", 2, null, 0, weaponType);
            success &= RegisterQuicknessSword(itemDb, weapon, title);
            success &= RegisterRapierConditionSword(itemDb, weapon, conditionDb,
                idPrefix + "Plus2OfPetrification", title + " +2 of Petrification",
                "ConditionPetrified", "Constitution", 11, weaponType);
            if (includeDemonicInfluence)
                success &= RegisterRapierDemonicSword(itemDb, spellDb, weapon, conditionDb,
                    idPrefix + "Plus2OfDemonicInfluence", title + " +2 of Demonic Influence", weaponType, demonicDc);
            success &= RegisterRapierSpellSword(itemDb, spellDb, weapon,
                idPrefix + "Plus3OfMindNumbing", title + " +3 of Mind Numbing",
                "Feeblemind", 3, "Wisdom", 11, weaponType);

            SlayerSpec[] slayers =
            {
                new SlayerSpec(idPrefix + "Plus2VsGoblinoidsPlus3", title + " +2 +3 vs Goblinoids", 2,
                    new[] { "Goblinoids" }, new[] { 1 }),
                new SlayerSpec(idPrefix + "Plus2VsGiantsPlus3FeyPlus4", title + " +2 +3 vs Giants, +4 vs Fey", 2,
                    new[] { "Giants", "Fey" }, new[] { 1, 2 }),
                new SlayerSpec(idPrefix + "Plus2VsDwarvesPlus3", title + " +2 +3 vs Dwarves", 2,
                    new[] { "Dwarves" }, new[] { 1 }),
                new SlayerSpec(idPrefix + "Plus2VsElvesPlus3", title + " +2 +3 vs Elves", 2,
                    new[] { "Elves" }, new[] { 1 }),
                new SlayerSpec(idPrefix + "Plus2VsHalflingsPlus3", title + " +2 +3 vs Halflings", 2,
                    new[] { "Halflings" }, new[] { 1 }),
                new SlayerSpec(idPrefix + "Plus2VsBeastsPlus3", title + " +2 +3 vs Beasts", 2,
                    new[] { "Beasts" }, new[] { 1 }),
                new SlayerSpec(idPrefix + "Plus2VsUndeadPlus3", title + " +2 +3 vs Undead", 2,
                    new[] { "Undead" }, new[] { 1 }),
                new SlayerSpec(idPrefix + "Plus2VsAbominationsPlus3", title + " +2 +3 vs Abominations", 2,
                    new[] { "Abominations" }, new[] { 1 }),
                new SlayerSpec(idPrefix + "Plus3VsDragons", title + " +3 vs Dragons", 0,
                    new[] { "Dragons" }, new[] { 3 })
            };
            foreach (SlayerSpec spec in slayers)
                success &= RegisterSlayer(itemDb, weapon, spec, weaponType);
            Debug.Log(Prefix + "Advanced " + weaponType + " registration finished: " + success + ".");
            return success;
        }

        private sealed class ShortswordSlayerSpec
        {
            internal readonly string Id;
            internal readonly string Title;
            internal readonly int BaseBonus;
            internal readonly string[] TargetTokens;

            internal ShortswordSlayerSpec(string id, string title, int baseBonus, string[] targetTokens)
            {
                Id = id;
                Title = title;
                BaseBonus = baseBonus;
                TargetTokens = targetTokens;
            }
        }

        private static bool RegisterShortswordSpellWeapon(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            string id, string title, int bonus, string spellId,
            string saveAbility, int fixedDc)
        {
            if (itemDb.GetElement(id, true) != null) return true;
            SpellDefinition spell = FindSpell(spellDb, spellId);
            if (spell == null || spell.EffectDescription == null ||
                spell.EffectDescription.EffectForms == null || spell.EffectDescription.EffectForms.Count == 0)
            {
                Debug.LogError(Prefix + "Could not find usable spell " + spellId + " for " + title + ".");
                return false;
            }
            return RegisterShortswordEffectWeapon(itemDb, template, id, title, bonus,
                spell.EffectDescription, null, saveAbility, fixedDc);
        }

        private static bool RegisterShortswordConditionWeapon(Database<ItemDefinition> itemDb,
            ItemDefinition template, Database<ConditionDefinition> conditionDb,
            string id, string title, string conditionId, string saveAbility, int fixedDc)
        {
            ConditionDefinition condition = FindCondition(conditionDb, conditionId);
            if (condition == null)
            {
                Debug.LogError(Prefix + "Could not find condition " + conditionId + " for " + title + ".");
                return false;
            }
            return RegisterShortswordEffectWeapon(itemDb, template, id, title, 2,
                null, condition, saveAbility, fixedDc);
        }

        private static bool RegisterShortswordDemonicInfluence(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            Database<ConditionDefinition> conditionDb)
        {
            const string id = "ShortswordPlus2OfDemonicInfluence";
            const string title = "Shortsword +2 of Demonic Influence";
            if (itemDb.GetElement(id, true) != null) return true;

            ConditionDefinition condition = FindCondition(conditionDb, "DemonicInfluence");
            if (condition != null)
                return RegisterShortswordEffectWeapon(itemDb, template, id, title, 2,
                    null, condition, "Constitution", 11);

            SpellDefinition spell = FindSpell(spellDb, "DemonicInfluence");
            if (spell != null && spell.EffectDescription != null &&
                spell.EffectDescription.EffectForms != null && spell.EffectDescription.EffectForms.Count > 0)
                return RegisterShortswordEffectWeapon(itemDb, template, id, title, 2,
                    spell.EffectDescription, null, "Constitution", 11);

            Debug.LogError(Prefix + "Could not resolve Demonic Influence spell or condition for the Shortsword.");
            return false;
        }

        private static bool RegisterShortswordEffectWeapon(Database<ItemDefinition> itemDb,
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
                string effectName = condition != null && condition.GuiPresentation != null
                    ? condition.GuiPresentation.Title
                    : title.Replace("Shortsword +2 of ", "").Replace("Shortsword +3 of ", "");
                if (!SetPresentation(item, template, title,
                    "A magical +" + bonus + " shortsword. On a hit, " + effectName +
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

        private static bool RegisterShortswordQuickness(Database<ItemDefinition> itemDb, ItemDefinition template)
        {
            const string id = "ShortswordPlus2OfQuickness";
            const string title = "Shortsword +2 of Quickness";
            if (itemDb.GetElement(id, true) != null) return true;
            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null) return false;
                item.ForceName(id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;
                if (!SetPresentation(item, template, title,
                    "A magical +2 shortsword that grants its wielder one additional main attack."))
                    return false;

                FeatureDefinitionAttackModifier modifier =
                    WeaponFactory.CreateAttackModifier(id, title, 2, template.GuiPresentation);
                FeatureDefinitionAdditionalAction extraAttack =
                    ScriptableObject.CreateInstance<FeatureDefinitionAdditionalAction>();
                if (extraAttack == null) return false;
                extraAttack.ForceName(id + "AdditionalAttack");
                extraAttack.SetUserContentGUID(Guid.NewGuid().ToString());
                extraAttack.IsUserContent = true;
                GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
                presentation.Title = title + " Additional Attack";
                presentation.Description = "Grants one additional main attack while this shortsword is equipped.";
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

        private static bool RegisterShortswordSlayer(Database<ItemDefinition> itemDb,
            ItemDefinition template, ShortswordSlayerSpec spec)
        {
            if (itemDb.GetElement(spec.Id, true) != null) return true;
            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null) return false;
                item.ForceName(spec.Id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                string description = spec.BaseBonus > 0
                    ? "A magical shortsword that grants +" + spec.BaseBonus +
                      " to attack and damage, with an additional conditional bonus against " +
                      string.Join(" and ", spec.TargetTokens) + "."
                    : "A magical shortsword that grants +3 to attack and damage against Dragons only.";
                if (!SetPresentation(item, template, spec.Title, description)) return false;

                if (spec.BaseBonus > 0)
                {
                    FeatureDefinitionAttackModifier modifier = WeaponFactory.CreateAttackModifier(
                        spec.Id, spec.Title, spec.BaseBonus, template.GuiPresentation);
                    if (modifier == null || !AttachFeature(itemDb, item, template, modifier))
                        return false;
                }

                itemDb.Add(item);
                return itemDb.GetElement(spec.Id, true) != null;
            }
            catch (Exception ex)
            {
                Debug.LogError(Prefix + "Could not register " + spec.Title + ": " + ex);
                return false;
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
                "LongswordPlus2OfSlow", "Slow Longsword +2", "Slow", null, 0);
            success &= RegisterQuicknessSword(itemDb, longsword, "Longsword");
            success &= RegisterConditionSword(itemDb, longsword, conditionDb,
                "LongswordPlus2OfPetrification", "Longsword +2 of Petrification",
                "ConditionPetrified", "Constitution", 11);
            success &= RegisterDemonicSword(itemDb, spellDb, longsword, conditionDb);
            success &= RegisterSpellSword(itemDb, spellDb, longsword,
                "LongswordPlus3OfMindNumbing", "Longsword +3 of Mind Numbing",
                "Feeblemind", "Wisdom", 14);

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
                new SlayerSpec("LongswordPlus3VsSorrAkkath", "Longsword +3 vs Sorr-Akkath", 3,
                    new string[0], new int[0]),
                new SlayerSpec("LongswordPlus3VsDragons", "Longsword +3 vs Dragons", 3,
                    new string[0], new int[0]),
                new SlayerSpec("LongswordPlus3VsUndead", "Longsword +3 vs Undead", 3,
                    new string[0], new int[0])
            };

            foreach (SlayerSpec spec in slayers)
                success &= RegisterSlayer(itemDb, longsword, spec, "longsword");

            Debug.Log(Prefix + "Advanced longsword registration finished: " + success + ".");
            return success;
        }

        private static bool RegisterRapierSpellSword(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            string id, string title, string spellId, int bonus,
            string saveAbility, int fixedDc, string weaponType)
        {
            if (itemDb.GetElement(id, true) != null) return true;
            SpellDefinition spell = FindSpell(spellDb, spellId);
            if (spell == null || spell.EffectDescription == null ||
                spell.EffectDescription.EffectForms == null || spell.EffectDescription.EffectForms.Count == 0)
            {
                Debug.LogError(Prefix + "Could not find usable spell " + spellId + " for " + title + ".");
                return false;
            }
            return RegisterEffectSword(itemDb, template, id, title, bonus,
                spell.EffectDescription, null, saveAbility, fixedDc, weaponType);
        }

        private static bool RegisterRapierConditionSword(Database<ItemDefinition> itemDb,
            ItemDefinition template, Database<ConditionDefinition> conditionDb,
            string id, string title, string conditionId, string saveAbility, int fixedDc, string weaponType)
        {
            ConditionDefinition condition = FindCondition(conditionDb, conditionId);
            if (condition == null) return false;
            return RegisterEffectSword(itemDb, template, id, title, 2,
                null, condition, saveAbility, fixedDc, weaponType);
        }

        private static bool RegisterRapierDemonicSword(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            Database<ConditionDefinition> conditionDb, string id, string title, string weaponType, int fixedDc = 11)
        {
            if (itemDb.GetElement(id, true) != null) return true;
            ConditionDefinition condition = FindCondition(conditionDb, "DemonicInfluence");
            if (condition != null)
                return RegisterEffectSword(itemDb, template, id, title, 2,
                    null, condition, "Constitution", fixedDc, weaponType);
            SpellDefinition spell = FindSpell(spellDb, "DemonicInfluence");
            if (spell != null && spell.EffectDescription != null &&
                spell.EffectDescription.EffectForms != null && spell.EffectDescription.EffectForms.Count > 0)
                return RegisterEffectSword(itemDb, template, id, title, 2,
                    spell.EffectDescription, null, "Constitution", fixedDc, weaponType);
            Debug.LogError(Prefix + "Could not resolve Demonic Influence spell or condition for Rapier.");
            return false;
        }

        private static bool RegisterSpellSword(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            string id, string title, string spellId, string saveAbility, int fixedDc)
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

            return RegisterEffectSword(itemDb, template, id, title, 2,
                spell.EffectDescription, null, saveAbility, fixedDc, "longsword");
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
                null, condition, saveAbility, fixedDc, "longsword");
        }

        private static bool RegisterDemonicSword(Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb, ItemDefinition template,
            Database<ConditionDefinition> conditionDb)
        {
            const string id = "LongswordPlus2OfDemonicInfluence";
            const string title = "Longsword +2 of Demonic Influence";
            if (itemDb.GetElement(id, true) != null)
                return true;

            ConditionDefinition condition = FindCondition(conditionDb, "DemonicInfluence");
            if (condition != null)
                return RegisterEffectSword(itemDb, template, id, title, 2,
                    null, condition, "Constitution", 11, "longsword");

            SpellDefinition spell = FindSpell(spellDb, "DemonicInfluence");
            if (spell != null && spell.EffectDescription != null)
                return RegisterEffectSword(itemDb, template, id, title, 2,
                    spell.EffectDescription, null, "Constitution", 11, "longsword");

            Debug.LogError(Prefix + "Could not resolve Demonic Influence spell or condition.");
            return false;
        }

        private static bool RegisterEffectSword(Database<ItemDefinition> itemDb,
            ItemDefinition template, string id, string title, int bonus,
            EffectDescription source, ConditionDefinition condition,
            string saveAbility, int fixedDc, string weaponType)
        {
            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null) return false;
                item.ForceName(id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;
                if (!SetPresentation(item, template, title,
                    "A magical +" + bonus + " " + weaponType + ". On a hit, " +
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
            string id = WeaponFactory.NormalizeItemId(weaponType) + "Plus2OfQuickness";
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
            ItemDefinition template, SlayerSpec spec, string weaponType)
        {
            if (itemDb.GetElement(spec.Id, true) != null) return true;
            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                item.ForceName(spec.Id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;
                string description;
                if (spec.BaseBonus <= 0 && spec.TargetTokens.Length == 1 &&
                    spec.TargetBonuses.Length == 1)
                {
                    description = "A magical " + weaponType + " that grants +" +
                        spec.TargetBonuses[0] + " to attack and damage against " +
                        spec.TargetTokens[0] + ".";
                }
                else
                {
                    description = "A magical +" + spec.BaseBonus + " " + weaponType +
                        " that grants +" + spec.BaseBonus + " to attack and damage";
                    if (spec.TargetTokens.Length > 0)
                        description += ", with additional bonuses against " +
                            string.Join(" and ", spec.TargetTokens) + ".";
                    else description += ".";
                }
                if (!SetPresentation(item, template, spec.Title, description)) return false;
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
