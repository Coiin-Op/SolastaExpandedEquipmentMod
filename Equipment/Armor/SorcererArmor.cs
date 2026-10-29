using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class SorcererArmor
    {
        public static bool Register(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod)
        {
            return ArmorFactory.RegisterArmorFamily(
                itemDb,
                mod,
                "SorcerersArmor",
                "Sorcerer's Armor",
                "SorcererArmor");
        }
    }
}