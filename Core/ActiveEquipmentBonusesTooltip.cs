using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Adds a live summary of the character's currently active feature bonuses
    /// to the character portrait hover tooltip.
    ///
    /// The summary is built from RulesetCharacter.EnumerateFeaturesToBrowse,
    /// so it reports active game features rather than item descriptions.
    /// </summary>
    internal static class ActiveEquipmentBonusesTooltip
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";
        private const string SectionHeader = "Active Bonuses & Defenses";
        private static bool initialized;
        private static int loggedErrors;

        internal static void Initialize()
        {
            if (initialized)
                return;

            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(TooltipFeatureCharacterDescription), "Bind");

                MethodInfo postfix = AccessTools.Method(
                    typeof(ActiveEquipmentBonusesTooltip), nameof(BindPostfix));

                if (target == null || postfix == null)
                {
                    Debug.LogError(LogPrefix +
                        "Active equipment tooltip could not initialize. " +
                        "TooltipFeatureCharacterDescription.Bind or postfix was not found.");
                    return;
                }

                Harmony harmony = new Harmony(
                    "SolastaExpandedEquipmentMod.ActiveEquipmentBonusesTooltip");

                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                initialized = true;

                Debug.Log(LogPrefix +
                    "Active equipment bonuses tooltip patch installed.");
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix +
                    "Active equipment bonuses tooltip initialization failed: " + ex);
            }
        }

        private static void BindPostfix(
            TooltipFeatureCharacterDescription __instance,
            ITooltip tooltip)
        {
            try
            {
                if (__instance == null || tooltip == null)
                    return;

                RulesetCharacter character = GetCharacter(ReadMember(tooltip, "Context"));
                if (character == null)
                    return;

                GuiLabel headerLabel = ReadMember(__instance, "headerLabel") as GuiLabel;
                if (headerLabel == null || string.IsNullOrEmpty(headerLabel.Text))
                    return;

                // Avoid appending the section more than once to a reused tooltip.
                if (headerLabel.Text.IndexOf(SectionHeader, StringComparison.Ordinal) >= 0)
                    return;

                string summary = BuildSummary(character);
                if (string.IsNullOrEmpty(summary))
                    return;

                headerLabel.Text += "\n\n" + SectionHeader + "\n" + summary;

                RectTransform rectTransform = __instance.GetComponent<RectTransform>();
                RebuildTooltipLayout(rectTransform);
            }
            catch (Exception ex)
            {
                if (loggedErrors < 5)
                {
                    loggedErrors++;
                    Debug.LogWarning(LogPrefix +
                        "Active equipment tooltip update failed: " + ex);
                }
            }
        }


        // Resolve Unity's UI layout helper at runtime so this mod does not
        // need a compile-time reference to UnityEngine.UI.dll.
        private static void RebuildTooltipLayout(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            try
            {
                Type layoutRebuilderType =
                    AccessTools.TypeByName("UnityEngine.UI.LayoutRebuilder");

                if (layoutRebuilderType == null)
                    return;

                MethodInfo rebuildMethod = AccessTools.Method(
                    layoutRebuilderType,
                    "ForceRebuildLayoutImmediate",
                    new[] { typeof(RectTransform) });

                if (rebuildMethod != null)
                    rebuildMethod.Invoke(null, new object[] { rectTransform });
            }
            catch (Exception ex)
            {
                if (loggedErrors < 5)
                {
                    loggedErrors++;
                    Debug.LogWarning(LogPrefix +
                        "Tooltip layout refresh was skipped: " + ex.Message);
                }
            }
        }

        private static RulesetCharacter GetCharacter(object context)
        {
            GameLocationCharacter locationCharacter = context as GameLocationCharacter;
            if (locationCharacter != null)
                return locationCharacter.RulesetActor as RulesetCharacter;

            GameCampaignCharacter campaignCharacter = context as GameCampaignCharacter;
            return campaignCharacter == null ? null : campaignCharacter.RulesetCharacter;
        }

        private static string BuildSummary(RulesetCharacter character)
        {
            // Only use the game's active equipment feature collection.
            // EnumerateFeaturesToBrowse also includes ancestry, class, feats,
            // conditions, and other character features, which made this popup noisy.
            List<FeatureDefinition> features = GetActiveEquipmentFeatures(character);
            if (features.Count == 0)
                return null;

            List<string> lines = new List<string>();

            Dictionary<string, int> attributeBonuses =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> saveBonuses =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> resistances =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> damageImmunities =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> vulnerabilities =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> conditionImmunities =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> movementEffects =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> reductions =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> otherEffects =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (FeatureDefinition feature in features)
            {
                if (feature == null)
                    continue;

                FeatureDefinitionAttributeModifier attribute =
                    feature as FeatureDefinitionAttributeModifier;
                if (attribute != null)
                {
                    AddAttributeBonus(attribute, attributeBonuses, otherEffects);
                    continue;
                }

                FeatureDefinitionSavingThrowAffinity savingThrow =
                    feature as FeatureDefinitionSavingThrowAffinity;
                if (savingThrow != null)
                {
                    AddSavingThrowBonuses(savingThrow, saveBonuses);
                    continue;
                }

                FeatureDefinitionDamageAffinity damage =
                    feature as FeatureDefinitionDamageAffinity;
                if (damage != null)
                {
                    AddDamageDefense(
                        damage, resistances, damageImmunities, vulnerabilities, reductions);
                    continue;
                }

                FeatureDefinitionConditionAffinity conditionAffinity =
                    feature as FeatureDefinitionConditionAffinity;
                if (conditionAffinity != null)
                {
                    if (conditionAffinity.ConditionAffinityType ==
                        RuleDefinitions.ConditionAffinityType.Immunity)
                    {
                        string conditionName = FormatIdentifier(conditionAffinity.ConditionType, "Condition");
                        if (!string.IsNullOrEmpty(conditionName))
                            conditionImmunities.Add(conditionName);
                    }
                    continue;
                }

                FeatureDefinitionMovementAffinity movement =
                    feature as FeatureDefinitionMovementAffinity;
                if (movement != null)
                {
                    if (movement.ImmuneDifficultTerrain)
                        movementEffects.Add("Ignores difficult terrain");
                    continue;
                }

                // Keep other equipment features only when they have a usable
                // presentation title. Never print raw localization keys or
                // the generic NoContentTitle placeholder.
                string title = GetFeatureTitle(feature);
                if (!string.IsNullOrEmpty(title))
                    otherEffects.Add(title);
            }

            AddDictionaryLines(lines, attributeBonuses, "Attribute bonuses");
            AddSavingThrowLine(lines, saveBonuses);

            if (resistances.Count > 0)
                lines.Add("Resistant to: " + JoinSorted(resistances));
            if (damageImmunities.Count > 0)
                lines.Add("Immune to damage: " + JoinSorted(damageImmunities));
            if (vulnerabilities.Count > 0)
                lines.Add("Vulnerable to: " + JoinSorted(vulnerabilities));
            if (reductions.Count > 0)
                lines.Add("Damage reduced: " + JoinSortedReductions(reductions));
            if (conditionImmunities.Count > 0)
                lines.Add("Condition immunities: " + JoinSorted(conditionImmunities));
            if (movementEffects.Count > 0)
                lines.Add("Movement: " + JoinSorted(movementEffects));
            if (otherEffects.Count > 0)
                lines.Add("Other equipment effects: " + JoinSorted(otherEffects));

            if (lines.Count == 0)
                return null;

            return string.Join("\n", lines.ToArray());
        }

        private static List<FeatureDefinition> GetActiveEquipmentFeatures(RulesetCharacter character)
        {
            List<FeatureDefinition> result = new List<FeatureDefinition>();
            IDictionary activeItemFeatures = ReadMember(character, "activeItemFeatures") as IDictionary;
            if (activeItemFeatures == null)
                return result;

            HashSet<FeatureDefinition> seen = new HashSet<FeatureDefinition>();
            foreach (DictionaryEntry entry in activeItemFeatures)
            {
                IEnumerable itemFeatures = entry.Value as IEnumerable;
                if (itemFeatures == null)
                    continue;

                foreach (object candidate in itemFeatures)
                {
                    FeatureDefinition feature = candidate as FeatureDefinition;
                    if (feature != null && seen.Add(feature))
                        result.Add(feature);
                }
            }

            return result;
        }

        private static string FormatIdentifier(string value, string prefix)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (!string.IsNullOrEmpty(prefix) &&
                value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                value = value.Substring(prefix.Length);

            return SplitWords(value);
        }

        private static string SplitWords(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current) &&
                    (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1])))
                    builder.Append(' ');
                builder.Append(current);
            }

            return builder.ToString();
        }

        private static void AddAttributeBonus(
            FeatureDefinitionAttributeModifier feature,
            Dictionary<string, int> bonuses,
            HashSet<string> otherEffects)
        {
            string attribute = Convert.ToString(ReadMember(feature, "modifiedAttribute"));
            object valueObject = ReadMember(feature, "modifierValue");
            object operationObject = ReadMember(feature, "modifierOperation");

            if (string.IsNullOrEmpty(attribute) || valueObject == null)
                return;

            int value;
            if (!int.TryParse(Convert.ToString(valueObject), out value) || value == 0)
                return;

            string operation = Convert.ToString(operationObject);
            if (!string.IsNullOrEmpty(operation) &&
                operation.IndexOf("Add", StringComparison.OrdinalIgnoreCase) < 0)
            {
                otherEffects.Add(attribute + " " + operation + " " + value);
                return;
            }

            int current;
            bonuses.TryGetValue(attribute, out current);
            bonuses[attribute] = current + value;
        }

        private static void AddSavingThrowBonuses(
            FeatureDefinitionSavingThrowAffinity feature,
            Dictionary<string, int> bonuses)
        {
            object groupsObject = ReadMember(feature, "affinityGroups");
            IEnumerable groups = groupsObject as IEnumerable;
            if (groups == null)
                return;

            foreach (object group in groups)
            {
                if (group == null)
                    continue;

                string ability = Convert.ToString(ReadMember(group, "abilityScoreName"));
                string modifierType = Convert.ToString(
                    ReadMember(group, "savingThrowModifierType"));
                object numberObject = ReadMember(group, "savingThrowModifierDiceNumber");
                object dieTypeObject = ReadMember(group, "savingThrowModifierDieType");

                if (string.IsNullOrEmpty(ability) || numberObject == null)
                    continue;

                // The mod's protection cloaks use AddDice with D1, which is a
                // fixed numeric saving-throw bonus. Do not misreport other dice.
                if (!string.Equals(modifierType, "AddDice", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Convert.ToString(dieTypeObject), "D1", StringComparison.OrdinalIgnoreCase))
                    continue;

                int amount;
                if (!int.TryParse(Convert.ToString(numberObject), out amount) || amount == 0)
                    continue;

                int current;
                bonuses.TryGetValue(ability, out current);
                bonuses[ability] = current + amount;
            }
        }

        private static void AddDamageDefense(
            FeatureDefinitionDamageAffinity feature,
            HashSet<string> resistances,
            HashSet<string> immunities,
            HashSet<string> vulnerabilities,
            Dictionary<string, int> reductions)
        {
            string damageType = feature.DamageType;
            if (string.IsNullOrEmpty(damageType))
                return;

            string title = GetDamageTypeTitle(damageType);

            if (feature.FlatDamageReduction > 0)
            {
                int current;
                if (!reductions.TryGetValue(title, out current) ||
                    feature.FlatDamageReduction > current)
                    reductions[title] = feature.FlatDamageReduction;
            }

            switch (feature.DamageAffinityType)
            {
                case RuleDefinitions.DamageAffinityType.Resistance:
                    resistances.Add(title);
                    break;
                case RuleDefinitions.DamageAffinityType.Immunity:
                    immunities.Add(title);
                    break;
                case RuleDefinitions.DamageAffinityType.Vulnerability:
                    vulnerabilities.Add(title);
                    break;
            }
        }

        private static void AddDictionaryLines(
            List<string> lines,
            Dictionary<string, int> values,
            string heading)
        {
            if (values.Count == 0)
                return;

            List<string> entries = new List<string>();
            foreach (KeyValuePair<string, int> pair in values)
                entries.Add(pair.Key + " " + FormatSigned(pair.Value));

            entries.Sort(StringComparer.OrdinalIgnoreCase);
            lines.Add(heading + ": " + string.Join(", ", entries.ToArray()));
        }

        private static void AddSavingThrowLine(
            List<string> lines,
            Dictionary<string, int> bonuses)
        {
            if (bonuses.Count == 0)
                return;

            string[] abilities =
            {
                "Strength", "Dexterity", "Constitution",
                "Intelligence", "Wisdom", "Charisma"
            };

            bool allSame = bonuses.Count == abilities.Length;
            int common = 0;

            if (allSame)
            {
                common = bonuses[abilities[0]];
                foreach (string ability in abilities)
                {
                    int value;
                    if (!bonuses.TryGetValue(ability, out value) || value != common)
                    {
                        allSame = false;
                        break;
                    }
                }
            }

            if (allSame)
            {
                lines.Add("Saving throws: " + FormatSigned(common) + " to all");
                return;
            }

            List<string> entries = new List<string>();
            foreach (KeyValuePair<string, int> pair in bonuses)
                entries.Add(pair.Key + " " + FormatSigned(pair.Value));

            entries.Sort(StringComparer.OrdinalIgnoreCase);
            lines.Add("Saving throws: " + string.Join(", ", entries.ToArray()));
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        private static string GetDamageTypeTitle(string damageType)
        {
            Database<DamageDefinition> database =
                DatabaseRepository.GetDatabase<DamageDefinition>();

            DamageDefinition definition =
                database == null ? null : database.GetElement(damageType, true);

            if (definition != null)
                return definition.FormatTitle();

            return damageType.StartsWith("Damage", StringComparison.Ordinal)
                ? damageType.Substring("Damage".Length)
                : damageType;
        }

        private static string GetFeatureTitle(FeatureDefinition feature)
        {
            if (feature == null)
                return null;

            GuiPresentation presentation = feature.GuiPresentation;
            string title = presentation == null ? null : presentation.Title;
            if (string.IsNullOrEmpty(title))
                return null;

            // Solasta stores many titles as keys such as Feature/&FooTitle.
            // Do not leak those raw keys into the tooltip, and omit its generic
            // placeholder for features that intentionally have no content title.
            if (title.IndexOf("NoContentTitle", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;

            int marker = title.IndexOf("/&", StringComparison.Ordinal);
            if (marker >= 0)
                title = title.Substring(marker + 2);

            if (title.EndsWith("Title", StringComparison.OrdinalIgnoreCase))
                title = title.Substring(0, title.Length - "Title".Length);

            return SplitWords(title);
        }

        private static string JoinSorted(HashSet<string> values)
        {
            List<string> sorted = new List<string>(values);
            sorted.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", sorted.ToArray());
        }

        private static string JoinSortedReductions(Dictionary<string, int> values)
        {
            List<string> sorted = new List<string>();
            foreach (KeyValuePair<string, int> pair in values)
                sorted.Add(pair.Key + " by " + pair.Value);

            sorted.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", sorted.ToArray());
        }

        private static object ReadMember(object target, string name)
        {
            if (target == null)
                return null;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(target);

                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null &&
                    property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }

            return null;
        }
    }
}
