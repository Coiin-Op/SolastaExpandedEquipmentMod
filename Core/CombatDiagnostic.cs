using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    public static class CombatDiagnostic
    {
        private static bool initialized;
        private static int loggedCalls;

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            try
            {
                Type targetType = AccessTools.TypeByName("RulesetCharacter");

                MethodInfo targetMethod = targetType == null
                    ? null
                    : AccessTools.Method(
                        targetType,
                        "ComputeAttackModifier");

                if (targetMethod == null)
                {
                    Debug.LogError(
                        "[SEE CombatDiagnostic] Could not find " +
                        "RulesetCharacter.ComputeAttackModifier.");

                    return;
                }

                Harmony harmony = new Harmony(
                    "SolastaExpandedEquipmentMod.CombatDiagnostic");

                MethodInfo postfixMethod = typeof(CombatDiagnostic).GetMethod(
                    nameof(AfterComputeAttackModifier),
                    BindingFlags.Static | BindingFlags.NonPublic);

                HarmonyMethod postfix = new HarmonyMethod(postfixMethod);

                harmony.Patch(targetMethod, postfix: postfix);

                initialized = true;

                Debug.Log(
                    "[SEE CombatDiagnostic] Patched " + targetMethod);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[SEE CombatDiagnostic] Initialization failed: " + ex);
            }
        }

        private static void AfterComputeAttackModifier(
            object __instance,
            object[] __args)
        {
            try
            {
                if (loggedCalls >= 5)
                {
                    return;
                }

                loggedCalls++;

                Debug.Log(
                    "[SEE CombatDiagnostic] CALL #" + loggedCalls);

                // The RulesetCharacter instance is the defender.
                LogIdentity("DEFENDER", __instance);

                // Argument zero is the attacker.
                if (__args != null && __args.Length > 0)
                {
                    LogIdentity("ATTACKER", __args[0]);
                }

                // Log the argument types and modifier-related values.
                if (__args != null)
                {
                    for (int i = 0; i < __args.Length; i++)
                    {
                        if (i == 0)
                        {
                            continue;
                        }

                        LogObjectDetails("ARG" + i, __args[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[SEE CombatDiagnostic] Logging failed: " + ex);
            }
        }

        private static void LogIdentity(string label, object value)
        {
            if (value == null)
            {
                Debug.Log(
                    "[SEE CombatDiagnostic] " + label + "=null");

                return;
            }

            Debug.Log(
                "[SEE CombatDiagnostic] " + label +
                " Type=" + value.GetType().FullName);

            LogMembers(value, label, IsIdentityMember);
        }

        private static void LogObjectDetails(string label, object value)
        {
            if (value == null)
            {
                Debug.Log(
                    "[SEE CombatDiagnostic] " + label + "=null");

                return;
            }

            Debug.Log(
                "[SEE CombatDiagnostic] " + label +
                " Type=" + value.GetType().FullName);

            LogMembers(value, label, IsModifierMember);
        }

        private static void LogMembers(
            object value,
            string label,
            Func<string, bool> filter)
        {
            Type type = value.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            for (Type current = type;
                 current != null;
                 current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(flags))
                {
                    if (!filter(field.Name))
                    {
                        continue;
                    }

                    try
                    {
                        object memberValue = field.GetValue(value);

                        LogMember(
                            label + "." + field.Name,
                            field.FieldType,
                            memberValue);
                    }
                    catch
                    {
                        // Ignore inaccessible diagnostic fields.
                    }
                }

                foreach (PropertyInfo property in current.GetProperties(flags))
                {
                    if (!filter(property.Name) ||
                        property.GetIndexParameters().Length != 0 ||
                        property.GetGetMethod(true) == null)
                    {
                        continue;
                    }

                    try
                    {
                        object memberValue = property.GetValue(value, null);

                        LogMember(
                            label + "." + property.Name,
                            property.PropertyType,
                            memberValue);
                    }
                    catch
                    {
                        // Ignore properties that cannot safely be read.
                    }
                }
            }
        }

        private static void LogMember(
            string name,
            Type declaredType,
            object value)
        {
            if (value == null)
            {
                Debug.Log(
                    "[SEE CombatDiagnostic] " + name +
                    "=null DeclaredType=" + declaredType.FullName);

                return;
            }

            Debug.Log(
                "[SEE CombatDiagnostic] " + name +
                "=" + SafeToString(value) +
                " RuntimeType=" + value.GetType().FullName);

            if (!(value is IEnumerable enumerable) ||
                value is string ||
                !IsModifierMember(name))
            {
                return;
            }

            int count = 0;

            foreach (object item in enumerable)
            {
                if (count >= 10)
                {
                    Debug.Log(
                        "[SEE CombatDiagnostic] " + name +
                        ": additional entries omitted");

                    break;
                }

                Debug.Log(
                    "[SEE CombatDiagnostic] " + name +
                    "[" + count + "]=" + SafeToString(item));

                count++;
            }
        }

        private static bool IsIdentityMember(string name)
        {
            return name.IndexOf(
                       "name",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "monster",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "definition",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "family",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "beast",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "race",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "type",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "category",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "creature",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "character",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "guid",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsModifierMember(string name)
        {
            return name.IndexOf(
                       "attack",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "damage",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "modifier",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "bonus",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "trend",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "roll",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(
                       "value",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string SafeToString(object value)
        {
            try
            {
                return value == null ? "null" : value.ToString();
            }
            catch
            {
                return "<ToString failed>";
            }
        }
    }
}
