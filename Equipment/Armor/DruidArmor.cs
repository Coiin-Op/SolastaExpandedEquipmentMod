using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class DruidArmor
    {
        public static bool Register(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod)
        {
            return ArmorFactory.RegisterArmorFamily(
                itemDb,
                mod,
                "DruidsArmor",
                "Druid's Armor",
                "LeatherDruid");
        }
    }
}