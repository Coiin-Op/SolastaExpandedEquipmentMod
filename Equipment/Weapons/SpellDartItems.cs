using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>Registers spell-effect darts, using the dart's weapon effect forms.</summary>
    internal static class SpellDartItems
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";
        internal const int RandomCurseMarkerFilterId = 1837462051;

        private sealed class DartSpec
        {
            internal readonly string Id;
            internal readonly string Title;
            internal readonly string Rarity;
            internal readonly string SpellId;
            internal readonly string SpellTitle;

            internal DartSpec(string id, string title, string rarity, string spellId, string spellTitle)
            {
                Id = id;
                Title = title;
                Rarity = rarity;
                SpellId = spellId;
                SpellTitle = spellTitle;
            }
        }

        internal static bool Register()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            if (itemDb == null || spellDb == null)
                return false;

            ItemDefinition template = itemDb.GetElement("Dart", true);
            if (template == null)
            {
                Debug.LogError(LogPrefix + "Could not find the base Dart item.");
                return false;
            }

            DartSpec[] specs =
            {
                new DartSpec("DartOfThunder", "Dart of Thunder", "Uncommon", "Thunderwave", "Thunderwave"),
                new DartSpec("DartOfCurses", "Dart of Curses", "Rare", "BestowCurse", "Bestow Curse"),
                new DartSpec("DartOfSlow", "Dart of Slow", "Rare", "Slow", "Slow"),
                new DartSpec("DartOfConfusion", "Dart of Confusion", "Rare", "Confusion", "Confusion")
            };

            bool success = true;
            foreach (DartSpec spec in specs)
                success &= RegisterOne(itemDb, spellDb, template, spec);

            Debug.Log(LogPrefix + "Spell dart registration completed: " + success + ".");
            return success;
        }

        private static bool RegisterOne(Database<ItemDefinition> itemDb, Database<SpellDefinition> spellDb,
            ItemDefinition template, DartSpec spec)
        {
            if (itemDb.GetElement(spec.Id, true) != null)
                return true;

            SpellDefinition spell = spellDb.GetElement(spec.SpellId, true);
            if (spell == null)
            {
                Debug.LogError(LogPrefix + "Could not find " + spec.SpellId + " for " + spec.Title + ".");
                return false;
            }

            bool randomCurse = spec.SpellId == "BestowCurse" && spell.SpellsBundle;
            if (!randomCurse && (spell.EffectDescription == null || spell.EffectDescription.EffectForms == null ||
                spell.EffectDescription.EffectForms.Count == 0))
                spell = PickBundleVariant(spellDb, spell);

            if (spell == null || spell.EffectDescription == null ||
                (!randomCurse && (spell.EffectDescription.EffectForms == null || spell.EffectDescription.EffectForms.Count == 0)))
            {
                Debug.LogError(LogPrefix + "No usable spell forms were found for " + spec.Title + ".");
                return false;
            }

            try
            {
                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null)
                    return false;

                item.ForceName(spec.Id);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                object weaponDescription = ReadMember(item, "WeaponDescription");
                object weaponEffect = ReadMember(weaponDescription, "EffectDescription");
                IList weaponForms = ReadMember(weaponEffect, "EffectForms") as IList;
                if (weaponDescription == null || weaponEffect == null || weaponForms == null)
                {
                    Debug.LogError(LogPrefix + "Could not access the dart's WeaponDescription forms for " + spec.Title + ".");
                    return false;
                }

                weaponForms.Clear();
                if (randomCurse)
                {
                    EffectForm marker = CreateRandomCurseMarker(spell);
                    if (marker == null)
                        return false;
                    weaponForms.Add(marker);
                }
                else
                {
                    foreach (EffectForm form in spell.EffectDescription.EffectForms)
                    {
                        EffectForm copy = EffectForm.GetCopy(form);
                        if (copy == null)
                            return false;
                        weaponForms.Add(copy);
                    }
                }

                CopyMember(spell.EffectDescription, weaponEffect, "HasSavingThrow");
                CopyMember(spell.EffectDescription, weaponEffect, "SavingThrowAbility");
                CopyMember(spell.EffectDescription, weaponEffect, "DifficultyClassComputation");
                CopyMember(spell.EffectDescription, weaponEffect, "SavingThrowDifficultyAbility");
                CopyMember(spell.EffectDescription, weaponEffect, "FixedSavingThrowDifficultyClass");
                CopyMember(spell.EffectDescription, weaponEffect, "DurationType");
                CopyMember(spell.EffectDescription, weaponEffect, "DurationParameter");
                CopyMember(spell.EffectDescription, weaponEffect, "EndOfEffect");
                CopyMember(spell.EffectDescription, weaponEffect, "HasShoveRoll");

                GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
                presentation.Title = spec.Title;
                presentation.Description = spec.Title + ". On a hit, applies " + spec.SpellTitle +
                    " to the target. The dart is consumed on use.";

                if (!SetMember(item, "guiPresentation", presentation) ||
                    !SetMember(item, "magical", true) ||
                    !SetMember(item, "requiresAttunement", false) ||
                    !SetMember(item, "requiresIdentification", false) ||
                    !SetEnumMember(item, "itemRarity", spec.Rarity))
                {
                    Debug.LogError(LogPrefix + "Could not configure " + spec.Title + ".");
                    return false;
                }

                itemDb.Add(item);
                bool registered = itemDb.GetElement(spec.Id, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered " + spec.Title + " with " + weaponForms.Count +
                        " on-hit spell effect form(s).");
                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + spec.Title + ": " + ex);
                return false;
            }
        }

        internal static SpellDefinition PickBundleVariant(Database<SpellDefinition> spellDb, SpellDefinition spell)
        {
            if (spell == null || spell.SubspellsList == null || spell.SubspellsList.Count == 0)
                return spell;

            List<SpellDefinition> usable = new List<SpellDefinition>();
            foreach (SpellDefinition variant in spell.SubspellsList)
                if (variant != null && variant.EffectDescription != null &&
                    variant.EffectDescription.EffectForms != null && variant.EffectDescription.EffectForms.Count > 0)
                    usable.Add(variant);

            if (usable.Count == 0)
                return spell;
            return usable[UnityEngine.Random.Range(0, usable.Count)];
        }

        internal static EffectForm CreateRandomCurseMarker(SpellDefinition bundle)
        {
            if (bundle == null || bundle.SubspellsList == null)
                return null;

            EffectForm template = null;
            foreach (SpellDefinition variant in bundle.SubspellsList)
            {
                if (variant != null && variant.EffectDescription != null &&
                    variant.EffectDescription.EffectForms != null && variant.EffectDescription.EffectForms.Count > 0)
                {
                    template = variant.EffectDescription.EffectForms[0];
                    break;
                }
            }

            EffectForm marker = template == null ? null : EffectForm.GetCopy(template);
            if (marker == null || !SetMember(marker, "hasFilterId", true) ||
                !SetMember(marker, "filterId", RandomCurseMarkerFilterId))
                return null;
            return marker;
        }

        private static void CopyMember(object source, object destination, string name)
        {
            object value = ReadMember(source, name);
            if (value != null)
                SetMember(destination, char.ToLowerInvariant(name[0]) + name.Substring(1), value);
        }

        private static object ReadMember(object target, string name)
        {
            if (target == null) return null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 && property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }
            return null;
        }

        private static bool SetMember(object target, string name, object value)
        {
            if (target == null) return false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    if (field.IsInitOnly) return false;
                    field.SetValue(target, value);
                    return true;
                }
                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 && property.GetSetMethod(true) != null)
                {
                    property.SetValue(target, value, null);
                    return true;
                }
            }
            return false;
        }

        private static bool SetEnumMember(object target, string name, string value)
        {
            Type type = target == null ? null : target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null && field.FieldType.IsEnum)
                    return SetMember(target, name, Enum.Parse(field.FieldType, value, true));
                PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.PropertyType.IsEnum)
                    return SetMember(target, name, Enum.Parse(property.PropertyType, value, true));
            }
            return false;
        }
    }
}
