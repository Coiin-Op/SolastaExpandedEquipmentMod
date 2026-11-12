
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SolastaExpandedEquipmentMod
{
    public static class WeaponFactory
    {
        public static bool RegisterBeastSlayerLongsword()
        {
            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                if (itemDb == null)
                {
                    Debug.LogError(
                        "[Solasta Expanded Equipment] Item database unavailable.");
                    return false;
                }

                const string itemId = "BeastSlayerLongsword";
                const string displayName = "Beast Slayer Longsword";

                ItemDefinition existing = itemDb.GetElement(itemId, true);

                if (existing != null)
                {
                    Debug.Log(
                        "[Solasta Expanded Equipment] Beast Slayer Longsword already exists.");
                    return true;
                }

                ItemDefinition baseSword = itemDb.GetElement("Longsword", true);

                if (baseSword == null)
                {
                    Debug.LogError(
                        "[Solasta Expanded Equipment] Could not find original Longsword.");
                    return false;
                }

                ItemDefinition sword =
                    UnityEngine.Object.Instantiate(baseSword);

                if (sword == null)
                {
                    return false;
                }

                sword.ForceName(itemId);
                sword.SetUserContentGUID(Guid.NewGuid().ToString());
                sword.IsUserContent = true;

                if (baseSword.GuiPresentation != null)
                {
                    GuiPresentation presentation =
                        new GuiPresentation(baseSword.GuiPresentation);

                    presentation.Title = displayName;
                    presentation.Description =
                        "A magical longsword that grants +1 to attack and damage, " +
                        "with additional +2 bonuses against Beasts.";

                    if (!SetField(sword, "guiPresentation", presentation))
                    {
                        Debug.LogWarning(
                            "[Solasta Expanded Equipment] Could not set sword presentation.");
                    }
                }

                if (!SetField(sword, "magical", true))
                {
                    Debug.LogError(
                        "[Solasta Expanded Equipment] Could not mark sword magical.");
                    return false;
                }

                FeatureDefinitionAttackModifier modifier =
                    CreateAttackModifier(itemId, displayName, 1, baseSword.GuiPresentation);

                if (modifier == null)
                {
                    return false;
                }

                List<ItemPropertyDescription> properties = sword.StaticProperties;

                if (properties == null)
                {
                    properties = new List<ItemPropertyDescription>();

                    if (!SetField(sword, "staticProperties", properties))
                    {
                        Debug.LogError(
                            "[Solasta Expanded Equipment] Could not initialize sword properties.");
                        return false;
                    }
                }

                ItemPropertyDescription property =
                    CreateFeatureProperty(itemDb, baseSword, modifier);

                if (property == null)
                {
                    return false;
                }

                properties.Add(property);
                itemDb.Add(sword);

                if (itemDb.GetElement(itemId, true) == null)
                {
                    Debug.LogError(
                        "[Solasta Expanded Equipment] Sword registration verification failed.");
                    return false;
                }

                Debug.Log(
                    "[Solasta Expanded Equipment] Registered Beast Slayer Longsword.");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[Solasta Expanded Equipment] Beast Slayer Longsword registration failed: " + ex);
                return false;
            }
        }
        public static bool RegisterBeastSlayerShortsword()
        {
            return RegisterBeastSlayerVariant(
                "Shortsword",
                "BeastSlayerShortsword",
                "Beast Slayer Shortsword",
                "shortsword");
        }

        public static bool RegisterBeastSlayerGreatsword()
        {
            return RegisterBeastSlayerVariant(
                "Greatsword",
                "BeastSlayerGreatsword",
                "Beast Slayer Greatsword",
                "greatsword");
        }

        public static bool RegisterFeySlayerShortsword()
        {
            return RegisterFeySlayerVariant(
                "Shortsword",
                "FeySlayerShortsword",
                "Fey Slayer Shortsword",
                "shortsword");
        }

        public static bool RegisterFeySlayerDagger()
        {
            return RegisterFeySlayerVariant(
                "Dagger",
                "FeySlayerDagger",
                "Fey Slayer Dagger",
                "dagger");
        }

        public static bool RegisterMagicalSlayerBow()
        {
            return RegisterMagicalSlayerVariant(
                "Longbow",
                "MagicalSlayerBow",
                "Magical Slayer Bow",
                "longbow");
        }

        private static bool RegisterMagicalSlayerVariant(
            string baseWeaponId,
            string itemId,
            string displayName,
            string weaponType)
        {
            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                if (itemDb == null)
                {
                    Debug.LogError(LogPrefix + "Item database unavailable for " + displayName + ".");
                    return false;
                }

                ItemDefinition existing = itemDb.GetElement(itemId, true);
                if (existing != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition baseWeapon = itemDb.GetElement(baseWeaponId, true);
                if (baseWeapon == null)
                {
                    Debug.LogError(LogPrefix + "Could not find original " + baseWeaponId + " for " + displayName + ".");
                    return false;
                }

                ItemDefinition weapon = UnityEngine.Object.Instantiate(baseWeapon);
                if (weapon == null)
                {
                    Debug.LogError(LogPrefix + "Could not clone " + displayName + ".");
                    return false;
                }

                weapon.ForceName(itemId);
                weapon.SetUserContentGUID(Guid.NewGuid().ToString());
                weapon.IsUserContent = true;

                if (baseWeapon.GuiPresentation != null)
                {
                    GuiPresentation presentation = new GuiPresentation(baseWeapon.GuiPresentation);
                    presentation.Title = displayName;
                    presentation.Description =
                        "A magical " + weaponType + " that grants +1 to attack and damage, " +
                        "with an additional +2 bonus to attack rolls against magical creatures.";

                    if (!SetField(weapon, "guiPresentation", presentation))
                    {
                        Debug.LogWarning(LogPrefix + "Could not set presentation for " + displayName + ".");
                    }
                }

                if (!SetField(weapon, "magical", true))
                {
                    Debug.LogError(LogPrefix + "Could not mark " + displayName + " magical.");
                    return false;
                }

                FeatureDefinitionAttackModifier modifier =
                    CreateAttackModifier(itemId, displayName, 1, baseWeapon.GuiPresentation);
                if (modifier == null)
                {
                    return false;
                }

                List<ItemPropertyDescription> properties = weapon.StaticProperties;
                if (properties == null)
                {
                    properties = new List<ItemPropertyDescription>();
                    if (!SetField(weapon, "staticProperties", properties))
                    {
                        Debug.LogError(LogPrefix + "Could not initialize properties for " + displayName + ".");
                        return false;
                    }
                }

                ItemPropertyDescription property =
                    CreateFeatureProperty(itemDb, baseWeapon, modifier);
                if (property == null)
                {
                    Debug.LogError(LogPrefix + "Could not create the attack/damage property for " + displayName + ".");
                    return false;
                }

                properties.Add(property);
                itemDb.Add(weapon);

                if (itemDb.GetElement(itemId, true) == null)
                {
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");
                    return false;
                }

                Debug.Log(LogPrefix + "Registered " + displayName + ".");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Registration failed for " + displayName + ": " + ex);
                return false;
            }
        }

        private static bool RegisterFeySlayerVariant(
            string baseWeaponId,
            string itemId,
            string displayName,
            string weaponType)
        {
            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                if (itemDb == null)
                {
                    Debug.LogError(LogPrefix + "Item database unavailable for " + displayName + ".");
                    return false;
                }

                ItemDefinition existing = itemDb.GetElement(itemId, true);
                if (existing != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition baseWeapon = itemDb.GetElement(baseWeaponId, true);
                if (baseWeapon == null)
                {
                    Debug.LogError(LogPrefix + "Could not find original " + baseWeaponId + " for " + displayName + ".");
                    return false;
                }

                ItemDefinition weapon = UnityEngine.Object.Instantiate(baseWeapon);
                if (weapon == null)
                {
                    Debug.LogError(LogPrefix + "Could not clone " + displayName + ".");
                    return false;
                }

                weapon.ForceName(itemId);
                weapon.SetUserContentGUID(Guid.NewGuid().ToString());
                weapon.IsUserContent = true;

                if (baseWeapon.GuiPresentation != null)
                {
                    GuiPresentation presentation = new GuiPresentation(baseWeapon.GuiPresentation);
                    presentation.Title = displayName;
                    presentation.Description =
                        "A magical " + weaponType + " that grants +1 to attack and damage, " +
                        "with an additional +2 bonus to attack rolls against Fey.";

                    if (!SetField(weapon, "guiPresentation", presentation))
                    {
                        Debug.LogWarning(LogPrefix + "Could not set presentation for " + displayName + ".");
                    }
                }

                if (!SetField(weapon, "magical", true))
                {
                    Debug.LogError(LogPrefix + "Could not mark " + displayName + " magical.");
                    return false;
                }

                FeatureDefinitionAttackModifier modifier =
                    CreateAttackModifier(itemId, displayName, 1, baseWeapon.GuiPresentation);
                if (modifier == null)
                {
                    return false;
                }

                List<ItemPropertyDescription> properties = weapon.StaticProperties;
                if (properties == null)
                {
                    properties = new List<ItemPropertyDescription>();
                    if (!SetField(weapon, "staticProperties", properties))
                    {
                        Debug.LogError(LogPrefix + "Could not initialize properties for " + displayName + ".");
                        return false;
                    }
                }

                ItemPropertyDescription property =
                    CreateFeatureProperty(itemDb, baseWeapon, modifier);
                if (property == null)
                {
                    Debug.LogError(LogPrefix + "Could not create the attack/damage property for " + displayName + ".");
                    return false;
                }

                properties.Add(property);
                itemDb.Add(weapon);

                if (itemDb.GetElement(itemId, true) == null)
                {
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");
                    return false;
                }

                Debug.Log(LogPrefix + "Registered " + displayName + ".");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Registration failed for " + displayName + ": " + ex);
                return false;
            }
        }

        // Registers one longsword for each requested creature family except Beasts.
        // Beast Slayer Longsword is an established item with a tested ID and remains
        // registered by RegisterBeastSlayerLongsword() above.
        public static bool RegisterCreatureFamilySlayerLongswords()
        {
            return RegisterCreatureFamilySlayerWeapons("Longsword", "longsword");
        }

        public static bool RegisterCreatureFamilySlayerShortswords()
        {
            return RegisterCreatureFamilySlayerWeapons("Shortsword", "shortsword");
        }

        public static bool RegisterCreatureFamilySlayerScimitars()
        {
            return RegisterCreatureFamilySlayerWeapons("Scimitar", "scimitar");
        }

        public static bool RegisterCreatureFamilySlayerRapiers()
        {
            return RegisterCreatureFamilySlayerWeapons("Rapier", "rapier");
        }

        public static bool RegisterCreatureFamilySlayerGreatswords()
        {
            return RegisterCreatureFamilySlayerWeapons("Greatsword", "greatsword");
        }

        public static bool RegisterCreatureFamilySlayerKatanas()
        {
            return RegisterCreatureFamilySlayerWeapons("Katana", "katana");
        }

        public static bool RegisterCreatureFamilySlayerLongbows()
        {
            return RegisterCreatureFamilySlayerWeapons("Longbow", "longbow");
        }

        public static bool RegisterCreatureFamilySlayerShortbows()
        {
            return RegisterCreatureFamilySlayerWeapons("Shortbow", "shortbow");
        }

        public static bool RegisterCreatureFamilySlayerMaces()
        {
            return RegisterCreatureFamilySlayerWeapons("Mace", "mace");
        }

        public static bool RegisterCreatureFamilySlayerWarhammers()
        {
            return RegisterCreatureFamilySlayerWeapons("Warhammer", "warhammer");
        }
        public static bool RegisterCreatureFamilySlayerDaggers()
        {
            bool basicDaggersRegistered =
                RegisterCreatureFamilySlayerWeapons("Dagger", "dagger");
            bool advancedDaggersRegistered =
                RegisterAdvancedCreatureFamilySlayerDaggers();

            return basicDaggersRegistered && advancedDaggersRegistered;
        }

        private static bool RegisterAdvancedCreatureFamilySlayerDaggers()
        {
            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();
                if (itemDb == null)
                {
                    Debug.LogError(LogPrefix + "Item database unavailable for advanced slayer daggers.");
                    return false;
                }

                ItemDefinition baseDagger = itemDb.GetElement("Dagger", true);
                if (baseDagger == null || baseDagger.GuiPresentation == null)
                {
                    Debug.LogError(LogPrefix + "Vanilla Dagger template unavailable for advanced slayer daggers.");
                    return false;
                }

                // These are +2 daggers universally, with +1 more against the
                // named target. The Giants/Fey dagger is one item with two
                // different conditional bonuses. The Dragon dagger is +3 only
                // against Dragons and has no universal attack/damage bonus.
                string[][] specs =
                {
                    new[] { "DaggerPlus2VsGoblinoidsPlus3", "Dagger +2, +3 vs Goblinoids", "Goblinoid", "2" },
                    new[] { "DaggerPlus2VsGiantsPlus3FeyPlus4", "Dagger +2, +3 vs Giants, +4 vs Fey", "Giants and Fey", "2" },
                    new[] { "DaggerPlus2VsDwarvesPlus3", "Dagger +2, +3 vs Dwarves", "Dwarves", "2" },
                    new[] { "DaggerPlus2VsElvesPlus3", "Dagger +2, +3 vs Elves", "Elves", "2" },
                    new[] { "DaggerPlus2VsHalflingsPlus3", "Dagger +2, +3 vs Halflings", "Halflings", "2" },
                    new[] { "DaggerPlus2VsBeastsPlus3", "Dagger +2, +3 vs Beasts", "Beasts", "2" },
                    new[] { "DaggerPlus2VsHumansPlus3", "Dagger +2, +3 vs Humans", "Humans", "2" },
                    new[] { "DaggerPlus2VsUndeadPlus3", "Dagger +2, +3 vs Undead", "Undead", "2" },
                    new[] { "DaggerPlus2VsAbominationsPlus3", "Dagger +2, +3 vs Abominations", "Abominations", "2" },
                    new[] { "DaggerPlus3VsDragons", "Dagger +3 vs Dragons", "Dragons", "0" }
                };

                int succeeded = 0;
                foreach (string[] spec in specs)
                {
                    int baseBonus = int.Parse(spec[3]);
                    if (RegisterAdvancedCreatureFamilySlayerDagger(
                        itemDb, baseDagger, spec[0], spec[1], spec[2], baseBonus))
                    {
                        succeeded++;
                    }
                }

                Debug.Log(LogPrefix + "Advanced creature-family dagger registration complete: " +
                    succeeded + "/" + specs.Length + " registered.");
                return succeeded == specs.Length;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Advanced creature-family dagger registration failed: " + ex);
                return false;
            }
        }

        private static bool RegisterAdvancedCreatureFamilySlayerDagger(
            Database<ItemDefinition> itemDb,
            ItemDefinition baseDagger,
            string itemId,
            string displayName,
            string targetFamily,
            int baseBonus)
        {
            try
            {
                ItemDefinition existing = itemDb.GetElement(itemId, true);
                string description = baseBonus > 0
                    ? "A magical dagger that grants +2 to attack and damage, with an additional +1 to attack and damage against " + targetFamily + "."
                    : "A magical dagger that grants +3 to attack and damage against Dragons.";

                if (existing != null)
                {
                    existing.IsUserContent = true;
                    if (existing.GuiPresentation != null)
                    {
                        GuiPresentation existingGui = new GuiPresentation(existing.GuiPresentation);
                        existingGui.Title = displayName;
                        existingGui.Description = description;
                        SetField(existing, "guiPresentation", existingGui);
                    }
                    Debug.Log(LogPrefix + displayName + " already exists; kept existing definition.");
                    return true;
                }

                ItemDefinition dagger = UnityEngine.Object.Instantiate(baseDagger);
                if (dagger == null)
                {
                    Debug.LogError(LogPrefix + "Could not clone " + displayName + ".");
                    return false;
                }

                dagger.ForceName(itemId);
                dagger.SetUserContentGUID(Guid.NewGuid().ToString());
                dagger.IsUserContent = true;

                GuiPresentation presentation = new GuiPresentation(baseDagger.GuiPresentation);
                presentation.Title = displayName;
                presentation.Description = description;
                if (!SetField(dagger, "guiPresentation", presentation) ||
                    !SetField(dagger, "magical", true))
                {
                    Debug.LogError(LogPrefix + "Could not configure " + displayName + ".");
                    return false;
                }

                if (baseBonus > 0)
                {
                    FeatureDefinitionAttackModifier modifier =
                        CreateAttackModifier(itemId, displayName, baseBonus, baseDagger.GuiPresentation);
                    if (modifier == null)
                        return false;

                    List<ItemPropertyDescription> properties = dagger.StaticProperties;
                    if (properties == null)
                    {
                        properties = new List<ItemPropertyDescription>();
                        if (!SetField(dagger, "staticProperties", properties))
                            return false;
                    }

                    ItemPropertyDescription property =
                        CreateFeatureProperty(itemDb, baseDagger, modifier);
                    if (property == null)
                        return false;
                    properties.Add(property);
                }

                itemDb.Add(dagger);
                if (itemDb.GetElement(itemId, true) == null)
                {
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");
                    return false;
                }

                Debug.Log(LogPrefix + "Registered " + displayName +
                    " | BaseBonus=+" + baseBonus + " | ConditionalBonus=" +
                    (baseBonus > 0 ? "+1 vs target" : "+3 vs Dragons"));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + displayName + ": " + ex);
                return false;
            }
        }

        public static bool RegisterAdditionalWeaponBatches()
        {
            string[][] batches =
            {
                new[] { "Greataxe", "greataxe", "Greataxe", "11", "true", "true" },
                new[] { "Maul", "maul", "Maul", "13", "true", "true" },
                new[] { "Battleaxe", "battleaxe", "Battleaxe", "11", "true", "true" },
                new[] { "Handaxe", "handaxe", "Handaxe", "11", "true", "true" },
                new[] { "Halberd", "halberd", "Halberd", "11", "true", "true" },
                new[] { "Pike", "pike", "Pike", "11", "true", "true" },
                new[] { "Long Mace", "longmace", "CELongMace", "11", "true", "true" },
                new[] { "Morningstar", "morningstar", "Morningstar", "11", "true", "true" },
                new[] { "Quarterstaff", "quarterstaff", "Quarterstaff", "11", "true", "true" },
                new[] { "Javelin", "javelin", "Javelin", "11", "true", "true" },
                new[] { "Spear", "spear", "Spear", "11", "true", "true" },
                new[] { "Arrow", "arrow", "Arrow", "11", "false", "true" },
                new[] { "Bolt", "bolt", "Bolt", "11", "false", "true" },
                new[] { "Dart", "dart", "Dart", "11", "false", "false" }
            };

            bool success = true;
            foreach (string[] batch in batches)
            {
                string title = batch[0];
                string weaponType = batch[1];
                string templateId = batch[2];
                int demonicDc = int.Parse(batch[3]);
                bool includeDemonicInfluence = bool.Parse(batch[4]);
                bool includeWeakeningAndPower = bool.Parse(batch[5]);

                bool family = RegisterGeneratedFamilySlayers(title, templateId, weaponType);
                bool magical = RegisterMagicalBows(templateId, weaponType, includeWeakeningAndPower);
                bool advanced = AdvancedLongswordFactory.RegisterGeneratedWeaponSet(
                    title, weaponType, demonicDc, includeDemonicInfluence);
                success &= family && magical && advanced;
            }

            return success;
        }

        private static bool RegisterGeneratedFamilySlayers(
            string title, string templateId, string weaponType)
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            ItemDefinition template = ResolveWeaponTemplate(itemDb, templateId, weaponType);
            if (itemDb == null || template == null || template.GuiPresentation == null)
            {
                Debug.LogError(LogPrefix + "No base template found for " + title + " batch (" + templateId + ").");
                return false;
            }

            string idPrefix = NormalizeItemId(title);
            string[][] targets =
            {
                new[] { "Giants", "Giants" }, new[] { "Aberrations", "Aberrations" },
                new[] { "ArtificerConstructs", "Artificer Constructs" }, new[] { "Beasts", "Beasts" },
                new[] { "Celestials", "Celestials" }, new[] { "Constructs", "Constructs" },
                new[] { "Elementals", "Elementals" }, new[] { "Fey", "Fey" },
                new[] { "Humanoids", "Humanoids" }, new[] { "Monstrosities", "Monstrosities" },
                new[] { "Plants", "Plants" }, new[] { "Undead", "Undead" },
                new[] { "SorrAkkath", "Sorr-Akkath" }, new[] { "Dragons", "Dragons" }
            };

            int succeeded = 0;
            foreach (string[] target in targets)
            {
                if (RegisterCreatureFamilySlayerWeapon(itemDb, template,
                    idPrefix + "Vs" + target[0] + "Plus1",
                    title + " +1, +2 vs " + target[1], target[1], weaponType))
                    succeeded++;
            }

            Debug.Log(LogPrefix + "Creature-family " + title + " registration complete: " +
                succeeded + "/" + targets.Length + " items registered.");
            return succeeded == targets.Length;
        }

        internal static ItemDefinition ResolveWeaponTemplate(
            Database<ItemDefinition> itemDb, string requestedId, string weaponType)
        {
            if (itemDb == null)
                return null;

            string normalized = NormalizeItemId(requestedId);
            string[] candidates =
            {
                requestedId, normalized, "CE" + normalized,
                weaponType == "halberd" ? "CEHalberd" : null,
                weaponType == "pike" ? "CEPike" : null,
                weaponType == "longmace" ? "LongMace" : null,
                weaponType == "longmace" ? "Long Mace" : null
            };
            foreach (string candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate)) continue;
                ItemDefinition item = itemDb.GetElement(candidate, true);
                if (item != null) return item;
            }
            return null;
        }

        internal static string NormalizeItemId(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            System.Text.StringBuilder result = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
                if (char.IsLetterOrDigit(c)) result.Append(c);
            return result.ToString();
        }
        private static bool RegisterCreatureFamilySlayerWeapons(string baseWeaponId, string weaponType)
        {
            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                if (itemDb == null)
                {
                    Debug.LogError(LogPrefix + "Item database unavailable for creature-family longswords.");
                    return false;
                }

                ItemDefinition baseSword = ResolveWeaponTemplate(itemDb, baseWeaponId, weaponType);
                if (baseSword == null && weaponType == "katana")
                {
                    baseSword = itemDb.GetElement("Longsword", true);
                    Debug.LogWarning(LogPrefix + "Katana template not found; using Longsword as the Katana base template.");
                }
                if (baseSword == null || baseSword.GuiPresentation == null)
                {
                    Debug.LogError(LogPrefix + "Vanilla " + weaponType + " template unavailable or incomplete.");
                    return false;
                }

                string[][] specs = weaponType == "warhammer" ? new string[][]
                {
                    new[] { "WarhammerVsGiantsPlus1", "Warhammer +1, +2 vs Giants", "Giants" },
                    new[] { "WarhammerVsAberrationsPlus1", "Warhammer +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "WarhammerVsArtificerConstructsPlus1", "Warhammer +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "WarhammerVsBeastsPlus1", "Warhammer +1, +2 vs Beasts", "Beasts" },
                    new[] { "WarhammerVsCelestialsPlus1", "Warhammer +1, +2 vs Celestials", "Celestials" },
                    new[] { "WarhammerVsConstructsPlus1", "Warhammer +1, +2 vs Constructs", "Constructs" },
                    new[] { "WarhammerVsElementalsPlus1", "Warhammer +1, +2 vs Elementals", "Elementals" },
                    new[] { "WarhammerVsFeyPlus1", "Warhammer +1, +2 vs Fey", "Fey" },
                    new[] { "WarhammerVsHumanoidsPlus1", "Warhammer +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "WarhammerVsMonstrositiesPlus1", "Warhammer +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "WarhammerVsPlantsPlus1", "Warhammer +1, +2 vs Plants", "Plants" },
                    new[] { "WarhammerVsUndeadPlus1", "Warhammer +1, +2 vs Undead", "Undead" },
                    new[] { "WarhammerVsSorrAkkathPlus1", "Warhammer +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "WarhammerVsDragonsPlus1", "Warhammer +1, +2 vs Dragons", "Dragons" }
                } : weaponType == "mace" ? new string[][]
                {
                    new[] { "MaceVsGiantsPlus1", "Mace +1, +2 vs Giants", "Giants" },
                    new[] { "MaceVsAberrationsPlus1", "Mace +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "MaceVsArtificerConstructsPlus1", "Mace +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "MaceVsBeastsPlus1", "Mace +1, +2 vs Beasts", "Beasts" },
                    new[] { "MaceVsCelestialsPlus1", "Mace +1, +2 vs Celestials", "Celestials" },
                    new[] { "MaceVsConstructsPlus1", "Mace +1, +2 vs Constructs", "Constructs" },
                    new[] { "MaceVsElementalsPlus1", "Mace +1, +2 vs Elementals", "Elementals" },
                    new[] { "MaceVsFeyPlus1", "Mace +1, +2 vs Fey", "Fey" },
                    new[] { "MaceVsHumanoidsPlus1", "Mace +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "MaceVsMonstrositiesPlus1", "Mace +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "MaceVsPlantsPlus1", "Mace +1, +2 vs Plants", "Plants" },
                    new[] { "MaceVsUndeadPlus1", "Mace +1, +2 vs Undead", "Undead" },
                    new[] { "MaceVsSorrAkkathPlus1", "Mace +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "MaceVsDragonsPlus1", "Mace +1, +2 vs Dragons", "Dragons" }
                } : weaponType == "longbow" ? new string[][]
                {
                    new[] { "LongbowVsGiantsPlus1", "Longbow +1, +2 vs Giants", "Giants" },
                    new[] { "LongbowVsAberrationsPlus1", "Longbow +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "LongbowVsArtificerConstructsPlus1", "Longbow +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "LongbowVsBeastsPlus1", "Longbow +1, +2 vs Beasts", "Beasts" },
                    new[] { "LongbowVsCelestialsPlus1", "Longbow +1, +2 vs Celestials", "Celestials" },
                    new[] { "LongbowVsConstructsPlus1", "Longbow +1, +2 vs Constructs", "Constructs" },
                    new[] { "LongbowVsElementalsPlus1", "Longbow +1, +2 vs Elementals", "Elementals" },
                    new[] { "LongbowVsFeyPlus1", "Longbow +1, +2 vs Fey", "Fey" },
                    new[] { "LongbowVsHumanoidsPlus1", "Longbow +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "LongbowVsMonstrositiesPlus1", "Longbow +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "LongbowVsPlantsPlus1", "Longbow +1, +2 vs Plants", "Plants" },
                    new[] { "LongbowVsUndeadPlus1", "Longbow +1, +2 vs Undead", "Undead" },
                    new[] { "LongbowVsSorrAkkathPlus1", "Longbow +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "LongbowVsDragonsPlus1", "Longbow +1, +2 vs Dragons", "Dragons" }
                } : weaponType == "shortbow" ? new string[][]
                {
                    new[] { "ShortbowVsGiantsPlus1", "Shortbow +1, +2 vs Giants", "Giants" },
                    new[] { "ShortbowVsAberrationsPlus1", "Shortbow +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "ShortbowVsArtificerConstructsPlus1", "Shortbow +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "ShortbowVsBeastsPlus1", "Shortbow +1, +2 vs Beasts", "Beasts" },
                    new[] { "ShortbowVsCelestialsPlus1", "Shortbow +1, +2 vs Celestials", "Celestials" },
                    new[] { "ShortbowVsConstructsPlus1", "Shortbow +1, +2 vs Constructs", "Constructs" },
                    new[] { "ShortbowVsElementalsPlus1", "Shortbow +1, +2 vs Elementals", "Elementals" },
                    new[] { "ShortbowVsFeyPlus1", "Shortbow +1, +2 vs Fey", "Fey" },
                    new[] { "ShortbowVsHumanoidsPlus1", "Shortbow +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "ShortbowVsMonstrositiesPlus1", "Shortbow +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "ShortbowVsPlantsPlus1", "Shortbow +1, +2 vs Plants", "Plants" },
                    new[] { "ShortbowVsUndeadPlus1", "Shortbow +1, +2 vs Undead", "Undead" },
                    new[] { "ShortbowVsSorrAkkathPlus1", "Shortbow +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "ShortbowVsDragonsPlus1", "Shortbow +1, +2 vs Dragons", "Dragons" }
                } : weaponType == "katana" ? new string[][]
                {
                    new[] { "KatanaVsGiantsPlus1", "Katana +1, +2 vs Giants", "Giants" },
                    new[] { "KatanaVsAberrationsPlus1", "Katana +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "KatanaVsArtificerConstructsPlus1", "Katana +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "KatanaVsBeastsPlus1", "Katana +1, +2 vs Beasts", "Beasts" },
                    new[] { "KatanaVsCelestialsPlus1", "Katana +1, +2 vs Celestials", "Celestials" },
                    new[] { "KatanaVsConstructsPlus1", "Katana +1, +2 vs Constructs", "Constructs" },
                    new[] { "KatanaVsElementalsPlus1", "Katana +1, +2 vs Elementals", "Elementals" },
                    new[] { "KatanaVsFeyPlus1", "Katana +1, +2 vs Fey", "Fey" },
                    new[] { "KatanaVsHumanoidsPlus1", "Katana +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "KatanaVsMonstrositiesPlus1", "Katana +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "KatanaVsPlantsPlus1", "Katana +1, +2 vs Plants", "Plants" },
                    new[] { "KatanaVsUndeadPlus1", "Katana +1, +2 vs Undead", "Undead" },
                    new[] { "KatanaVsSorrAkkathPlus1", "Katana +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "KatanaVsDragonsPlus1", "Katana +1, +2 vs Dragons", "Dragons" }
                } : weaponType == "greatsword" ? new string[][]
                {
                    new[] { "GreatswordVsGiantsPlus1", "Greatsword +1, +2 vs Giants", "Giants" },
                    new[] { "GreatswordVsAberrationsPlus1", "Greatsword +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "GreatswordVsArtificerConstructsPlus1", "Greatsword +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "GreatswordVsBeastsPlus1", "Greatsword +1, +2 vs Beasts", "Beasts" },
                    new[] { "GreatswordVsCelestialsPlus1", "Greatsword +1, +2 vs Celestials", "Celestials" },
                    new[] { "GreatswordVsConstructsPlus1", "Greatsword +1, +2 vs Constructs", "Constructs" },
                    new[] { "GreatswordVsElementalsPlus1", "Greatsword +1, +2 vs Elementals", "Elementals" },
                    new[] { "GreatswordVsFeyPlus1", "Greatsword +1, +2 vs Fey", "Fey" },
                    new[] { "GreatswordVsHumanoidsPlus1", "Greatsword +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "GreatswordVsMonstrositiesPlus1", "Greatsword +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "GreatswordVsPlantsPlus1", "Greatsword +1, +2 vs Plants", "Plants" },
                    new[] { "GreatswordVsUndeadPlus1", "Greatsword +1, +2 vs Undead", "Undead" },
                    new[] { "GreatswordVsSorrAkkathPlus1", "Greatsword +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "GreatswordVsDragonsPlus1", "Greatsword +1, +2 vs Dragons", "Dragons" }
                } : weaponType == "rapier" ? new string[][]
                {
                    new[] { "RapierVsGiantsPlus1", "Rapier +1, +2 vs Giants", "Giants" },
                    new[] { "RapierVsAberrationsPlus1", "Rapier +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "RapierVsArtificerConstructsPlus1", "Rapier +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "RapierVsBeastsPlus1", "Rapier +1, +2 vs Beasts", "Beasts" },
                    new[] { "RapierVsCelestialsPlus1", "Rapier +1, +2 vs Celestials", "Celestials" },
                    new[] { "RapierVsConstructsPlus1", "Rapier +1, +2 vs Constructs", "Constructs" },
                    new[] { "RapierVsElementalsPlus1", "Rapier +1, +2 vs Elementals", "Elementals" },
                    new[] { "RapierVsFeyPlus1", "Rapier +1, +2 vs Fey", "Fey" },
                    new[] { "RapierVsHumanoidsPlus1", "Rapier +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "RapierVsMonstrositiesPlus1", "Rapier +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "RapierVsPlantsPlus1", "Rapier +1, +2 vs Plants", "Plants" },
                    new[] { "RapierVsUndeadPlus1", "Rapier +1, +2 vs Undead", "Undead" },
                    new[] { "RapierVsSorrAkkathPlus1", "Rapier +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "RapierVsDragonsPlus1", "Rapier +1, +2 vs Dragons", "Dragons" }
                } : weaponType == "dagger" ? new string[][]
                {
                    new[] { "DaggerVsGiantsPlus1", "Dagger +1, +2 vs Giants", "Giants" },
                    new[] { "DaggerVsAberrationsPlus1", "Dagger +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "DaggerVsArtificerConstructsPlus1", "Dagger +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "DaggerVsBeastsPlus1", "Dagger +1, +2 vs Beasts", "Beasts" },
                    new[] { "DaggerVsCelestialsPlus1", "Dagger +1, +2 vs Celestials", "Celestials" },
                    new[] { "DaggerVsConstructsPlus1", "Dagger +1, +2 vs Constructs", "Constructs" },
                    new[] { "DaggerVsElementalsPlus1", "Dagger +1, +2 vs Elementals", "Elementals" },
                    new[] { "DaggerVsFeyPlus1", "Dagger +1, +2 vs Fey", "Fey" },
                    new[] { "DaggerVsHumanoidsPlus1", "Dagger +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "DaggerVsMonstrositiesPlus1", "Dagger +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "DaggerVsPlantsPlus1", "Dagger +1, +2 vs Plants", "Plants" },
                    new[] { "DaggerVsUndeadPlus1", "Dagger +1, +2 vs Undead", "Undead" },
                    new[] { "DaggerVsSorrAkkathPlus1", "Dagger +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "DaggerVsDragonsPlus1", "Dagger +1, +2 vs Dragons", "Dragons" }
                } : weaponType == "shortsword" ? new string[][]
                {
                    new[] { "ShortswordVsGiantsPlus1", "Shortsword +1, +2 vs Giants", "Giants" },
                    new[] { "ShortswordVsAberrationsPlus1", "Shortsword +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "ShortswordVsArtificerConstructsPlus1", "Shortsword +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "ShortswordVsBeastsPlus1", "Shortsword +1, +2 vs Beasts", "Beasts" },
                    new[] { "ShortswordVsCelestialsPlus1", "Shortsword +1, +2 vs Celestials", "Celestials" },
                    new[] { "ShortswordVsConstructsPlus1", "Shortsword +1, +2 vs Constructs", "Constructs" },
                    new[] { "ShortswordVsElementalsPlus1", "Shortsword +1, +2 vs Elementals", "Elementals" },
                    new[] { "ShortswordVsFeyPlus1", "Shortsword +1, +2 vs Fey", "Fey" },
                    new[] { "ShortswordVsHumanoidsPlus1", "Shortsword +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "ShortswordVsMonstrositiesPlus1", "Shortsword +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "ShortswordVsPlantsPlus1", "Shortsword +1, +2 vs Plants", "Plants" },
                    new[] { "ShortswordVsUndeadPlus1", "Shortsword +1, +2 vs Undead", "Undead" },
                    new[] { "ShortswordVsSorrAkkathPlus1", "Shortsword +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "ShortswordVsDragonsPlus1", "Shortsword +1, +2 vs Dragons", "Dragons" }
                } : weaponType == "scimitar" ? new string[][]
                {
                    new[] { "ScimitarVsGiantsPlus1", "Scimitar +1, +2 vs Giants", "Giants" },
                    new[] { "ScimitarVsAberrationsPlus1", "Scimitar +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "ScimitarVsArtificerConstructsPlus1", "Scimitar +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "ScimitarVsBeastsPlus1", "Scimitar +1, +2 vs Beasts", "Beasts" },
                    new[] { "ScimitarVsCelestialsPlus1", "Scimitar +1, +2 vs Celestials", "Celestials" },
                    new[] { "ScimitarVsConstructsPlus1", "Scimitar +1, +2 vs Constructs", "Constructs" },
                    new[] { "ScimitarVsElementalsPlus1", "Scimitar +1, +2 vs Elementals", "Elementals" },
                    new[] { "ScimitarVsFeyPlus1", "Scimitar +1, +2 vs Fey", "Fey" },
                    new[] { "ScimitarVsHumanoidsPlus1", "Scimitar +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "ScimitarVsMonstrositiesPlus1", "Scimitar +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "ScimitarVsPlantsPlus1", "Scimitar +1, +2 vs Plants", "Plants" },
                    new[] { "ScimitarVsUndeadPlus1", "Scimitar +1, +2 vs Undead", "Undead" },
                    new[] { "ScimitarVsSorrAkkathPlus1", "Scimitar +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "ScimitarVsDragonsPlus1", "Scimitar +1, +2 vs Dragons", "Dragons" }
                } : new string[][]
                {
                    new[] { "LongswordVsGiantsPlus1", "Longsword +1, +2 vs Giants", "Giants" },
                    new[] { "LongswordVsAberrationsPlus1", "Longsword +1, +2 vs Aberrations", "Aberrations" },
                    new[] { "LongswordVsArtificerConstructsPlus1", "Longsword +1, +2 vs Artificer Constructs", "Artificer Constructs" },
                    new[] { "LongswordVsCelestialsPlus1", "Longsword +1, +2 vs Celestials", "Celestials" },
                    new[] { "LongswordVsConstructsPlus1", "Longsword +1, +2 vs Constructs", "Constructs" },
                    new[] { "LongswordVsElementalsPlus1", "Longsword +1, +2 vs Elementals", "Elementals" },
                    new[] { "LongswordVsFeyPlus1", "Longsword +1, +2 vs Fey", "Fey" },
                    new[] { "LongswordVsHumanoidsPlus1", "Longsword +1, +2 vs Humanoids", "Humanoids" },
                    new[] { "LongswordVsMonstrositiesPlus1", "Longsword +1, +2 vs Monstrosities", "Monstrosities" },
                    new[] { "LongswordVsPlantsPlus1", "Longsword +1, +2 vs Plants", "Plants" },
                    new[] { "LongswordVsUndeadPlus1", "Longsword +1, +2 vs Undead", "Undead" },
                    new[] { "LongswordVsSorrAkkathPlus1", "Longsword +1, +2 vs Sorr-Akkath", "Sorr-Akkath" },
                    new[] { "LongswordVsDragonsPlus1", "Longsword +1, +2 vs Dragons", "Dragons" }
                };

                int succeeded = 0;
                foreach (string[] spec in specs)
                {
                    if (RegisterCreatureFamilySlayerWeapon(
                        itemDb, baseSword, spec[0], spec[1], spec[2], weaponType))
                    {
                        succeeded++;
                    }
                }

                Debug.Log(LogPrefix + "Creature-family " + weaponType + " registration complete: " +
                    succeeded + "/" + specs.Length + " items registered.");

                return succeeded == specs.Length;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Creature-family " + weaponType + " registration failed: " + ex);
                return false;
            }
        }

        private static bool RegisterCreatureFamilySlayerWeapon(
            Database<ItemDefinition> itemDb,
            ItemDefinition baseSword,
            string itemId,
            string displayName,
            string creatureFamily,
            string weaponType)
        {
            try
            {
                ItemDefinition existing = itemDb.GetElement(itemId, true);
                if (existing != null)
                {
                    existing.IsUserContent = true;

                    if (existing.GuiPresentation != null)
                    {
                        GuiPresentation existingGui = new GuiPresentation(existing.GuiPresentation);
                        existingGui.Title = displayName;
                        existingGui.Description =
                            "A magical " + weaponType + " that grants +1 to attack and damage, " +
                            "with an additional +1 to attack and damage against " + creatureFamily +
                            " (for +2 total attack and damage against that family).";
                        SetField(existing, "guiPresentation", existingGui);
                    }

                    Debug.Log(LogPrefix + displayName + " already exists; kept existing definition.");
                    return true;
                }

                ItemDefinition sword = UnityEngine.Object.Instantiate(baseSword);
                if (sword == null)
                {
                    Debug.LogError(LogPrefix + "Could not clone " + displayName + ".");
                    return false;
                }

                sword.ForceName(itemId);
                sword.SetUserContentGUID(Guid.NewGuid().ToString());
                sword.IsUserContent = true;

                GuiPresentation presentation = new GuiPresentation(baseSword.GuiPresentation);
                presentation.Title = displayName;
                presentation.Description =
                    "A magical " + weaponType + " that grants +1 to attack and damage, " +
                    "with an additional +1 to attack and damage against " + creatureFamily +
                    " (for +2 total attack and damage against that family).";

                if (!SetField(sword, "guiPresentation", presentation))
                {
                    Debug.LogError(LogPrefix + "Could not set presentation for " + displayName + ".");
                    return false;
                }

                if (!SetField(sword, "magical", true))
                {
                    Debug.LogError(LogPrefix + "Could not mark " + displayName + " magical.");
                    return false;
                }

                FeatureDefinitionAttackModifier modifier =
                    CreateAttackModifier(itemId, displayName, 1, baseSword.GuiPresentation);
                if (modifier == null)
                    return false;

                List<ItemPropertyDescription> properties = sword.StaticProperties;
                if (properties == null)
                {
                    properties = new List<ItemPropertyDescription>();
                    if (!SetField(sword, "staticProperties", properties))
                    {
                        Debug.LogError(LogPrefix + "Could not initialize properties for " + displayName + ".");
                        return false;
                    }
                }

                ItemPropertyDescription property =
                    CreateFeatureProperty(itemDb, baseSword, modifier);
                if (property == null)
                {
                    Debug.LogError(LogPrefix + "Could not create attack/damage property for " + displayName + ".");
                    return false;
                }

                properties.Add(property);
                itemDb.Add(sword);

                ItemDefinition registered = itemDb.GetElement(itemId, true);
                if (registered == null)
                {
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");
                    return false;
                }

                Debug.Log(LogPrefix + "Registered " + displayName +
                    " | BaseBonus=+1 attack/+1 damage | ConditionalBonus=+1 attack/+1 damage vs " +
                    creatureFamily + " | TotalVsFamily=+2/+2.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + displayName + ": " + ex);
                return false;
            }
        }

        private static bool RegisterBeastSlayerVariant(
            string baseWeaponId,
            string itemId,
            string displayName,
            string weaponType)
        {
            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                if (itemDb == null)
                {
                    Debug.LogError(LogPrefix + "Item database unavailable for " + displayName + ".");
                    return false;
                }

                ItemDefinition existing = itemDb.GetElement(itemId, true);
                if (existing != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition baseWeapon = itemDb.GetElement(baseWeaponId, true);
                if (baseWeapon == null)
                {
                    Debug.LogError(LogPrefix + "Could not find original " + baseWeaponId + " for " + displayName + ".");
                    return false;
                }

                ItemDefinition weapon = UnityEngine.Object.Instantiate(baseWeapon);
                if (weapon == null)
                {
                    Debug.LogError(LogPrefix + "Could not clone " + displayName + ".");
                    return false;
                }

                weapon.ForceName(itemId);
                weapon.SetUserContentGUID(Guid.NewGuid().ToString());
                weapon.IsUserContent = true;

                if (baseWeapon.GuiPresentation != null)
                {
                    GuiPresentation presentation = new GuiPresentation(baseWeapon.GuiPresentation);
                    presentation.Title = displayName;
                    presentation.Description =
                        "A magical " + weaponType + " that grants +1 to attack and damage, " +
                        "with additional +2 bonuses against Beasts.";

                    if (!SetField(weapon, "guiPresentation", presentation))
                    {
                        Debug.LogWarning(LogPrefix + "Could not set presentation for " + displayName + ".");
                    }
                }

                if (!SetField(weapon, "magical", true))
                {
                    Debug.LogError(LogPrefix + "Could not mark " + displayName + " magical.");
                    return false;
                }

                FeatureDefinitionAttackModifier modifier =
                    CreateAttackModifier(itemId, displayName, 1, baseWeapon.GuiPresentation);
                if (modifier == null)
                {
                    return false;
                }

                List<ItemPropertyDescription> properties = weapon.StaticProperties;
                if (properties == null)
                {
                    properties = new List<ItemPropertyDescription>();
                    if (!SetField(weapon, "staticProperties", properties))
                    {
                        Debug.LogError(LogPrefix + "Could not initialize properties for " + displayName + ".");
                        return false;
                    }
                }

                ItemPropertyDescription property =
                    CreateFeatureProperty(itemDb, baseWeapon, modifier);
                if (property == null)
                {
                    Debug.LogError(LogPrefix + "Could not create the attack/damage property for " + displayName + ".");
                    return false;
                }

                properties.Add(property);
                itemDb.Add(weapon);

                if (itemDb.GetElement(itemId, true) == null)
                {
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");
                    return false;
                }

                Debug.Log(LogPrefix + "Registered " + displayName + ".");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Registration failed for " + displayName + ": " + ex);
                return false;
            }
        }

        private const string LogPrefix =
            "[Solasta Expanded Equipment] ";

        public static bool RegisterDarts()
        {
            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                if (itemDb == null)
                {
                    Debug.LogError(
                        LogPrefix + "Item database unavailable.");

                    return false;
                }

                ItemDefinition baseDart =
                    itemDb.GetElement("Dart", true);

                if (baseDart == null)
                {
                    Debug.LogError(
                        LogPrefix + "Could not find the original Dart definition.");

                    return false;
                }

                bool plus1 = RegisterDart(
                    itemDb, baseDart,
                    "ExpandedDartsPlus1", "Darts +1", 1);

                bool plus2 = RegisterDart(
                    itemDb, baseDart,
                    "ExpandedDartsPlus2", "Darts +2", 2);

                bool plus3 = RegisterDart(
                    itemDb, baseDart,
                    "ExpandedDartsPlus3", "Darts +3", 3);

                Debug.Log(
                    LogPrefix +
                    "Dart registration results: " +
                    "+1=" + plus1 + ", " +
                    "+2=" + plus2 + ", " +
                    "+3=" + plus3);

                return plus1 && plus2 && plus3;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    LogPrefix + "Dart registration failed: " + ex);

                return false;
            }
        }

        public static bool RegisterAspNestDart()
        {
            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();

                if (itemDb == null)
                {
                    Debug.LogError(LogPrefix + "Item database unavailable.");
                    return false;
                }

                const string itemId = "AspNestDart";
                const string displayName = "Asp's Nest";

                if (itemDb.GetElement(itemId, true) != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; skipping.");
                    return true;
                }

                ItemDefinition baseDart = itemDb.GetElement("Dart", true);
                if (baseDart == null)
                {
                    Debug.LogError(LogPrefix + "Could not find the original Dart definition.");
                    return false;
                }

                Database<ConditionDefinition> conditionDb =
                    DatabaseRepository.GetDatabase<ConditionDefinition>();
                if (conditionDb == null)
                {
                    Debug.LogError(LogPrefix + "Condition database unavailable for Asp's Nest.");
                    return false;
                }

                // Prefer the game's real Poisoned condition whenever it is available.
                // If the base definition is absent, fall back to a standalone condition
                // assembled from base-game features only. No Unfinished Business API is used.
                ConditionDefinition poisonedCondition =
                    conditionDb.GetElement("ConditionPoisoned", true);

                if (poisonedCondition == null)
                {
                    Debug.LogWarning(
                        LogPrefix + "ConditionPoisoned is not available; creating a standalone poison condition.");
                    poisonedCondition = GetOrCreateAspNestPoisonCondition(
                        conditionDb, baseDart.GuiPresentation);
                }

                if (poisonedCondition == null)
                {
                    Debug.LogError(LogPrefix + "Could not obtain a usable poison condition for Asp's Nest.");
                    return false;
                }

                ItemDefinition dart = UnityEngine.Object.Instantiate(baseDart);
                if (dart == null)
                {
                    Debug.LogError(LogPrefix + "Could not clone the original Dart for Asp's Nest.");
                    return false;
                }

                dart.ForceName(itemId);
                dart.SetUserContentGUID(Guid.NewGuid().ToString());
                dart.IsUserContent = true;

                if (baseDart.GuiPresentation != null)
                {
                    GuiPresentation presentation = new GuiPresentation(baseDart.GuiPresentation);
                    presentation.Title = displayName;
                    presentation.Description =
                        "A +1 magical dart coated in asp venom. On a hit, the target is Poisoned for 1 round.";

                    if (!SetField(dart, "guiPresentation", presentation))
                    {
                        Debug.LogWarning(LogPrefix + "Could not set Asp's Nest presentation.");
                    }
                }

                if (!SetField(dart, "magical", true))
                {
                    Debug.LogError(LogPrefix + "Could not mark Asp's Nest as magical.");
                    return false;
                }

                FeatureDefinitionAttackModifier modifier =
                    CreateAttackModifier(itemId, displayName, 1, baseDart.GuiPresentation);
                if (modifier == null)
                {
                    return false;
                }

                List<ItemPropertyDescription> properties = dart.StaticProperties;
                if (properties == null)
                {
                    properties = new List<ItemPropertyDescription>();
                    if (!SetField(dart, "staticProperties", properties))
                    {
                        Debug.LogError(LogPrefix + "Could not initialize Asp's Nest properties.");
                        return false;
                    }
                }

                ItemPropertyDescription property =
                    CreateFeatureProperty(itemDb, baseDart, modifier);
                if (property == null)
                {
                    return false;
                }
                properties.Add(property);

                if (!AttachPoisonedCondition(dart, poisonedCondition))
                {
                    Debug.LogError(LogPrefix + "Could not attach the poison condition to Asp's Nest.");
                    return false;
                }

                itemDb.Add(dart);
                if (itemDb.GetElement(itemId, true) == null)
                {
                    Debug.LogError(LogPrefix + "Asp's Nest registration verification failed.");
                    return false;
                }

                Debug.Log(LogPrefix + "Registered Asp's Nest: +1 attack/damage, applies Poisoned for 1 round on hit.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Asp's Nest registration failed: " + ex);
                return false;
            }
        }

        private static ConditionDefinition GetOrCreateAspNestPoisonCondition(
            Database<ConditionDefinition> conditionDb,
            GuiPresentation sourcePresentation)
        {
            const string conditionId = "ConditionAspNestPoisoned";

            try
            {
                ConditionDefinition existing = conditionDb.GetElement(conditionId, true);
                if (existing != null)
                {
                    return existing;
                }

                Database<FeatureDefinitionCombatAffinity> combatAffinityDb =
                    DatabaseRepository.GetDatabase<FeatureDefinitionCombatAffinity>();
                FeatureDefinitionCombatAffinity poisonedAttackAffinity = combatAffinityDb == null
                    ? null
                    : combatAffinityDb.GetElement("CombatAffinityPoisoned", true);

                Database<FeatureDefinitionAbilityCheckAffinity> abilityCheckAffinityDb =
                    DatabaseRepository.GetDatabase<FeatureDefinitionAbilityCheckAffinity>();
                FeatureDefinitionAbilityCheckAffinity poisonedAbilityCheckAffinity = abilityCheckAffinityDb == null
                    ? null
                    : abilityCheckAffinityDb.GetElement("AbilityCheckAffinityPoisoned", true);

                if (poisonedAttackAffinity == null || poisonedAbilityCheckAffinity == null)
                {
                    Debug.LogError(
                        LogPrefix + "Could not find the base-game poison features. " +
                        "CombatAffinityPoisoned=" + (poisonedAttackAffinity != null) + ", " +
                        "AbilityCheckAffinityPoisoned=" + (poisonedAbilityCheckAffinity != null) + ".");
                    return null;
                }

                ConditionDefinition condition =
                    ScriptableObject.CreateInstance<ConditionDefinition>();
                if (condition == null)
                {
                    Debug.LogError(LogPrefix + "Could not create ConditionAspNestPoisoned.");
                    return null;
                }

                condition.ForceName(conditionId);
                condition.SetUserContentGUID(Guid.NewGuid().ToString());
                condition.IsUserContent = true;

                List<FeatureDefinition> features = new List<FeatureDefinition>
                {
                    poisonedAttackAffinity,
                    poisonedAbilityCheckAffinity
                };

                bool featuresSet = SetMember(condition, "features", features);
                bool conditionTypeSet = SetEnumMember(condition, "conditionType", "Detrimental");

                // The game expects condition particle references to be non-null, even
                // when a custom condition doesn't use custom particles.
                bool startParticleSet = SetMember(
                    condition, "conditionStartParticleReference", new AssetReference());
                bool particleSet = SetMember(
                    condition, "conditionParticleReference", new AssetReference());
                bool endParticleSet = SetMember(
                    condition, "conditionEndParticleReference", new AssetReference());
                bool recurrentParticleSet = SetMember(
                    condition, "recurrentEffectParticleReference", new AssetReference());
                bool shaderSet = SetMember(
                    condition, "characterShaderReference", new AssetReference());

                GuiPresentation presentation = sourcePresentation == null
                    ? null
                    : new GuiPresentation(sourcePresentation);

                bool presentationSet = false;
                if (presentation != null)
                {
                    presentation.Title = "Poisoned";
                    presentation.Description =
                        "Disadvantage on attack rolls and ability checks.";

                    // Use the game's Poisoned status icon rather than the dart's icon.
                    SetMember(presentation, "spriteReference",
                        new AssetReferenceSprite("4231de4fdbf296c4b85f2afef769631b"));
                    presentationSet = SetMember(condition, "guiPresentation", presentation);
                }

                if (!featuresSet || !conditionTypeSet || !startParticleSet || !particleSet ||
                    !endParticleSet || !recurrentParticleSet || !shaderSet || !presentationSet)
                {
                    Debug.LogError(LogPrefix + "Could not fully configure ConditionAspNestPoisoned.");
                    UnityEngine.Object.Destroy(condition);
                    return null;
                }

                conditionDb.Add(condition);

                ConditionDefinition registered = conditionDb.GetElement(conditionId, true);
                if (registered == null)
                {
                    Debug.LogError(LogPrefix + "ConditionAspNestPoisoned was not added to the condition database.");
                    return null;
                }

                Debug.Log(LogPrefix + "Registered standalone poison condition ConditionAspNestPoisoned.");
                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to create ConditionAspNestPoisoned: " + ex);
                return null;
            }
        }

        private static bool AttachPoisonedCondition(
            ItemDefinition dart,
            ConditionDefinition poisonedCondition)
        {
            try
            {
                object weaponDescription = ReadMemberObject(dart, "WeaponDescription");
                object effectDescription = ReadMemberObject(weaponDescription, "EffectDescription");
                System.Collections.IList effectForms =
                    ReadMemberObject(effectDescription, "EffectForms") as System.Collections.IList;

                if (weaponDescription == null || effectDescription == null)
                {
                    Debug.LogError(LogPrefix + "Asp's Nest cannot access the dart's WeaponDescription/EffectDescription.");
                    return false;
                }

                // Some ammunition templates expose an effect description whose list is null.
                // Initialize that list instead of rejecting the item outright.
                if (effectForms == null)
                {
                    List<EffectForm> newEffectForms = new List<EffectForm>();
                    if (!SetMember(effectDescription, "effectForms", newEffectForms))
                    {
                        Debug.LogError(LogPrefix + "Asp's Nest could not initialize the dart's effect-form list.");
                        return false;
                    }

                    effectForms = newEffectForms;
                    Debug.Log(LogPrefix + "Initialized the dart's previously empty effect-form list for Asp's Nest.");
                }

                object conditionForm = FormatterServices.GetUninitializedObject(typeof(ConditionForm));
                object effectForm = FormatterServices.GetUninitializedObject(typeof(EffectForm));

                bool operationSet = SetEnumMember(
                    conditionForm, "Operation", "Add");
                bool conditionSet = SetMember(
                    conditionForm, "ConditionDefinition", poisonedCondition);
                bool conditionNameSet = SetMember(
                    conditionForm, "conditionDefinitionName", poisonedCondition.Name);
                bool applyToSelfSet = SetMember(conditionForm, "applyToSelf", false);
                bool forceOnSelfSet = SetMember(conditionForm, "forceOnSelf", false);
                object emptyConditionsList = CreateEmptyCollection(
                    conditionForm, "conditionsList", typeof(ConditionDefinition));
                bool conditionsListSet = emptyConditionsList != null && SetMember(
                    conditionForm, "conditionsList", emptyConditionsList);

                bool formTypeSet = SetEnumMember(
                    effectForm, "FormType", "Condition");
                bool conditionFormSet = SetMember(
                    effectForm, "ConditionForm", conditionForm);
                bool createdByCharacterSet = SetMember(
                    effectForm, "createdByCharacter", true);
                bool createdByConditionSet = SetMember(
                    effectForm, "createdByCondition", false);

                if (!operationSet || !conditionSet || !conditionNameSet ||
                    !applyToSelfSet || !forceOnSelfSet || !conditionsListSet ||
                    !formTypeSet || !conditionFormSet || !createdByCharacterSet ||
                    !createdByConditionSet)
                {
                    Debug.LogError(LogPrefix + "Could not configure Asp's Nest poison effect form.");
                    return false;
                }

                // A hit applies the game's normal Poisoned condition for one round.
                // Ending at the source's next turn start gives the condition a clear duration.
                bool durationTypeSet = SetEnumMember(
                    effectDescription, "DurationType", "Round");
                bool durationParameterSet = SetMember(
                    effectDescription, "DurationParameter", 1);
                bool endOfEffectSet = SetEnumMemberFromRuleDefinitions(
                    effectDescription, "EndOfEffect",
                    "TurnOccurenceType", "StartOfTurn");

                if (!durationTypeSet || !durationParameterSet || !endOfEffectSet)
                {
                    Debug.LogError(LogPrefix + "Could not set Asp's Nest poison duration.");
                    return false;
                }

                effectForms.Add(effectForm);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Error building Asp's Nest poison effect: " + ex);
                return false;
            }
        }

        private static object ReadMemberObject(object target, string memberName)
        {
            if (target == null) return null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(memberName, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);

                PropertyInfo property = current.GetProperty(memberName, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }

            return null;
        }

        private static bool SetMember(object target, string memberName, object value)
        {
            if (target == null) return false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(memberName, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    if (field.IsInitOnly) return false;
                    field.SetValue(target, value);
                    return true;
                }

                PropertyInfo property = current.GetProperty(memberName, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetSetMethod(true) != null)
                {
                    property.SetValue(target, value, null);
                    return true;
                }
            }

            return false;
        }

        private static object CreateEmptyCollection(
            object target, string memberName, Type elementType)
        {
            Type memberType = FindMemberType(target, memberName);
            if (memberType == null) return null;

            if (memberType.IsArray)
            {
                return Array.CreateInstance(elementType, 0);
            }

            Type listType = typeof(List<>).MakeGenericType(elementType);
            if (memberType.IsAssignableFrom(listType))
            {
                return Activator.CreateInstance(listType);
            }

            try
            {
                return Activator.CreateInstance(memberType);
            }
            catch
            {
                return null;
            }
        }

        private static Type FindMemberType(object target, string memberName)
        {
            if (target == null) return null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(memberName, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.FieldType;

                PropertyInfo property = current.GetProperty(memberName, flags | BindingFlags.DeclaredOnly);
                if (property != null) return property.PropertyType;
            }

            return null;
        }

        private static bool SetEnumMemberFromRuleDefinitions(
            object target, string memberName, string sourceEnumName, string sourceValueName)
        {
            Type targetMemberType = FindMemberType(target, memberName);
            if (targetMemberType == null || !targetMemberType.IsEnum) return false;

            Type sourceEnumType = typeof(RuleDefinitions).GetNestedType(
                sourceEnumName, BindingFlags.Public | BindingFlags.NonPublic);
            if (sourceEnumType == null || !sourceEnumType.IsEnum) return false;

            object sourceValue = Enum.Parse(sourceEnumType, sourceValueName);
            object targetValue = Enum.ToObject(
                targetMemberType, Convert.ToInt64(sourceValue));
            return SetMember(target, memberName, targetValue);
        }

        private static bool SetEnumMember(object target, string memberName, string enumName)
        {
            if (target == null) return false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(memberName, flags | BindingFlags.DeclaredOnly);
                if (field != null && field.FieldType.IsEnum)
                {
                    return SetMember(target, memberName, Enum.Parse(field.FieldType, enumName));
                }

                PropertyInfo property = current.GetProperty(memberName, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.PropertyType.IsEnum)
                {
                    return SetMember(target, memberName, Enum.Parse(property.PropertyType, enumName));
                }
            }

            return false;
        }

        private static bool RegisterDart(
            Database<ItemDefinition> itemDb,
            ItemDefinition baseDart,
            string itemId,
            string displayName,
            int bonus)
        {
            try
            {
                ItemDefinition existing =
                    itemDb.GetElement(itemId, true);

                if (existing != null)
                {
                    Debug.Log(
                        LogPrefix + displayName +
                        " already exists; skipping.");

                    return true;
                }

                // Clone the original Dart to preserve its weapon data,
                // appearance, icon, range, damage dice, and properties.
                ItemDefinition dart =
                    UnityEngine.Object.Instantiate(baseDart);

                if (dart == null)
                {
                    Debug.LogError(
                        LogPrefix + "Could not clone " + displayName + ".");

                    return false;
                }

                dart.ForceName(itemId);
                dart.SetUserContentGUID(Guid.NewGuid().ToString());
                dart.IsUserContent = true;

                // Keep the original presentation, changing only its text.
                if (baseDart.GuiPresentation != null)
                {
                    GuiPresentation presentation =
                        new GuiPresentation(baseDart.GuiPresentation);

                    presentation.Title = displayName;
                    presentation.Description =
                        displayName +
                        ". A magical dart with +" + bonus +
                        " to attack and damage rolls.";

                    if (!SetField(
                        dart, "guiPresentation", presentation))
                    {
                        Debug.LogWarning(
                            LogPrefix +
                            "Could not replace GUI presentation for " +
                            displayName + ".");
                    }
                }

                // Mark the item itself as magical.
                if (!SetField(dart, "magical", true))
                {
                    Debug.LogError(
                        LogPrefix +
                        "Could not set magical flag for " + displayName + ".");

                    return false;
                }

                // Create the feature that supplies the attack and damage
                // bonuses while the dart is used.
                FeatureDefinitionAttackModifier modifier =
                    CreateAttackModifier(itemId, displayName, bonus, baseDart.GuiPresentation);

                if (modifier == null)
                {
                    return false;
                }

                // Attach the modifier as an item property.
                List<ItemPropertyDescription> properties =
                    dart.StaticProperties;

                if (properties == null)
                {
                    properties = new List<ItemPropertyDescription>();

                    if (!SetField(dart, "staticProperties", properties))
                    {
                        Debug.LogError(
                            LogPrefix +
                            "Could not initialize static properties for " +
                            displayName + ".");

                        return false;
                    }
                }

                ItemPropertyDescription property =
                    CreateFeatureProperty(
                        itemDb, baseDart, modifier);

                if (property == null)
                {
                    Debug.LogError(
                        LogPrefix +
                        "Could not create feature property for " +
                        displayName + ".");

                    return false;
                }

                properties.Add(property);

                // Register the item definition.
                itemDb.Add(dart);

                ItemDefinition registered =
                    itemDb.GetElement(itemId, true);

                if (registered == null)
                {
                    Debug.LogError(
                        LogPrefix +
                        displayName + " was not found after registration.");

                    return false;
                }

                Debug.Log(
                    LogPrefix +
                    "Registered " + displayName +
                    " with +" + bonus +
                    " attack and damage.");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    LogPrefix + "Failed to register " +
                    displayName + ": " + ex);

                return false;
            }
        }


        private const string MagicalLongswordWeakeningConditionId = "ConditionSEELongswordWeakening";
        private static bool magicalLongswordWeakeningPatchInitialized;

        public static bool RegisterMagicalLongswords()
        {
            try
            {
                Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
                Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
                Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
                if (itemDb == null || spellDb == null || conditionDb == null)
                {
                    Debug.LogError(LogPrefix + "Item, spell, or condition database unavailable for magical longswords.");
                    return false;
                }

                if (!InitializeMagicalLongswordWeakeningPatch())
                    return false;

                ConditionDefinition proneCondition = conditionDb.GetElement("ConditionProne", true);
                ConditionDefinition baneCondition = conditionDb.GetElement("ConditionBaned", true);
                ConditionDefinition weakeningCondition = GetOrCreateMagicalLongswordWeakeningCondition(conditionDb);
                if (proneCondition == null || baneCondition == null || weakeningCondition == null)
                {
                    Debug.LogError(LogPrefix + "Could not resolve ConditionProne, ConditionBaned, or the custom Weakening condition.");
                    return false;
                }

                ItemDefinition baseSword = itemDb.GetElement("Longsword", true);
                if (baseSword == null || baseSword.GuiPresentation == null)
                {
                    Debug.LogError(LogPrefix + "Vanilla Longsword template unavailable for magical longswords.");
                    return false;
                }

                // Each damage sword adds exactly 1d6 of its listed damage type.
                // Status swords copy only the intended spell's on-hit forms.
                string[][] specs =
                {
                    new[] { "LongswordPlus1OfBlindness", "Longsword +1 of Blindness", "Blindness", "Blindness", "Status" },
                    new[] { "LongswordPlus1OfAcid", "Longsword +1 of Acid", "AcidSplash", "DamageAcid", "Damage" },
                    new[] { "LongswordPlus1OfFire", "Longsword +1 of Fire", "FireBolt", "DamageFire", "Damage" },
                    new[] { "LongswordPlus1OfLightning", "Longsword +1 of Lightning", "ShockingGrasp", "DamageLightning", "Damage" },
                    new[] { "LongswordPlus1OfCold", "Longsword +1 of Cold", "RayOfFrost", "DamageCold", "Damage" },
                    new[] { "LongswordPlus1OfPoison", "Longsword +1 of Poison", "PoisonSpray", "DamagePoison", "Damage" },
                    new[] { "LongswordPlus1OfWilting", "Longsword +1 of Wilting", "ChillTouch", "DamageNecrotic", "Damage" },
                    new[] { "LongswordPlus1OfLight", "Longsword +1 of Light", "SacredFlame", "DamageRadiant", "Damage" },
                    new[] { "LongswordPlus1OfBooming", "Longsword +1 of Booming", "Thunderwave", "DamageThunder", "Damage" },
                    new[] { "LongswordPlus1OfBane", "Longsword +1 of Bane", "Bane", "Bane", "Bane" },
                    new[] { "LongswordPlus1OfCurses", "Longsword +1 of Curses", "BestowCurse", "Bestow Curse", "RandomCurse" },
                    new[] { "LongswordPlus1OfWeakening", "Longsword +1 of Weakening", "RayOfEnfeeblement", "Weakening", "Weakening" },
                    new[] { "LongswordPlus1OfPower", "Longsword +1 of Power", "Grease", "Prone", "Prone" }
                };

                int succeeded = 0;
                foreach (string[] spec in specs)
                {
                    if (RegisterMagicalLongsword(itemDb, spellDb, baseSword,
                        "longsword",
                        spec[0], spec[1], spec[2], spec[3], spec[4],
                        proneCondition, baneCondition, weakeningCondition))
                        succeeded++;
                }

                Debug.Log(LogPrefix + "Magical longsword registration complete: " +
                    succeeded + "/" + specs.Length + " registered.");
                return succeeded == specs.Length;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Magical longsword registration failed: " + ex);
                return false;
            }
        }

        public static bool RegisterAdvancedLongswords()
        {
            return AdvancedLongswordFactory.Register();
        }

        public static bool RegisterAdvancedShortswords()
        {
            return AdvancedLongswordFactory.RegisterShortswords();
        }

        public static bool RegisterAdvancedScimitars()
        {
            return AdvancedLongswordFactory.RegisterScimitars();
        }

        public static bool RegisterAdvancedRapiers()
        {
            return AdvancedLongswordFactory.RegisterRapiers();
        }

        public static bool RegisterAdvancedGreatswords()
        {
            return AdvancedLongswordFactory.RegisterGreatswords();
        }

        public static bool RegisterAdvancedKatanas()
        {
            return AdvancedLongswordFactory.RegisterKatanas();
        }

        public static bool RegisterAdvancedLongbows()
        {
            return AdvancedLongswordFactory.RegisterLongbows();
        }

        public static bool RegisterAdvancedShortbows()
        {
            return AdvancedLongswordFactory.RegisterShortbows();
        }

        public static bool RegisterAdvancedWarhammers()
        {
            return AdvancedLongswordFactory.RegisterWarhammers();
        }
        public static bool RegisterAdvancedMaces()
        {
            return AdvancedLongswordFactory.RegisterMaces();
        }

        public static bool RegisterMagicalDaggers()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null ||
                !InitializeMagicalLongswordWeakeningPatch())
                return false;

            ConditionDefinition prone = conditionDb.GetElement("ConditionProne", true);
            ConditionDefinition bane = conditionDb.GetElement("ConditionBaned", true);
            ConditionDefinition weakening = GetOrCreateMagicalLongswordWeakeningCondition(conditionDb);
            ItemDefinition template = itemDb.GetElement("Dagger", true);
            if (prone == null || bane == null || weakening == null ||
                template == null || template.GuiPresentation == null)
            {
                Debug.LogError(LogPrefix + "Magical dagger registration failed: required template or conditions unavailable.");
                return false;
            }

            string[][] specs =
            {
                new[] { "DaggerPlus1OfBlindness", "Dagger +1 of Blindness", "Blindness", "Blindness", "Status" },
                new[] { "DaggerPlus1OfAcid", "Dagger +1 of Acid", "AcidSplash", "DamageAcid", "Damage" },
                new[] { "DaggerPlus1OfFire", "Dagger +1 of Fire", "FireBolt", "DamageFire", "Damage" },
                new[] { "DaggerPlus1OfLightning", "Dagger +1 of Lightning", "ShockingGrasp", "DamageLightning", "Damage" },
                new[] { "DaggerPlus1OfCold", "Dagger +1 of Cold", "RayOfFrost", "DamageCold", "Damage" },
                new[] { "DaggerPlus1OfPoison", "Dagger +1 of Poison", "PoisonSpray", "DamagePoison", "Damage" },
                new[] { "DaggerPlus1OfWilting", "Dagger +1 of Wilting", "ChillTouch", "DamageNecrotic", "Damage" },
                new[] { "DaggerPlus1OfLight", "Dagger +1 of Light", "SacredFlame", "DamageRadiant", "Damage" },
                new[] { "DaggerPlus1OfBoomin", "Dagger +1 of Boomin", "Thunderwave", "DamageThunder", "Damage" },
                new[] { "DaggerPlus1OfBane", "Dagger +1 of Bane", "Bane", "Bane", "Bane" },
                new[] { "DaggerPlus1OfCurses", "Dagger +1 of Curses", "BestowCurse", "Bestow Curse", "RandomCurse" },
                new[] { "DaggerPlus1OfWeakening", "Dagger +1 of Weakening", "RayOfEnfeeblement", "Weakening", "Weakening" },
                new[] { "DaggerPlus1OfPower", "Dagger +1 of Power", "Grease", "Prone", "Prone" }
            };

            int succeeded = 0;
            foreach (string[] spec in specs)
            {
                if (RegisterMagicalLongsword(itemDb, spellDb, template, "dagger",
                    spec[0], spec[1], spec[2], spec[3], spec[4], prone, bane, weakening))
                    succeeded++;
            }

            Debug.Log(LogPrefix + "Magical dagger registration complete: " + succeeded + "/" + specs.Length + ".");
            return succeeded == specs.Length;
        }

        public static bool RegisterQuicknessDagger()
        {
            const string itemId = "DaggerPlus2OfQuickness";
            const string displayName = "Dagger +2 of Quickness";

            try
            {
                Database<ItemDefinition> itemDb =
                    DatabaseRepository.GetDatabase<ItemDefinition>();
                if (itemDb == null)
                {
                    Debug.LogError(LogPrefix + "Item database unavailable for " + displayName + ".");
                    return false;
                }

                ItemDefinition existing = itemDb.GetElement(itemId, true);
                if (existing != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; kept existing definition.");
                    return true;
                }

                ItemDefinition baseDagger = itemDb.GetElement("Dagger", true);
                if (baseDagger == null || baseDagger.GuiPresentation == null)
                {
                    Debug.LogError(LogPrefix + "Vanilla Dagger template unavailable for " + displayName + ".");
                    return false;
                }

                ItemDefinition dagger = UnityEngine.Object.Instantiate(baseDagger);
                if (dagger == null)
                {
                    Debug.LogError(LogPrefix + "Could not clone the vanilla Dagger for " + displayName + ".");
                    return false;
                }

                dagger.ForceName(itemId);
                dagger.SetUserContentGUID(Guid.NewGuid().ToString());
                dagger.IsUserContent = true;

                GuiPresentation presentation = new GuiPresentation(baseDagger.GuiPresentation);
                presentation.Title = displayName;
                presentation.Description =
                    "A magical dagger that grants +2 to attack and damage. While equipped, it grants one additional weapon attack as part of the Attack action.";

                if (!SetField(dagger, "guiPresentation", presentation) ||
                    !SetField(dagger, "magical", true))
                {
                    Debug.LogError(LogPrefix + "Could not configure the presentation or magical flag for " + displayName + ".");
                    return false;
                }

                FeatureDefinitionAttackModifier modifier =
                    CreateAttackModifier(itemId, displayName, 2, baseDagger.GuiPresentation);
                if (modifier == null)
                    return false;

                List<ItemPropertyDescription> properties = dagger.StaticProperties;
                if (properties == null)
                {
                    properties = new List<ItemPropertyDescription>();
                    if (!SetField(dagger, "staticProperties", properties))
                    {
                        Debug.LogError(LogPrefix + "Could not initialize item properties for " + displayName + ".");
                        return false;
                    }
                }

                ItemPropertyDescription property =
                    CreateFeatureProperty(itemDb, baseDagger, modifier);
                if (property == null)
                {
                    Debug.LogError(LogPrefix + "Could not create the attack/damage property for " + displayName + ".");
                    return false;
                }

                properties.Add(property);
                itemDb.Add(dagger);

                if (itemDb.GetElement(itemId, true) == null)
                {
                    Debug.LogError(LogPrefix + "Registration verification failed for " + displayName + ".");
                    return false;
                }

                Debug.Log(LogPrefix + "Registered " + displayName + " with +2 attack/damage and the SEE Nick-style attack hook.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Registration failed for " + displayName + ": " + ex);
                return false;
            }
        }

        public static bool RegisterMagicalShortswords()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null ||
                !InitializeMagicalLongswordWeakeningPatch())
                return false;

            ConditionDefinition prone = conditionDb.GetElement("ConditionProne", true);
            ConditionDefinition bane = conditionDb.GetElement("ConditionBaned", true);
            ConditionDefinition weakening = GetOrCreateMagicalLongswordWeakeningCondition(conditionDb);
            ItemDefinition template = itemDb.GetElement("Shortsword", true);
            if (prone == null || bane == null || weakening == null ||
                template == null || template.GuiPresentation == null)
                return false;

            string[][] specs =
            {
                new[] { "ShortswordPlus1OfBlindness", "Shortsword +1 of Blindness", "Blindness", "Blindness", "Status" },
                new[] { "ShortswordPlus1OfAcid", "Shortsword +1 of Acid", "AcidSplash", "DamageAcid", "Damage" },
                new[] { "ShortswordPlus1OfFire", "Shortsword +1 of Fire", "FireBolt", "DamageFire", "Damage" },
                new[] { "ShortswordPlus1OfLightning", "Shortsword +1 of Lightning", "ShockingGrasp", "DamageLightning", "Damage" },
                new[] { "ShortswordPlus1OfCold", "Shortsword +1 of Cold", "RayOfFrost", "DamageCold", "Damage" },
                new[] { "ShortswordPlus1OfPoison", "Shortsword +1 of Poison", "PoisonSpray", "DamagePoison", "Damage" },
                new[] { "ShortswordPlus1OfWilting", "Shortsword +1 of Wilting", "ChillTouch", "DamageNecrotic", "Damage" },
                new[] { "ShortswordPlus1OfLight", "Shortsword +1 of Light", "SacredFlame", "DamageRadiant", "Damage" },
                new[] { "ShortswordPlus1OfBooming", "Shortsword +1 of Booming", "Thunderwave", "DamageThunder", "Damage" },
                new[] { "ShortswordPlus1OfBane", "Shortsword +1 of Bane", "Bane", "Bane", "Bane" },
                new[] { "ShortswordPlus1OfCurses", "Shortsword +1 of Curses", "BestowCurse", "Bestow Curse", "RandomCurse" },
                new[] { "ShortswordPlus1OfWeakening", "Shortsword +1 of Weakening", "RayOfEnfeeblement", "Weakening", "Weakening" },
                new[] { "ShortswordPlus1OfPower", "Shortsword +1 of Power", "Grease", "Prone", "Prone" }
            };
            int succeeded = 0;
            foreach (string[] spec in specs)
                if (RegisterMagicalLongsword(itemDb, spellDb, template, "shortsword",
                    spec[0], spec[1], spec[2], spec[3], spec[4], prone, bane, weakening))
                    succeeded++;
            Debug.Log(LogPrefix + "Magical shortsword registration complete: " + succeeded + "/" + specs.Length + ".");
            return succeeded == specs.Length;
        }

        public static bool RegisterMagicalScimitars()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null ||
                !InitializeMagicalLongswordWeakeningPatch())
                return false;

            ConditionDefinition prone = conditionDb.GetElement("ConditionProne", true);
            ConditionDefinition bane = conditionDb.GetElement("ConditionBaned", true);
            ConditionDefinition weakening = GetOrCreateMagicalLongswordWeakeningCondition(conditionDb);
            ItemDefinition template = itemDb.GetElement("Scimitar", true);
            if (prone == null || bane == null || weakening == null ||
                template == null || template.GuiPresentation == null)
                return false;

            string[][] specs =
            {
                new[] { "ScimitarPlus1OfBlindness", "Scimitar +1 of Blindness", "Blindness", "Blindness", "Status" },
                new[] { "ScimitarPlus1OfAcid", "Scimitar +1 of Acid", "AcidSplash", "DamageAcid", "Damage" },
                new[] { "ScimitarPlus1OfFire", "Scimitar +1 of Fire", "FireBolt", "DamageFire", "Damage" },
                new[] { "ScimitarPlus1OfLightning", "Scimitar +1 of Lightning", "ShockingGrasp", "DamageLightning", "Damage" },
                new[] { "ScimitarPlus1OfCold", "Scimitar +1 of Cold", "RayOfFrost", "DamageCold", "Damage" },
                new[] { "ScimitarPlus1OfPoison", "Scimitar +1 of Poison", "PoisonSpray", "DamagePoison", "Damage" },
                new[] { "ScimitarPlus1OfWilting", "Scimitar +1 of Wilting", "ChillTouch", "DamageNecrotic", "Damage" },
                new[] { "ScimitarPlus1OfLight", "Scimitar +1 of Light", "SacredFlame", "DamageRadiant", "Damage" },
                new[] { "ScimitarPlus1OfBoomin", "Scimitar +1 of Boomin", "Thunderwave", "DamageThunder", "Damage" },
                new[] { "ScimitarPlus1OfBane", "Scimitar +1 of Bane", "Bane", "Bane", "Bane" },
                new[] { "ScimitarPlus1OfCurses", "Scimitar +1 of Curses", "BestowCurse", "Bestow Curse", "RandomCurse" },
                new[] { "ScimitarPlus1OfWeakening", "Scimitar +1 of Weakening", "RayOfEnfeeblement", "Weakening", "Weakening" },
                new[] { "ScimitarPlus1OfPower", "Scimitar +1 of Power", "Grease", "Prone", "Prone" }
            };

            int succeeded = 0;
            foreach (string[] spec in specs)
                if (RegisterMagicalLongsword(itemDb, spellDb, template, "scimitar",
                    spec[0], spec[1], spec[2], spec[3], spec[4], prone, bane, weakening))
                    succeeded++;

            Debug.Log(LogPrefix + "Magical scimitar registration complete: " + succeeded + "/" + specs.Length + ".");
            return succeeded == specs.Length;
        }

        public static bool RegisterMagicalRapiers()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null ||
                !InitializeMagicalLongswordWeakeningPatch())
                return false;

            ConditionDefinition prone = conditionDb.GetElement("ConditionProne", true);
            ConditionDefinition bane = conditionDb.GetElement("ConditionBaned", true);
            ConditionDefinition weakening = GetOrCreateMagicalLongswordWeakeningCondition(conditionDb);
            ItemDefinition template = itemDb.GetElement("Rapier", true);
            if (prone == null || bane == null || weakening == null ||
                template == null || template.GuiPresentation == null)
                return false;

            string[][] specs =
            {
                new[] { "RapierPlus1OfBlindness", "Rapier +1 of Blindness", "Blindness", "Blindness", "Status" },
                new[] { "RapierPlus1OfAcid", "Rapier +1 of Acid", "AcidSplash", "DamageAcid", "Damage" },
                new[] { "RapierPlus1OfFire", "Rapier +1 of Fire", "FireBolt", "DamageFire", "Damage" },
                new[] { "RapierPlus1OfLightning", "Rapier +1 of Lightning", "ShockingGrasp", "DamageLightning", "Damage" },
                new[] { "RapierPlus1OfCold", "Rapier +1 of Cold", "RayOfFrost", "DamageCold", "Damage" },
                new[] { "RapierPlus1OfPoison", "Rapier +1 of Poison", "PoisonSpray", "DamagePoison", "Damage" },
                new[] { "RapierPlus1OfWilting", "Rapier +1 of Wilting", "ChillTouch", "DamageNecrotic", "Damage" },
                new[] { "RapierPlus1OfLight", "Rapier +1 of Light", "SacredFlame", "DamageRadiant", "Damage" },
                new[] { "RapierPlus1OfBoomin", "Rapier +1 of Boomin", "Thunderwave", "DamageThunder", "Damage" },
                new[] { "RapierPlus1OfBane", "Rapier +1 of Bane", "Bane", "Bane", "Bane" },
                new[] { "RapierPlus1OfCurses", "Rapier +1 of Curses", "BestowCurse", "Bestow Curse", "RandomCurse" },
                new[] { "RapierPlus1OfWeakening", "Rapier +1 of Weakening", "RayOfEnfeeblement", "Weakening", "Weakening" },
                new[] { "RapierPlus1OfPower", "Rapier +1 of Power", "Grease", "Prone", "Prone" }
            };

            int succeeded = 0;
            foreach (string[] spec in specs)
                if (RegisterMagicalLongsword(itemDb, spellDb, template, "rapier",
                    spec[0], spec[1], spec[2], spec[3], spec[4], prone, bane, weakening))
                    succeeded++;
            Debug.Log(LogPrefix + "Magical rapier registration complete: " + succeeded + "/" + specs.Length + ".");
            return succeeded == specs.Length;
        }

        public static bool RegisterMagicalGreatswords()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null ||
                !InitializeMagicalLongswordWeakeningPatch())
                return false;

            ConditionDefinition prone = conditionDb.GetElement("ConditionProne", true);
            ConditionDefinition bane = conditionDb.GetElement("ConditionBaned", true);
            ConditionDefinition weakening = GetOrCreateMagicalLongswordWeakeningCondition(conditionDb);
            ItemDefinition template = itemDb.GetElement("Greatsword", true);
            if (prone == null || bane == null || weakening == null ||
                template == null || template.GuiPresentation == null)
                return false;

            string[][] specs =
            {
                new[] { "GreatswordPlus1OfBlindness", "Greatsword +1 of Blindness", "Blindness", "Blindness", "Status" },
                new[] { "GreatswordPlus1OfAcid", "Greatsword +1 of Acid", "AcidSplash", "DamageAcid", "Damage" },
                new[] { "GreatswordPlus1OfFire", "Greatsword +1 of Fire", "FireBolt", "DamageFire", "Damage" },
                new[] { "GreatswordPlus1OfLightning", "Greatsword +1 of Lightning", "ShockingGrasp", "DamageLightning", "Damage" },
                new[] { "GreatswordPlus1OfCold", "Greatsword +1 of Cold", "RayOfFrost", "DamageCold", "Damage" },
                new[] { "GreatswordPlus1OfPoison", "Greatsword +1 of Poison", "PoisonSpray", "DamagePoison", "Damage" },
                new[] { "GreatswordPlus1OfWilting", "Greatsword +1 of Wilting", "ChillTouch", "DamageNecrotic", "Damage" },
                new[] { "GreatswordPlus1OfLight", "Greatsword +1 of Light", "SacredFlame", "DamageRadiant", "Damage" },
                new[] { "GreatswordPlus1OfBoomin", "Greatsword +1 of Boomin", "Thunderwave", "DamageThunder", "Damage" },
                new[] { "GreatswordPlus1OfBane", "Greatsword +1 of Bane", "Bane", "Bane", "Bane" },
                new[] { "GreatswordPlus1OfCurses", "Greatsword +1 of Curses", "BestowCurse", "Bestow Curse", "RandomCurse" },
                new[] { "GreatswordPlus1OfWeakening", "Greatsword +1 of Weakening", "RayOfEnfeeblement", "Weakening", "Weakening" },
                new[] { "GreatswordPlus1OfPower", "Greatsword +1 of Power", "Grease", "Prone", "Prone" }
            };
            int succeeded = 0;
            foreach (string[] spec in specs)
                if (RegisterMagicalLongsword(itemDb, spellDb, template, "greatsword",
                    spec[0], spec[1], spec[2], spec[3], spec[4], prone, bane, weakening))
                    succeeded++;
            Debug.Log(LogPrefix + "Magical greatsword registration complete: " + succeeded + "/" + specs.Length + ".");
            return succeeded == specs.Length;
        }

        public static bool RegisterMagicalKatanas()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null ||
                !InitializeMagicalLongswordWeakeningPatch())
                return false;

            ConditionDefinition prone = conditionDb.GetElement("ConditionProne", true);
            ConditionDefinition bane = conditionDb.GetElement("ConditionBaned", true);
            ConditionDefinition weakening = GetOrCreateMagicalLongswordWeakeningCondition(conditionDb);
            ItemDefinition template = itemDb.GetElement("Katana", true);
            if (template == null)
            {
                template = itemDb.GetElement("Longsword", true);
                Debug.LogWarning(LogPrefix + "Katana template not found; using Longsword as the Katana base template.");
            }
            if (prone == null || bane == null || weakening == null ||
                template == null || template.GuiPresentation == null)
                return false;

            string[][] specs =
            {
                new[] { "KatanaPlus1OfBlindness", "Katana +1 of Blindness", "Blindness", "Blindness", "Status" },
                new[] { "KatanaPlus1OfAcid", "Katana +1 of Acid", "AcidSplash", "DamageAcid", "Damage" },
                new[] { "KatanaPlus1OfFire", "Katana +1 of Fire", "FireBolt", "DamageFire", "Damage" },
                new[] { "KatanaPlus1OfLightning", "Katana +1 of Lightning", "ShockingGrasp", "DamageLightning", "Damage" },
                new[] { "KatanaPlus1OfCold", "Katana +1 of Cold", "RayOfFrost", "DamageCold", "Damage" },
                new[] { "KatanaPlus1OfPoison", "Katana +1 of Poison", "PoisonSpray", "DamagePoison", "Damage" },
                new[] { "KatanaPlus1OfWilting", "Katana +1 of Wilting", "ChillTouch", "DamageNecrotic", "Damage" },
                new[] { "KatanaPlus1OfLight", "Katana +1 of Light", "SacredFlame", "DamageRadiant", "Damage" },
                new[] { "KatanaPlus1OfBoomin", "Katana +1 of Boomin", "Thunderwave", "DamageThunder", "Damage" },
                new[] { "KatanaPlus1OfBane", "Katana +1 of Bane", "Bane", "Bane", "Bane" },
                new[] { "KatanaPlus1OfCurses", "Katana +1 of Curses", "BestowCurse", "Bestow Curse", "RandomCurse" },
                new[] { "KatanaPlus1OfWeakening", "Katana +1 of Weakening", "RayOfEnfeeblement", "Weakening", "Weakening" },
                new[] { "KatanaPlus1OfPower", "Katana +1 of Power", "Grease", "Prone", "Prone" }
            };
            int succeeded = 0;
            foreach (string[] spec in specs)
                if (RegisterMagicalLongsword(itemDb, spellDb, template, "katana",
                    spec[0], spec[1], spec[2], spec[3], spec[4], prone, bane, weakening))
                    succeeded++;
            Debug.Log(LogPrefix + "Magical katana registration complete: " + succeeded + "/" + specs.Length + ".");
            return succeeded == specs.Length;
        }

        public static bool RegisterMagicalLongbows()
        {
            return RegisterMagicalBows("Longbow", "longbow");
        }

        public static bool RegisterMagicalShortbows()
        {
            return RegisterMagicalBows("Shortbow", "shortbow");
        }

        public static bool RegisterMagicalWarhammers()
        {
            return RegisterMagicalBows("Warhammer", "warhammer");
        }
        public static bool RegisterMagicalMaces()
        {
            return RegisterMagicalBows("Mace", "mace");
        }

        private static bool RegisterMagicalBows(string baseWeaponId, string weaponType, bool includeWeakeningAndPower = true)
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            Database<ConditionDefinition> conditionDb = DatabaseRepository.GetDatabase<ConditionDefinition>();
            if (itemDb == null || spellDb == null || conditionDb == null ||
                !InitializeMagicalLongswordWeakeningPatch())
                return false;

            ConditionDefinition prone = conditionDb.GetElement("ConditionProne", true);
            ConditionDefinition bane = conditionDb.GetElement("ConditionBaned", true);
            ConditionDefinition weakening = GetOrCreateMagicalLongswordWeakeningCondition(conditionDb);
            ItemDefinition template = ResolveWeaponTemplate(itemDb, baseWeaponId, weaponType);
            if (prone == null || bane == null || weakening == null || template == null ||
                template.GuiPresentation == null)
                return false;

            string title = baseWeaponId == "CELongMace" ? "Long Mace" : baseWeaponId;
            string idPrefix = NormalizeItemId(title);
            List<string[]> specs = new List<string[]>
            {
                new[] { idPrefix + "Plus1OfBlindness", title + " +1 of Blindness", "Blindness", "Blindness", "Status" },
                new[] { idPrefix + "Plus1OfAcid", title + " +1 of Acid", "AcidSplash", "DamageAcid", "Damage" },
                new[] { idPrefix + "Plus1OfFire", title + " +1 of Fire", "FireBolt", "DamageFire", "Damage" },
                new[] { idPrefix + "Plus1OfLightning", title + " +1 of Lightning", "ShockingGrasp", "DamageLightning", "Damage" },
                new[] { idPrefix + "Plus1OfCold", title + " +1 of Cold", "RayOfFrost", "DamageCold", "Damage" },
                new[] { idPrefix + "Plus1OfPoison", title + " +1 of Poison", "PoisonSpray", "DamagePoison", "Damage" },
                new[] { idPrefix + "Plus1OfWilting", title + " +1 of Wilting", "ChillTouch", "DamageNecrotic", "Damage" },
                new[] { idPrefix + "Plus1OfLight", title + " +1 of Light", "SacredFlame", "DamageRadiant", "Damage" },
                new[] { idPrefix + "Plus1OfBoomin", title + " +1 of Boomin", "Thunderwave", "DamageThunder", "Damage" },
                new[] { idPrefix + "Plus1OfBane", title + " +1 of Bane", "Bane", "Bane", "Bane" },
                new[] { idPrefix + "Plus1OfCurses", title + " +1 of Curses", "BestowCurse", "Bestow Curse", "RandomCurse" }
            };
            if (includeWeakeningAndPower)
            {
                specs.Add(new[] { idPrefix + "Plus1OfWeakening", title + " +1 of Weakening", "RayOfEnfeeblement", "Weakening", "Weakening" });
                specs.Add(new[] { idPrefix + "Plus1OfPower", title + " +1 of Power", "Grease", "Prone", "Prone" });
            }

            int succeeded = 0;
            foreach (string[] spec in specs)
                if (RegisterMagicalLongsword(itemDb, spellDb, template, weaponType,
                    spec[0], spec[1], spec[2], spec[3], spec[4], prone, bane, weakening))
                    succeeded++;
            Debug.Log(LogPrefix + "Magical " + title + " registration complete: " +
                succeeded + "/" + specs.Count + ".");
            return succeeded == specs.Count;
        }

        private static bool RegisterMagicalLongsword(
            Database<ItemDefinition> itemDb,
            Database<SpellDefinition> spellDb,
            ItemDefinition baseSword,
            string weaponType,
            string itemId,
            string displayName,
            string spellId,
            string effectKey,
            string effectMode,
            ConditionDefinition proneCondition,
            ConditionDefinition baneCondition,
            ConditionDefinition weakeningCondition)
        {
            try
            {
                ItemDefinition existing = itemDb.GetElement(itemId, true);
                if (existing != null)
                {
                    Debug.Log(LogPrefix + displayName + " already exists; kept existing definition.");
                    return true;
                }

                // Pure bonus-damage swords and the Prone sword do not need spell lookups.
                // Bonus damage is built directly as a native Damage EffectForm.
                SpellDefinition sourceSpell = null;
                if (effectMode != "Damage" && effectMode != "Prone")
                {
                    sourceSpell = spellDb.GetElement(spellId, true);
                    if (sourceSpell == null)
                    {
                        Debug.LogError(LogPrefix + "Could not find spell template " + spellId + " for " + displayName + ".");
                        return false;
                    }
                }

                ItemDefinition sword = UnityEngine.Object.Instantiate(baseSword);
                if (sword == null)
                    return false;

                sword.ForceName(itemId);
                sword.SetUserContentGUID(Guid.NewGuid().ToString());
                sword.IsUserContent = true;

                GuiPresentation presentation = new GuiPresentation(baseSword.GuiPresentation);
                presentation.Title = displayName;
                presentation.Description = "A magical +1 " + weaponType + ". " + GetMagicalLongswordDescription(effectMode, effectKey) +
                    " Its magical effect triggers when the weapon hits a target.";
                if (!SetField(sword, "guiPresentation", presentation) || !SetField(sword, "magical", true))
                {
                    Debug.LogError(LogPrefix + "Could not configure presentation or magical flag for " + displayName + ".");
                    return false;
                }

                FeatureDefinitionAttackModifier modifier =
                    CreateAttackModifier(itemId, displayName, 1, baseSword.GuiPresentation);
                if (modifier == null)
                    return false;

                List<ItemPropertyDescription> properties = sword.StaticProperties;
                if (properties == null)
                {
                    properties = new List<ItemPropertyDescription>();
                    if (!SetField(sword, "staticProperties", properties))
                        return false;
                }

                ItemPropertyDescription property = CreateFeatureProperty(itemDb, baseSword, modifier);
                if (property == null)
                    return false;
                properties.Add(property);

                object weaponDescription = ReadMemberObject(sword, "WeaponDescription");
                object weaponEffect = ReadMemberObject(weaponDescription, "EffectDescription");
                System.Collections.IList weaponForms =
                    ReadMemberObject(weaponEffect, "EffectForms") as System.Collections.IList;
                if (weaponDescription == null || weaponEffect == null || weaponForms == null)
                {
                    Debug.LogError(LogPrefix + "Could not access WeaponDescription effect forms for " + displayName + ".");
                    return false;
                }

                // Elemental variants keep the base Longsword hit forms (normal slashing
                // weapon damage) and append one 1d6 elemental rider. Status variants
                // replace the template forms with their intended on-hit effect.
                if (effectMode != "Damage")
                    weaponForms.Clear();
                if (!BuildMagicalLongswordForms(sourceSpell, weaponEffect, weaponForms,
                    effectMode, effectKey, displayName,
                    proneCondition, baneCondition, weakeningCondition))
                    return false;

                itemDb.Add(sword);
                bool registered = itemDb.GetElement(itemId, true) != null;
                if (registered)
                    Debug.Log(LogPrefix + "Registered " + displayName + " with +1 attack/damage and " + effectMode + " on-hit effect.");
                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Failed to register " + displayName + ": " + ex);
                return false;
            }
        }

        private static string GetMagicalLongswordDescription(string mode, string key)
        {
            if (mode == "Damage")
            {
                string damageType = key == "DamageThunder"
                    ? "sonic"
                    : key.Replace("Damage", "").ToLowerInvariant();
                return "+1d6 " + damageType + " damage.";
            }
            if (mode == "RandomCurse") return "On a hit, randomly applies one effect from the Bestow Curse spell.";
            if (mode == "Weakening") return "On a hit, the target must succeed on a DC 11 Strength saving throw or become Weakened, taking a -2 penalty to Strength saving throws.";
            if (mode == "Prone") return "On a hit, the target must fail a DC 11 Constitution saving throw or fall Prone.";
            if (key == "Blindness") return "On a hit, may Blind the target.";
            if (key == "Bane") return "On a hit, applies the Banestruck effect.";
            return "On a hit, applies its magical effect.";
        }

        private static bool BuildMagicalLongswordForms(
            SpellDefinition sourceSpell,
            object weaponEffect,
            System.Collections.IList weaponForms,
            string mode,
            string key,
            string displayName,
            ConditionDefinition proneCondition,
            ConditionDefinition baneCondition,
            ConditionDefinition weakeningCondition)
        {
            try
            {
                if (mode == "Damage")
                {
                    EffectForm bonusDamage = CreateMagicalLongswordDamageEffectForm(key);
                    if (bonusDamage == null)
                    {
                        Debug.LogError(LogPrefix + "Could not create the direct 1d6 " + key + " damage form for " + displayName + ".");
                        return false;
                    }

                    weaponForms.Add(bonusDamage);
                    bool damageEffectConfigured =
                        SetMember(weaponEffect, "hasSavingThrow", false) &&
                        SetEnumMember(weaponEffect, "durationType", "Instantaneous") &&
                        SetMember(weaponEffect, "durationParameter", 1);

                    if (!damageEffectConfigured)
                    {
                        Debug.LogError(LogPrefix + "Could not configure the immediate on-hit damage effect for " + displayName + ".");
                        return false;
                    }

                    return true;
                }

                if (mode == "Prone")
                {
                    EffectForm proneForm = CreateMagicalLongswordConditionForm(proneCondition, true);
                    if (proneForm == null)
                    {
                        Debug.LogError(LogPrefix + "Could not create the native Prone condition form for " + displayName + ".");
                        return false;
                    }

                    weaponForms.Add(proneForm);

                    bool saveConfigured =
                        SetMember(weaponEffect, "hasSavingThrow", true) &&
                        SetMember(weaponEffect, "savingThrowAbility", "Constitution") &&
                        SetEnumMember(weaponEffect, "difficultyClassComputation", "FixedValue") &&
                        SetMember(weaponEffect, "fixedSavingThrowDifficultyClass", 11) &&
                        SetEnumMember(weaponEffect, "durationType", "Hour") &&
                        SetMember(weaponEffect, "durationParameter", 1);

                    if (!saveConfigured)
                    {
                        Debug.LogError(LogPrefix + "Could not configure the DC 11 Constitution save and duration for " + displayName + ".");
                        return false;
                    }

                    return true;
                }

                EffectDescription sourceDescription = sourceSpell.EffectDescription;
                if (mode == "Bane")
                {
                    EffectForm baneForm = CreateMagicalLongswordConditionForm(baneCondition, false);
                    if (baneForm == null)
                    {
                        Debug.LogError(LogPrefix + "Could not create the Banestruck condition form for " + displayName + ".");
                        return false;
                    }

                    weaponForms.Add(baneForm);
                    CopyLongswordEffectMetadata(sourceDescription, weaponEffect);
                    SetMember(weaponEffect, "hasSavingThrow", false);
                    return true;
                }

                if (mode == "RandomCurse")
                {
                    SpellDefinition bundle = sourceSpell;
                    if (bundle.SubspellsList == null || bundle.SubspellsList.Count == 0)
                    {
                        Debug.LogError(LogPrefix + "Bestow Curse has no variants for " + displayName + ".");
                        return false;
                    }
                    EffectForm marker = SpellDartItems.CreateRandomCurseMarker(bundle);
                    if (marker == null)
                        return false;
                    weaponForms.Add(marker);
                    SpellDefinition firstVariant = null;
                    foreach (SpellDefinition variant in bundle.SubspellsList)
                    {
                        if (variant != null && variant.EffectDescription != null &&
                            variant.EffectDescription.EffectForms != null && variant.EffectDescription.EffectForms.Count > 0)
                        {
                            firstVariant = variant;
                            break;
                        }
                    }
                    if (firstVariant == null)
                        return false;
                    CopyLongswordEffectMetadata(firstVariant.EffectDescription, weaponEffect);
                    return true;
                }

                if (sourceDescription == null || sourceDescription.EffectForms == null || sourceDescription.EffectForms.Count == 0)
                {
                    Debug.LogError(LogPrefix + "Spell " + sourceSpell.Name + " has no effect forms for " + displayName + ".");
                    return false;
                }

                int copied = 0;
                foreach (EffectForm sourceForm in sourceDescription.EffectForms)
                {
                    if (sourceForm == null)
                        continue;
                    if (mode == "Weakening" && sourceForm.FormType != EffectForm.EffectFormType.Condition)
                        continue;

                    EffectForm copy = EffectForm.GetCopy(sourceForm);
                    if (copy == null)
                        continue;

                    if (mode == "Weakening")
                    {
                        object conditionForm = ReadMemberObject(copy, "ConditionForm");
                        if (conditionForm == null || weakeningCondition == null ||
                            !SetMember(conditionForm, "ConditionDefinition", weakeningCondition) ||
                            !SetMember(conditionForm, "conditionDefinitionName", weakeningCondition.Name))
                        {
                            Debug.LogError(LogPrefix + "Could not substitute the custom Weakening condition for " + displayName + ".");
                            return false;
                        }
                        if (!SetMember(copy, "hasSavingThrow", true) ||
                            !SetEnumMember(copy, "savingThrowAffinity", "Negates"))
                        {
                            Debug.LogError(LogPrefix + "Could not retain the saving throw on the Weakening condition for " + displayName + ".");
                            return false;
                        }
                    }

                    weaponForms.Add(copy);
                    copied++;
                }

                if (copied == 0)
                {
                    Debug.LogError(LogPrefix + "No applicable effect forms found in " + sourceSpell.Name + " for " + displayName + ".");
                    return false;
                }

                CopyLongswordEffectMetadata(sourceDescription, weaponEffect);
                if (mode == "Weakening")
                {
                    bool weakeningSaveConfigured =
                        SetMember(weaponEffect, "hasSavingThrow", true) &&
                        SetEnumMember(weaponEffect, "difficultyClassComputation", "FixedValue") &&
                        SetMember(weaponEffect, "fixedSavingThrowDifficultyClass", 11) &&
                        SetMember(weaponEffect, "savingThrowAbility", "Strength") &&
                        SetEnumMember(weaponEffect, "durationType", "Round") &&
                        SetMember(weaponEffect, "durationParameter", 1);
                    if (!weakeningSaveConfigured)
                    {
                        Debug.LogError(LogPrefix + "Could not configure the DC 11 Strength save for " + displayName + ".");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Could not build on-hit forms for " + displayName + ": " + ex);
                return false;
            }
        }

        private static EffectForm CreateMagicalLongswordDamageEffectForm(string damageType)
        {
            try
            {
                // The weapon adds a single native damage form. It does not cast a spell.
                EffectForm effectForm = new EffectForm();
                effectForm.DamageForm = new DamageForm();
                effectForm.FormType = EffectForm.EffectFormType.Damage;
                effectForm.DamageForm.DiceNumber = 1;
                effectForm.DamageForm.DieType = RuleDefinitions.DieType.D6;
                effectForm.DamageForm.DamageType = damageType;
                effectForm.HasSavingThrow = false;
                effectForm.SavingThrowAffinity = RuleDefinitions.EffectSavingThrowType.None;
                return effectForm;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Could not construct a direct 1d6 " + damageType + " damage form: " + ex);
                return null;
            }
        }

        private static bool InitializeMagicalLongswordWeakeningPatch()
        {
            if (magicalLongswordWeakeningPatchInitialized)
                return true;

            try
            {
                MethodInfo target = HarmonyLib.AccessTools.Method(
                    typeof(RulesetActor),
                    "ComputeBaseSavingThrowBonus",
                    new[] { typeof(string), typeof(List<RuleDefinitions.TrendInfo>) });
                MethodInfo postfix = typeof(WeaponFactory).GetMethod(
                    nameof(ApplyMagicalLongswordWeakeningPenalty),
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (target == null || postfix == null)
                {
                    Debug.LogError(LogPrefix + "Could not locate the saving throw method for the Weakening sword.");
                    return false;
                }

                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony(
                    "SolastaExpandedEquipmentMod.MagicalLongswordWeakening");
                harmony.Patch(target, postfix: new HarmonyLib.HarmonyMethod(postfix));
                magicalLongswordWeakeningPatchInitialized = true;
                Debug.Log(LogPrefix + "Weakening longsword Strength-save penalty patch initialized.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Could not initialize the Weakening longsword save patch: " + ex);
                return false;
            }
        }

        private static void ApplyMagicalLongswordWeakeningPenalty(
            RulesetActor __instance,
            string abilityScoreName,
            List<RuleDefinitions.TrendInfo> savingThrowModifierTrends,
            ref int __result)
        {
            if (__instance == null || !string.Equals(abilityScoreName, "Strength", StringComparison.Ordinal) ||
                __instance.AllConditions == null)
                return;

            foreach (RulesetCondition activeCondition in __instance.AllConditions)
            {
                if (activeCondition == null || activeCondition.ConditionDefinition == null ||
                    !string.Equals(activeCondition.ConditionDefinition.Name,
                        MagicalLongswordWeakeningConditionId, StringComparison.Ordinal))
                    continue;

                __result -= 2;
                if (savingThrowModifierTrends != null)
                {
                    savingThrowModifierTrends.Add(new RuleDefinitions.TrendInfo(
                        -2,
                        RuleDefinitions.FeatureSourceType.Condition,
                        "Longsword Weakening",
                        activeCondition.ConditionDefinition));
                }
                break;
            }
        }

        private static ConditionDefinition GetOrCreateMagicalLongswordWeakeningCondition(
            Database<ConditionDefinition> conditionDb)
        {
            if (conditionDb == null)
                return null;

            ConditionDefinition existing = conditionDb.GetElement(
                MagicalLongswordWeakeningConditionId, true);
            if (existing != null)
                return existing;

            ConditionDefinition enfeebled = conditionDb.GetElement("ConditionEnfeebled", true);
            if (enfeebled == null || enfeebled.GuiPresentation == null)
            {
                Debug.LogError(LogPrefix + "Could not find the native ConditionEnfeebled template.");
                return null;
            }

            try
            {
                ConditionDefinition weakening = UnityEngine.Object.Instantiate(enfeebled);
                if (weakening == null)
                    return null;

                weakening.ForceName(MagicalLongswordWeakeningConditionId);
                weakening.SetUserContentGUID(Guid.NewGuid().ToString());
                weakening.IsUserContent = true;

                GuiPresentation presentation = new GuiPresentation(enfeebled.GuiPresentation);
                presentation.Title = "Weakening";
                presentation.Description =
                    "The creature deals half damage with Strength-based attacks and takes a -2 penalty to Strength saving throws.";

                if (!SetField(weakening, "guiPresentation", presentation))
                {
                    Debug.LogError(LogPrefix + "Could not assign the Weakening condition presentation.");
                    UnityEngine.Object.Destroy(weakening);
                    return null;
                }

                conditionDb.Add(weakening);
                ConditionDefinition registered = conditionDb.GetElement(
                    MagicalLongswordWeakeningConditionId, true);
                if (registered == null)
                {
                    Debug.LogError(LogPrefix + "Weakening condition database verification failed.");
                    return null;
                }

                Debug.Log(LogPrefix + "Registered custom Weakening condition with the native Enfeebled effect and -2 Strength-save penalty.");
                return registered;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Could not register the custom Weakening condition: " + ex);
                return null;
            }
        }

        internal static EffectForm CreateMagicalLongswordConditionForm(ConditionDefinition condition, bool requiresSavingThrow)
        {
            if (condition == null)
                return null;

            try
            {
                object conditionForm = FormatterServices.GetUninitializedObject(typeof(ConditionForm));
                object effectForm = FormatterServices.GetUninitializedObject(typeof(EffectForm));
                if (conditionForm == null || effectForm == null)
                    return null;

                object emptyConditionsList = CreateEmptyCollection(
                    conditionForm, "conditionsList", typeof(ConditionDefinition));

                bool conditionConfigured =
                    SetEnumMember(conditionForm, "Operation", "Add") &&
                    SetMember(conditionForm, "ConditionDefinition", condition) &&
                    SetMember(conditionForm, "conditionDefinitionName", condition.Name) &&
                    SetMember(conditionForm, "applyToSelf", false) &&
                    SetMember(conditionForm, "forceOnSelf", false) &&
                    emptyConditionsList != null &&
                    SetMember(conditionForm, "conditionsList", emptyConditionsList);

                bool effectConfigured =
                    SetEnumMember(effectForm, "FormType", "Condition") &&
                    SetMember(effectForm, "ConditionForm", conditionForm) &&
                    SetMember(effectForm, "createdByCharacter", true) &&
                    SetMember(effectForm, "createdByCondition", false) &&
                    SetMember(effectForm, "hasSavingThrow", requiresSavingThrow) &&
                    SetEnumMember(effectForm, "savingThrowAffinity", requiresSavingThrow ? "Negates" : "None") &&
                    SetEnumMember(effectForm, "saveOccurence", "StartOfTurn") &&
                    SetMember(effectForm, "canSaveToCancel", false) &&
                    SetMember(effectForm, "dcModifier", 0);

                if (!conditionConfigured || !effectConfigured)
                    return null;

                return effectForm as EffectForm;
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Could not create magical longsword condition form: " + ex);
                return null;
            }
        }

        private static void CopyLongswordEffectMetadata(EffectDescription source, object destination)
        {
            string[] members =
            {
                "HasSavingThrow", "SavingThrowAbility", "DifficultyClassComputation",
                "SavingThrowDifficultyAbility", "FixedSavingThrowDifficultyClass",
                "DurationType", "DurationParameter", "EndOfEffect", "HasShoveRoll"
            };
            foreach (string member in members)
            {
                object value = ReadMemberObject(source, member);
                if (value != null)
                    SetMember(destination, char.ToLowerInvariant(member[0]) + member.Substring(1), value);
            }
        }

        internal static FeatureDefinitionAttackModifier CreateAttackModifier(
            string itemId,
            string displayName,
            int bonus,
            GuiPresentation sourcePresentation)
        {
            FeatureDefinitionAttackModifier modifier =
                ScriptableObject.CreateInstance<
                    FeatureDefinitionAttackModifier>();

            if (modifier == null)
            {
                Debug.LogError(
                    LogPrefix +
                    "Could not create attack modifier for " +
                    displayName + ".");

                return null;
            }

            modifier.ForceName(itemId + "AttackModifier");
            modifier.SetUserContentGUID(Guid.NewGuid().ToString());
            modifier.IsUserContent = true;

            // Feature definitions are included in Unfinished Business' documentation
            // dump. Give this custom feature its own non-null GUI presentation so
            // DumpOthers/StripXmlTags never receives a null description from it.
            if (sourcePresentation == null)
            {
                Debug.LogError(
                    LogPrefix +
                    "Cannot create attack modifier for " + displayName +
                    ": source GUI presentation is missing.");

                UnityEngine.Object.Destroy(modifier);
                return null;
            }

            GuiPresentation modifierPresentation =
                new GuiPresentation(sourcePresentation);
            modifierPresentation.Title = displayName + " Attack Modifier";
            modifierPresentation.Description =
                "Provides +" + bonus +
                " to attack rolls and +" + bonus +
                " to damage rolls when applied by " + displayName + ".";

            if (!SetField(modifier, "guiPresentation", modifierPresentation) ||
                modifier.GuiPresentation == null ||
                string.IsNullOrEmpty(modifier.GuiPresentation.Description))
            {
                Debug.LogError(
                    LogPrefix +
                    "Could not assign a valid GUI presentation to the attack modifier for " +
                    displayName + ".");

                UnityEngine.Object.Destroy(modifier);
                return null;
            }

            Debug.Log(
                LogPrefix + "Created attack modifier " + modifier.Name +
                " with a non-empty GUI description.");

            bool attackMethodSet = SetField(
                modifier,
                "attackRollModifierMethod",
                RuleDefinitions.AttackModifierMethod.FlatValue);

            bool attackBonusSet = SetField(
                modifier,
                "attackRollModifier",
                bonus);

            bool damageMethodSet = SetField(
                modifier,
                "damageRollModifierMethod",
                RuleDefinitions.AttackModifierMethod.FlatValue);

            bool damageBonusSet = SetField(
                modifier,
                "damageRollModifier",
                bonus);

            // The item itself is marked magical. Leaving this feature flag false
            // prevents the duplicate "Magical" tag in the item's tooltip.
            bool magicalWeaponSet = SetField(
                modifier,
                "magicalWeapon",
                false);

            if (!attackMethodSet ||
                !attackBonusSet ||
                !damageMethodSet ||
                !damageBonusSet ||
                !magicalWeaponSet)
            {
                Debug.LogError(
                    LogPrefix +
                    "Could not configure all modifier fields for " +
                    displayName + ".");

                UnityEngine.Object.Destroy(modifier);
                return null;
            }

            // Register the feature when the game's typed feature database
            // is available. The item property also holds a direct reference.
            try
            {
                Database<FeatureDefinitionAttackModifier> featureDb =
                    DatabaseRepository.GetDatabase<
                        FeatureDefinitionAttackModifier>();

                if (featureDb != null)
                {
                    FeatureDefinitionAttackModifier existing =
                        featureDb.GetElement(modifier.Name, true);

                    if (existing == null)
                    {
                        featureDb.Add(modifier);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    LogPrefix +
                    "Feature database registration warning for " +
                    displayName + ": " + ex.Message);
            }

            return modifier;
        }

        internal static ItemPropertyDescription CreateFeatureProperty(
            Database<ItemDefinition> itemDb,
            ItemDefinition baseItem,
            FeatureDefinition feature)
        {
            ItemPropertyDescription template = null;

            // Prefer a property already attached to the original Dart.
            if (baseItem != null &&
                baseItem.StaticProperties != null &&
                baseItem.StaticProperties.Count > 0)
            {
                template = baseItem.StaticProperties[0];
            }

            // Otherwise, find a property on another existing item so we
            // can use the game's supported copy constructor.
            if (template == null)
            {
                foreach (ItemDefinition item in itemDb.GetAllElements())
                {
                    if (item == null || item.StaticProperties == null)
                    {
                        continue;
                    }

                    foreach (ItemPropertyDescription candidate
                        in item.StaticProperties)
                    {
                        if (candidate != null)
                        {
                            template = candidate;
                            break;
                        }
                    }

                    if (template != null)
                    {
                        break;
                    }
                }
            }

            ItemPropertyDescription property;

            if (template != null)
            {
                property = new ItemPropertyDescription(template);
            }
            else
            {
                // Fallback for databases with no existing item properties.
                property = (ItemPropertyDescription)
                    FormatterServices.GetUninitializedObject(
                        typeof(ItemPropertyDescription));
            }

            bool appliesSet = SetField(
                property, "appliesOnItemOnly", true);

            bool typeSet = SetField(
                property,
                "type",
                ItemPropertyDescription.PropertyType.Feature);

            bool featureSet = SetField(
                property, "featureDefinition", feature);

            bool conditionSet = SetField(
                property, "conditionDefinition", null);

            bool knowledgeSet = SetField(
                property,
                "knowledgeAffinity",
                EquipmentDefinitions.KnowledgeAffinity.ActiveAndVisible);

            if (!appliesSet ||
                !typeSet ||
                !featureSet ||
                !conditionSet ||
                !knowledgeSet)
            {
                Debug.LogError(
                    LogPrefix +
                    "Could not configure the feature property.");

                return null;
            }

            return property;
        }

        internal static bool SetField(
            object target,
            string fieldName,
            object value)
        {
            if (target == null)
            {
                return false;
            }

            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                if (field != null)
                {
                    if (field.IsInitOnly)
                    {
                        return false;
                    }

                    field.SetValue(target, value);
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }
    }
}
