using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class WarlockArmor
    {
        public static bool Register(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod)
        {
            return ArmorFactory.RegisterArmorFamily(
                itemDb,
                mod,
                "WarlocksArmor",
                "Warlock's Armor",
                "Warlock_Armor");
        }
    }
}