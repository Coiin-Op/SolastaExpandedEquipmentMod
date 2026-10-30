using System;
using UnityModManagerNet;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    public static class ArmorRegistration
    {
        private static UnityModManager.ModEntry mod;
        private static bool diagnosticLogged;

        // Track each armor family independently so a temporarily unavailable
        // template does not prevent other families from registering.
        private static bool wizardRegistered, sorcererRegistered, warlockRegistered, druidRegistered;
        private static bool plateRegistered, clericPlateRegistered, halfPlateRegistered;
        private static bool breastplateRegistered, chainMailRegistered, chainShirtRegistered;
        private static bool scaleMailRegistered, hideRegistered, studdedLeatherRegistered;
        private static bool paddedRegistered, leatherRegistered;

        private static bool wizardFailureReported, sorcererFailureReported, warlockFailureReported, druidFailureReported;
        private static bool plateFailureReported, clericPlateFailureReported, halfPlateFailureReported;
        private static bool breastplateFailureReported, chainMailFailureReported, chainShirtFailureReported;
        private static bool scaleMailFailureReported, hideFailureReported, studdedLeatherFailureReported;
        private static bool paddedFailureReported, leatherFailureReported;

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
                Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
                if (itemDb == null)
                    return false;

                if (!diagnosticLogged)
                {
                    LogAppearanceSources(itemDb);
                    diagnosticLogged = true;
                }

                wizardRegistered = RegisterFamily(itemDb, "Wizard armor", wizardRegistered,
                    ref wizardFailureReported, () => WizardArmor.Register(itemDb, mod));
                sorcererRegistered = RegisterFamily(itemDb, "Sorcerer armor", sorcererRegistered,
                    ref sorcererFailureReported, () => SorcererArmor.Register(itemDb, mod));
                warlockRegistered = RegisterFamily(itemDb, "Warlock armor", warlockRegistered,
                    ref warlockFailureReported, () => WarlockArmor.Register(itemDb, mod));
                druidRegistered = RegisterFamily(itemDb, "Druid armor", druidRegistered,
                    ref druidFailureReported, () => DruidArmor.Register(itemDb, mod));

                plateRegistered = RegisterFamily(itemDb, "Plate armor", plateRegistered,
                    ref plateFailureReported, () => PlateArmor.Register(itemDb, mod));
                clericPlateRegistered = RegisterFamily(itemDb, "Cleric plate armor", clericPlateRegistered,
                    ref clericPlateFailureReported, () => ClericPlateArmor.Register(itemDb, mod));
                halfPlateRegistered = RegisterFamily(itemDb, "Half-plate armor", halfPlateRegistered,
                    ref halfPlateFailureReported, () => HalfPlateArmor.Register(itemDb, mod));

                breastplateRegistered = RegisterFamily(itemDb, "Breastplate armor", breastplateRegistered,
                    ref breastplateFailureReported, () => BreastplateArmor.Register(itemDb, mod));
                chainMailRegistered = RegisterFamily(itemDb, "Chain mail armor", chainMailRegistered,
                    ref chainMailFailureReported, () => ChainMailArmor.Register(itemDb, mod));
                chainShirtRegistered = RegisterFamily(itemDb, "Chain shirt armor", chainShirtRegistered,
                    ref chainShirtFailureReported, () => ChainShirtArmor.Register(itemDb, mod));
                scaleMailRegistered = RegisterFamily(itemDb, "Scale mail armor", scaleMailRegistered,
                    ref scaleMailFailureReported, () => ScaleMailArmor.Register(itemDb, mod));
                hideRegistered = RegisterFamily(itemDb, "Hide armor", hideRegistered,
                    ref hideFailureReported, () => HideArmor.Register(itemDb, mod));
                studdedLeatherRegistered = RegisterFamily(itemDb, "Studded leather armor", studdedLeatherRegistered,
                    ref studdedLeatherFailureReported, () => StuddedLeatherArmor.Register(itemDb, mod));
                paddedRegistered = RegisterFamily(itemDb, "Padded armor", paddedRegistered,
                    ref paddedFailureReported, () => PaddedArmor.Register(itemDb, mod));
                leatherRegistered = RegisterFamily(itemDb, "Leather armor", leatherRegistered,
                    ref leatherFailureReported, () => LeatherArmor.Register(itemDb, mod));

                return wizardRegistered && sorcererRegistered && warlockRegistered && druidRegistered &&
                       plateRegistered && clericPlateRegistered && halfPlateRegistered &&
                       breastplateRegistered && chainMailRegistered && chainShirtRegistered &&
                       scaleMailRegistered && hideRegistered && studdedLeatherRegistered &&
                       paddedRegistered && leatherRegistered;
            }
            catch (Exception ex)
            {
                mod.Logger.Error("[SolastaExpandedEquipmentMod] Armor registration coordinator error: " + ex);
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
                    mod.Logger.Log("[SolastaExpandedEquipmentMod] " + familyName + " registration succeeded.");
                    return true;
                }

                if (!failureReported)
                {
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] " + familyName +
                        " registration returned failure; other armor families will continue.");
                    failureReported = true;
                }
            }
            catch (Exception ex)
            {
                if (!failureReported)
                {
                    mod.Logger.Error("[SolastaExpandedEquipmentMod] " + familyName +
                        " registration threw an exception: " + ex);
                    failureReported = true;
                }
            }

            return false;
        }

        private static void LogAppearanceSources(Database<ItemDefinition> itemDb)
        {
            string[] ids = { "ClothesWizard", "SorcererArmor", "Warlock_Armor", "LeatherDruid" };
            mod.Logger.Log("[SolastaExpandedEquipmentMod] Checking armor appearance sources.");

            foreach (string id in ids)
            {
                try
                {
                    ItemDefinition source = itemDb.GetElement(id, true);
                    if (source == null)
                    {
                        mod.Logger.Warning("[SolastaExpandedEquipmentMod] Appearance source not found: " + id);
                        continue;
                    }

                    ItemPresentation presentation = source.ItemPresentation;
                    mod.Logger.Log("[SolastaExpandedEquipmentMod] Appearance source found: " + id +
                        " | Addressable=" +
                        (presentation == null ? "<no presentation>" : presentation.ArmorAddressableName));
                }
                catch (Exception ex)
                {
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not inspect " + id + ": " + ex.Message);
                }
            }
        }
    }
}
