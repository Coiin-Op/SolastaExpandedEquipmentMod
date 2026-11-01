using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Builds a self-contained Chromatic Orb spell bundle using vanilla Solasta
    /// spell definitions as templates. No Unfinished Business definitions are used.
    /// </summary>
    internal static class ChromaticOrbSpellFactory
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";
        private const string ParentSpellId = "SEEChromaticOrb";

        private sealed class OrbVariant
        {
            public readonly string Id;
            public readonly string Title;
            public readonly string DamageType;
            public readonly string ParticleSpellId;

            public OrbVariant(string id, string title, string damageType, string particleSpellId)
            {
                Id = id;
                Title = title;
                DamageType = damageType;
                ParticleSpellId = particleSpellId;
            }
        }

        private static readonly OrbVariant[] Variants =
        {
            // DamageType values must match DamageDefinition database element names exactly.
            new OrbVariant("SEEChromaticOrbAcid", "Acid Orb", "DamageAcid", "AcidSplash"),
            new OrbVariant("SEEChromaticOrbCold", "Cold Orb", "DamageCold", "RayOfFrost"),
            new OrbVariant("SEEChromaticOrbFire", "Fire Orb", "DamageFire", "FireBolt"),
            new OrbVariant("SEEChromaticOrbLightning", "Lightning Orb", "DamageLightning", "ShockingGrasp"),
            new OrbVariant("SEEChromaticOrbPoison", "Poison Orb", "DamagePoison", "PoisonSpray"),
            new OrbVariant("SEEChromaticOrbThunder", "Thunder Orb", "DamageThunder", "Thunderwave")
        };

        internal static SpellDefinition GetOrCreate(Database<SpellDefinition> spellDb)
        {
            if (spellDb == null)
            {
                Debug.LogError(LogPrefix + "Chromatic Orb could not be built: spell database unavailable.");
                return null;
            }

            SpellDefinition attackTemplate = spellDb.GetElement("FireBolt", true);
            SpellDefinition bundleTemplate = spellDb.GetElement("BestowCurse", true);
            if (attackTemplate == null || bundleTemplate == null)
            {
                Debug.LogError(LogPrefix + "Chromatic Orb needs the vanilla FireBolt and BestowCurse spell templates.");
                return null;
            }

            List<SpellDefinition> subspells = new List<SpellDefinition>();
            foreach (OrbVariant variant in Variants)
            {
                SpellDefinition particleTemplate = spellDb.GetElement(variant.ParticleSpellId, true);
                if (particleTemplate == null)
                {
                    Debug.LogError(LogPrefix + "Chromatic Orb could not find vanilla particle template " +
                        variant.ParticleSpellId + ".");
                    return null;
                }

                SpellDefinition spell = spellDb.GetElement(variant.Id, true);
                bool isNew = spell == null;
                if (isNew)
                {
                    spell = UnityEngine.Object.Instantiate(attackTemplate);
                    if (spell == null)
                    {
                        Debug.LogError(LogPrefix + "Chromatic Orb could not clone the FireBolt template for " + variant.Title + ".");
                        return null;
                    }

                    spell.ForceName(variant.Id);
                    spell.SetUserContentGUID(Guid.NewGuid().ToString());
                    spell.IsUserContent = true;
                }

                if (!ConfigureVariant(spell, attackTemplate, particleTemplate, variant))
                {
                    Debug.LogError(LogPrefix + "Chromatic Orb could not configure the " + variant.Title + " variant.");
                    return null;
                }

                if (isNew)
                {
                    spellDb.Add(spell);
                    spell = spellDb.GetElement(variant.Id, true) ?? spell;
                }

                subspells.Add(spell);
            }

            SpellDefinition parent = spellDb.GetElement(ParentSpellId, true);
            bool parentIsNew = parent == null;
            if (parentIsNew)
            {
                // Bestow Curse is a vanilla spell bundle. Replacing its variants lets
                // Solasta retain its native subspell-selection behavior.
                parent = UnityEngine.Object.Instantiate(bundleTemplate);
                if (parent == null)
                {
                    Debug.LogError(LogPrefix + "Chromatic Orb could not clone the vanilla spell-bundle template.");
                    return null;
                }

                parent.ForceName(ParentSpellId);
                parent.SetUserContentGUID(Guid.NewGuid().ToString());
                parent.IsUserContent = true;
            }

            if (!ConfigureParent(parent, attackTemplate, subspells))
            {
                Debug.LogError(LogPrefix + "Chromatic Orb parent spell bundle could not be configured.");
                return null;
            }

            if (parentIsNew)
            {
                spellDb.Add(parent);
                parent = spellDb.GetElement(ParentSpellId, true) ?? parent;
            }

            Debug.Log(LogPrefix + "Registered self-contained Chromatic Orb spell bundle with six elemental variants.");
            return parent;
        }

        private static bool ConfigureVariant(
            SpellDefinition spell,
            SpellDefinition attackTemplate,
            SpellDefinition particleTemplate,
            OrbVariant variant)
        {
            if (spell == null || attackTemplate == null || attackTemplate.EffectDescription == null)
                return false;

            // EffectDescription is exposed as a read-only property by the game.
            // Give this spell its own copy through the existing reflection helper,
            // then edit that copy rather than assigning to the property directly.
            EffectDescription effectDescription = new EffectDescription();
            effectDescription.Copy(attackTemplate.EffectDescription);
            if (!RingItems.SetMember(spell, "effectDescription", effectDescription))
                return false;

            bool configuredDamage = false;
            foreach (EffectForm form in effectDescription.EffectForms)
            {
                if (form == null || form.FormType != EffectForm.EffectFormType.Damage || form.DamageForm == null)
                    continue;

                form.DamageForm.DamageType = variant.DamageType;
                form.DamageForm.DiceNumber = 3;
                form.DamageForm.DieType = RuleDefinitions.DieType.D8;
                configuredDamage = true;
            }

            if (!configuredDamage)
                return false;

            ConfigureTargeting(effectDescription);
            if (effectDescription.EffectParticleParameters != null &&
                particleTemplate.EffectDescription != null &&
                particleTemplate.EffectDescription.EffectParticleParameters != null)
            {
                effectDescription.EffectParticleParameters.Copy(
                    particleTemplate.EffectDescription.EffectParticleParameters);
            }

            GuiPresentation presentation = particleTemplate.GuiPresentation == null
                ? (spell.GuiPresentation == null ? new GuiPresentation() : new GuiPresentation(spell.GuiPresentation))
                : new GuiPresentation(particleTemplate.GuiPresentation);
            presentation.Title = variant.Title;
            presentation.Description = "Make a ranged spell attack against one creature. On a hit, the orb deals 3d8 " +
                variant.DamageType.Replace("Damage", "").ToLowerInvariant() + " damage.";

            bool guiSet = RingItems.SetMember(spell, "guiPresentation", presentation);
            bool levelSet = RingItems.SetMember(spell, "spellLevel", 1);
            bool bundleSet = RingItems.SetMember(spell, "spellsBundle", false);
            bool implementedSet = RingItems.SetMember(spell, "implemented", true);

            return guiSet && levelSet && bundleSet && implementedSet;
        }

        private static bool ConfigureParent(
            SpellDefinition parent,
            SpellDefinition attackTemplate,
            List<SpellDefinition> subspells)
        {
            if (parent == null || parent.EffectDescription == null || subspells == null || subspells.Count != 6)
                return false;

            ConfigureTargeting(parent.EffectDescription);
            if (parent.EffectDescription.EffectForms != null)
                parent.EffectDescription.EffectForms.Clear();

            if (parent.EffectDescription.EffectParticleParameters != null &&
                attackTemplate.EffectDescription != null &&
                attackTemplate.EffectDescription.EffectParticleParameters != null)
            {
                parent.EffectDescription.EffectParticleParameters.Copy(
                    attackTemplate.EffectDescription.EffectParticleParameters);
            }

            if (parent.SubspellsList == null)
                return false;

            parent.SubspellsList.Clear();
            parent.SubspellsList.AddRange(subspells);

            GuiPresentation presentation = attackTemplate.GuiPresentation == null
                ? new GuiPresentation()
                : new GuiPresentation(attackTemplate.GuiPresentation);
            presentation.Title = "Chromatic Orb";
            presentation.Description = "Choose acid, cold, fire, lightning, poison, or thunder. Make a ranged spell attack " +
                "against one creature within 12 cells. On a hit, the target takes 3d8 damage of the chosen type.";

            bool guiSet = RingItems.SetMember(parent, "guiPresentation", presentation);
            bool levelSet = RingItems.SetMember(parent, "spellLevel", 1);
            // SpellDefinition stores the school definition's database name.
            bool schoolSet = RingItems.SetMember(parent, "schoolOfMagic", "SchoolEvocation");
            bool bundleSet = RingItems.SetMember(parent, "spellsBundle", true);
            bool implementedSet = RingItems.SetMember(parent, "implemented", true);
            bool castingSet = RingItems.SetEnumMember(parent, "castingTime", "Action");
            bool vocalSet = RingItems.SetEnumMember(parent, "vocalSpellSemeType", "Attack");
            bool materialSet = RingItems.SetEnumMember(parent, "materialComponentType", "None");

            return guiSet && levelSet && schoolSet && bundleSet && implementedSet &&
                castingSet && vocalSet && materialSet;
        }

        private static void ConfigureTargeting(EffectDescription effect)
        {
            if (effect == null)
                return;

            RingItems.SetEnumMember(effect, "targetSide", "Enemy");
            RingItems.SetEnumMember(effect, "rangeType", "RangeHit");
            RingItems.SetMember(effect, "rangeParameter", 12);
            RingItems.SetEnumMember(effect, "targetType", "IndividualsUnique");
            RingItems.SetMember(effect, "targetParameter", 1);
            RingItems.SetEnumMember(effect, "durationType", "Instantaneous");
            RingItems.SetMember(effect, "durationParameter", 0);
            RingItems.SetMember(effect, "hasSavingThrow", false);
        }
    }
}
