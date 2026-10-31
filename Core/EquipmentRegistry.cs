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
        private static bool stavesRegistered;
        private static bool ammunitionRegistered;

        private static bool armorFailureReported;
        private static bool weaponsFailureReported;
        private static bool ringsFailureReported;
        private static bool bootsFailureReported;
        private static bool stavesFailureReported;
        private static bool ammunitionFailureReported;

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

            stavesRegistered = RegisterCategory(
                "Staves", stavesRegistered, ref stavesFailureReported,
                () => StaffItems.Register());

            ammunitionRegistered = RegisterCategory(
                "Arrows and bolts", ammunitionRegistered, ref ammunitionFailureReported,
                () => AmmunitionItems.RegisterArrowsAndBolts());

            return armorRegistered &&
                   weaponsRegistered &&
                   ringsRegistered &&
                   bootsRegistered &&
                   stavesRegistered &&
                   ammunitionRegistered;
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
}