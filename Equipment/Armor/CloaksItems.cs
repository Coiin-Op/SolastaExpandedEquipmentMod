using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Registers the eleven custom cloaks. Cloaks are cloned from the vanilla
    /// Cloak of Protection so their shoulder equipment slot and presentation
    /// data remain valid for the game.
    /// </summary>
    public static class CloaksItems
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";

        public static bool Register()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            if (itemDb == null)
            {
                Debug.LogError(LogPrefix + "Cloak registration failed: item database unavailable.");
                return false;
            }

            ItemDefinition cloakTemplate = itemDb.GetElement("CloakOfProtection", true);
            if (cloakTemplate == null)
            {
                Debug.LogError(LogPrefix + "Cloak registration failed: vanilla CloakOfProtection template unavailable.");
                return false;
            }

            if (!HasSlot(cloakTemplate, EquipmentDefinitions.SlotTypeShoulders))
            {
                Debug.LogError(LogPrefix + "CloakOfProtection does not contain the expected ShouldersSlot.");
                return false;
            }

            ItemPropertyDescription propertyTemplate = FindPropertyTemplate(itemDb);
            if (propertyTemplate == null)
            {
                Debug.LogError(LogPrefix + "Cloak registration failed: no item-property template was found.");
                return false;
            }

            bool success = true;

            success &= RegisterProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "CloakOfProtectionPlus2", "Cloak of Protection +2", 2, "Rare");
            success &= RegisterProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "CloakOfProtectionPlus3", "Cloak of Protection +3", 3, "VeryRare");

            success &= RegisterElementalProtectionCloak(itemDb, cloakTemplate, propertyTemplate);

            success &= RegisterPhysicalProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "Piercing", "Piercing", 1, "Uncommon");
            success &= RegisterPhysicalProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "Piercing", "Piercing", 2, "Rare");
            success &= RegisterPhysicalProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "Piercing", "Piercing", 3, "VeryRare");

            success &= RegisterPhysicalProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "Bludgeoning", "Bludgeoning", 1, "Uncommon");
            success &= RegisterPhysicalProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "Bludgeoning", "Bludgeoning", 2, "Rare");
            success &= RegisterPhysicalProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "Bludgeoning", "Bludgeoning", 3, "VeryRare");

            success &= RegisterPhysicalProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "Slashing", "Slashing", 1, "Uncommon");
            success &= RegisterPhysicalProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "Slashing", "Slashing", 2, "Rare");
            success &= RegisterPhysicalProtectionCloak(itemDb, cloakTemplate, propertyTemplate,
                "Slashing", "Slashing", 3, "VeryRare");

            Debug.Log(LogPrefix + "Cloak registration completed. Success=" + success + ".");
            return success;
        }

        private static bool RegisterProtectionCloak(
            Database<ItemDefinition> itemDb,
            ItemDefinition template,
            ItemPropertyDescription propertyTemplate,
            string itemId,
            string title,
            int bonus,
            string rarity)
        {
            List<FeatureDefinition> features = new List<FeatureDefinition>
            {
                ArmorEnchantmentFactory.CreateAttributeModifier(
                    itemId + "ArmorClassBonus", "ArmorClass", "Additive", bonus),
                ArmorEnchantmentFactory.CreateSavingThrowBonus(
                    itemId + "SavingThrowBonus", bonus)
            };

            string description = title + ". While worn, grants +" + bonus +
                " to Armor Class and all saving throws.";
            return RegisterCloak(itemDb, template, propertyTemplate,
                itemId, title, description, rarity, features);
        }

        private static bool RegisterElementalProtectionCloak(
            Database<ItemDefinition> itemDb,
            ItemDefinition template,
            ItemPropertyDescription propertyTemplate)
        {
            const string itemId = "CloakOfElementalProtection";
            const string title = "Cloak of Elemental Protection";

            List<FeatureDefinition> features = new List<FeatureDefinition>
            {
                ArmorEnchantmentFactory.CreateDamageResistance(itemId + "FireResistance", "DamageFire"),
                ArmorEnchantmentFactory.CreateDamageResistance(itemId + "LightningResistance", "DamageLightning"),
                ArmorEnchantmentFactory.CreateDamageResistance(itemId + "ColdResistance", "DamageCold")
            };

            return RegisterCloak(itemDb, template, propertyTemplate,
                itemId, title,
                title + ". While worn, the wearer has resistance to fire, lightning, and cold damage.",
                "Rare", features);
        }

        private static bool RegisterPhysicalProtectionCloak(
            Database<ItemDefinition> itemDb,
            ItemDefinition template,
            ItemPropertyDescription propertyTemplate,
            string damageType,
            string displayDamageType,
            int tier,
            string rarity)
        {
            string itemId = "CloakOf" + damageType + "ProtectionPlus" + tier;
            string title = "Cloak of " + displayDamageType + " Protection +" + tier;

            FeatureDefinitionDamageAffinity reduction =
                ArmorEnchantmentFactory.CreateFlatDamageReduction(
                    itemId + "DamageReduction", "Damage" + damageType, tier);

            List<FeatureDefinition> features = new List<FeatureDefinition> { reduction };

            return RegisterCloak(itemDb, template, propertyTemplate,
                itemId, title,
                title + ". While worn, reduces incoming " + displayDamageType.ToLowerInvariant() +
                " damage by " + tier + ".",
                rarity, features);
        }

        private static bool RegisterCloak(
            Database<ItemDefinition> itemDb,
            ItemDefinition template,
            ItemPropertyDescription propertyTemplate,
            string itemId,
            string title,
            string description,
            string rarity,
            List<FeatureDefinition> features)
        {
            try
            {
                ItemDefinition existing = itemDb.GetElement(itemId, true);
                if (existing != null)
                {
                    bool existingHasShoulderSlot = HasSlot(existing, EquipmentDefinitions.SlotTypeShoulders);
                    if (!existingHasShoulderSlot)
                    {
                        Debug.LogError(LogPrefix + title +
                            " already exists but does not use ShouldersSlot.");
                        return false;
                    }

                    Debug.Log(LogPrefix + "Already registered: " + title + ".");
                    return true;
                }

                if (features == null || features.Count == 0)
                    return false;

                foreach (FeatureDefinition feature in features)
                {
                    if (feature == null)
                    {
                        Debug.LogError(LogPrefix + "A required feature could not be created for " + title + ".");
                        return false;
                    }
                }

                ItemDefinition item = UnityEngine.Object.Instantiate(template);
                if (item == null)
                    return false;

                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                if (!HasSlot(item, EquipmentDefinitions.SlotTypeShoulders))
                {
                    Debug.LogError(LogPrefix + "Cloned item lost ShouldersSlot: " + title + ".");
                    return false;
                }

                if (template.GuiPresentation != null)
                {
                    GuiPresentation gui = new GuiPresentation(template.GuiPresentation);
                    gui.Title = title;
                    gui.Description = description;
                    if (!SetMember(item, "guiPresentation", gui))
                    {
                        Debug.LogError(LogPrefix + "Could not set GUI presentation for " + title + ".");
                        return false;
                    }
                }

                // The vanilla template may already contain the +1 protection property.
                // Replace its properties so the custom tier receives only its own effects.
                List<ItemPropertyDescription> properties = new List<ItemPropertyDescription>();
                foreach (FeatureDefinition feature in features)
                {
                    ItemPropertyDescription property = new ItemPropertyDescription(propertyTemplate);
                    bool appliesSet = SetMember(property, "appliesOnItemOnly", false);
                    bool typeSet = SetEnumMember(property, "type", "Feature");
                    bool featureSet = SetMember(property, "featureDefinition", feature);
                    bool conditionSet = SetMember(property, "conditionDefinition", null);
                    bool knowledgeSet = SetEnumMember(property, "knowledgeAffinity", "ActiveAndVisible");

                    if (!appliesSet || !typeSet || !featureSet || !conditionSet || !knowledgeSet)
                    {
                        Debug.LogError(LogPrefix + "Could not attach a feature to " + title + ".");
                        return false;
                    }

                    properties.Add(property);
                }

                if (!SetMember(item, "staticProperties", properties) ||
                    !SetMember(item, "magical", true) ||
                    !SetMember(item, "requiresAttunement", true) ||
                    !SetEnumMember(item, "itemRarity", rarity) ||
                    !SetMember(item, "isUsableDevice", false) ||
                    !SetMember(item, "usableDeviceDescription", null))
                {
                    Debug.LogError(LogPrefix + "Could not finish configuring " + title + ".");
                    return false;
                }

                itemDb.Add(item);

                ItemDefinition registered = itemDb.GetElement(itemId, true);
                if (registered == null || !HasSlot(registered, EquipmentDefinitions.SlotTypeShoulders))
                {
                    Debug.LogError(LogPrefix + "Registration verification failed for " + title + ".");
                    return false;
                }

                Debug.Log(LogPrefix + "Registered " + title + " | Slot=ShouldersSlot | Rarity=" + rarity + ".");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + title + ": " + ex);
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

        private static bool HasSlot(ItemDefinition item, string requiredSlot)
        {
            if (item == null || string.IsNullOrEmpty(requiredSlot))
                return false;

            object slots = ReadMember(item, "SlotTypes", "slotTypes");
            IEnumerable enumerable = slots as IEnumerable;
            if (enumerable == null || slots is string)
                return false;

            foreach (object slot in enumerable)
            {
                if (slot == null)
                    continue;

                string slotName = slot as string;
                if (slotName == null)
                    slotName = Convert.ToString(ReadMember(slot, "Name", "name")) ?? Convert.ToString(slot);

                if (string.Equals(slotName, requiredSlot, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static object ReadMember(object target, params string[] names)
        {
            if (target == null)
                return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                foreach (string name in names)
                {
                    FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                    if (field != null)
                        return field.GetValue(target);

                    PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                    if (property != null && property.GetIndexParameters().Length == 0 &&
                        property.GetGetMethod(true) != null)
                        return property.GetValue(target, null);
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
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetSetMethod(true) != null)
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
