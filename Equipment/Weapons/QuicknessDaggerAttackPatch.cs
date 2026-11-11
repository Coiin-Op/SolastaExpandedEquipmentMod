using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Gives the Dagger +2 of Quickness one Nick-style extra weapon attack.
    /// This implementation uses only the game's own attack modes and Harmony;
    /// it has no reference to SolastaUnfinishedBusiness.
    ///
    /// If the dagger's attack mode is a Bonus action (off-hand), it is moved to
    /// the Main action. If it is already a Main action (main-hand), its attack
    /// count is increased by one. The previous change is restored immediately
    /// before the next RefreshAttackModes call, so unequipping the dagger removes
    /// the benefit and repeated refreshes cannot stack it.
    /// </summary>
    public static class QuicknessDaggerAttackPatch
    {
        private const string LogPrefix = "[SEE Quickness Dagger] ";
        private const string QuicknessDaggerItemId = "DaggerPlus2OfQuickness";

        private static bool initialized;

        private sealed class AppliedState
        {
            public object AttackMode;
            public int OriginalAttacksNumber;
            public bool HasOriginalAttacksNumber;
            public object OriginalActionType;
            public string ActionTypeMemberName;
            public bool LoggedQuicknessActivation;

            public void Clear()
            {
                AttackMode = null;
                OriginalAttacksNumber = 0;
                HasOriginalAttacksNumber = false;
                OriginalActionType = null;
                ActionTypeMemberName = null;
            }
        }

        private static readonly ConditionalWeakTable<object, AppliedState> AppliedStates =
            new ConditionalWeakTable<object, AppliedState>();

        public static bool Initialize()
        {
            if (initialized)
                return true;

            try
            {
                Type characterType = AccessTools.TypeByName("RulesetCharacter");
                MethodInfo refreshMethod = characterType == null
                    ? null
                    : AccessTools.Method(characterType, "RefreshAttackModes");

                if (refreshMethod == null)
                {
                    Debug.LogError(LogPrefix + "Could not find RulesetCharacter.RefreshAttackModes.");
                    return false;
                }

                MethodInfo prefixMethod = typeof(QuicknessDaggerAttackPatch).GetMethod(
                    nameof(BeforeRefreshAttackModes),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo postfixMethod = typeof(QuicknessDaggerAttackPatch).GetMethod(
                    nameof(AfterRefreshAttackModes),
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (prefixMethod == null || postfixMethod == null)
                {
                    Debug.LogError(LogPrefix + "Could not locate the attack-mode patch callbacks.");
                    return false;
                }

                Harmony harmony = new Harmony("SolastaExpandedEquipmentMod.QuicknessDaggerAttackPatch");
                harmony.Patch(
                    refreshMethod,
                    prefix: new HarmonyMethod(prefixMethod),
                    postfix: new HarmonyMethod(postfixMethod));

                initialized = true;
                Debug.Log(LogPrefix + "PATCH ACTIVE. Quickness Dagger will add one Nick-style attack while equipped. Patched " + refreshMethod + ".");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Initialization failed: " + ex);
                return false;
            }
        }

        private static void BeforeRefreshAttackModes(object __instance)
        {
            if (__instance == null)
                return;

            try
            {
                AppliedState state;
                if (!AppliedStates.TryGetValue(__instance, out state) || state.AttackMode == null)
                    return;

                IList attackModes = GetAttackModes(__instance);
                if (attackModes != null && ContainsReference(attackModes, state.AttackMode))
                {
                    if (state.HasOriginalAttacksNumber)
                    {
                        SetMember(state.AttackMode, "attacksNumber", state.OriginalAttacksNumber);
                    }

                    if (state.OriginalActionType != null &&
                        !string.IsNullOrEmpty(state.ActionTypeMemberName))
                    {
                        SetMember(state.AttackMode, state.ActionTypeMemberName, state.OriginalActionType);
                    }
                }

                state.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + "Could not restore the previous attack-mode adjustment: " + ex.Message);
            }
        }

        private static void AfterRefreshAttackModes(object __instance)
        {
            if (__instance == null)
                return;

            try
            {
                IList attackModes = GetAttackModes(__instance);
                if (attackModes == null || attackModes.Count == 0)
                    return;

                object offhandMode = null;
                object mainHandMode = null;

                for (int i = 0; i < attackModes.Count; i++)
                {
                    object attackMode = attackModes[i];
                    if (!IsQuicknessDaggerAttackMode(attackMode))
                        continue;

                    object actionType = ReadMember(attackMode, "ActionType", "actionType");
                    string actionTypeName = actionType == null ? string.Empty : actionType.ToString();

                    // Prefer the off-hand mode so it can be moved from Bonus to Main,
                    // which is the central Nick-style behavior.
                    if (actionTypeName.IndexOf("Bonus", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        offhandMode = attackMode;
                        break;
                    }

                    if (mainHandMode == null &&
                        actionTypeName.IndexOf("Main", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mainHandMode = attackMode;
                    }
                }

                if (offhandMode != null)
                {
                    ApplyNickStyleActionShift(__instance, offhandMode);
                    return;
                }

                if (mainHandMode != null)
                {
                    ApplyMainHandExtraAttack(__instance, mainHandMode);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + "Could not add the Quickness attack: " + ex);
            }
        }

        private static void ApplyNickStyleActionShift(object character, object attackMode)
        {
            string actionTypeMemberName;
            object originalActionType;
            if (!TryGetMember(attackMode, "actionType", "ActionType", out actionTypeMemberName, out originalActionType))
            {
                Debug.LogWarning(LogPrefix + "The off-hand Quickness dagger mode has no readable ActionType.");
                return;
            }

            if (!TrySetEnumMember(attackMode, actionTypeMemberName, "Main"))
            {
                Debug.LogWarning(LogPrefix + "Could not move the off-hand Quickness dagger attack into the Main action.");
                return;
            }

            AppliedState state = GetState(character);
            state.AttackMode = attackMode;
            state.OriginalActionType = originalActionType;
            state.ActionTypeMemberName = actionTypeMemberName;

            if (!state.LoggedQuicknessActivation)
            {
                state.LoggedQuicknessActivation = true;
                Debug.Log(LogPrefix + "Nick-style attack enabled by moving the equipped Quickness Dagger's off-hand mode from Bonus to Main.");
            }
        }

        private static void ApplyMainHandExtraAttack(object character, object attackMode)
        {
            object originalValue = ReadMember(attackMode, "attacksNumber");
            if (originalValue == null)
            {
                Debug.LogWarning(LogPrefix + "The main-hand Quickness dagger mode has no attacksNumber field.");
                return;
            }

            int originalAttacks;
            try
            {
                originalAttacks = Convert.ToInt32(originalValue);
            }
            catch
            {
                Debug.LogWarning(LogPrefix + "Could not read the main-hand attack count.");
                return;
            }

            if (!SetMember(attackMode, "attacksNumber", originalAttacks + 1))
            {
                Debug.LogWarning(LogPrefix + "Could not increase the main-hand Quickness dagger attack count.");
                return;
            }

            AppliedState state = GetState(character);
            state.AttackMode = attackMode;
            state.OriginalAttacksNumber = originalAttacks;
            state.HasOriginalAttacksNumber = true;

            if (!state.LoggedQuicknessActivation)
            {
                state.LoggedQuicknessActivation = true;
                Debug.Log(LogPrefix + "One additional Main-action attack enabled by the equipped Quickness Dagger. Attack count " + originalAttacks + " -> " + (originalAttacks + 1) + ".");
            }
        }

        private static AppliedState GetState(object character)
        {
            AppliedState state;
            if (!AppliedStates.TryGetValue(character, out state))
            {
                state = new AppliedState();
                AppliedStates.Add(character, state);
            }
            return state;
        }

        private static IList GetAttackModes(object character)
        {
            object modes = ReadMember(character, "AttackModes", "attackModes");
            return modes as IList;
        }

        private static bool IsQuicknessDaggerAttackMode(object attackMode)
        {
            if (attackMode == null)
                return false;

            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition",
                "sourceDefinition", "SourceDefinition",
                "itemDefinition", "ItemDefinition",
                "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                object value = ReadMember(attackMode, memberName);
                if (ContainsQuicknessItemId(value, 0))
                    return true;
            }

            // Some game versions place the item definition one level deeper.
            foreach (MemberInfo member in GetMembers(attackMode.GetType()))
            {
                if (member.Name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) < 0 &&
                    member.Name.IndexOf("item", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                object value = GetMemberValue(attackMode, member);
                if (ContainsQuicknessItemId(value, 0))
                    return true;
            }

            return false;
        }

        private static bool ContainsQuicknessItemId(object value, int depth)
        {
            if (value == null || depth > 2)
                return false;

            string name = ReadMember(value, "Name") as string;
            if (!string.IsNullOrEmpty(name) &&
                name.IndexOf(QuicknessDaggerItemId, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            string text = value as string;
            if (string.IsNullOrEmpty(text))
                text = value.ToString();
            if (!string.IsNullOrEmpty(text) &&
                text.IndexOf(QuicknessDaggerItemId, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (depth == 2)
                return false;

            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition",
                "itemDefinition", "ItemDefinition",
                "definition", "Definition", "item", "Item"
            })
            {
                object nested = ReadMember(value, memberName);
                if (nested != null && !ReferenceEquals(nested, value) &&
                    ContainsQuicknessItemId(nested, depth + 1))
                    return true;
            }

            return false;
        }

        private static bool ContainsReference(IList list, object value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], value))
                    return true;
            }
            return false;
        }

        private static object ReadMember(object target, params string[] memberNames)
        {
            if (target == null || memberNames == null)
                return null;

            foreach (string memberName in memberNames)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                for (Type current = target.GetType(); current != null; current = current.BaseType)
                {
                    FieldInfo field = current.GetField(memberName, flags | BindingFlags.DeclaredOnly);
                    if (field != null)
                        return field.GetValue(target);

                    PropertyInfo property = current.GetProperty(memberName, flags | BindingFlags.DeclaredOnly);
                    if (property != null && property.GetIndexParameters().Length == 0 &&
                        property.GetGetMethod(true) != null)
                        return property.GetValue(target, null);
                }
            }

            return null;
        }

        private static bool SetMember(object target, string memberName, object value)
        {
            if (target == null || string.IsNullOrEmpty(memberName))
                return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(memberName, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    if (field.IsInitOnly)
                        return false;
                    field.SetValue(target, value);
                    return true;
                }

                PropertyInfo property = current.GetProperty(memberName, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetSetMethod(true) != null)
                {
                    property.SetValue(target, value, null);
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetMember(
            object target,
            string firstName,
            string secondName,
            out string memberName,
            out object value)
        {
            memberName = null;
            value = null;

            if (target == null)
                return false;

            object candidate = ReadMember(target, firstName);
            if (candidate != null)
            {
                memberName = firstName;
                value = candidate;
                return true;
            }

            candidate = ReadMember(target, secondName);
            if (candidate != null)
            {
                memberName = secondName;
                value = candidate;
                return true;
            }

            return false;
        }

        private static bool TrySetEnumMember(object target, string memberName, string enumName)
        {
            if (target == null || string.IsNullOrEmpty(memberName))
                return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(memberName, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    if (field.IsInitOnly)
                        return false;
                    if (field.FieldType.IsEnum)
                    {
                        field.SetValue(target, Enum.Parse(field.FieldType, enumName));
                        return true;
                    }
                    if (field.FieldType == typeof(string))
                    {
                        field.SetValue(target, enumName);
                        return true;
                    }
                }

                PropertyInfo property = current.GetProperty(memberName, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetSetMethod(true) != null)
                {
                    if (property.PropertyType.IsEnum)
                    {
                        property.SetValue(target, Enum.Parse(property.PropertyType, enumName), null);
                        return true;
                    }
                    if (property.PropertyType == typeof(string))
                    {
                        property.SetValue(target, enumName, null);
                        return true;
                    }
                }
            }

            return false;
        }

        private static MemberInfo[] GetMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            List<MemberInfo> members = new List<MemberInfo>();
            for (Type current = type; current != null; current = current.BaseType)
            {
                members.AddRange(current.GetFields(flags | BindingFlags.DeclaredOnly));
                members.AddRange(current.GetProperties(flags | BindingFlags.DeclaredOnly));
            }
            return members.ToArray();
        }

        private static object GetMemberValue(object target, MemberInfo member)
        {
            if (target == null || member == null)
                return null;

            FieldInfo field = member as FieldInfo;
            if (field != null)
                return field.GetValue(target);

            PropertyInfo property = member as PropertyInfo;
            if (property != null && property.GetIndexParameters().Length == 0 &&
                property.GetGetMethod(true) != null)
                return property.GetValue(target, null);

            return null;
        }
    }
}
