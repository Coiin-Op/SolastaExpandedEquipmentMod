using System;
using System.Reflection;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class ArmorFactory
    {
        public static bool RegisterArmorFamily(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod,
            string familyId,
            string familyName,
            string appearanceSourceId)
        {
            if (itemDb == null || mod == null)
                return false;

            ItemDefinition baseSource =
                itemDb.GetElement("ClothesWizard", true);

            ItemDefinition appearanceSource =
                itemDb.GetElement(appearanceSourceId, true);

            if (baseSource == null ||
                baseSource.ArmorDescription == null ||
                baseSource.GuiPresentation == null ||
                baseSource.ItemPresentation == null)
            {
                mod.Logger.Warning(
                    "[SolastaExpandedEquipmentMod] Base armor source is unavailable.");

                return false;
            }

            if (appearanceSource == null ||
                appearanceSource.GuiPresentation == null ||
                appearanceSource.ItemPresentation == null)
            {
                mod.Logger.Warning(
                    "[SolastaExpandedEquipmentMod] Appearance source unavailable: " +
                    appearanceSourceId);

                return false;
            }

            bool success = true;

            for (int tier = 1; tier <= 3; tier++)
            {
                string itemId = familyId + "Plus" + tier;
                string itemName = familyName + " +" + tier;

                try
                {
                    ItemDefinition existing =
                        itemDb.GetElement(itemId, true);

                    if (existing != null)
                    {
                        mod.Logger.Log(
                            "[SolastaExpandedEquipmentMod] Already registered: " +
                            itemName);

                        continue;
                    }

                    ItemDefinition item =
                        UnityEngine.Object.Instantiate(baseSource);

                    item.ForceName(itemId);
                    item.SetUserContentGUID(Guid.NewGuid().ToString());

                    ItemPresentation presentation =
                        new ItemPresentation(appearanceSource.ItemPresentation);

                    if (familyId == "WizardsAdventuringGarb")
                    {
                        SetField(
                            presentation,
                            "useArmorAddressableName",
                            true);

                        SetField(
                            presentation,
                            "armorAddressableName",
                            "ClothesWizard");
                    }
                    else if (familyId == "WarlocksArmor" ||
                             familyId == "DruidsArmor")
                    {
                        SetField(
                            presentation,
                            "useArmorAddressableName",
                            true);

                        SetField(
                            presentation,
                            "armorAddressableName",
                            appearanceSource.Name);
                    }

                    SetField(
                        presentation,
                        "isArmorAddressableNameGenderSpecific",
                        false);

                    SetField(
                        item,
                        "itemPresentation",
                        presentation);

                    ArmorDescription armor =
                        new ArmorDescription(baseSource.ArmorDescription);

                    SetField(
                        armor,
                        "armorClassValue",
                        12 + tier);

                    SetField(
                        item,
                        "armorDefinition",
                        armor);

                    GuiPresentation gui =
                        new GuiPresentation(appearanceSource.GuiPresentation);

                    gui.Title = itemName;
                    gui.Description =
                        itemName +
                        ". Custom equipment from Solasta Expanded Equipment. " +
                        "Appearance source: " + appearanceSourceId +
                        ". Base AC: " + (12 + tier) + ".";

                    SetField(
                        item,
                        "guiPresentation",
                        gui);

                    itemDb.Add(item);

                    ItemDefinition registered =
                        itemDb.GetElement(itemId, true);

                    if (registered == null)
                    {
                        success = false;

                        mod.Logger.Error(
                            "[SolastaExpandedEquipmentMod] Registration verification failed: " +
                            itemName);

                        continue;
                    }

                    mod.Logger.Log(
                        "[SolastaExpandedEquipmentMod] Registered " +
                        itemName +
                        " | AppearanceSource=" + appearanceSourceId +
                        " | Base AC=" + (12 + tier));
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

        internal static bool SetField(
            object target,
            string fieldName,
            object value)
        {
            if (target == null)
                return false;

            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

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
