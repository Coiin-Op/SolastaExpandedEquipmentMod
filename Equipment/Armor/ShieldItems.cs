using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TA;

namespace SolastaExpandedEquipmentMod
{
    public static class ShieldItems
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";
        private static bool unidentifiedMembersLogged;

        private sealed class ShieldSpec
        {
            public readonly string ItemId;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly string ResistanceDamageType;

            public ShieldSpec(
                string itemId,
                string displayName,
                string description,
                string resistanceDamageType = null)
            {
                ItemId = itemId;
                DisplayName = displayName;
                Description = description;
                ResistanceDamageType = resistanceDamageType;
            }
        }

        public static bool Register()
        {
            Database<ItemDefinition> itemDb =
                DatabaseRepository.GetDatabase<ItemDefinition>();

            if (itemDb == null)
            {
                Debug.LogError(LogPrefix +
                    "Shield registration failed: item database unavailable.");
                return false;
            }

            // Creature-family shields retain their tested definitions. The four oddballs
            // added here are the +3 missile-defense shield and three true damage-resistance
            // shields. All four retain the universal +1 AC supplied by vanilla Shield+1.
            ShieldSpec[] shields =
            {
                new ShieldSpec("ShieldVsGiantsPlus1", "Shield +1, +2 vs Giants",
                    "+1 AC against all attacks and +2 AC total against Giants."),
                new ShieldSpec("ShieldVsAberrationsPlus1", "Shield +1, +2 vs Aberrations",
                    "+1 AC against all attacks and +2 AC total against Aberrations."),
                new ShieldSpec("ShieldVsArtificerConstructsPlus1", "Shield +1, +2 vs Artificer Constructs",
                    "+1 AC against all attacks and +2 AC total against Artificer Constructs."),
                new ShieldSpec("ShieldVsBeastsPlus1", "Shield +1, +2 vs Beasts",
                    "+1 AC against all attacks and +2 AC total against Beasts."),
                new ShieldSpec("ShieldVsCelestialsPlus1", "Shield +1, +2 vs Celestials",
                    "+1 AC against all attacks and +2 AC total against Celestials."),
                new ShieldSpec("ShieldVsConstructsPlus1", "Shield +1, +2 vs Constructs",
                    "+1 AC against all attacks and +2 AC total against Constructs."),
                new ShieldSpec("ShieldVsElementalsPlus1", "Shield +1, +2 vs Elementals",
                    "+1 AC against all attacks and +2 AC total against Elementals."),
                new ShieldSpec("ShieldVsFeyPlus1", "Shield +1, +2 vs Fey",
                    "+1 AC against all attacks and +2 AC total against Fey."),
                new ShieldSpec("ShieldVsHumanoidsPlus1", "Shield +1, +2 vs Humanoids",
                    "+1 AC against all attacks and +2 AC total against Humanoids."),
                new ShieldSpec("ShieldVsMonstrositiesPlus1", "Shield +1, +2 vs Monstrosities",
                    "+1 AC against all attacks and +2 AC total against Monstrosities."),
                new ShieldSpec("ShieldVsPlantsPlus1", "Shield +1, +2 vs Plants",
                    "+1 AC against all attacks and +2 AC total against Plants."),
                new ShieldSpec("ShieldVsUndeadPlus1", "Shield +1, +2 vs Undead",
                    "+1 AC against all attacks and +2 AC total against Undead."),
                new ShieldSpec("ShieldVsSorrAkkathPlus1", "Shield +1, +2 vs Sorr-Akkath",
                    "+1 AC against all attacks and +2 AC total against Sorr-Akkath."),
                new ShieldSpec("ShieldVsDragonsPlus1", "Shield +1, +2 vs Dragons",
                    "+1 AC against all attacks and +2 AC total against Dragons."),

                // Special combination shield from the requested list:
                // +1 universally, +2 total vs Fey, and +3 total vs Elementals.
                new ShieldSpec("ShieldVsFeyAndElementalsPlus1",
                    "Shield +1, +2 vs Fey, +3 vs Elementals",
                    "+1 AC against all attacks, +2 AC total against Fey, and +3 AC total against Elementals."),

                // Missile defense adds +2 AC over the universal +1, for +3 total
                // against ranged weapon/projectile attacks (not ranged spell attacks).
                new ShieldSpec("ShieldVsMissilesPlus1",
                    "Shield +1, +3 vs Missiles",
                    "+1 AC against all attacks and +3 AC total against ranged weapon and projectile attacks."),

                // These use the game's native DamageAffinityType.Resistance feature.
                // Solasta's normal resistance rules halve the matching damage type.
                new ShieldSpec("ShieldPlus1OfFireResistance",
                    "Shield +1 of Fire Resistance",
                    "+1 AC against all attacks and resistance to fire damage, halving incoming fire damage.",
                    "DamageFire"),
                new ShieldSpec("ShieldPlus1OfLightningResistance",
                    "Shield +1 of Electrical Resistance",
                    "+1 AC against all attacks and resistance to lightning (electrical) damage, halving incoming lightning damage.",
                    "DamageLightning"),
                new ShieldSpec("ShieldPlus1OfColdResistance",
                    "Shield +1 of Cold Resistance",
                    "+1 AC against all attacks and resistance to cold damage, halving incoming cold damage.",
                    "DamageCold"),

                // +2 tier: +2 AC universally, +3 total against the named family.
                new ShieldSpec("ShieldVsAberrationsPlus2", "Shield +2, +3 vs Aberrations",
                    "+2 AC against all attacks and +3 AC total against Aberrations."),
                new ShieldSpec("ShieldVsArtificerConstructsPlus2", "Shield +2, +3 vs Artificer Constructs",
                    "+2 AC against all attacks and +3 AC total against Artificer Constructs."),
                new ShieldSpec("ShieldVsBeastsPlus2", "Shield +2, +3 vs Beasts",
                    "+2 AC against all attacks and +3 AC total against Beasts."),
                new ShieldSpec("ShieldVsCelestialsPlus2", "Shield +2, +3 vs Celestials",
                    "+2 AC against all attacks and +3 AC total against Celestials."),
                new ShieldSpec("ShieldVsConstructsPlus2", "Shield +2, +3 vs Constructs",
                    "+2 AC against all attacks and +3 AC total against Constructs."),
                new ShieldSpec("ShieldVsDragonsPlus2", "Shield +2, +3 vs Dragons",
                    "+2 AC against all attacks and +3 AC total against Dragons."),
                new ShieldSpec("ShieldVsElementalsPlus2", "Shield +2, +3 vs Elementals",
                    "+2 AC against all attacks and +3 AC total against Elementals."),
                new ShieldSpec("ShieldVsFeyPlus2", "Shield +2, +3 vs Fey",
                    "+2 AC against all attacks and +3 AC total against Fey."),
                new ShieldSpec("ShieldVsGiantsPlus2", "Shield +2, +3 vs Giants",
                    "+2 AC against all attacks and +3 AC total against Giants."),
                new ShieldSpec("ShieldVsHumanoidsPlus2", "Shield +2, +3 vs Humanoids",
                    "+2 AC against all attacks and +3 AC total against Humanoids."),
                new ShieldSpec("ShieldVsMonstrositiesPlus2", "Shield +2, +3 vs Monstrosities",
                    "+2 AC against all attacks and +3 AC total against Monstrosities."),
                new ShieldSpec("ShieldVsPlantsPlus2", "Shield +2, +3 vs Plants",
                    "+2 AC against all attacks and +3 AC total against Plants."),
                new ShieldSpec("ShieldVsUndeadPlus2", "Shield +2, +3 vs Undead",
                    "+2 AC against all attacks and +3 AC total against Undead."),
                new ShieldSpec("ShieldVsSorrAkkathPlus2", "Shield +2, +3 vs Sorr-Akkath",
                    "+2 AC against all attacks and +3 AC total against Sorr-Akkath."),
                new ShieldSpec("ShieldVsMissilesPlus2", "Shield +2, +3 vs Missiles",
                    "+2 AC against all attacks and +3 AC total against ranged weapon and projectile attacks.")
            };

            int succeeded = 0;
            foreach (ShieldSpec spec in shields)
            {
                if (RegisterCreatureShield(itemDb, spec))
                    succeeded++;
            }

            Debug.Log(LogPrefix + "Shield registration complete: " +
                succeeded + "/" + shields.Length + " registered. " +
                "Includes missile defense and fire/lightning/cold resistance variants.");

            return succeeded == shields.Length;
        }

