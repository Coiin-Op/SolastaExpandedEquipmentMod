using System;
using System.Reflection;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class ArmorFactory
    {
        // Legacy overload: preserve the established custom-garb AC progression (13/14/15).
        public static bool RegisterArmorFamily(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod,
            string familyId,
            string familyName,
            string appearanceSourceId)
        {
            return RegisterArmorFamilyCore(
                itemDb, mod, familyId, familyName,
                "ClothesWizard", appearanceSourceId, 12, true);
        }

        // General overload for standard armor. The base template supplies armor rules;
        // the appearance template supplies the visual presentation.
        public static bool RegisterArmorFamily(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod,
            string familyId,
            string familyName,
            string baseArmorSourceId,
            string appearanceSourceId,
            int fallbackBaseArmorClass)
        {
            return RegisterArmorFamilyCore(
                itemDb, mod, familyId, familyName,
                baseArmorSourceId, appearanceSourceId,
                fallbackBaseArmorClass, false);
        }

        private static bool RegisterArmorFamilyCore(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod,
            string familyId,
            string familyName,
            string baseArmorSourceId,
            string appearanceSourceId,
            int baseArmorClass,
            bool useProvidedBaseArmorClass)
        {
            if (itemDb == null || mod == null)
                return false;

            ItemDefinition baseSource = itemDb.GetElement(baseArmorSourceId, true);
            ItemDefinition appearanceSource = itemDb.GetElement(appearanceSourceId, true);

            if (baseSource == null || baseSource.ArmorDescription == null ||
                baseSource.GuiPresentation == null || baseSource.ItemPresentation == null)
            {
                mod.Logger.Warning(
                    "[SolastaExpandedEquipmentMod] Base armor source unavailable: " +
                    baseArmorSourceId);
                return false;
            }

            if (appearanceSource == null || appearanceSource.GuiPresentation == null ||
                appearanceSource.ItemPresentation == null)
            {
                mod.Logger.Warning(
                    "[SolastaExpandedEquipmentMod] Appearance source unavailable: " +
                    appearanceSourceId);
                return false;
            }

            int sourceArmorClass = useProvidedBaseArmorClass
                ? baseArmorClass
                : ReadArmorClass(baseSource.ArmorDescription, baseArmorClass);

            bool success = true;

            for (int tier = 1; tier <= 3; tier++)
            {
                string itemId = familyId + "Plus" + tier;
                string itemName = familyName + " +" + tier;

                try
                {
                    if (itemDb.GetElement(itemId, true) != null)
                    {
                        mod.Logger.Log(
                            "[SolastaExpandedEquipmentMod] Already registered: " + itemName);
                        continue;
                    }

                    ItemDefinition item = UnityEngine.Object.Instantiate(baseSource);
                    item.ForceName(itemId);
                    item.SetUserContentGUID(Guid.NewGuid().ToString());

                    ItemPresentation presentation =
                        new ItemPresentation(appearanceSource.ItemPresentation);

                    if (familyId == "WizardsAdventuringGarb")
                    {
                        SetField(presentation, "useArmorAddressableName", true);
                        SetField(presentation, "armorAddressableName", "ClothesWizard");
                    }
                    else if (familyId == "WarlocksArmor" || familyId == "DruidsArmor")
                    {
                        SetField(presentation, "useArmorAddressableName", true);
                        SetField(presentation, "armorAddressableName", appearanceSource.Name);
                    }

                    SetField(presentation, "isArmorAddressableNameGenderSpecific", false);
                    SetField(item, "itemPresentation", presentation);

                    // Preserve all armor-template properties and change only its AC.
                    ArmorDescription armor =
                        new ArmorDescription(baseSource.ArmorDescription);
                    SetField(armor, "armorClassValue", sourceArmorClass + tier);
                    SetField(item, "armorDefinition", armor);

                    GuiPresentation gui =
                        new GuiPresentation(appearanceSource.GuiPresentation);
                    gui.Title = itemName;
                    gui.Description = itemName +
                        ". Custom equipment from Solasta Expanded Equipment. " +
                        "Base armor: " + baseArmorSourceId +
                        ". Appearance source: " + appearanceSourceId +
                        ". Base AC: " + sourceArmorClass +
                        "; enhanced AC: " + (sourceArmorClass + tier) + ".";
                    SetField(item, "guiPresentation", gui);

                    itemDb.Add(item);

                    if (itemDb.GetElement(itemId, true) == null)
                    {
                        success = false;
                        mod.Logger.Error(
                            "[SolastaExpandedEquipmentMod] Registration verification failed: " +
                            itemName);
                        continue;
                    }

                    mod.Logger.Log(
                        "[SolastaExpandedEquipmentMod] Registered " + itemName +
                        " | BaseArmor=" + baseArmorSourceId +
                        " | AppearanceSource=" + appearanceSourceId +
                        " | EnhancedAC=" + (sourceArmorClass + tier));
                }
                catch (Exception ex)
                {
                    success = false;
                    mod.Logger.Error(
                        "[SolastaExpandedEquipmentMod] Failed to register " +
                        itemName + ": " + ex);
                }
            }

            return success;
        }

        private static int ReadArmorClass(ArmorDescription armor, int fallbackValue)
        {
            if (armor == null)
                return fallbackValue;

            Type type = armor.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    "armorClassValue",
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null && field.FieldType == typeof(int))
                    return (int)field.GetValue(armor);

                PropertyInfo property = type.GetProperty(
                    "ArmorClassValue",
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (property != null && property.PropertyType == typeof(int) &&
                    property.CanRead)
                    return (int)property.GetValue(armor, null);

                type = type.BaseType;
            }

            return fallbackValue;
        }

        internal static bool SetField(object target, string fieldName, object value)
        {
            if (target == null)
                return false;

            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null)
                {
                    if (field.IsInitOnly)
                        return false;

                    field.SetValue(target, value);
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }
    }
}
