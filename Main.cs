using System;
using UnityModManagerNet;

using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    public static class Main
    {
        private static UnityModManager.ModEntry mod;
        private static bool equipmentRegistered;
        private static float nextEquipmentRegistrationAttemptTime;
        private static float nextEquipmentRegistrationRetryLogTime;

        public static bool Load(UnityModManager.ModEntry entry)
        {
            mod = entry;
            CombatDiagnostic.Initialize();
            BeastSlayerCombatPatch.Initialize();
            SpellSlotRingPatch.Initialize();
            ArmorDailyLootPatch.Initialize();
            entry.OnUpdate = OnUpdate;

            // Initialize the armor registration system.
            ArmorRegistration.Initialize(entry);

            mod.Logger.Log(
                "[SolastaExpandedEquipmentMod] Loaded. Waiting for game databases.");

            return true;
        }

        private static void OnUpdate(
            UnityModManager.ModEntry entry,
            float deltaTime)
        {
            if (equipmentRegistered)
                return;

            try
            {
                // The game database may not be ready immediately after loading.
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                Database<SpellDefinition> spellDb =
                    DatabaseRepository.GetDatabase<SpellDefinition>();

                // Wait until both databases exist before registering any equipment.
                if (itemDb == null || spellDb == null)
                    return;

                if (Time.realtimeSinceStartup < nextEquipmentRegistrationAttemptTime)
                    return;

                // Some definitions are added by other mods after the base item database exists.
                // Retry periodically instead of calling every registrar every frame.
                nextEquipmentRegistrationAttemptTime = Time.realtimeSinceStartup + 2f;

                // Register equipment through the central registry.
                if (!EquipmentRegistry.RegisterAll())
                {
                    if (Time.realtimeSinceStartup >= nextEquipmentRegistrationRetryLogTime)
                    {
                        mod.Logger.Log(
                            "[SolastaExpandedEquipmentMod] Registration is incomplete. Will retry in about 2 seconds.");
                        nextEquipmentRegistrationRetryLogTime = Time.realtimeSinceStartup + 10f;
                    }
                    return;
                }

                equipmentRegistered = true;

                mod.Logger.Log(
                    "[SolastaExpandedEquipmentMod] Equipment registration completed.");
            }
            catch (Exception ex)
            {
                mod.Logger.Error(
                    "[SolastaExpandedEquipmentMod] Initialization error: " + ex);
            }
        }
    }
}
