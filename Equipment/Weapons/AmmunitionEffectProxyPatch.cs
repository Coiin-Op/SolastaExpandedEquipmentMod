using System;
using System.Collections.Generic;
using System.Reflection;
using TA;
using HarmonyLib;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Ammunition effect forms run from an attack mode without an active RulesetEffect.
    /// Fog Cloud and Darkness need one to bind their persistent effect proxies.
    /// </summary>
    internal static class AmmunitionEffectProxyPatch
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";
        private static bool initialized;

        internal static void Initialize()
        {
            if (initialized)
                return;

            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(RulesetImplementationManagerLocation), "ApplySummonForm");
                MethodInfo effectFormsTarget = AccessTools.Method(
                    typeof(RulesetImplementationManager), "ApplyEffectForms");
                MethodInfo prefixMethod = typeof(AmmunitionEffectProxyPatch).GetMethod(
                    nameof(BeforeApplySummonForm), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo postfixMethod = typeof(AmmunitionEffectProxyPatch).GetMethod(
                    nameof(AfterApplySummonForm), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo effectFormsPrefix = typeof(AmmunitionEffectProxyPatch).GetMethod(
                    nameof(BeforeApplyEffectForms), BindingFlags.Static | BindingFlags.NonPublic);
                if (target == null || prefixMethod == null || postfixMethod == null ||
                    effectFormsTarget == null || effectFormsPrefix == null)
                {
                    Debug.LogError(LogPrefix + "Could not find the ammo effect-form or summon-form handler.");
                    return;
                }

                Harmony harmony = new Harmony("SolastaExpandedEquipmentMod.AmmunitionEffectProxy");
                harmony.Patch(target,
                        prefix: new HarmonyMethod(prefixMethod),
                        postfix: new HarmonyMethod(postfixMethod));
                harmony.Patch(effectFormsTarget, prefix: new HarmonyMethod(effectFormsPrefix));
                initialized = true;
                Debug.Log(LogPrefix + "Ammunition effect-proxy and random-curse support is active.");
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Could not initialize ammunition effect-proxy support: " + ex);
            }
        }

        private static void BeforeApplyEffectForms(
            List<EffectForm> effectForms,
            ref RulesetImplementationDefinitions.ApplyFormsParams formsParams)
        {
            if (effectForms == null || formsParams.attackMode == null)
                return;

            int markerIndex = -1;
            for (int i = 0; i < effectForms.Count; i++)
            {
                EffectForm form = effectForms[i];
                if (form != null && form.FilterId == SpellDartItems.RandomCurseMarkerFilterId)
                {
                    markerIndex = i;
                    break;
                }
            }

            if (markerIndex < 0)
                return;

            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            SpellDefinition bundle = spellDb == null ? null : spellDb.GetElement("BestowCurse", true);
            if (bundle == null || bundle.SubspellsList == null || bundle.SubspellsList.Count == 0)
            {
                Debug.LogError(LogPrefix + "Could not find Bestow Curse variants for cursed ammunition.");
                return;
            }

            List<SpellDefinition> variants = new List<SpellDefinition>();
            foreach (SpellDefinition variant in bundle.SubspellsList)
                if (variant != null && variant.EffectDescription != null &&
                    variant.EffectDescription.EffectForms != null && variant.EffectDescription.EffectForms.Count > 0)
                    variants.Add(variant);

            if (variants.Count == 0)
                return;

            SpellDefinition selected = variants[UnityEngine.Random.Range(0, variants.Count)];
            List<EffectForm> selectedForms = new List<EffectForm>();
            foreach (EffectForm form in selected.EffectDescription.EffectForms)
            {
                EffectForm copy = EffectForm.GetCopy(form);
                if (copy != null)
                    selectedForms.Add(copy);
            }

            if (selectedForms.Count == 0)
                return;

            effectForms.RemoveAt(markerIndex);
            effectForms.InsertRange(markerIndex, selectedForms);
            Debug.Log(LogPrefix + "Cursed ammunition selected " + selected.Name + " on hit.");
        }

        private static void BeforeApplySummonForm(
            RulesetImplementationManagerLocation __instance,
            EffectForm effectForm,
            ref RulesetImplementationDefinitions.ApplyFormsParams formsParams)
        {
            if (formsParams.activeEffect != null || formsParams.attackMode == null ||
                formsParams.sourceCharacter == null || effectForm == null ||
                effectForm.FormType != EffectForm.EffectFormType.Summon || effectForm.SummonForm == null ||
                effectForm.SummonForm.SummonType != SummonForm.Type.EffectProxy)
                return;

            string proxyName = effectForm.SummonForm.EffectProxyDefinitionName;
            string spellName;
            if (string.Equals(proxyName, "AmmoProxyDarkness", StringComparison.Ordinal) ||
                string.Equals(proxyName, "ProxyDarkness", StringComparison.Ordinal))
                spellName = "Darkness";
            else if (string.Equals(proxyName, "AmmoProxyFogCloud", StringComparison.Ordinal) ||
                string.Equals(proxyName, "ProxyFogCloud", StringComparison.Ordinal))
                spellName = "FogCloud";
            else
                return;

            try
            {
                GameLocationCharacter hitTarget = formsParams.targetCharacter == null
                    ? null : GameLocationCharacter.GetFromActor(formsParams.targetCharacter);
                if (hitTarget == null)
                {
                    Debug.LogWarning(LogPrefix + "Could not find the impact target's map position for " +
                        spellName + ". Skipping its effect proxy.");
                    return;
                }

                formsParams.position = hitTarget.LocationPosition;

                Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
                SpellDefinition spell = spellDb == null ? null : spellDb.GetElement(spellName, true);
                if (spell == null)
                {
                    Debug.LogError(LogPrefix + "Could not find spell " + spellName +
                        " to create the ammunition effect proxy context.");
                    return;
                }

                RulesetEffectSpell proxyContext =
                    __instance.InstantiateEffectRitual(formsParams.sourceCharacter, spell, false);
                if (proxyContext == null)
                {
                    Debug.LogError(LogPrefix + "Could not create the " + spellName +
                        " ammunition effect proxy context.");
                    return;
                }

                formsParams.activeEffect = proxyContext;
                Debug.Log(LogPrefix + "Created a " + spellName +
                    " effect context for the ammunition impact proxy at " +
                    formsParams.position + ".");
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Could not create the " + spellName +
                    " ammunition effect proxy context: " + ex);
            }
        }

        private static void AfterApplySummonForm(
            EffectForm effectForm,
            ref RulesetImplementationDefinitions.ApplyFormsParams formsParams)
        {
            if (effectForm == null || effectForm.FormType != EffectForm.EffectFormType.Summon ||
                effectForm.SummonForm == null || formsParams.attackMode == null)
                return;

            string proxyName = effectForm.SummonForm.EffectProxyDefinitionName;
            if (!proxyName.StartsWith("AmmoProxy", StringComparison.Ordinal))
                return;

            RulesetEffect activeEffect = formsParams.activeEffect;
            if (activeEffect != null)
            {
                try
                {
                    IGameLocationEnvironmentService environment =
                        ServiceRepository.GetService<IGameLocationEnvironmentService>();
                    if (environment == null)
                    {
                        Debug.LogWarning(LogPrefix + "Could not register the " + proxyName +
                            " visual effect because the location environment service is unavailable.");
                    }
                    else
                    {
                        bool alreadyRegistered = false;
                        if (environment.PersonalGameLocationEffects != null)
                        {
                            foreach (GameLocationEffect registeredEffect in environment.PersonalGameLocationEffects)
                            {
                                if (registeredEffect != null && registeredEffect.RulesetEffect != null &&
                                    registeredEffect.RulesetEffect.Guid == activeEffect.Guid)
                                {
                                    alreadyRegistered = true;
                                    break;
                                }
                            }
                        }

                        if (!alreadyRegistered)
                            environment.RegisterPersonalEffect(activeEffect);

                        GameLocationEffect locationEffect = activeEffect.EntityImplementation as GameLocationEffect;
                        if (locationEffect != null)
                        {
                            locationEffect.Position = formsParams.position;
                            locationEffect.Position2 = formsParams.position;
                            locationEffect.SourceOriginalPosition = formsParams.position;
                        }

                        // Ammo hits apply their forms directly and skip CharacterActionMagicEffect's
                        // ongoing-area update. Reuse the game's own covered-cell calculation and fire the
                        // registered visual listener so the spell's persistent cell particles are created.
                        MethodInfo computeCoveredCells = environment.GetType().GetMethod(
                            "ComputeCoveredCells", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (computeCoveredCells == null)
                        {
                            Debug.LogWarning(LogPrefix + "Could not find the covered-cell calculator for " + proxyName + ".");
                        }
                        else
                        {
                            HashSet<int3> coveredCells = new HashSet<int3>();
                            HashSet<int3> emissiveBorderCells = new HashSet<int3>();
                            computeCoveredCells.Invoke(environment,
                                new object[] { activeEffect, coveredCells, emissiveBorderCells });
                            activeEffect.Updated?.Invoke(activeEffect, coveredCells, emissiveBorderCells);
                            Debug.Log(LogPrefix + "Created persistent " + proxyName + " spell visuals across " +
                                coveredCells.Count + " covered cell(s); effect was already registered=" +
                                alreadyRegistered + ".");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(LogPrefix + "Could not register the " + proxyName +
                        " visual effect: " + ex);
                }
            }

            int trackedCount = activeEffect == null || activeEffect.TrackedEffectProxyGuids == null
                ? 0 : activeEffect.TrackedEffectProxyGuids.Count;
            if (trackedCount == 0)
            {
                Debug.LogWarning(LogPrefix + proxyName + " summon returned without a tracked effect proxy.");
                return;
            }

            ulong proxyGuid = activeEffect.TrackedEffectProxyGuids[trackedCount - 1];
            RulesetCharacterEffectProxy proxy;
            if (!RulesetEntity.TryGetEntity(proxyGuid, out proxy) || proxy == null)
            {
                Debug.LogWarning(LogPrefix + proxyName + " tracked proxy GUID " + proxyGuid +
                    " could not be resolved.");
                return;
            }

            GameLocationCharacter locationCharacter = GameLocationCharacter.GetFromActor(proxy);
            Debug.Log(LogPrefix + proxyName + " result: emptyPresentation=" +
                proxy.EffectProxyDefinition.IsEmptyPresentation +
                ", hasPresentation=" + proxy.EffectProxyDefinition.HasPresentation +
                ", location=" + (locationCharacter == null ? "<none>" : locationCharacter.LocationPosition.ToString()) +
                ", visible=" + (locationCharacter != null && locationCharacter.Visible) +
                ", selfRegister=" + (locationCharacter != null && locationCharacter.ShouldSelfRegisterInOccupants) + ".");
        }
    }
}