        private static bool RegisterCreatureShield(
            Database<ItemDefinition> itemDb,
            ShieldSpec spec)
        {
            try
            {
                ItemDefinition existing = itemDb.GetElement(spec.ItemId, true);
                if (existing != null)
                {
                    existing.IsUserContent = true;

                    GuiPresentation existingGui = existing.GuiPresentation != null
                        ? new GuiPresentation(existing.GuiPresentation)
                        : new GuiPresentation();
                    existingGui.Title = spec.DisplayName;
                    existingGui.Description = BuildDescription(spec.DisplayName, spec.Description);
                    ArmorFactory.SetField(existing, "guiPresentation", existingGui);
                    bool existingUnidentifiedDescriptionReady = SetUnidentifiedDescription(existing, spec);

                    bool resistanceReady = EnsureResistanceFeature(itemDb, existing, spec);
                    bool isPlusTwo = spec.ItemId.EndsWith("Plus2", StringComparison.OrdinalIgnoreCase);
                    bool existingIconUpdated = ApplyShieldIcon(itemDb, existing, isPlusTwo);
                    bool existingGuiReady = EnsureGuiItemDefinition(existing);
                    Debug.Log(LogPrefix + "Already registered: " + spec.DisplayName +
                        " | UniversalACBonus=" + (isPlusTwo ? "+2" : "+1") + " | ResistanceReady=" + resistanceReady +
                        " | GuiWrapperReady=" + existingGuiReady +
                        " | UnidentifiedDescriptionReady=" + existingUnidentifiedDescriptionReady +
                        " | TierIconUpdated=" + existingIconUpdated +
                        " | IsUserContent=" + existing.IsUserContent);
                    return existing.IsUserContent && resistanceReady && existingGuiReady;
                }

                // Clone the matching vanilla tier so its native universal AC bonus
                // remains intact. Keep the ordinary Shield model; use the tier icon.
                bool plusTwo = spec.ItemId.EndsWith("Plus2", StringComparison.OrdinalIgnoreCase);
                string templateName = plusTwo ? "Shield+2" : "Shield+1";
                ItemDefinition shieldSource = itemDb.GetElement(templateName, true);
                ItemDefinition appearanceSource = itemDb.GetElement("Shield", true);

                if (shieldSource == null || shieldSource.ArmorDescription == null ||
                    shieldSource.GuiPresentation == null || shieldSource.ItemPresentation == null)
                {
                    Debug.LogError(LogPrefix +
                        "Vanilla " + templateName + " template unavailable or incomplete for " + spec.DisplayName + ".");
                    return false;
                }

                if (appearanceSource == null || appearanceSource.ItemPresentation == null)
                {
                    Debug.LogWarning(LogPrefix +
                        "Vanilla Shield appearance source unavailable; using " + templateName + " presentation for " +
                        spec.DisplayName + ".");
                    appearanceSource = shieldSource;
                }

                ItemDefinition shield = UnityEngine.Object.Instantiate(shieldSource);
                if (shield == null)
                {
                    Debug.LogError(LogPrefix + "Could not clone vanilla " + templateName + " for " + spec.DisplayName + ".");
                    return false;
                }

                shield.ForceName(spec.ItemId);
                shield.SetUserContentGUID(Guid.NewGuid().ToString());
                shield.IsUserContent = true;

                ItemPresentation itemPresentation =
                    new ItemPresentation(appearanceSource.ItemPresentation);
                ArmorFactory.SetField(shield, "itemPresentation", itemPresentation);

                GuiPresentation gui = new GuiPresentation(shieldSource.GuiPresentation);
                gui.Title = spec.DisplayName;
                gui.Description = BuildDescription(spec.DisplayName, spec.Description);
                ArmorFactory.SetField(shield, "guiPresentation", gui);
                bool newShieldUnidentifiedDescriptionReady = SetUnidentifiedDescription(shield, spec);

                if (!EnsureResistanceFeature(itemDb, shield, spec))
                {
                    Debug.LogError(LogPrefix + "Could not attach the resistance feature to " +
                        spec.DisplayName + ".");
                    return false;
                }

                bool iconUpdated = ApplyShieldIcon(itemDb, shield, plusTwo);

                itemDb.Add(shield);
                ItemDefinition registered = itemDb.GetElement(spec.ItemId, true);
                if (registered == null)
                {
                    Debug.LogError(LogPrefix +
                        "Registration verification failed: " + spec.DisplayName + ".");
                    return false;
                }

                if (!registered.IsUserContent)
                    registered.IsUserContent = true;

                bool guiReady = EnsureGuiItemDefinition(registered);
                Debug.Log(LogPrefix + "Registered " + spec.DisplayName +
                    " | Template=" + templateName + " | UniversalACBonus=" + (plusTwo ? "+2" : "+1") +
                    " | ConditionalBonus=" + GetConditionalDescription(spec.ItemId) +
                    " | DamageResistance=" + (spec.ResistanceDamageType ?? "None") +
                    " | GuiWrapperReady=" + guiReady +
                    " | UnidentifiedDescriptionReady=" + newShieldUnidentifiedDescriptionReady +
                    " | WoodenIconUpdated=" + iconUpdated +
                    " | IsUserContent=" + registered.IsUserContent);
                return registered.IsUserContent && guiReady;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + spec.DisplayName + ": " + ex);
                return false;
            }
        }

        private static bool EnsureResistanceFeature(
            Database<ItemDefinition> itemDb,
            ItemDefinition shield,
            ShieldSpec spec)
        {
            if (shield == null || spec == null)
                return false;

            if (string.IsNullOrEmpty(spec.ResistanceDamageType))
                return true;

            string featureId = spec.ItemId + "DamageResistance";
            FeatureDefinitionDamageAffinity resistance =
                ArmorEnchantmentFactory.CreateDamageResistance(
                    featureId, spec.ResistanceDamageType);
            if (resistance == null)
                return false;

            List<ItemPropertyDescription> properties = new List<ItemPropertyDescription>();
            if (shield.StaticProperties != null)
            {
                foreach (ItemPropertyDescription sourceProperty in shield.StaticProperties)
                {
                    if (sourceProperty == null)
                        continue;

                    if (sourceProperty.FeatureDefinition != null &&
                        string.Equals(sourceProperty.FeatureDefinition.Name, featureId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        // Registration retries must not add the same resistance twice.
                        return true;
                    }

                    // Keep all vanilla Shield+1 properties, including its universal +1 AC.
                    properties.Add(new ItemPropertyDescription(sourceProperty));
                }
            }

            ItemPropertyDescription propertyTemplate = FindPropertyTemplate(itemDb);
            if (propertyTemplate == null)
            {
                Debug.LogError(LogPrefix + "No item-property template is available for " +
                    spec.DisplayName + ".");
                return false;
            }

            ItemPropertyDescription property = new ItemPropertyDescription(propertyTemplate);
            bool configured =
                ArmorFactory.SetField(property, "appliesOnItemOnly", false) &&
                ArmorFactory.SetField(property, "type", ItemPropertyDescription.PropertyType.Feature) &&
                ArmorFactory.SetField(property, "featureDefinition", resistance) &&
                ArmorFactory.SetField(property, "conditionDefinition", null) &&
                ArmorFactory.SetField(property, "knowledgeAffinity",
                    EquipmentDefinitions.KnowledgeAffinity.ActiveAndVisible);

            if (!configured)
            {
                Debug.LogError(LogPrefix + "Could not configure the " +
                    spec.ResistanceDamageType + " resistance property for " + spec.DisplayName + ".");
                return false;
            }

            properties.Add(property);
            if (!ArmorFactory.SetField(shield, "staticProperties", properties))
            {
                Debug.LogError(LogPrefix + "Could not assign the resistance property to " +
                    spec.DisplayName + ".");
                return false;
            }

            Debug.Log(LogPrefix + "Attached native damage resistance to " + spec.DisplayName +
                " | Feature=" + featureId +
                " | DamageType=" + spec.ResistanceDamageType +
                " | ExistingShieldPropertiesPreserved=" + (properties.Count > 1));
            return true;
        }

        private static ItemPropertyDescription FindPropertyTemplate(Database<ItemDefinition> itemDb)
        {
            if (itemDb == null)
                return null;

            foreach (ItemDefinition item in itemDb.GetAllElements())
            {
                if (item == null || item.StaticProperties == null)
                    continue;

                foreach (ItemPropertyDescription property in item.StaticProperties)
                    if (property != null)
                        return property;
            }

            return null;
        }

        private static bool SetUnidentifiedDescription(ItemDefinition shield, ShieldSpec spec)
        {
            if (shield == null || spec == null)
                return false;

            // The broad runtime diagnostic located this field on ItemPresentation,
            // not ItemDefinition. The shield's presentation is cloned/assigned above,
            // so set the field on that presentation instance.
            ItemPresentation presentation = shield.ItemPresentation;
            if (presentation == null)
            {
                Debug.LogWarning(LogPrefix + "ItemPresentation is unavailable for " +
                    spec.DisplayName + ". Could not assign unidentified description.");
                return false;
            }

            bool isPlusTwo = spec.ItemId.EndsWith("Plus2", StringComparison.OrdinalIgnoreCase);
            string description = isPlusTwo
                ? "An exceptional wooden shield, expertly crafted and beautifully finished."
                : "A finely crafted wooden shield, sturdy and pleasing to the eye.";

            bool assigned = ArmorFactory.SetField(
                presentation, "unidentifiedDescription", description);

            if (!assigned)
            {
                Debug.LogWarning(LogPrefix + "Could not set ItemPresentation.unidentifiedDescription for " +
                    spec.DisplayName + ". The identified description is unchanged.");
            }
            else
            {
                Debug.Log(LogPrefix + "Assigned unidentified description through " +
                    "ItemPresentation for " + spec.DisplayName + ".");
            }

            return assigned;
        }

        private static void LogUnidentifiedMembers(Type runtimeType)
        {
            Debug.Log(LogPrefix + "========== BROAD UNIDENTIFIED-DESCRIPTION DIAGNOSTIC START ==========");
            Debug.Log(LogPrefix + "Shield runtime type=" + SafeTypeName(runtimeType));

            int assembliesScanned = 0;
            int typesScanned = 0;
            int unidentifiedMatches = 0;
            int descriptionMembers = 0;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly == null)
                    continue;

                assembliesScanned++;
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                    Debug.LogWarning(LogPrefix + "ReflectionTypeLoadException while scanning assembly=" +
                        SafeAssemblyName(assembly) + " | usableTypes=" + CountNonNullTypes(types));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(LogPrefix + "Could not enumerate assembly=" +
                        SafeAssemblyName(assembly) + " | " + ex.GetType().Name + ": " + ex.Message);
                    continue;
                }

                if (types == null)
                    continue;

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    typesScanned++;
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                    FieldInfo[] fields;
                    PropertyInfo[] properties;
                    MethodInfo[] methods;
                    try
                    {
                        fields = type.GetFields(flags);
                        properties = type.GetProperties(flags);
                        methods = type.GetMethods(flags);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(LogPrefix + "Could not inspect type=" + SafeTypeName(type) +
                            " | " + ex.GetType().Name + ": " + ex.Message);
                        continue;
                    }

                    foreach (FieldInfo field in fields)
                    {
                        bool unident = ContainsUnident(field.Name);
                        bool description = IsRelevantDescriptionMember(type, field.Name, field.FieldType);
                        if (unident)
                        {
                            unidentifiedMatches++;
                            Debug.Log(LogPrefix + "UNIDENTIFIED FIELD | Assembly=" + SafeAssemblyName(assembly) +
                                " | DeclaringType=" + SafeTypeName(type) + " | Name=" + field.Name +
                                " | FieldType=" + SafeTypeName(field.FieldType) +
                                " | Static=" + field.IsStatic + " | ReadOnly=" + field.IsInitOnly);
                        }
                        else if (description)
                        {
                            descriptionMembers++;
                            Debug.Log(LogPrefix + "RELATED DESCRIPTION FIELD | Assembly=" + SafeAssemblyName(assembly) +
                                " | DeclaringType=" + SafeTypeName(type) + " | Name=" + field.Name +
                                " | FieldType=" + SafeTypeName(field.FieldType) + " | Static=" + field.IsStatic);
                        }
                    }

                    foreach (PropertyInfo property in properties)
                    {
                        bool unident = ContainsUnident(property.Name);
                        bool description = IsRelevantDescriptionMember(type, property.Name, property.PropertyType);
                        MethodInfo getter = property.GetGetMethod(true);
                        MethodInfo setter = property.GetSetMethod(true);
                        if (unident)
                        {
                            unidentifiedMatches++;
                            Debug.Log(LogPrefix + "UNIDENTIFIED PROPERTY | Assembly=" + SafeAssemblyName(assembly) +
                                " | DeclaringType=" + SafeTypeName(type) + " | Name=" + property.Name +
                                " | PropertyType=" + SafeTypeName(property.PropertyType) +
                                " | Getter=" + (getter != null) + " | Setter=" + (setter != null));
                        }
                        else if (description)
                        {
                            descriptionMembers++;
                            Debug.Log(LogPrefix + "RELATED DESCRIPTION PROPERTY | Assembly=" + SafeAssemblyName(assembly) +
                                " | DeclaringType=" + SafeTypeName(type) + " | Name=" + property.Name +
                                " | PropertyType=" + SafeTypeName(property.PropertyType) +
                                " | Getter=" + (getter != null) + " | Setter=" + (setter != null));
                        }
                    }

                    foreach (MethodInfo method in methods)
                    {
                        if (!ContainsUnident(method.Name))
                            continue;

                        unidentifiedMatches++;
                        Debug.Log(LogPrefix + "UNIDENTIFIED METHOD | Assembly=" + SafeAssemblyName(assembly) +
                            " | DeclaringType=" + SafeTypeName(type) + " | Name=" + method.Name +
                            " | ReturnType=" + SafeTypeName(method.ReturnType) +
                            " | Parameters=" + FormatParameterTypes(method.GetParameters()));
                    }
                }
            }

            Debug.Log(LogPrefix + "DIAGNOSTIC TOTALS | Assemblies=" + assembliesScanned +
                " | Types=" + typesScanned + " | UnidentifiedNameMatches=" + unidentifiedMatches +
                " | RelatedDescriptionMembers=" + descriptionMembers);
            Debug.Log(LogPrefix + "========== BROAD UNIDENTIFIED-DESCRIPTION DIAGNOSTIC END ==========");
        }

        private static bool ContainsUnident(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                name.IndexOf("unident", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsRelevantDescriptionMember(Type declaringType, string memberName, Type memberType)
        {
            if (memberType != typeof(string) || string.IsNullOrEmpty(memberName) ||
                memberName.IndexOf("description", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            string typeName = declaringType != null ? declaringType.Name : string.Empty;
            return typeName.IndexOf("Item", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Gui", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Presentation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Tooltip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Inventory", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Equipment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Definition", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatParameterTypes(ParameterInfo[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return "<none>";

            List<string> names = new List<string>();
            foreach (ParameterInfo parameter in parameters)
                names.Add(SafeTypeName(parameter.ParameterType));
            return string.Join(", ", names.ToArray());
        }

        private static int CountNonNullTypes(Type[] types)
        {
            if (types == null)
                return 0;
            int count = 0;
            foreach (Type type in types)
                if (type != null)
                    count++;
            return count;
        }

        private static string SafeTypeName(Type type)
        {
            return type == null ? "<null>" : (type.FullName ?? type.Name);
        }

        private static string SafeAssemblyName(Assembly assembly)
        {
            try
            {
                return assembly == null || assembly.GetName() == null
                    ? "<null>"
                    : assembly.GetName().Name;
            }
            catch
            {
                return "<unavailable>";
            }
        }

        private static string BuildDescription(string displayName, string mechanics)
        {
            return displayName + ". " + mechanics +
                " Its protection applies while this shield is equipped in the off-hand. " +
                "Custom equipment from Solasta Expanded Equipment.";
        }

        private static string GetConditionalDescription(string itemId)
        {
            if (string.Equals(itemId, "ShieldVsFeyAndElementalsPlus1", StringComparison.OrdinalIgnoreCase))
                return "+1 additional AC vs Fey and +2 additional AC vs Elementals (total +2/+3 AC).";
            if (string.Equals(itemId, "ShieldVsMissilesPlus1", StringComparison.OrdinalIgnoreCase))
                return "+2 additional AC vs ranged weapon/projectile attacks (total +3 AC).";
            if (string.Equals(itemId, "ShieldPlus1OfFireResistance", StringComparison.OrdinalIgnoreCase))
                return "Fire damage resistance (half damage).";
            if (string.Equals(itemId, "ShieldPlus1OfLightningResistance", StringComparison.OrdinalIgnoreCase))
                return "Lightning/electrical damage resistance (half damage).";
            if (string.Equals(itemId, "ShieldPlus1OfColdResistance", StringComparison.OrdinalIgnoreCase))
                return "Cold damage resistance (half damage).";
            return "+1 vs named creature group (additional attack penalty)";
        }

        private static bool EnsureGuiItemDefinition(ItemDefinition item)
        {
            if (item == null)
                return false;

            IGuiWrapperService guiService = ServiceRepository.GetService<IGuiWrapperService>();
            GuiWrapperManager guiManager = guiService as GuiWrapperManager;
            if (guiManager == null)
                return false;

            try
            {
                // Wait until the game's native item-wrapper pass has completed.
                // This is the same readiness sentinel used by the working Companion items.
                if (!guiManager.HasGuiItemDefintiion("RingDarkvision"))
                    return false;

                if (!guiManager.HasGuiItemDefintiion(item.Name))
                    guiManager.AddUserItem(item);

                return guiManager.HasGuiItemDefintiion(item.Name) &&
                       guiManager.GetGuiItemDefinition(item.Name) != null;
            }
            catch (ArgumentException)
            {
                // A duplicate is acceptable only if a valid wrapper already exists.
                return guiManager.HasGuiItemDefintiion(item.Name) &&
                       guiManager.GetGuiItemDefinition(item.Name) != null;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Could not register GUI wrapper for " +
                    item.Name + ": " + ex);
                return false;
            }
        }

        private static bool ApplyShieldIcon(
            Database<ItemDefinition> itemDb,
            ItemDefinition customShield,
            bool plusTwo)
        {
            if (itemDb == null || customShield == null || customShield.GuiPresentation == null)
            {
                Debug.LogWarning(LogPrefix +
                    "Cannot apply the shield-tier icon: shield GUI presentation unavailable.");
                return false;
            }

            ItemDefinition iconSource = itemDb.GetElement(plusTwo ? "Shield+2" : "Shield_Wooden", true);
            if (iconSource == null || iconSource.GuiPresentation == null ||
                iconSource.GuiPresentation.SpriteReference == null)
            {
                Debug.LogWarning(LogPrefix +
                    "Icon source for " + (plusTwo ? "Shield+2" : "Shield_Wooden") + " or its sprite is unavailable.");
                return false;
            }

            bool assigned = ArmorFactory.SetField(
                customShield.GuiPresentation,
                "spriteReference",
                iconSource.GuiPresentation.SpriteReference);

            if (assigned)
            {
                Debug.Log(LogPrefix +
                    "Applied " + (plusTwo ? "+2" : "+1 wooden") + " shield icon | IconOnly=True | ShieldRulesCopied=False");
            }
            else
            {
                Debug.LogWarning(LogPrefix + "Could not assign the wooden shield icon.");
            }

            return assigned;
        }

    }
}
