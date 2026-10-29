using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Registers Greater Boots of Speed using only the base game's four Haste features.
    /// The item is passive while worn and does not depend on Unfinished Business.
    /// </summary>
    public static class BootsItems
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";

        public static bool Register()
        {
            const string itemId = "GreaterBootsOfSpeed";
            const string displayName = "Greater Boots of Speed";

            try
            {
                Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
                if (itemDb == null)
                {
                    Debug.LogError(LogPrefix + "Greater Boots of Speed registration failed: item database unavailable.");
                    return false;
                }

                if (itemDb.GetElement(itemId, true) != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition baseBoots = itemDb.GetElement("BootsOfStridingAndSpringing", true);
                if (baseBoots == null || baseBoots.GuiPresentation == null)
                {
                    Debug.LogError(LogPrefix + "Could not find BootsOfStridingAndSpringing as a template.");
                    return false;
                }

                // Read the actual base-game Haste features directly. This avoids a failure
                // when ConditionHasted has not yet populated the condition database.
                List<FeatureDefinition> hasteFeatures = GetHasteFeatures();
                if (hasteFeatures == null || hasteFeatures.Count != 4)
                {
                    Debug.LogError(LogPrefix + "Greater Boots of Speed could not retrieve all four base-game Haste features. Retry after databases finish loading.");
                    return false;
                }

                ItemPropertyDescription propertyTemplate = FindPropertyTemplate(itemDb);
                if (propertyTemplate == null)
                {
                    Debug.LogError(LogPrefix + "Could not find an item-property template for " + displayName + ".");
                    return false;
                }

                ItemDefinition item = UnityEngine.Object.Instantiate(baseBoots);
                if (item == null)
                {
                    Debug.LogError(LogPrefix + "Could not clone the boots template.");
                    return false;
                }

                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                GuiPresentation presentation = new GuiPresentation(baseBoots.GuiPresentation);
                presentation.Title = displayName;
                presentation.Description = "While worn, grants the mechanical benefits of Haste: double walking speed, +2 AC, advantage on Dexterity saving throws, and one additional restricted action each turn.";
                if (!SetMember(item, "guiPresentation", presentation))
                    Debug.LogWarning(LogPrefix + "Could not set the presentation for " + displayName + ".");

                SetMember(item, "magical", true);
                SetMember(item, "requiresAttunement", true);
                SetMember(item, "requiresIdentification", false);
                SetEnumMember(item, "itemRarity", "VeryRare");

                List<ItemPropertyDescription> properties = new List<ItemPropertyDescription>();
                int added = AddFeaturesAsProperties(hasteFeatures, properties, propertyTemplate);
                if (added != 4)
                {
                    Debug.LogError(LogPrefix + "Could not attach all Haste features to " + displayName + ". Added=" + added + ".");
                    return false;
                }

                if (!SetMember(item, "staticProperties", properties))
                {
                    Debug.LogError(LogPrefix + "Could not set Haste features for " + displayName + ".");
                    return false;
                }

                itemDb.Add(item);
                bool registered = itemDb.GetElement(itemId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered " + displayName + " with four active Haste features while worn.");
                else
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");

                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register Greater Boots of Speed: " + ex);
                return false;
            }
        }

        private static List<FeatureDefinition> GetHasteFeatures()
        {
            List<FeatureDefinition> features = new List<FeatureDefinition>();
            if (!TryAddFeature<FeatureDefinitionAdditionalAction>("AdditionalActionHasted", features)) return null;
            if (!TryAddFeature<FeatureDefinitionMovementAffinity>("MovementAffinityConditionHasted", features)) return null;
            if (!TryAddFeature<FeatureDefinitionAttributeModifier>("AttributeModifierHasted", features)) return null;
            if (!TryAddFeature<FeatureDefinitionSavingThrowAffinity>("SavingThrowAffinityConditionHasted", features)) return null;
            return features;
        }

        private static bool TryAddFeature<T>(string featureId, List<FeatureDefinition> features)
            where T : FeatureDefinition
        {
            Database<T> featureDb = DatabaseRepository.GetDatabase<T>();
            if (featureDb == null)
            {
                Debug.LogWarning(LogPrefix + "Feature database unavailable for " + featureId + ".");
                return false;
            }

            T feature = featureDb.GetElement(featureId, true);
            if (feature == null)
            {
                Debug.LogWarning(LogPrefix + "Could not find base-game Haste feature " + featureId + ".");
                return false;
            }

            features.Add(feature);
            return true;
        }

        private static int AddFeaturesAsProperties(
            List<FeatureDefinition> features,
            List<ItemPropertyDescription> properties,
            ItemPropertyDescription propertyTemplate)
        {
            if (features == null)
                return 0;

            int added = 0;
            foreach (FeatureDefinition feature in features)
            {
                if (feature == null)
                    continue;

                ItemPropertyDescription property = new ItemPropertyDescription(propertyTemplate);
                bool appliesSet = SetMember(property, "appliesOnItemOnly", false);
                bool typeSet = SetEnumMember(property, "type", "Feature");
                bool featureSet = SetMember(property, "featureDefinition", feature);
                bool conditionSet = SetMember(property, "conditionDefinition", null);
                bool knowledgeSet = SetEnumMember(property, "knowledgeAffinity", "ActiveAndVisible");

                if (appliesSet && typeSet && featureSet && conditionSet && knowledgeSet)
                {
                    properties.Add(property);
                    added++;
                }
                else
                {
                    Debug.LogWarning(LogPrefix + "Could not configure the item property for Haste feature " + feature.Name + ".");
                }
            }

            return added;
        }

        private static ItemPropertyDescription FindPropertyTemplate(Database<ItemDefinition> itemDb)
        {
            foreach (ItemDefinition item in itemDb.GetAllElements())
            {
                if (item == null || item.StaticProperties == null)
                    continue;

                foreach (ItemPropertyDescription property in item.StaticProperties)
                {
                    if (property != null)
                        return property;
                }
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
                if (property != null && property.GetIndexParameters().Length == 0 && property.GetSetMethod(true) != null)
                {
                    property.SetValue(target, value, null);
                    return true;
                }
            }

            return false;
        }

        private static bool SetEnumMember(object target, string name, string enumValue)
        {
            if (target == null)
                return false;

            Type memberType = GetMemberType(target.GetType(), name);
            if (memberType == null || !memberType.IsEnum)
                return false;

            try
            {
                return SetMember(target, name, Enum.Parse(memberType, enumValue, true));
            }
            catch
            {
                return false;
            }
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
    }
}
