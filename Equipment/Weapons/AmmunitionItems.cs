using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>Registers spell-effect arrows and crossbow bolts.</summary>
    internal static class AmmunitionItems
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";

        private sealed class AmmoSpec
        {
            internal readonly string Id;
            internal readonly string Title;
            internal readonly string Rarity;
            internal readonly string SpellId;
            internal readonly string SpellTitle;

            internal AmmoSpec(string id, string title, string rarity, string spellId, string spellTitle)
            {
                Id = id;
                Title = title;
                Rarity = rarity;
                SpellId = spellId;
                SpellTitle = spellTitle;
            }
        }

        internal static bool RegisterArrowsAndBolts()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<EffectProxyDefinition> proxyDb = DatabaseRepository.GetDatabase<EffectProxyDefinition>();
            if (itemDb == null || spellDb == null || proxyDb == null)
            {
                Debug.LogWarning(LogPrefix + "Arrow/bolt registration is waiting for item, spell, and effect-proxy databases.");
                return false;
            }

            bool proxyDefinitionsReady =
                RegisterVisibleAmmoProxy(proxyDb, "AmmoProxyDarkness", "ProxyDarkness") &&
                RegisterVisibleAmmoProxy(proxyDb, "AmmoProxyFogCloud", "ProxyFogCloud");

            AmmoSpec[] specs =
            {
                new AmmoSpec("ArrowOfFogCloud", "Arrow of Fog Cloud", "Uncommon", "FogCloud", "Fog Cloud"),
                new AmmoSpec("ArrowOfDarkness", "Arrow of Darkness", "Uncommon", "Darkness", "Darkness"),
                new AmmoSpec("ArrowOfThunder", "Arrow of Thunder", "Uncommon", "Thunderwave", "Thunderwave"),
                new AmmoSpec("ArrowOfCurses", "Arrow of Curses", "Rare", "BestowCurse", "Bestow Curse"),
                new AmmoSpec("ArrowOfSlow", "Arrow of Slow", "Rare", "Slow", "Slow"),
                new AmmoSpec("ArrowOfConfusion", "Arrow of Confusion", "Rare", "Confusion", "Confusion"),
                new AmmoSpec("CrossbowBoltOfFogCloud", "Crossbow Bolt of Fog Cloud", "Uncommon", "FogCloud", "Fog Cloud"),
                new AmmoSpec("CrossbowBoltOfDarkness", "Crossbow Bolt of Darkness", "Uncommon", "Darkness", "Darkness"),
                new AmmoSpec("CrossbowBoltOfThunder", "Crossbow Bolt of Thunder", "Uncommon", "Thunderwave", "Thunderwave"),
                new AmmoSpec("CrossbowBoltOfCurses", "Crossbow Bolt of Curses", "Rare", "BestowCurse", "Bestow Curse"),
                new AmmoSpec("CrossbowBoltOfSlow", "Crossbow Bolt of Slow", "Rare", "Slow", "Slow"),
                new AmmoSpec("CrossbowBoltOfConfusion", "Crossbow Bolt of Confusion", "Rare", "Confusion", "Confusion")
            };

            bool success = proxyDefinitionsReady;
            foreach (AmmoSpec spec in specs)
            {
                string baseId = spec.Id.StartsWith("CrossbowBolt", StringComparison.Ordinal)
                    ? "Bolt" : "Arrow";
                success &= RegisterOne(itemDb, spellDb, baseId, spec);
            }

            Debug.Log(LogPrefix + "Spell ammunition registration completed: " + success + ".");
            return success;
        }

        private static bool RegisterOne(
            Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb,
            string baseItemId,
            AmmoSpec spec)
        {
            ItemDefinition existing = itemDb.GetElement(spec.Id, true);
            if (existing != null)
                return true;

            ItemDefinition template = FindAmmoTemplate(itemDb, baseItemId);
            SpellDefinition spell = spellDb.GetElement(spec.SpellId, true);
            bool randomCurse = spell != null && spec.SpellId == "BestowCurse" && spell.SpellsBundle;
            if (spell != null && !randomCurse && (spell.EffectDescription == null || spell.EffectDescription.EffectForms == null ||
                spell.EffectDescription.EffectForms.Count == 0))
                spell = SpellDartItems.PickBundleVariant(spellDb, spell);

            if (template == null || spell == null || spell.EffectDescription == null)
            {
                Debug.LogError(LogPrefix + "Could not find " + (template == null ? baseItemId : spec.SpellId) +
                    " for " + spec.Title + ".");
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

                GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
                presentation.Title = spec.Title;
                presentation.Description = spec.Title + ". On a hit, applies " + spec.SpellTitle +
                    " to the target. The ammunition is consumed on use.";

                object ammunitionDescription = ReadMember(item, "AmmunitionDescription");
                object ammunitionEffect = ReadMember(ammunitionDescription, "EffectDescription");
                object spellEffect = ReadMember(spell, "EffectDescription");
                IList ammunitionForms = ReadMember(ammunitionEffect, "EffectForms") as IList;
                IList spellForms = ReadMember(spellEffect, "EffectForms") as IList;
                if (ammunitionDescription == null || ammunitionEffect == null || ammunitionForms == null ||
                    (!randomCurse && (spellForms == null || spellForms.Count == 0)))
                {
                    Debug.LogError(LogPrefix + "Could not access spell effect forms for " + spec.Title + ".");
                    return false;
                }

                if (randomCurse)
                {
                    EffectForm marker = SpellDartItems.CreateRandomCurseMarker(spell);
                    if (marker == null)
                    {
                        Debug.LogError(LogPrefix + "Could not create the random-curse marker for " + spec.Title + ".");
                        return false;
                    }
                    ammunitionForms.Add(marker);
                }
                else
                {
                    foreach (object spellForm in spellForms)
                    {
                        object copiedForm = spellForm is EffectForm form ? EffectForm.GetCopy(form) : null;
                        if (copiedForm == null)
                        {
                            Debug.LogError(LogPrefix + "Could not copy a " + spec.SpellTitle + " effect for " + spec.Title + ".");
                            return false;
                        }

                        if (copiedForm is EffectForm copiedEffectForm &&
                            copiedEffectForm.FormType == EffectForm.EffectFormType.Summon &&
                            copiedEffectForm.SummonForm != null &&
                            copiedEffectForm.SummonForm.SummonType == SummonForm.Type.EffectProxy)
                        {
                            string visibleProxyName = GetVisibleAmmoProxyName(spec.SpellId);
                            if (!string.IsNullOrEmpty(visibleProxyName))
                            {
                                bool rebound = SetMember(
                                    copiedEffectForm.SummonForm, "effectProxyDefinitionName", visibleProxyName);
                                Debug.Log(LogPrefix + "Ammo " + spec.Title + " uses proxy " +
                                    copiedEffectForm.SummonForm.EffectProxyDefinitionName +
                                    " (rebound=" + rebound + ").");
                            }
                        }

                        ammunitionForms.Add(copiedForm);
                    }
                }

                // Preserve the arrow/bolt's own range and targeting, but carry over the
                // spell's saving throw and duration rules for the additional hit effects.
                CopyMember(spellEffect, ammunitionEffect, "HasSavingThrow");
                CopyMember(spellEffect, ammunitionEffect, "SavingThrowAbility");
                CopyMember(spellEffect, ammunitionEffect, "DifficultyClassComputation");
                CopyMember(spellEffect, ammunitionEffect, "SavingThrowDifficultyAbility");
                CopyMember(spellEffect, ammunitionEffect, "FixedSavingThrowDifficultyClass");
                CopyMember(spellEffect, ammunitionEffect, "DurationType");
                CopyMember(spellEffect, ammunitionEffect, "DurationParameter");
                CopyMember(spellEffect, ammunitionEffect, "EndOfEffect");
                CopyMember(spellEffect, ammunitionEffect, "HasShoveRoll");
                CopyNestedMember(spellEffect, ammunitionEffect, "EffectParticleParameters");
                CopyMember(spellEffect, ammunitionEffect, "AnimationMagicEffect");

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
                    Debug.Log(LogPrefix + "Registered " + spec.Title + " with " + ammunitionForms.Count +
                        " on-hit spell effect form(s).");
                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + spec.Title + ": " + ex);
                return false;
            }
        }

        private static ItemDefinition FindAmmoTemplate(Database<ItemDefinition> itemDb, string itemId)
        {
            ItemDefinition preferred = itemDb.GetElement(itemId, true);
            if (IsAmmunition(preferred))
                return preferred;

            string marker = itemId.Equals("Arrow", StringComparison.Ordinal) ? "arrow" : "bolt";
            foreach (ItemDefinition candidate in itemDb.GetAllElements())
            {
                if (!IsAmmunition(candidate))
                    continue;

                string name = Normalize(candidate.Name);
                string title = Normalize(candidate.GuiPresentation == null ? null : candidate.GuiPresentation.Title);
                if (name.Equals(marker, StringComparison.Ordinal) || title.Equals(marker, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        private static bool IsAmmunition(ItemDefinition item)
        {
            if (item == null || item.GuiPresentation == null)
                return false;
            return item.IsAmmunition && item.AmmunitionDescription != null;
        }

        private static bool RegisterVisibleAmmoProxy(
            Database<EffectProxyDefinition> proxyDb, string customId, string templateId)
        {
            if (proxyDb.GetElement(customId, true) != null)
                return true;

            EffectProxyDefinition template = proxyDb.GetElement(templateId, true);
            if (template == null || template.GuiPresentation == null)
            {
                Debug.LogError(LogPrefix + "Could not find effect proxy template " + templateId + ".");
                return false;
            }

            try
            {
                EffectProxyDefinition proxy = UnityEngine.Object.Instantiate(template);
                if (proxy == null)
                    return false;

                proxy.ForceName(customId);
                proxy.SetUserContentGUID(Guid.NewGuid().ToString());
                proxy.IsUserContent = true;
                if (!SetMember(proxy, "isEmptyPresentation", false))
                {
                    Debug.LogError(LogPrefix + "Could not enable the visible presentation for " + customId + ".");
                    return false;
                }

                proxyDb.Add(proxy);
                bool registered = proxyDb.GetElement(customId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered visible ammunition proxy " + customId + ".");
                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register visible ammunition proxy " + customId + ": " + ex);
                return false;
            }
        }

        private static string GetVisibleAmmoProxyName(string spellId)
        {
            if (string.Equals(spellId, "Darkness", StringComparison.Ordinal))
                return "AmmoProxyDarkness";
            if (string.Equals(spellId, "FogCloud", StringComparison.Ordinal))
                return "AmmoProxyFogCloud";
            return null;
        }

        private static bool CopyMember(object source, object destination, string memberName)
        {
            object value = ReadMember(source, memberName);
            return value == null || SetMember(destination, char.ToLowerInvariant(memberName[0]) + memberName.Substring(1), value);
        }

        private static bool CopyNestedMember(object source, object destination, string memberName)
        {
            object sourceValue = ReadMember(source, memberName);
            object destinationValue = ReadMember(destination, memberName);
            if (sourceValue == null || destinationValue == null)
                return sourceValue == null;

            MethodInfo copyMethod = destinationValue.GetType().GetMethod(
                "Copy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { sourceValue.GetType() }, null);
            if (copyMethod == null)
                return SetMember(destination,
                    char.ToLowerInvariant(memberName[0]) + memberName.Substring(1), sourceValue);

            copyMethod.Invoke(destinationValue, new[] { sourceValue });
            return true;
        }

        private static object ReadMember(object target, string name)
        {
            if (target == null)
                return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(target);
                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }

            return null;
        }

        private static bool SetMember(object target, string name, object value)
        {
            if (target == null)
                return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    if (field.IsInitOnly)
                        return false;
                    field.SetValue(target, value);
                    return true;
                }
                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetSetMethod(true) != null)
                {
                    property.SetValue(target, value, null);
                    return true;
                }
            }

            return false;
        }

        private static bool SetEnumMember(object target, string name, string value)
        {
            Type memberType = GetMemberType(target == null ? null : target.GetType(), name);
            if (memberType == null || !memberType.IsEnum)
                return false;
            try { return SetMember(target, name, Enum.Parse(memberType, value, true)); }
            catch { return false; }
        }

        private static Type GetMemberType(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.FieldType;
                PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.PropertyType;
            }
            return null;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            List<char> characters = new List<char>(value.Length);
            foreach (char character in value)
                if (char.IsLetterOrDigit(character))
                    characters.Add(char.ToLowerInvariant(character));
            return new string(characters.ToArray());
        }
    }
}
