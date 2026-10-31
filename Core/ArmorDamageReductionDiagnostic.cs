
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    internal static class ArmorDamageReductionDiagnostic
    {
        private const string LogPrefix = "[SEE ArmorReductionDiag] ";

        private static bool initialized;
        private static Harmony harmony;

        private static readonly HashSet<MethodBase> PatchedMethods =
            new HashSet<MethodBase>();

        internal static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            Debug.Log(LogPrefix +
                "STARTING FOCUSED DAMAGE REDUCTION DIAGNOSTIC");

            try
            {
                harmony = new Harmony(
                    "SolastaExpandedEquipmentMod.ArmorDamageReductionDiagnostic");

                PatchEquipmentMethod();
                PatchKnownDamageMethods();

                Debug.Log(LogPrefix +
                    "INITIALIZATION COMPLETE | TotalMethodsPatched=" +
                    PatchedMethods.Count);
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix +
                    "INITIALIZATION ERROR: " + ex);
            }
        }

        private static void PatchEquipmentMethod()
        {
            try
            {
                MethodInfo target =
                    AccessTools.Method(typeof(RulesetInventory), "OnItemEquiped");

                if (target == null)
                {
                    Debug.LogError(LogPrefix +
                        "EQUIPMENT PATCH FAILED: OnItemEquiped not found.");
                    return;
                }

                MethodInfo postfix =
                    AccessTools.Method(
                        typeof(ArmorDamageReductionDiagnostic),
                        nameof(EquipmentPostfix));

                harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(postfix));

                PatchedMethods.Add(target);

                Debug.Log(LogPrefix +
                    "EQUIPMENT PATCH ACTIVE | " + target);
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix +
                    "EQUIPMENT PATCH ERROR: " + ex);
            }
        }

        private static void EquipmentPostfix(
            RulesetInventory __instance,
            RulesetInventorySlot inventorySlot,
            RulesetItem equipedItem)
        {
            try
            {
                if (equipedItem == null ||
                    equipedItem.ItemDefinition == null)
                    return;

                ItemDefinition item = equipedItem.ItemDefinition;
                string itemId = item.Name;

                if (string.IsNullOrEmpty(itemId) ||
                    !IsProtectionArmor(itemId))
                    return;

                Debug.Log(LogPrefix +
                    "ARMOR EQUIPPED | Item=" + itemId +
                    " | Title=" +
                    (item.GuiPresentation == null
                        ? "<no title>"
                        : item.GuiPresentation.Title) +
                    " | Slot=" +
                    (inventorySlot == null
                        ? "<unknown>"
                        : inventorySlot.Name) +
                    " | BearerGuid=" + ReadBearerGuid(__instance));

                List<ItemPropertyDescription> properties =
                    item.StaticProperties;

                if (properties == null || properties.Count == 0)
                {
                    Debug.LogWarning(LogPrefix +
                        "ARMOR HAS NO STATIC PROPERTIES.");
                    return;
                }

                int found = 0;

                foreach (ItemPropertyDescription property in properties)
                {
                    if (property == null)
                        continue;

                    FeatureDefinitionDamageAffinity affinity =
                        property.FeatureDefinition as
                            FeatureDefinitionDamageAffinity;

                    if (affinity == null ||
                        affinity.FlatDamageReduction <= 0)
                        continue;

                    found++;

                    Debug.Log(LogPrefix +
                        "ARMOR REDUCTION FEATURE" +
                        " | Feature=" + affinity.Name +
                        " | DamageType=" + affinity.DamageType +
                        " | Reduction=" + affinity.FlatDamageReduction +
                        " | Affinity=" + affinity.DamageAffinityType +
                        " | FirstDamageFormOnly=" +
                        affinity.FlatDamageReductionOnlyAppliesToFirstDamageForm);
                }

                Debug.Log(LogPrefix +
                    "ARMOR FEATURE SCAN COMPLETE | FeaturesFound=" + found);
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix +
                    "EQUIPMENT DIAGNOSTIC ERROR: " + ex);
            }
        }

        private static void PatchKnownDamageMethods()
        {
            PatchMethodOnType(
                typeof(RulesetActor),
                "ModulateSustainedDamage");

            PatchMethodOnType(
                typeof(RulesetImplementationManager),
                "ModulateSustainedDamage");
        }

        private static void PatchMethodOnType(
            Type targetType,
            string methodName)
        {
            if (targetType == null)
            {
                Debug.LogError(LogPrefix +
                    "DAMAGE PATCH FAILED: target type was null for " +
                    methodName);
                return;
            }

            MethodInfo[] methods = targetType.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            int found = 0;

            foreach (MethodInfo target in methods)
            {
                if (target == null || target.Name != methodName)
                    continue;

                found++;

                Debug.Log(LogPrefix +
                    "DAMAGE METHOD FOUND | " +
                    target.DeclaringType.FullName + "." +
                    target.Name +
                    " | Signature=" + target);

                if (target.IsAbstract ||
                    target.DeclaringType.IsInterface)
                {
                    Debug.LogWarning(LogPrefix +
                        "DAMAGE PATCH SKIPPED: abstract/interface method | " +
                        target);
                    continue;
                }

                try
                {
                    PatchDamageMethod(target);
                }
                catch (Exception ex)
                {
                    Debug.LogError(LogPrefix +
                        "DAMAGE PATCH FAILED | Method=" +
                        target + " | Error=" + ex);
                }
            }

            if (found == 0)
            {
                Debug.LogError(LogPrefix +
                    "DAMAGE PATCH FAILED: " +
                    targetType.FullName + "." +
                    methodName + " was not found.");
            }
        }

        private static void PatchDamageMethod(MethodInfo target)
        {
            if (PatchedMethods.Contains(target))
                return;

            MethodInfo prefix =
                AccessTools.Method(
                    typeof(ArmorDamageReductionDiagnostic),
                    nameof(DamagePrefix));

            MethodInfo postfix;

            if (target.DeclaringType ==
                typeof(RulesetImplementationManager))
            {
                postfix =
                    AccessTools.Method(
                        typeof(ArmorDamageReductionDiagnostic),
                        nameof(DamageManagerPostfix));
            }
            else
            {
                postfix =
                    AccessTools.Method(
                        typeof(ArmorDamageReductionDiagnostic),
                        nameof(DamagePostfix));
            }

            harmony.Patch(
                target,
                prefix: new HarmonyMethod(prefix),
                postfix: new HarmonyMethod(postfix));

            PatchedMethods.Add(target);

            Debug.Log(LogPrefix +
                "DAMAGE PATCH ACTIVE | " + target);
        }

        private static void DamagePrefix(
            MethodBase __originalMethod,
            object[] __args)
        {
            try
            {
                LogDamageCall(
                    "DAMAGE ENTERING",
                    __originalMethod,
                    __args);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix +
                    "DAMAGE PREFIX LOGGING ERROR: " + ex);
            }
        }

        private static void DamagePostfix(
            MethodBase __originalMethod,
            object[] __args,
            object __result)
        {
            try
            {
                LogDamageCall(
                    "DAMAGE AFTER CALCULATION",
                    __originalMethod,
                    __args);

                Debug.Log(LogPrefix +
                    "DAMAGE METHOD RETURNED | Method=" +
                    __originalMethod.DeclaringType.FullName +
                    "." + __originalMethod.Name +
                    " | Result=" + FormatValue(__result));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix +
                    "DAMAGE POSTFIX LOGGING ERROR: " + ex);
            }
        }

        // This postfix reads the actual by-reference output parameter.
        private static void DamageManagerPostfix(
            IDamageAffinityProvider __0,
            string __1,
            RulesetActor __2,
            string __3,
            bool __6,
            ref int __7,
            float __result)
        {
            try
            {
                FeatureDefinition feature =
                    __0 as FeatureDefinition;

                Debug.Log(LogPrefix +
                    "MANAGER REDUCTION RESULT" +
                    " | Feature=" +
                    (feature == null ? "<unknown>" : feature.Name) +
                    " | Actor=" +
                    (__2 == null ? "<null>" : __2.GetType().Name) +
                    " | ProviderDamageType=" + __1 +
                    " | IncomingDamageType=" + __3 +
                    " | TypeMatches=" + (__1 == __3) +
                    " | Affinity=" + __0.DamageAffinityType +
                    " | ConfiguredReduction=" + __0.FlatDamageReduction +
                    " | FirstDamageFormOnly=" +
                    __0.FlatDamageReductionOnlyAppliesToFirstDamageForm +
                    " | WasFirstDamage=" + __6 +
                    " | ReturnedMultiplier=" + __result +
                    " | ACTUAL_OUT_DAMAGE_REDUCTION=" + __7);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix +
                    "MANAGER REDUCTION POSTFIX ERROR: " + ex);
            }
        }

        private static void LogDamageCall(
            string stage,
            MethodBase method,
            object[] args)
        {
            ParameterInfo[] parameters = method.GetParameters();

            Debug.Log(LogPrefix +
                stage + " | Method=" +
                method.DeclaringType.FullName + "." +
                method.Name);

            for (int i = 0; i < parameters.Length; i++)
            {
                object value =
                    args != null && i < args.Length
                        ? args[i]
                        : null;

                Debug.Log(LogPrefix +
                    "  ARGUMENT | Name=" +
                    parameters[i].Name +
                    " | Type=" +
                    parameters[i].ParameterType.Name +
                    " | Value=" + FormatValue(value));
            }
        }

        private static bool IsProtectionArmor(string itemId)
        {
            return
                itemId.IndexOf(
                    "OfPiercingProtection",
                    StringComparison.Ordinal) >= 0 ||

                itemId.IndexOf(
                    "OfSlashingProtection",
                    StringComparison.Ordinal) >= 0 ||

                itemId.IndexOf(
                    "OfBludgeoningProtection",
                    StringComparison.Ordinal) >= 0;
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "<null>";

            if (value is string ||
                value.GetType().IsPrimitive ||
                value.GetType().IsEnum ||
                value is decimal)
            {
                return Convert.ToString(value);
            }

            return "<" + value.GetType().FullName + ">";
        }

        private static string ReadBearerGuid(object inventory)
        {
            if (inventory == null)
                return "<null>";

            try
            {
                FieldInfo field =
                    AccessTools.Field(
                        inventory.GetType(),
                        "bearerGuid");

                if (field != null)
                    return Convert.ToString(field.GetValue(inventory));

                PropertyInfo property =
                    AccessTools.Property(
                        inventory.GetType(),
                        "BearerGuid");

                if (property != null)
                    return Convert.ToString(
                        property.GetValue(inventory, null));
            }
            catch (Exception ex)
            {
                return "<read failed: " + ex.Message + ">";
            }

            return "<member not found>";
        }
    }
}
