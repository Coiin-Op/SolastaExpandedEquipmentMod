
using System;
using UnityModManagerNet;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    public static class Main
    {
        private static UnityModManager.ModEntry mod;

        private static bool equipmentRegistered;

        private static bool itemDatabaseReadyLogged;
        private static bool spellDatabaseReadyLogged;
        private static bool conditionDatabaseReadyLogged;
        private static bool waitingForDatabasesLogged;

        private const float DatabaseStartupSettleDelaySeconds = 3f;
        private static float nextEquipmentRegistrationAttemptTime;
        private static float nextEquipmentRegistrationRetryLogTime;

        public static bool Load(UnityModManager.ModEntry entry)
        {
            mod = entry;

            // Start diagnostics before any SEE initializers.
            StartupDiagnostic.Initialize(entry);
            StartupDiagnostic.Checkpoint("Main.Load entered");

            StartupDiagnostic.RunStep(
                "CombatDiagnostic.Initialize",
                () => CombatDiagnostic.Initialize());

            StartupDiagnostic.RunStep(
                "BeastSlayerCombatPatch.Initialize",
                () => BeastSlayerCombatPatch.Initialize());

            StartupDiagnostic.RunStep(
                "QuicknessDaggerAttackPatch.Initialize",
                () => QuicknessDaggerAttackPatch.Initialize());

            // Deliberately disabled during shield-patch isolation.
            // Do not uncomment without a separate controlled test.
            // ShieldCombatPatch.Initialize();

            StartupDiagnostic.RunStep(
                "AmmunitionEffectProxyPatch.Initialize",
                () => AmmunitionEffectProxyPatch.Initialize());

            StartupDiagnostic.RunStep(
                "SpellSlotRingPatch.Initialize",
                () => SpellSlotRingPatch.Initialize());

            StartupDiagnostic.RunStep(
                "ArmorDailyLootPatch.Initialize",
                () => ArmorDailyLootPatch.Initialize());

            StartupDiagnostic.RunStep(
                "ArmorDamageReductionDiagnostic.Initialize",
                () => ArmorDamageReductionDiagnostic.Initialize());

            StartupDiagnostic.RunStep(
                "ActiveEquipmentBonusesTooltip.Initialize",
                () => ActiveEquipmentBonusesTooltip.Initialize());

            entry.OnUpdate = OnUpdate;

            StartupDiagnostic.Checkpoint(
                "OnUpdate callback assigned");

            StartupDiagnostic.RunStep(
                "ArmorRegistration.Initialize",
                () => ArmorRegistration.Initialize(entry));

            mod.Logger.Log(
                "[SolastaExpandedEquipmentMod] Loaded. Waiting for game databases.");

            StartupDiagnostic.Checkpoint("Main.Load completed");

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
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                Database<SpellDefinition> spellDb =
                    DatabaseRepository.GetDatabase<SpellDefinition>();

                Database<ConditionDefinition> conditionDb =
                    DatabaseRepository.GetDatabase<ConditionDefinition>();

                // Log database readiness only when its state changes.
                if (itemDb != null && !itemDatabaseReadyLogged)
                {
                    itemDatabaseReadyLogged = true;

                    StartupDiagnostic.Checkpoint(
                        "ItemDefinition database is READY");
                }

                if (spellDb != null && !spellDatabaseReadyLogged)
                {
                    spellDatabaseReadyLogged = true;

                    StartupDiagnostic.Checkpoint(
                        "SpellDefinition database is READY");
                }

                bool conditionDefinitionsReady = conditionDb != null &&
                    conditionDb.GetElement("ConditionPetrified", true) != null &&
                    conditionDb.GetElement("ConditionBanished", true) != null;

                if (conditionDefinitionsReady && !conditionDatabaseReadyLogged)
                {
                    conditionDatabaseReadyLogged = true;
                    nextEquipmentRegistrationAttemptTime =
                        Time.realtimeSinceStartup + DatabaseStartupSettleDelaySeconds;
                    StartupDiagnostic.Checkpoint(
                        "ConditionDefinition database is POPULATED; delaying custom registration for " +
                        DatabaseStartupSettleDelaySeconds + " seconds for database startup to settle");
                }

                // Wait until the definitions used by equipment registration are populated.
                if (itemDb == null || spellDb == null || !conditionDefinitionsReady)
                {
                    if (!waitingForDatabasesLogged)
                    {
                        waitingForDatabasesLogged = true;

                        StartupDiagnostic.Checkpoint(
                            "Waiting for game databases");
                    }

                    return;
                }

                if (Time.realtimeSinceStartup <
                    nextEquipmentRegistrationAttemptTime)
                {
                    return;
                }

                // Retry registration every two seconds.
                nextEquipmentRegistrationAttemptTime =
                    Time.realtimeSinceStartup + 2f;

                StartupDiagnostic.Checkpoint(
                    "EquipmentRegistry.RegisterAll BEGIN");

                bool registrationComplete =
                    EquipmentRegistry.RegisterAll();

                if (!registrationComplete)
                {
                    if (Time.realtimeSinceStartup >=
                        nextEquipmentRegistrationRetryLogTime)
                    {
                        StartupDiagnostic.Checkpoint(
                            "Equipment registration incomplete; retry scheduled");

                        nextEquipmentRegistrationRetryLogTime =
                            Time.realtimeSinceStartup + 10f;
                    }

                    return;
                }

                equipmentRegistered = true;

                mod.Logger.Log(
                    "[SolastaExpandedEquipmentMod] Equipment registration completed.");

                StartupDiagnostic.Checkpoint(
                    "Equipment registration completed successfully");
            }
            catch (Exception ex)
            {
                StartupDiagnostic.LogException(
                    "Main.OnUpdate", ex);

                mod.Logger.Error(
                    "[SolastaExpandedEquipmentMod] Initialization error: " + ex);
            }
        }
    }
}
