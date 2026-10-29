using System;
using UnityModManagerNet;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    public static class ArmorRegistration
    {
        private static UnityModManager.ModEntry mod;
        private static bool diagnosticLogged;

        // Track each armor family independently. A successful family is not re-run
        // merely because another family is still failing.
        private static bool wizardRegistered;
        private static bool sorcererRegistered;
        private static bool warlockRegistered;
        private static bool druidRegistered;
        private static bool plateRegistered;
        private static bool clericPlateRegistered;
        private static bool halfPlateRegistered;

        private static bool wizardFailureReported;
        private static bool sorcererFailureReported;
        private static bool warlockFailureReported;
        private static bool druidFailureReported;
        private static bool plateFailureReported;
        private static bool clericPlateFailureReported;
        private static bool halfPlateFailureReported;

        public static void Initialize(UnityModManager.ModEntry entry)
        {
            mod = entry;
        }

        public static bool Register()
        {
            if (mod == null)
                return false;

            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                if (itemDb == null)
                    return false;

                if (!diagnosticLogged)
                {
                    LogAppearanceSources(itemDb);
                    diagnosticLogged = true;
                }

                wizardRegistered = RegisterFamily(
                    itemDb, "Wizard armor", wizardRegistered,
                    ref wizardFailureReported, () => WizardArmor.Register(itemDb, mod));
                sorcererRegistered = RegisterFamily(
                    itemDb, "Sorcerer armor", sorcererRegistered,
                    ref sorcererFailureReported, () => SorcererArmor.Register(itemDb, mod));
                warlockRegistered = RegisterFamily(
                    itemDb, "Warlock armor", warlockRegistered,
                    ref warlockFailureReported, () => WarlockArmor.Register(itemDb, mod));
                druidRegistered = RegisterFamily(
                    itemDb, "Druid armor", druidRegistered,
                    ref druidFailureReported, () => DruidArmor.Register(itemDb, mod));
                plateRegistered = RegisterFamily(
                    itemDb, "Plate armor", plateRegistered,
                    ref plateFailureReported, () => PlateArmor.Register(itemDb, mod));
                clericPlateRegistered = RegisterFamily(
                    itemDb, "Cleric plate armor", clericPlateRegistered,
                    ref clericPlateFailureReported, () => ClericPlateArmor.Register(itemDb, mod));
                halfPlateRegistered = RegisterFamily(
                    itemDb, "Half-plate armor", halfPlateRegistered,
                    ref halfPlateFailureReported, () => HalfPlateArmor.Register(itemDb, mod));

                return wizardRegistered && sorcererRegistered && warlockRegistered &&
                       druidRegistered && plateRegistered && clericPlateRegistered &&
                       halfPlateRegistered;
            }
            catch (Exception ex)
            {
                mod.Logger.Error(
                    "[SolastaExpandedEquipmentMod] Armor registration coordinator error: " + ex);
                return false;
            }
        }

        private static bool RegisterFamily(
            Database<ItemDefinition> itemDb,
            string familyName,
            bool alreadyRegistered,
            ref bool failureReported,
            Func<bool> register)
        {
            if (alreadyRegistered)
                return true;

            try
            {
                bool success = register();
                if (success)
                {
                    failureReported = false;
                    mod.Logger.Log(
                        "[SolastaExpandedEquipmentMod] " + familyName + " registration succeeded.");
                    return true;
                }

                if (!failureReported)
                {
                    mod.Logger.Warning(
                        "[SolastaExpandedEquipmentMod] " + familyName +
                        " registration returned failure; other armor families will continue.");
                    failureReported = true;
                }
            }
            catch (Exception ex)
            {
                if (!failureReported)
                {
                    mod.Logger.Error(
                        "[SolastaExpandedEquipmentMod] " + familyName +
                        " registration threw an exception: " + ex);
                    failureReported = true;
                }
            }

            return false;
        }

        private static void LogAppearanceSources(Database<ItemDefinition> itemDb)
        {
            string[] ids =
            {
                "ClothesWizard",
                "SorcererArmor",
                "Warlock_Armor",
                "LeatherDruid"
            };

            mod.Logger.Log(
                "[SolastaExpandedEquipmentMod] Checking armor appearance sources.");

            foreach (string id in ids)
            {
                try
                {
                    ItemDefinition source = itemDb.GetElement(id, true);
                    if (source == null)
                    {
                        mod.Logger.Warning(
                            "[SolastaExpandedEquipmentMod] Appearance source not found: " + id);
                        continue;
                    }

                    ItemPresentation presentation = source.ItemPresentation;
                    mod.Logger.Log(
                        "[SolastaExpandedEquipmentMod] Appearance source found: " + id +
                        " | Addressable=" +
                        (presentation == null ? "<no presentation>" : presentation.ArmorAddressableName));
                }
                catch (Exception ex)
                {
                    mod.Logger.Warning(
                        "[SolastaExpandedEquipmentMod] Could not inspect " + id + ": " + ex.Message);
                }
            }
        }
    }
}
