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
    /// Creature-family shield protection. A creature-family shield reduces the
    /// attacker's attack roll by the shield's tier, but only while the shield is
    /// actively equipped and only when the attacker belongs to the matching family.
    /// </summary>
    public static class ShieldCombatPatch
    {
        private const string LogPrefix = "[SEE Creature Shields] ";
        private static bool initialized;
        private static int matchingCallsLogged;
        private const int MaxMatchingLogs = 30;

        private sealed class AppliedStamp
        {
            public object LastAttackValue;
        }

        private static readonly ConditionalWeakTable<object, AppliedStamp> AppliedModifiers =
            new ConditionalWeakTable<object, AppliedStamp>();

        public static void Initialize()
        {
            if (initialized)
                return;

            try
            {
                Type characterType = AccessTools.TypeByName("RulesetCharacter");
                MethodInfo target = characterType == null
                    ? null
                    : AccessTools.Method(characterType, "ComputeAttackModifier");

                if (target == null)
                {
                    Debug.LogError(LogPrefix +
                        "Could not find RulesetCharacter.ComputeAttackModifier.");
                    return;
                }

                Harmony harmony = new Harmony("SolastaExpandedEquipmentMod.ShieldCombatPatch");
                HarmonyMethod postfix = new HarmonyMethod(
                    typeof(ShieldCombatPatch).GetMethod(
                        nameof(AfterComputeAttackModifier),
                        BindingFlags.Static | BindingFlags.NonPublic));

                harmony.Patch(target, postfix: postfix);
                initialized = true;
                Debug.Log(LogPrefix +
                    "PATCH ACTIVE | Equipped +1/+2/+3 creature-family shields reduce matching attackers' attack rolls by 1/2/3. " +
                    "Uses active shield-slot checks; no UB dependency. Patched " + target);
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
                // ComputeAttackModifier executes on the attacker. args[0] is the
                // defender, and args[2] is the ActionModifier being prepared.
                if (__instance == null || __args == null || __args.Length < 3)
                    return;

                object defender = __args[0];
                object actionModifier = __args[2];
                if (defender == null || actionModifier == null)
                    return;

                string attackerFamily = ReadText(__instance, "CharacterFamily");
                if (!string.Equals(attackerFamily, "Beast", StringComparison.OrdinalIgnoreCase))
                    return;

                int shieldBonus = GetEquippedFamilyShieldBonus(defender, attackerFamily);
                if (shieldBonus <= 0)
                    return;

                object attackBefore = ReadNumericMember(
                    actionModifier, "AttackRollModifier", "attackRollModifier");

                if (attackBefore == null)
                {
                    Debug.LogWarning(LogPrefix +
                        "Matching shield found, but ActionModifier has no readable AttackRollModifier.");
                    return;
                }

                AppliedStamp stamp;
                if (AppliedModifiers.TryGetValue(actionModifier, out stamp) &&
                    ValuesEqual(stamp.LastAttackValue, attackBefore))
                {
                    return;
                }

                bool changed = AddToNumericMember(
                    actionModifier, -shieldBonus,
                    "AttackRollModifier", "attackRollModifier");

                object attackAfter = ReadNumericMember(
                    actionModifier, "AttackRollModifier", "attackRollModifier");

                if (changed)
                {
                    if (AppliedModifiers.TryGetValue(actionModifier, out stamp))
                        stamp.LastAttackValue = attackAfter;
                    else
                        AppliedModifiers.Add(actionModifier,
                            new AppliedStamp { LastAttackValue = attackAfter });

                    LogMatch("AttackerFamily=" + attackerFamily +
                        " | Defender=" + ReadText(defender, "Name") +
                        " | ShieldTier=" + shieldBonus +
                        " | AttackRollModifier=" + attackBefore + " -> " + attackAfter +
                        " | AppliedPenalty=" + (-shieldBonus));
                }
                else
                {
                    Debug.LogWarning(LogPrefix +
                        "Could not apply -" + shieldBonus +
                        " to ActionModifier.AttackRollModifier.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + "Shield combat calculation failed: " + ex);
            }
        }

        private static int GetEquippedFamilyShieldBonus(object defender, string attackerFamily)
        {
            if (defender == null ||
                !string.Equals(attackerFamily, "Beast", StringComparison.OrdinalIgnoreCase))
                return 0;

            object inventory = ReadMember(defender, "CharacterInventory");
            if (inventory == null)
                return 0;

            IDictionary slotsByType = ReadMember(inventory, "InventorySlotsByType") as IDictionary;
            if (slotsByType == null)
                return 0;

            Database<ArmorTypeDefinition> armorTypeDb =
                DatabaseRepository.GetDatabase<ArmorTypeDefinition>();
            if (armorTypeDb == null)
                return 0;

            int highestBonus = 0;

            foreach (DictionaryEntry slotGroup in slotsByType)
            {
                string slotType = slotGroup.Key == null ? string.Empty : slotGroup.Key.ToString();
                IEnumerable slots = slotGroup.Value as IEnumerable;
                if (slots == null)
                    continue;

                foreach (object slot in slots)
                {
                    object equippedItem = ReadMember(slot, "EquipedItem");
                    if (equippedItem == null)
                        continue;

                    object itemDefinition = ReadMember(equippedItem, "ItemDefinition");
                    if (itemDefinition == null)
                        continue;

                    string itemName = ReadText(itemDefinition, "Name");
                    if (string.IsNullOrEmpty(itemName) ||
                        !itemName.StartsWith("ShieldVs", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!ContainsValue(ReadMember(itemDefinition, "SlotsWhereActive"), slotType))
                        continue;

                    if (!ReadBoolean(itemDefinition, "IsArmor"))
                        continue;

                    object armorDescription = ReadMember(itemDefinition, "ArmorDescription");
                    string armorTypeName = ReadText(armorDescription, "ArmorType");
                    if (string.IsNullOrEmpty(armorTypeName))
                        continue;

                    ArmorTypeDefinition armorType = armorTypeDb.GetElement(armorTypeName, true);
                    string category = ReadText(armorType, "ArmorCategory");
                    if (string.IsNullOrEmpty(category) ||
                        (category.IndexOf("Shield", StringComparison.OrdinalIgnoreCase) < 0))
                        continue;

                    int tier = ReadShieldTier(itemName);
                    if (tier > highestBonus)
                        highestBonus = tier;
                }
            }

            return highestBonus;
        }

        private static int ReadShieldTier(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return 0;

            if (itemName.EndsWith("Plus3", StringComparison.OrdinalIgnoreCase))
                return 3;
            if (itemName.EndsWith("Plus2", StringComparison.OrdinalIgnoreCase))
                return 2;
            if (itemName.EndsWith("Plus1", StringComparison.OrdinalIgnoreCase))
                return 1;

            return 0;
        }

        private static void LogMatch(string message)
        {
            if (matchingCallsLogged >= MaxMatchingLogs)
                return;

            matchingCallsLogged++;
            Debug.Log(LogPrefix + "MATCH #" + matchingCallsLogged + " | " + message);
            if (matchingCallsLogged == MaxMatchingLogs)
                Debug.Log(LogPrefix + "Further matching-shield logs suppressed.");
        }

        private static object ReadMember(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name))
                return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(target);

                PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 && property.CanRead)
                    return property.GetValue(target, null);
            }

            return null;
        }

        private static string ReadText(object target, string name)
        {
            object value = ReadMember(target, name);
            return value == null ? string.Empty : value.ToString();
        }

        private static bool ReadBoolean(object target, string name)
        {
            object value = ReadMember(target, name);
            if (value is bool)
                return (bool)value;
            return false;
        }

        private static bool ContainsValue(object collection, string expected)
        {
            if (collection == null || string.IsNullOrEmpty(expected))
                return false;

            IEnumerable values = collection as IEnumerable;
            if (values == null)
                return false;

            foreach (object value in values)
            {
                if (value != null &&
                    string.Equals(value.ToString(), expected, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static object ReadNumericMember(object target, params string[] names)
        {
            if (target == null)
                return null;

            foreach (string name in names)
            {
                object value = ReadMember(target, name);
                if (value is int || value is float)
                    return value;
            }

            return null;
        }

        private static bool AddToNumericMember(
            object target, int amount, params string[] names)
        {
            if (target == null)
                return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                foreach (string name in names)
                {
                    FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                    if (field != null && !field.IsInitOnly)
                    {
                        if (field.FieldType == typeof(int))
                        {
                            field.SetValue(target, (int)field.GetValue(target) + amount);
                            return true;
                        }
                        if (field.FieldType == typeof(float))
                        {
                            field.SetValue(target, (float)field.GetValue(target) + amount);
                            return true;
                        }
                    }

                    PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                    if (property == null || !property.CanRead || !property.CanWrite ||
                        property.GetIndexParameters().Length != 0)
                        continue;

                    if (property.PropertyType == typeof(int))
                    {
                        property.SetValue(target,
                            (int)property.GetValue(target, null) + amount, null);
                        return true;
                    }
                    if (property.PropertyType == typeof(float))
                    {
                        property.SetValue(target,
                            (float)property.GetValue(target, null) + amount, null);
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ValuesEqual(object left, object right)
        {
            if (left == null || right == null)
                return left == right;
            return left.Equals(right);
        }
    }
}
