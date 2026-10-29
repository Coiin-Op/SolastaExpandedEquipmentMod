using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Applies the shared daily supplies enchantment after a real long rest.
    /// Any armor family can use the effect by naming its item with an OfPlenty suffix.
    /// </summary>
    internal static class ArmorDailyLootPatch
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";
        private static bool initialized;
        private static bool missingItemsLogged;

        internal static void Initialize()
        {
            if (initialized)
                return;

            try
            {
                Harmony harmony = new Harmony("SolastaExpandedEquipmentMod.ArmorDailyLoot");
                harmony.CreateClassProcessor(typeof(ApplyRestPatch)).Patch();
                harmony.CreateClassProcessor(typeof(CanReceiveShovePatch)).Patch();
                harmony.CreateClassProcessor(typeof(PaladinHalfPlateStrengthCapPatch)).Patch();
                initialized = true;
                Debug.Log(LogPrefix + "Daily armor supplies patch initialized.");
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Could not initialize daily armor supplies patch: " + ex);
            }
        }

        [HarmonyPatch(typeof(RulesetCharacter), "ApplyRest")]
        private static class ApplyRestPatch
        {
            [HarmonyPostfix]
            private static void Postfix(
                RulesetCharacter __instance,
                RuleDefinitions.RestType __0,
                bool __1)
            {
                if (__instance == null || __1 || __0 != RuleDefinitions.RestType.LongRest)
                    return;

                RulesetCharacterHero hero = __instance as RulesetCharacterHero;
                if (hero == null || !IsWearingPlentyArmor(hero))
                    return;

                GrantDailySupplies(hero);
            }
        }

        [HarmonyPatch(typeof(RulesetCharacter), "get_CanReceiveShove")]
        private static class CanReceiveShovePatch
        {
            [HarmonyPostfix]
            private static void Postfix(RulesetCharacter __instance, ref bool __result)
            {
                RulesetCharacterHero hero = __instance as RulesetCharacterHero;
                if (hero != null && IsWearingArmorWithSuffix(hero, "OfLightningResistance"))
                    __result = false;
            }
        }

        private static bool IsWearingPlentyArmor(RulesetCharacterHero hero)
        {
            return IsWearingArmorWithSuffix(hero, "OfPlenty");
        }

        private static bool IsWearingArmorWithSuffix(RulesetCharacterHero hero, string suffix)
        {
            if (hero.CharacterInventory == null || hero.CharacterInventory.InventorySlotsByName == null)
                return false;

            RulesetInventorySlot torso;
            if (!hero.CharacterInventory.InventorySlotsByName.TryGetValue(
                    EquipmentDefinitions.SlotTypeTorso, out torso) || torso == null || torso.EquipedItem == null)
                return false;

            string itemId = torso.EquipedItem.ItemDefinition == null
                ? null
                : torso.EquipedItem.ItemDefinition.Name;
            return !string.IsNullOrEmpty(itemId) && itemId.EndsWith(suffix, StringComparison.Ordinal);
        }

        private static void GrantDailySupplies(RulesetCharacterHero hero)
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            if (itemDb == null)
                return;

            List<ItemDefinition> components = new List<ItemDefinition>();
            foreach (ItemDefinition item in itemDb.GetAllElements())
            {
                if (item == null || item.ItemTags == null || !item.CanBeStacked)
                    continue;

                if (item.ItemTags.Contains(TagsDefinitions.ItemTagComponent) ||
                    item.ItemTags.Contains(TagsDefinitions.ItemTagIngredient))
                    components.Add(item);
            }

            ItemDefinition ration = FindRation(itemDb);
            if (components.Count == 0 || ration == null)
            {
                if (!missingItemsLogged)
                {
                    Debug.LogWarning(LogPrefix + "Could not find stackable component items and a ration item for the daily supplies effect.");
                    missingItemsLogged = true;
                }
                return;
            }

            for (int i = 0; i < 20; i++)
            {
                ItemDefinition definition = components[UnityEngine.Random.Range(0, components.Count)];
                GrantStack(hero, definition, 1);
            }

            GrantStack(hero, ration, 3);
            Debug.Log(LogPrefix + "Granted 20 random components and 3 food rations to " + hero.Name + " after a long rest.");
        }

        private static ItemDefinition FindRation(Database<ItemDefinition> itemDb)
        {
            ItemDefinition ration = itemDb.GetElement("Ration", true);
            if (ration != null)
                return ration;

            foreach (ItemDefinition item in itemDb.GetAllElements())
            {
                if (item == null || !item.IsFood)
                    continue;

                string id = item.Name ?? string.Empty;
                string title = item.GuiPresentation == null ? string.Empty : item.GuiPresentation.Title ?? string.Empty;
                if (id.IndexOf("Ration", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    title.IndexOf("Ration", StringComparison.OrdinalIgnoreCase) >= 0)
                    return item;
            }
            return null;
        }

        private static void GrantStack(RulesetCharacterHero hero, ItemDefinition definition, int count)
        {
            RulesetItem item = new RulesetItem(definition);
            if (item == null)
                return;

            if (count > 1)
                item.IncreaseStack(count - 1);

            hero.GrantItem(item, false, true, false, true, false);
        }
    }
}
