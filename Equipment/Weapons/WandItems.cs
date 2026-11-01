using System;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Registers magical wands independently from rings. Keeping each equipment
    /// type in its own class lets the item-picker issue be isolated by category.
    /// </summary>
    public static class WandItems
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";

        // Individual wand isolation switches. Start with Death's Door only;
        // add one wand at a time after the item-picker test passes.
        private const bool ENABLE_WAND_DEATHS_DOOR = true;
        private const bool ENABLE_WAND_CONE_OF_COLD = true;
        private const bool ENABLE_WAND_RAY_OF_ENFEEBLEMENT = true;
        private const bool ENABLE_WAND_CHROMATIC_ORB = true;
        private const bool ENABLE_WAND_ACID_ARROW = true;

        public static bool Register()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            if (itemDb == null)
            {
                Debug.LogError(LogPrefix + "Wand registration failed: item database unavailable.");
                return false;
            }

            bool success = RegisterSpellWands(itemDb);
            Debug.Log(LogPrefix + "Wand registration results: " + success + ".");
            return success;
        }

        private static bool RegisterSpellWands(Database<ItemDefinition> itemDb)
        {
            bool anyEnabled = false;
            bool success = true;

            if (ENABLE_WAND_DEATHS_DOOR)
            {
                anyEnabled = true;
                success = RegisterSpellWand(itemDb, "WandOfDeathsDoor", "Wand of Death's Door",
                    "Revivify", 5, "VeryRare") && success;
            }

            if (ENABLE_WAND_CONE_OF_COLD)
            {
                anyEnabled = true;
                success = RegisterSpellWand(itemDb, "WandOfConeOfCold", "Wand of Cone of Cold",
                    "ConeOfCold", 3, "VeryRare") && success;
            }

            if (ENABLE_WAND_RAY_OF_ENFEEBLEMENT)
            {
                anyEnabled = true;
                success = RegisterSpellWand(itemDb, "WandOfRayOfEnfeeblement", "Wand of Ray of Enfeeblement",
                    "RayOfEnfeeblement", 5, "Uncommon") && success;
            }

            if (ENABLE_WAND_CHROMATIC_ORB)
            {
                anyEnabled = true;
                success = RegisterSpellWand(itemDb, "WandOfChromaticOrb", "Wand of Chromatic Orb",
                    "ChromaticOrb", 5, "Uncommon") && success;
            }

            if (ENABLE_WAND_ACID_ARROW)
            {
                anyEnabled = true;
                success = RegisterSpellWand(itemDb, "WandOfAcidArrow", "Wand of Acid Arrow",
                    "AcidArrow", 5, "Uncommon") && success;
            }

            Debug.Log(LogPrefix + "Wand isolation switches: " +
                "Death's Door=" + ENABLE_WAND_DEATHS_DOOR +
                ", Cone of Cold=" + ENABLE_WAND_CONE_OF_COLD +
                ", Ray of Enfeeblement=" + ENABLE_WAND_RAY_OF_ENFEEBLEMENT +
                ", Chromatic Orb=" + ENABLE_WAND_CHROMATIC_ORB +
                ", Acid Arrow=" + ENABLE_WAND_ACID_ARROW + ".");

            return !anyEnabled || success;
        }

        private static bool RegisterSpellWand(
            Database<ItemDefinition> itemDb,
            string itemId,
            string displayName,
            string spellId,
            int charges,
            string rarity)
        {
            try
            {
                if (itemDb.GetElement(itemId, true) != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition baseWand = RingItems.FindWandTemplate(itemDb);
                if (baseWand == null)
                {
                    Debug.LogError(LogPrefix + "Could not find a usable wand template for " + displayName + ".");
                    return false;
                }

                Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
                if (spellDb == null)
                {
                    Debug.LogError(LogPrefix + "Spell database unavailable for " + displayName + ".");
                    return false;
                }

                SpellDefinition spell = string.Equals(spellId, "ChromaticOrb", StringComparison.OrdinalIgnoreCase)
                    ? ChromaticOrbSpellFactory.GetOrCreate(spellDb)
                    : spellDb.GetElement(spellId, true);
                if (spell == null)
                {
                    Debug.LogError(LogPrefix + "Could not create or find spell '" + spellId + "' for " + displayName + ".");
                    return false;
                }

                ItemDefinition item = UnityEngine.Object.Instantiate(baseWand);
                if (item == null)
                    return false;

                item.ForceName(itemId);
                item.SetUserContentGUID(Guid.NewGuid().ToString());
                item.IsUserContent = true;

                string spellName = spell.GuiPresentation == null
                    ? spellId
                    : (spell.GuiPresentation.Title ?? spellId);
                string description = "A magical wand with " + charges + " charges. Expend 1 charge to cast " +
                    spellName + ". It regains all expended charges at dawn.";

                if (!RingItems.SetPresentation(item, baseWand, displayName, description))
                    Debug.LogWarning(LogPrefix + "Could not completely set the presentation for " + displayName + ".");

                bool magicalSet = RingItems.SetMember(item, "magical", true);
                bool attunementSet = RingItems.SetMember(item, "requiresAttunement", true);
                bool raritySet = RingItems.SetEnumMember(item, "itemRarity", rarity);
                bool deviceFlagSet = RingItems.SetMember(item, "isUsableDevice", true);

                if (!magicalSet || !attunementSet || !raritySet || !deviceFlagSet)
                {
                    Debug.LogError(LogPrefix + "Could not set all wand item fields for " + displayName + ".");
                    return false;
                }

                if (!RingItems.AttachSpellFunction(item, baseWand, spell, "Instantaneous", charges))
                {
                    Debug.LogError(LogPrefix + "Could not configure the spell function for " + displayName + ".");
                    return false;
                }

                itemDb.Add(item);
                bool registered = itemDb.GetElement(itemId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered " + displayName + " with " + charges + " charges, recharging at dawn.");
                else
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");

                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + displayName + ": " + ex);
                return false;
            }
        }
    }
}
