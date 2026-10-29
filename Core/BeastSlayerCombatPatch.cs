using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Adds a conditional +1 to attack and damage against the matching creature family.
    /// Combined with the weapon's existing +1 feature, this gives +2 total against
    /// the matching family and +1 total against other targets.
    /// </summary>
    public static class BeastSlayerCombatPatch
    {
        private const string LogPrefix = "[SEE SlayerWeapons] ";
        private static bool initialized;
        private static int matchingCallsLogged;
        private const int MaxMatchingLogs = 20;

        // If the game calls this postfix again on the same ActionModifier without
        // rebuilding its values, do not add the conditional bonuses twice.
        private sealed class AppliedStamp
        {
            public object LastAttackValue;
            public object LastDamageValue;
        }

        private static readonly ConditionalWeakTable<object, AppliedStamp> AppliedModifiers =
            new ConditionalWeakTable<object, AppliedStamp>();

        public static void Initialize()
        {
            if (initialized) return;

            try
            {
                Type characterType = AccessTools.TypeByName("RulesetCharacter");
                MethodInfo target = characterType == null
                    ? null
                    : AccessTools.Method(characterType, "ComputeAttackModifier");

                if (target == null)
                {
                    Debug.LogError(LogPrefix + "Could not find RulesetCharacter.ComputeAttackModifier.");
                    return;
                }

                var harmony = new Harmony("SolastaExpandedEquipmentMod.BeastSlayerCombatPatch");
                var postfix = new HarmonyMethod(
                    typeof(BeastSlayerCombatPatch).GetMethod(
                        nameof(AfterComputeAttackModifier),
                        BindingFlags.Static | BindingFlags.NonPublic));

                harmony.Patch(target, postfix: postfix);
                initialized = true;
                Debug.Log(LogPrefix + "PATCH ACTIVE - matching Slayer weapons get +1 additional attack and damage bonus against their target families ( +2 total with the base +1 feature). Patched " + target);
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Patch initialization failed: " + ex);
            }
        }

        private static void AfterComputeAttackModifier(object __instance, object[] __args)
        {
            try
            {
                // ComputeAttackModifier is called on the ATTACKING character.
                // For this game's signature, args[0] is the defender/target,
                // args[1] is RulesetAttackMode, and args[2] is ActionModifier.
                if (__args == null || __args.Length < 3) return;

                object defender = __args[0];
                object attackMode = __args[1];
                object actionModifier = __args[2];
                if (defender == null || attackMode == null || actionModifier == null) return;

                string family = ReadMember(defender, "CharacterFamily");
                bool isBeastTarget =
                    string.Equals(family, "Beast", StringComparison.OrdinalIgnoreCase);
                bool isFeyTarget =
                    string.Equals(family, "Fey", StringComparison.OrdinalIgnoreCase);
                bool isMagicalTarget = IsMagicalCreatureFamily(family);

                // Match weapon and target families as alternatives, not cumulative
                // requirements. For example, Fey is also in the magical-family set.
                bool matchedSlayerRule =
                    (isBeastTarget && IsBeastSlayerAttack(attackMode)) ||
                    (isFeyTarget && IsFeySlayerAttack(attackMode)) ||
                    (isMagicalTarget && IsMagicalSlayerAttack(attackMode));

                if (!matchedSlayerRule) return;

                object attackBefore = ReadNumericMember(actionModifier,
                    new[] { "AttackRollModifier", "attackRollModifier" });
                object damageBefore = ReadNumericMember(actionModifier,
                    new[] { "DamageRollModifier", "damageRollModifier" });
                int modifierId = RuntimeHelpers.GetHashCode(actionModifier);

                AppliedStamp stamp;
                if (AppliedModifiers.TryGetValue(actionModifier, out stamp) &&
                    ValuesEqual(stamp.LastAttackValue, attackBefore) &&
                    ValuesEqual(stamp.LastDamageValue, damageBefore))
                {
                    LogMatchingCall("Duplicate calculation seen; conditional bonuses not added twice. " +
                        "Attacker=" + DescribeCharacter(__instance) +
                        "; Defender=" + DescribeCharacter(defender) +
                        "; ActionModifierId=" + modifierId +
                        "; AttackRollModifier=" + DisplayValue(attackBefore) +
                        "; DamageRollModifier=" + DisplayValue(damageBefore));
                    return;
                }

                // The weapon already provides its base +1. Add only +1 here so
                // the total becomes +2 against the matching creature family.
                bool attackChanged = AddToNumericMember(actionModifier,
                    new[] { "AttackRollModifier", "attackRollModifier" }, 1);
                bool damageChanged = AddToNumericMember(actionModifier,
                    new[] { "DamageRollModifier", "damageRollModifier" }, 1);

                object attackAfter = ReadNumericMember(actionModifier,
                    new[] { "AttackRollModifier", "attackRollModifier" });
                object damageAfter = ReadNumericMember(actionModifier,
                    new[] { "DamageRollModifier", "damageRollModifier" });

                if (attackChanged || damageChanged)
                {
                    if (AppliedModifiers.TryGetValue(actionModifier, out stamp))
                    {
                        stamp.LastAttackValue = attackAfter;
                        stamp.LastDamageValue = damageAfter;
                    }
                    else
                    {
                        AppliedModifiers.Add(actionModifier, new AppliedStamp
                        {
                            LastAttackValue = attackAfter,
                            LastDamageValue = damageAfter
                        });
                    }
                }

                LogMatchingCall("Conditional +1 attack and +1 damage against " + family + ". " +
                    "Attacker=" + DescribeCharacter(__instance) +
                    "; Defender=" + DescribeCharacter(defender) +
                    "; AttackMode=" + DescribeAttackMode(attackMode) +
                    "; ActionModifierId=" + modifierId +
                    "; AttackBefore=" + DisplayValue(attackBefore) +
                    "; AttackAfter=" + DisplayValue(attackAfter) +
                    "; DamageBefore=" + DisplayValue(damageBefore) +
                    "; DamageAfter=" + DisplayValue(damageAfter) +
                    "; AttackModifierUpdated=" + attackChanged +
                    "; DamageModifierUpdated=" + damageChanged);

                if (!attackChanged)
                    Debug.LogWarning(LogPrefix + "Could not find a writable numeric AttackRollModifier on ActionModifier.");
                if (!damageChanged)
                    Debug.LogWarning(LogPrefix + "Could not find a writable numeric DamageRollModifier on ActionModifier.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + "Conditional attack bonus failed: " + ex);
            }
        }

        private static void LogMatchingCall(string message)
        {
            if (matchingCallsLogged < MaxMatchingLogs)
            {
                matchingCallsLogged++;
                Debug.Log(LogPrefix + "MATCH #" + matchingCallsLogged + "; " + message);
                if (matchingCallsLogged == MaxMatchingLogs)
                    Debug.Log(LogPrefix + "Further matching-call logs suppressed.");
            }
        }

        private static bool IsBeastSlayerAttack(object attackMode)
        {
            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition",
                "sourceDefinition", "SourceDefinition",
                "itemDefinition", "ItemDefinition",
                "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                object value = ReadMemberObject(attackMode, memberName);
                if (value == null) continue;

                string name = ReadMember(value, "Name");
                if (IsBeastSlayerWeaponName(name))
                    return true;

                if (IsBeastSlayerWeaponName(SafeToString(value)))
                    return true;
            }

            // Some game versions nest the weapon definition under another member.
            foreach (MemberInfo member in GetMembers(attackMode.GetType()))
            {
                if (member.Name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) < 0 &&
                    member.Name.IndexOf("item", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                object value = GetValue(attackMode, member);
                if (value == null) continue;

                if (IsBeastSlayerWeaponName(ReadMember(value, "Name")))
                    return true;
            }

            return false;
        }

        private static bool IsBeastSlayerWeaponName(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            return value.IndexOf("BeastSlayerLongsword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("BeastSlayerShortsword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("BeastSlayerGreatsword", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsFeySlayerAttack(object attackMode)
        {
            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition",
                "sourceDefinition", "SourceDefinition",
                "itemDefinition", "ItemDefinition",
                "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                object value = ReadMemberObject(attackMode, memberName);
                if (value == null) continue;

                string name = ReadMember(value, "Name");
                if (IsFeySlayerWeaponName(name))
                    return true;

                if (IsFeySlayerWeaponName(SafeToString(value)))
                    return true;
            }

            // Some game versions nest the weapon definition under another member.
            foreach (MemberInfo member in GetMembers(attackMode.GetType()))
            {
                if (member.Name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) < 0 &&
                    member.Name.IndexOf("item", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                object value = GetValue(attackMode, member);
                if (value == null) continue;

                if (IsFeySlayerWeaponName(ReadMember(value, "Name")))
                    return true;
            }

            return false;
        }

        private static bool IsFeySlayerWeaponName(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            return value.IndexOf("FeySlayerShortsword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("FeySlayerDagger", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMagicalSlayerAttack(object attackMode)
        {
            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition",
                "sourceDefinition", "SourceDefinition",
                "itemDefinition", "ItemDefinition",
                "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                object value = ReadMemberObject(attackMode, memberName);
                if (value == null) continue;

                if (IsMagicalSlayerWeaponName(ReadMember(value, "Name")) ||
                    IsMagicalSlayerWeaponName(SafeToString(value)))
                    return true;
            }

            // Some game versions nest the weapon definition under another member.
            foreach (MemberInfo member in GetMembers(attackMode.GetType()))
            {
                if (member.Name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) < 0 &&
                    member.Name.IndexOf("item", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                object value = GetValue(attackMode, member);
                if (value != null && IsMagicalSlayerWeaponName(ReadMember(value, "Name")))
                    return true;
            }

            return false;
        }

        private static bool IsMagicalSlayerWeaponName(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf("MagicalSlayerBow", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMagicalCreatureFamily(string family)
        {
            if (string.IsNullOrEmpty(family)) return false;

            // Solasta's ordinary bestiary uses creature families such as
            // Celestial, Construct, Dragon, Elemental, Fey, Fiend,
            // Monstrosity, Plant and Undead rather than one universal "Magical" family.
            // Accept the literal family too, in case another mod defines it.
            string[] magicalFamilies =
            {
                "Magical",
                "Celestial",
                "Construct",
                "Dragon",
                "Elemental",
                "Fey",
                "Fiend",
                "Monstrosity",
                "Plant",
                "Undead"
            };

            foreach (string magicalFamily in magicalFamilies)
            {
                if (string.Equals(family, magicalFamily, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool AddToNumericMember(object target, string[] names, int amount)
        {
            if (target == null) return false;
            Type type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (string name in names)
            {
                for (Type current = type; current != null; current = current.BaseType)
                {
                    FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                    if (field != null && !field.IsInitOnly &&
                        (field.FieldType == typeof(int) || field.FieldType == typeof(float)))
                    {
                        if (field.FieldType == typeof(int))
                            field.SetValue(target, (int)field.GetValue(target) + amount);
                        else
                            field.SetValue(target, (float)field.GetValue(target) + amount);
                        return true;
                    }

                    PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                    if (property == null || !property.CanRead || !property.CanWrite ||
                        property.GetIndexParameters().Length != 0) continue;

                    if (property.PropertyType == typeof(int))
                    {
                        property.SetValue(target, (int)property.GetValue(target, null) + amount, null);
                        return true;
                    }
                    if (property.PropertyType == typeof(float))
                    {
                        property.SetValue(target, (float)property.GetValue(target, null) + amount, null);
                        return true;
                    }
                }
            }
            return false;
        }

        private static object ReadNumericMember(object target, string[] names)
        {
            if (target == null) return null;
            foreach (string name in names)
            {
                object value = ReadMemberObject(target, name);
                if (value is int || value is float) return value;
            }
            return null;
        }

        private static bool ValuesEqual(object left, object right)
        {
            if (left == null || right == null) return left == right;
            return left.Equals(right);
        }

        private static string DisplayValue(object value)
        {
            return value == null ? "<not found>" : SafeToString(value);
        }

        private static string DescribeCharacter(object value)
        {
            if (value == null) return "<null>";
            return SafeReadMember(value, "Name") + "/" + SafeReadMember(value, "SystemName") +
                "/Family=" + SafeReadMember(value, "CharacterFamily");
        }

        private static string DescribeAttackMode(object attackMode)
        {
            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition", "sourceDefinition", "SourceDefinition",
                "itemDefinition", "ItemDefinition", "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                object value = ReadMemberObject(attackMode, memberName);
                if (value != null)
                    return memberName + "=" + value.GetType().Name + "(" + SafeReadMember(value, "Name") + ")";
            }
            return attackMode == null ? "<null>" : attackMode.GetType().Name;
        }

        private static string SafeReadMember(object target, string name)
        {
            try
            {
                object value = ReadMemberObject(target, name);
                return value == null ? "<null>" : SafeToString(value);
            }
            catch { return "<read failed>"; }
        }

        private static object ReadMemberObject(object target, string name)
        {
            if (target == null) return null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
                PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }
            return null;
        }

        private static string ReadMember(object target, string name)
        {
            object value = ReadMemberObject(target, name);
            return value == null ? null : SafeToString(value);
        }

        private static MemberInfo[] GetMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var all = new List<MemberInfo>();
            for (Type current = type; current != null; current = current.BaseType)
            {
                all.AddRange(current.GetFields(flags));
                all.AddRange(current.GetProperties(flags));
            }
            return all.ToArray();
        }

        private static object GetValue(object target, MemberInfo member)
        {
            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null) return field.GetValue(target);
                PropertyInfo property = member as PropertyInfo;
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }
            catch { }
            return null;
        }

        private static string SafeToString(object value)
        {
            try { return value == null ? "<null>" : value.ToString(); }
            catch { return "<ToString failed>"; }
        }
    }
}
