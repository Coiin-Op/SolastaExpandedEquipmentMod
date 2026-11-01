using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    public static class EquipmentRegistry
    {
        // Keep successful categories complete even if a different category fails.
        private static bool armorRegistered;
        private static bool weaponsRegistered;
        private static bool ringsRegistered;
        private static bool bootsRegistered;
        private static bool glovesRegistered;
        private static bool stavesRegistered;
        private static bool ammunitionRegistered;
        private static bool cloaksRegistered;

        private static bool armorFailureReported;
        private static bool weaponsFailureReported;
        private static bool ringsFailureReported;
        private static bool bootsFailureReported;
        private static bool glovesFailureReported;
        private static bool stavesFailureReported;
        private static bool ammunitionFailureReported;
        private static bool cloaksFailureReported;

        public static bool RegisterAll()
        {
            armorRegistered = RegisterCategory(
                "Armor", armorRegistered, ref armorFailureReported,
                () => ArmorRegistration.Register());

            weaponsRegistered = RegisterCategory(
                "Weapons", weaponsRegistered, ref weaponsFailureReported,
                () => WeaponRegistration.Register());

            ringsRegistered = RegisterCategory(
                "Rings and wands", ringsRegistered, ref ringsFailureReported,
                () => RingItems.Register());

            bootsRegistered = RegisterCategory(
                "Boots", bootsRegistered, ref bootsFailureReported,
                () => BootsItems.Register());

            glovesRegistered = RegisterCategory(
                "Gloves", glovesRegistered, ref glovesFailureReported,
                () => GlovesItems.Register());

            stavesRegistered = RegisterCategory(
                "Staves", stavesRegistered, ref stavesFailureReported,
                () => StaffItems.Register());

            ammunitionRegistered = RegisterCategory(
                "Arrows and bolts", ammunitionRegistered, ref ammunitionFailureReported,
                () => AmmunitionItems.RegisterArrowsAndBolts());

            cloaksRegistered = RegisterCategory(
                "Cloaks", cloaksRegistered, ref cloaksFailureReported,
                () => CloaksItems.Register());

            return armorRegistered &&
                   weaponsRegistered &&
                   ringsRegistered &&
                   bootsRegistered &&
                   glovesRegistered &&
                   stavesRegistered &&
                   ammunitionRegistered &&
                   cloaksRegistered;
        }

        private static bool RegisterCategory(
            string categoryName,
            bool alreadyRegistered,
            ref bool failureReported,
            System.Func<bool> register)
        {
            if (alreadyRegistered)
                return true;

            try
            {
                bool success = register();

                if (success)
                {
                    failureReported = false;

                    Debug.Log(
                        "[Solasta Expanded Equipment] " +
                        categoryName + " registration succeeded.");

                    return true;
                }

                if (!failureReported)
                {
                    Debug.LogWarning(
                        "[Solasta Expanded Equipment] " +
                        categoryName +
                        " registration returned failure; " +
                        "other categories will continue.");

                    failureReported = true;
                }
            }
            catch (System.Exception ex)
            {
                if (!failureReported)
                {
                    Debug.LogError(
                        "[Solasta Expanded Equipment] " +
                        categoryName +
                        " registration threw an exception: " + ex);

                    failureReported = true;
                }
            }

            return false;
        }
    }


    /// <summary>
    /// Registers magic gloves. Shocking Grasp uses the game's native additional-damage
    /// provider and native ConditionShocked effect. Color Spray is a three-charge device.
    /// </summary>
    public static class GlovesItems
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";
        private const string ShockingFeatureName = "AdditionalDamageGlovesOfShockingGrasp";

        public static bool Register()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            if (itemDb == null || spellDb == null)
            {
                Debug.LogError(LogPrefix + "Glove registration failed: item or spell database unavailable.");
                return false;
            }

            bool shocking = RegisterShockingGraspGloves(itemDb);
            bool colorSpray = RegisterColorSprayGloves(itemDb, spellDb);

            Debug.Log(LogPrefix + "Glove registration results: Shocking Grasp=" + shocking +
                ", Color Spray=" + colorSpray + ".");
            return shocking && colorSpray;
        }

        private static bool RegisterShockingGraspGloves(Database<ItemDefinition> itemDb)
        {
            const string itemId = "GlovesOfShockingGrasp";
            const string displayName = "Gloves of Shocking Grasp";

            try
            {
                Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
                ConditionDefinition shocked = conditionDb == null ? null : conditionDb.GetElement("ConditionShocked", true);
                if (shocked == null)
                {
                    Debug.LogError(LogPrefix + "Could not find ConditionShocked for " + displayName + ".");
                    return false;
                }

                FeatureDefinitionAdditionalDamage shockingFeature = GetOrCreateShockingFeature(shocked);
                if (shockingFeature == null)
                {
                    Debug.LogError(LogPrefix + "Could not create or repair the native Shocking Grasp hit feature.");
                    return false;
                }

                ItemDefinition existing = itemDb.GetElement(itemId, true);
                if (existing != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; Shocking Grasp feature GUI presentation verified.");
                    return true;
                }

                ItemDefinition template = itemDb.GetElement("GlovesOfMissileSnaring", true);
                ItemPropertyDescription propertyTemplate = FindPropertyTemplate(itemDb);
                if (template == null || template.GuiPresentation == null || shocked == null || propertyTemplate == null)
                {
                    Debug.LogError(LogPrefix + "Could not find the glove template, ConditionShocked, or item-property template for " + displayName + ".");
                    return false;
                }

                ItemPropertyDescription property = new ItemPropertyDescription(propertyTemplate);
                bool propertyConfigured =
                    SetMember(property, "appliesOnItemOnly", false) &&
                    SetEnumMember(property, "type", "Feature") &&
                    SetMember(property, "featureDefinition", shockingFeature) &&
                    SetMember(property, "conditionDefinition", null) &&
                    SetEnumMember(property, "knowledgeAffinity", "ActiveAndVisible");

                if (!propertyConfigured)
                {
                    Debug.LogError(LogPrefix + "Could not configure the Shocking Grasp glove property.");
                    return false;
                }

                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null)
                    return false;

                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
                presentation.Title = displayName;
                presentation.Description = "On a successful melee weapon hit, deals 1d8 Lightning damage and inflicts Shocking Grasp, preventing the target from taking reactions until the end of its next turn.";
                SetMember(item, "guiPresentation", presentation);
                SetMember(item, "magical", true);
                SetMember(item, "requiresAttunement", true);
                SetMember(item, "requiresIdentification", false);
                SetEnumMember(item, "itemRarity", "Rare");
                SetMember(item, "isUsableDevice", false);
                SetMember(item, "usableDeviceDescription", null);

                List<ItemPropertyDescription> properties = new List<ItemPropertyDescription>();
                properties.Add(property);
                if (!SetMember(item, "staticProperties", properties))
                {
                    Debug.LogError(LogPrefix + "Could not attach Shocking Grasp to " + displayName + ".");
                    return false;
                }

                itemDb.Add(item);
                bool registered = itemDb.GetElement(itemId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered " + displayName + " with 1d8 Lightning damage and ConditionShocked on melee hit.");
                else
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");
                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + displayName + ": " + ex);
                return false;
            }
        }

        private static FeatureDefinitionAdditionalDamage GetOrCreateShockingFeature(ConditionDefinition shocked)
        {
            Database<FeatureDefinitionAdditionalDamage> featureDb =
                DatabaseRepository.GetDatabase<FeatureDefinitionAdditionalDamage>();
            if (featureDb == null)
                return null;

            FeatureDefinitionAdditionalDamage feature = featureDb.GetElement(ShockingFeatureName, true);
            if (feature != null)
            {
                if (!EnsureShockingFeaturePresentation(feature, featureDb))
                {
                    Debug.LogError(LogPrefix + "Existing Shocking Grasp feature has no usable GUI presentation.");
                    return null;
                }

                return feature;
            }

            FeatureDefinitionAdditionalDamage template = null;
            foreach (FeatureDefinitionAdditionalDamage candidate in featureDb.GetAllElements())
            {
                if (candidate == null)
                    continue;

                if (template == null)
                    template = candidate;

                if (candidate.GuiPresentation != null &&
                    !string.IsNullOrEmpty(candidate.GuiPresentation.Description))
                {
                    template = candidate;
                    break;
                }
            }

            if (template == null)
            {
                Debug.LogError(LogPrefix + "No base-game additional-damage feature is available as a template.");
                return null;
            }

            feature = UnityEngine.Object.Instantiate(template);
            if (feature == null)
                return null;

            feature.ForceName(ShockingFeatureName);
            feature.SetUserContentGUID(Guid.NewGuid().ToString());
            feature.IsUserContent = true;


            List<ConditionOperationDescription> conditionOperations =
                new List<ConditionOperationDescription>();

            ConditionOperationDescription conditionOperation =
                new ConditionOperationDescription();

            conditionOperation.ConditionDefinition = shocked;
            conditionOperation.Operation =
                ConditionOperationDescription.ConditionOperation.Add;

            conditionOperations.Add(conditionOperation);


            bool configured =
                SetMember(feature, "notificationTag", "GlovesOfShockingGrasp") &&
                SetEnumMember(feature, "limitedUsage", "None") &&
                SetMember(feature, "firstTargetOnly", false) &&
                SetEnumMember(feature, "targetSide", "Enemy") &&
                SetMember(feature, "otherSimilarAdditionalDamages", new List<FeatureDefinitionAdditionalDamage>()) &&
                SetEnumMember(feature, "triggerCondition", "AlwaysActive") &&
                SetEnumMember(feature, "requiredProperty", "MeleeWeapon") &&
                SetMember(feature, "attackModeOnly", true) &&
                SetMember(feature, "attackOnly", false) &&
                SetMember(feature, "requiredTargetCondition", null) &&
                SetEnumMember(feature, "requiredTargetSenseType", "None") &&
                SetMember(feature, "requiredTargetCreatureTag", string.Empty) &&
                SetMember(feature, "requiredCharacterFamily", null) &&
                SetMember(feature, "requiredSpecificSpell", null) &&
                SetEnumMember(feature, "requiredAncestryType", "Sorcerer") &&
                SetEnumMember(feature, "damageValueDetermination", "Die") &&
                SetEnumMember(feature, "damageDieType", "D8") &&
                SetMember(feature, "damageDiceNumber", 1) &&
                SetEnumMember(feature, "additionalDamageType", "Specific") &&
                SetMember(feature, "specificDamageType", "DamageLightning") &&
                SetEnumMember(feature, "damageAdvancement", "None") &&
                SetMember(feature, "diceByRankTable", new List<DiceByRank>()) &&
                SetMember(feature, "familiesWithAdditionalDice", new List<string>()) &&
                SetMember(feature, "familiesDiceNumber", 1) &&
                SetMember(feature, "ignoreCriticalDoubleDice", false) &&
                SetMember(feature, "hasSavingThrow", false) &&
                SetEnumMember(feature, "damageSaveAffinity", "None") &&
                SetMember(feature, "conditionOperations", conditionOperations) &&
                SetMember(feature, "addLightSource", false) &&
                SetMember(feature, "particlesBasedOnAncestryDamageType", false);

            if (configured)
                configured = EnsureShockingFeaturePresentation(feature, featureDb);

            if (!configured)
            {
                Debug.LogError(LogPrefix + "Could not configure all Shocking Grasp additional-damage fields or its GUI presentation.");
                UnityEngine.Object.Destroy(feature);
                return null;
            }

            featureDb.Add(feature);

            FeatureDefinitionAdditionalDamage registered =
                featureDb.GetElement(ShockingFeatureName, true);

            if (registered == null || !EnsureShockingFeaturePresentation(registered, featureDb))
            {
                Debug.LogError(LogPrefix + "Shocking Grasp feature failed its post-registration GUI-presentation check.");
                return null;
            }

            return registered;
        }

        private static bool EnsureShockingFeaturePresentation(
            FeatureDefinitionAdditionalDamage feature,
            Database<FeatureDefinitionAdditionalDamage> featureDb)
        {
            if (feature == null || featureDb == null)
                return false;

            GuiPresentation sourcePresentation = feature.GuiPresentation;

            if (sourcePresentation == null ||
                string.IsNullOrEmpty(sourcePresentation.Description))
            {
                sourcePresentation = null;

                foreach (FeatureDefinitionAdditionalDamage candidate in featureDb.GetAllElements())
                {
                    if (candidate == null || object.ReferenceEquals(candidate, feature) ||
                        candidate.GuiPresentation == null ||
                        string.IsNullOrEmpty(candidate.GuiPresentation.Description))
                        continue;

                    sourcePresentation = candidate.GuiPresentation;
                    break;
                }
            }

            // Do not let an absent template presentation produce a null GUI value.
            // The feature's own title and description are assigned immediately below.
            GuiPresentation presentation = sourcePresentation != null
                ? new GuiPresentation(sourcePresentation)
                : new GuiPresentation();

            presentation.Title = "Shocking Grasp on Hit";
            presentation.Description =
                "Adds 1d8 Lightning damage and the Shocking Grasp condition to melee weapon hits.";

            if (!SetMember(feature, "guiPresentation", presentation))
                return false;

            GuiPresentation assigned = feature.GuiPresentation;
            return assigned != null &&
                !string.IsNullOrEmpty(assigned.Title) &&
                !string.IsNullOrEmpty(assigned.Description);
        }

        private static bool RegisterColorSprayGloves(
            Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb)
        {
            const string itemId = "GlovesOfColorSpray";
            const string displayName = "Gloves of Color Spray";

            try
            {
                if (itemDb.GetElement(itemId, true) != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition template = itemDb.GetElement("GlovesOfMissileSnaring", true);
                SpellDefinition colorSpray = spellDb.GetElement("ColorSpray", true);
                if (template == null || template.GuiPresentation == null || colorSpray == null)
                {
                    Debug.LogError(LogPrefix + "Could not find the glove template or Color Spray spell for " + displayName + ".");
                    return false;
                }

                List<KeyValuePair<SpellDefinition, int>> spellUses =
                    new List<KeyValuePair<SpellDefinition, int>>();
                spellUses.Add(new KeyValuePair<SpellDefinition, int>(colorSpray, 3));

                UsableDeviceDescription device = RingItems.CreateSpellFunctionsByRecharge(itemDb, spellUses);
                if (device == null)
                {
                    Debug.LogError(LogPrefix + "Could not create the three-use daily Color Spray device.");
                    return false;
                }

                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null)
                    return false;

                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
                presentation.Title = displayName;
                presentation.Description = "Cast Color Spray three times per day. Recharges at dawn.";
                SetMember(item, "guiPresentation", presentation);
                SetMember(item, "magical", true);
                SetMember(item, "requiresAttunement", true);
                SetMember(item, "requiresIdentification", false);
                SetEnumMember(item, "itemRarity", "Rare");
                SetMember(item, "staticProperties", new List<ItemPropertyDescription>());
                SetMember(item, "usableDeviceDescription", device);
                SetMember(item, "isUsableDevice", true);

                itemDb.Add(item);
                bool registered = itemDb.GetElement(itemId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered " + displayName + " with 3 uses, recharging at dawn.");
                else
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");
                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + displayName + ": " + ex);
                return false;
            }
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