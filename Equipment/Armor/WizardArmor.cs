using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class WizardArmor
    {
        public static bool Register(
            Database<ItemDefinition> itemDb,
            UnityModManager.ModEntry mod)
        {
            bool baseArmorsRegistered = ArmorFactory.RegisterArmorFamily(
                itemDb,
                mod,
                "WizardsAdventuringGarb",
                "Wizard's Adventuring Garb",
                "ClothesWizard");

            bool enchantmentsRegistered =
                ArmorEnchantmentFactory.RegisterWizardGarbVariants(itemDb);

            return baseArmorsRegistered && enchantmentsRegistered;
        }
    }
}
