using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>
    /// Preserves the existing conditional Slayer weapon bonuses and applies the shield set.
    /// Creature-family shields add conditional AC against matching attackers; the missile shield
    /// adds protection against ranged weapon/projectile attacks. +1 shields retain +1 universal AC
    /// and gain the existing conditional bonus; +2 shields retain +2 universal AC and gain only
    /// +1 conditional AC, for +3 total against their designated target.
    /// </summary>
    public static class BeastSlayerCombatPatch
    {
        private const string LogPrefix = "[SEE SlayerWeapons] ";
        private static bool initialized;
        private static int matchingCallsLogged;
        private const int MaxMatchingLogs = 20;
        private const string ShieldLogPrefix = "[SEE Creature Shields] ";
        private static int shieldCallsLogged;
        private const int MaxShieldLogs = 20;

        // If the game calls this postfix again on the same ActionModifier without
        // rebuilding its values, do not add the conditional bonuses twice.
        private sealed class AppliedStamp
        {
            public object LastAttackValue;
            public object LastDamageValue;
        }

        private static readonly ConditionalWeakTable<object, AppliedStamp> AppliedModifiers =
            new ConditionalWeakTable<object, AppliedStamp>();

        public static void Initialize()
        {
            if (initialized) return;

            try
            {
                Type characterType = AccessTools.TypeByName("RulesetCharacter");
                MethodInfo target = characterType == null
                    ? null
                    : AccessTools.Method(characterType, "ComputeAttackModifier");

                if (target == null)
                {
                    Debug.LogError(LogPrefix + "Could not find RulesetCharacter.ComputeAttackModifier.");
                    return;
                }

                var harmony = new Harmony("SolastaExpandedEquipmentMod.BeastSlayerCombatPatch");
                var postfix = new HarmonyMethod(
                    typeof(BeastSlayerCombatPatch).GetMethod(
                        nameof(AfterComputeAttackModifier),
                        BindingFlags.Static | BindingFlags.NonPublic));

                harmony.Patch(target, postfix: postfix);
                initialized = true;
                Debug.Log(LogPrefix + "PATCH ACTIVE - Existing Slayer weapon bonuses preserved. " +
                    "Creature-family shields and missile defense active. Shields retain universal +1 AC; " +
                    "conditional AC applies only to matching attacker families or ranged weapon attacks. Patched " + target);
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + "Patch initialization failed: " + ex);
            }
        }

        private static void AfterComputeAttackModifier(object __instance, object[] __args)
        {
            try
            {
                // ComputeAttackModifier is called on the ATTACKING character.
                // args[0] is the defender, args[1] is RulesetAttackMode,
                // and args[2] is the ActionModifier being prepared.
                if (__instance == null || __args == null || __args.Length < 3) return;

                object defender = __args[0];
                object attackMode = __args[1];
                object actionModifier = __args[2];
                if (defender == null || attackMode == null || actionModifier == null) return;

                string defenderFamily = ReadMember(defender, "CharacterFamily");
                bool isBeastTarget =
                    string.Equals(defenderFamily, "Beast", StringComparison.OrdinalIgnoreCase);
                bool isFeyTarget =
                    string.Equals(defenderFamily, "Fey", StringComparison.OrdinalIgnoreCase);
                bool isMagicalTarget = IsMagicalCreatureFamily(defenderFamily);

                // Preserve the existing Slayer weapon rules exactly: target-family
                // matches are alternatives, not cumulative requirements.
                int conditionalSlayerBonus =
                    ((isBeastTarget && IsBeastSlayerAttack(attackMode)) ||
                     (isFeyTarget && IsFeySlayerAttack(attackMode)) ||
                     (isMagicalTarget && IsMagicalSlayerAttack(attackMode)))
                        ? 1
                        : GetCreatureFamilySlayerLongswordBonus(attackMode, defender, defenderFamily);
                bool matchedSlayerRule = conditionalSlayerBonus > 0;

                // Creature-family shields protect only the currently equipped defender,
                // and only from an attacker matching the shield's configured family.
                string attackerFamily = ReadMember(__instance, "CharacterFamily");
                string matchedShieldName;
                int additionalShieldBonus = GetEquippedCreatureShieldBonus(
                    defender, __instance, attackerFamily, attackMode, out matchedShieldName);

                if (!matchedSlayerRule && additionalShieldBonus <= 0) return;

                object attackBefore = ReadNumericMember(actionModifier,
                    new[] { "AttackRollModifier", "attackRollModifier" });
                object damageBefore = ReadNumericMember(actionModifier,
                    new[] { "DamageRollModifier", "damageRollModifier" });
                int modifierId = RuntimeHelpers.GetHashCode(actionModifier);

                // Share the existing ActionModifier stamp so the combined effects
                // cannot add a Slayer bonus or shield penalty twice to one calculation.
                AppliedStamp stamp;
                if (AppliedModifiers.TryGetValue(actionModifier, out stamp) &&
                    ValuesEqual(stamp.LastAttackValue, attackBefore) &&
                    ValuesEqual(stamp.LastDamageValue, damageBefore))
                {
                    if (additionalShieldBonus > 0)
                    {
                        LogShieldMatch("Duplicate calculation seen; shield penalty not applied twice. " +
                            "Shield=" + matchedShieldName +
                            "; Attacker=" + DescribeCharacter(__instance) +
                            "; Defender=" + DescribeCharacter(defender) +
                            "; ActionModifierId=" + modifierId);
                    }
                    return;
                }

                bool slayerAttackChanged = false;
                bool slayerDamageChanged = false;
                bool shieldAttackChanged = false;

                if (matchedSlayerRule)
                {
                    // The weapon already supplies its base +1. Add only +1 here,
                    // preserving the original +2 total against its matching family.
                    slayerAttackChanged = AddToNumericMember(actionModifier,
                        new[] { "AttackRollModifier", "attackRollModifier" }, conditionalSlayerBonus);
                    slayerDamageChanged = AddToNumericMember(actionModifier,
                        new[] { "DamageRollModifier", "damageRollModifier" }, conditionalSlayerBonus);
                }

                if (additionalShieldBonus > 0)
                {
                    shieldAttackChanged = AddToNumericMember(actionModifier,
                        new[] { "AttackRollModifier", "attackRollModifier" }, -additionalShieldBonus);
                }

                object attackAfter = ReadNumericMember(actionModifier,
                    new[] { "AttackRollModifier", "attackRollModifier" });
                object damageAfter = ReadNumericMember(actionModifier,
                    new[] { "DamageRollModifier", "damageRollModifier" });

                if (slayerAttackChanged || slayerDamageChanged || shieldAttackChanged)
                {
                    if (AppliedModifiers.TryGetValue(actionModifier, out stamp))
                    {
                        stamp.LastAttackValue = attackAfter;
                        stamp.LastDamageValue = damageAfter;
                    }
                    else
                    {
                        AppliedModifiers.Add(actionModifier, new AppliedStamp
                        {
                            LastAttackValue = attackAfter,
                            LastDamageValue = damageAfter
                        });
                    }
                }

                if (matchedSlayerRule)
                {
                    LogMatchingCall("Conditional +" + conditionalSlayerBonus +
                        " attack and +" + conditionalSlayerBonus +
                        " damage against " + defenderFamily + ". " +
                        "Attacker=" + DescribeCharacter(__instance) +
                        "; Defender=" + DescribeCharacter(defender) +
                        "; AttackMode=" + DescribeAttackMode(attackMode) +
                        "; ActionModifierId=" + modifierId +
                        "; AttackBefore=" + DisplayValue(attackBefore) +
                        "; AttackAfter=" + DisplayValue(attackAfter) +
                        "; DamageBefore=" + DisplayValue(damageBefore) +
                        "; DamageAfter=" + DisplayValue(damageAfter) +
                        "; SlayerAttackUpdated=" + slayerAttackChanged +
                        "; SlayerDamageUpdated=" + slayerDamageChanged);

                    if (!slayerAttackChanged)
                        Debug.LogWarning(LogPrefix + "Could not find a writable numeric AttackRollModifier on ActionModifier.");
                    if (!slayerDamageChanged)
                        Debug.LogWarning(LogPrefix + "Could not find a writable numeric DamageRollModifier on ActionModifier.");
                }

                if (additionalShieldBonus > 0)
                {
                    if (shieldAttackChanged)
                    {
                        LogShieldMatch("Creature-family shield defense applied. " +
                            "Shield=" + matchedShieldName +
                            "; Attacker=" + DescribeCharacter(__instance) +
                            "; Defender=" + DescribeCharacter(defender) +
                            "; AttackMode=" + DescribeAttackMode(attackMode) +
                            "; AdditionalConditionalAC=+" + additionalShieldBonus +
                            "; ActionModifierId=" + modifierId +
                            "; AttackRollModifier=" + DisplayValue(attackBefore) +
                            " -> " + DisplayValue(attackAfter) +
                            "; AppliedAttackPenalty=" + (-additionalShieldBonus));
                    }
                    else
                    {
                        Debug.LogWarning(ShieldLogPrefix +
                            matchedShieldName + " found equipped, but the attack-roll modifier could not be changed.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + "Conditional combat modifier failed: " + ex);
            }
        }

        private static void LogMatchingCall(string message)
        {
            if (matchingCallsLogged < MaxMatchingLogs)
            {
                matchingCallsLogged++;
                Debug.Log(LogPrefix + "MATCH #" + matchingCallsLogged + "; " + message);
                if (matchingCallsLogged == MaxMatchingLogs)
                    Debug.Log(LogPrefix + "Further matching-call logs suppressed.");
            }
        }

        private static void LogShieldMatch(string message)
        {
            if (shieldCallsLogged >= MaxShieldLogs) return;

            shieldCallsLogged++;
            Debug.Log(ShieldLogPrefix + "MATCH #" + shieldCallsLogged + "; " + message);
            if (shieldCallsLogged == MaxShieldLogs)
                Debug.Log(ShieldLogPrefix + "Further matching-shield logs suppressed.");
        }

        private static int GetEquippedCreatureShieldBonus(
            object defender,
            object attacker,
            string attackerFamily,
            object attackMode,
            out string matchedShieldName)
        {
            matchedShieldName = null;
            if (defender == null || attacker == null) return 0;

            // InventorySlotsByName is the same inventory structure already used by
            // this project in Rings.cs. Inspect only the active OffHandSlot.
            object inventory = ReadMemberObject(defender, "CharacterInventory") ??
                               ReadMemberObject(defender, "characterInventory");
            if (inventory == null) return 0;

            object slotsObject = ReadMemberObject(inventory, "InventorySlotsByName") ??
                                 ReadMemberObject(inventory, "inventorySlotsByName");
            IEnumerable slots = slotsObject as IEnumerable;
            if (slots == null) return 0;

            int highestBonus = 0;
            foreach (object entry in slots)
            {
                object slot = ReadMemberObject(entry, "Value") ??
                              ReadMemberObject(entry, "value");
                if (slot == null) continue;

                object slotType = ReadMemberObject(slot, "SlotTypeDefinition") ??
                                  ReadMemberObject(slot, "slotTypeDefinition");
                string slotTypeName = ReadMember(slotType, "Name") ??
                                      ReadMember(slotType, "name");
                if (!string.Equals(slotTypeName, "OffHandSlot", StringComparison.OrdinalIgnoreCase))
                    continue;

                object equippedItem = ReadMemberObject(slot, "EquipedItem") ??
                                      ReadMemberObject(slot, "equipedItem");
                if (equippedItem == null) continue;

                object itemDefinition = ReadMemberObject(equippedItem, "ItemDefinition") ??
                                        ReadMemberObject(equippedItem, "itemDefinition");
                string itemName = ReadMember(itemDefinition, "Name") ??
                                  ReadMember(itemDefinition, "name");
                if (string.IsNullOrEmpty(itemName)) continue;

                int bonus = GetConditionalShieldBonus(itemName, attacker, attackerFamily, attackMode);
                if (bonus > highestBonus)
                {
                    highestBonus = bonus;
                    matchedShieldName = ReadMember(itemDefinition, "GuiPresentation") ?? itemName;
                    object gui = ReadMemberObject(itemDefinition, "GuiPresentation") ??
                                 ReadMemberObject(itemDefinition, "guiPresentation");
                    string title = ReadMember(gui, "Title") ?? ReadMember(gui, "title");
                    if (!string.IsNullOrEmpty(title)) matchedShieldName = title;
                }
            }

            return highestBonus;
        }

        private static int GetConditionalShieldBonus(
            string itemName,
            object attacker,
            string attackerFamily,
            object attackMode)
        {
            // +1 family shields have +1 universal AC and gain +1 more against their
            // selected family. Their missile variant gains +2 conditional AC (+3 total).
            // +2 family shields have +2 universal AC and gain only +1 more (+3 total).
            // Ranged spell attacks are excluded from both missile shields.
            if (ItemIs(itemName, "ShieldVsMissilesPlus1"))
                return IsMissileWeaponAttack(attackMode) ? 2 : 0;
            if (ItemIs(itemName, "ShieldVsMissilesPlus2"))
                return IsMissileWeaponAttack(attackMode) ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsGiantsPlus1"))
                return FamilyIs(attackerFamily, "Giant") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsGiantsPlus2"))
                return FamilyIs(attackerFamily, "Giant") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsAberrationsPlus1"))
                return FamilyIs(attackerFamily, "Aberration") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsAberrationsPlus2"))
                return FamilyIs(attackerFamily, "Aberration") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsArtificerConstructsPlus1"))
                return IsArtificerConstruct(attacker, attackerFamily) ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsArtificerConstructsPlus2"))
                return IsArtificerConstruct(attacker, attackerFamily) ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsBeastsPlus1"))
                return FamilyIs(attackerFamily, "Beast") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsBeastsPlus2"))
                return FamilyIs(attackerFamily, "Beast") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsCelestialsPlus1"))
                return FamilyIs(attackerFamily, "Celestial") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsCelestialsPlus2"))
                return FamilyIs(attackerFamily, "Celestial") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsConstructsPlus1"))
                return FamilyIs(attackerFamily, "Construct") &&
                       !IsArtificerConstruct(attacker, attackerFamily) ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsConstructsPlus2"))
                return FamilyIs(attackerFamily, "Construct") &&
                       !IsArtificerConstruct(attacker, attackerFamily) ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsElementalsPlus1"))
                return FamilyIs(attackerFamily, "Elemental") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsElementalsPlus2"))
                return FamilyIs(attackerFamily, "Elemental") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsFeyPlus1"))
                return FamilyIs(attackerFamily, "Fey") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsFeyPlus2"))
                return FamilyIs(attackerFamily, "Fey") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsHumanoidsPlus1"))
                return FamilyIs(attackerFamily, "Humanoid") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsHumanoidsPlus2"))
                return FamilyIs(attackerFamily, "Humanoid") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsMonstrositiesPlus1"))
                return FamilyIs(attackerFamily, "Monstrosity") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsMonstrositiesPlus2"))
                return FamilyIs(attackerFamily, "Monstrosity") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsPlantsPlus1"))
                return FamilyIs(attackerFamily, "Plant") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsPlantsPlus2"))
                return FamilyIs(attackerFamily, "Plant") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsUndeadPlus1"))
                return FamilyIs(attackerFamily, "Undead") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsUndeadPlus2"))
                return FamilyIs(attackerFamily, "Undead") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsSorrAkkathPlus1"))
                return IsSorrAkkath(attacker, attackerFamily) ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsSorrAkkathPlus2"))
                return IsSorrAkkath(attacker, attackerFamily) ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsDragonsPlus1"))
                return FamilyIs(attackerFamily, "Dragon") ? 1 : 0;
            if (ItemIs(itemName, "ShieldVsDragonsPlus2"))
                return FamilyIs(attackerFamily, "Dragon") ? 1 : 0;

            // Special shield: +1 universally, +2 total vs Fey, +3 total vs Elementals.
            if (ItemIs(itemName, "ShieldVsFeyAndElementalsPlus1"))
            {
                if (FamilyIs(attackerFamily, "Elemental")) return 2;
                if (FamilyIs(attackerFamily, "Fey")) return 1;
            }

            return 0;
        }

        private static bool IsMissileWeaponAttack(object attackMode)
        {
            if (attackMode == null)
                return false;

            object rangedValue = ReadMemberObject(attackMode, "Ranged") ??
                                 ReadMemberObject(attackMode, "ranged");
            object thrownValue = ReadMemberObject(attackMode, "Thrown") ??
                                 ReadMemberObject(attackMode, "thrown");

            bool isRanged = rangedValue is bool && (bool)rangedValue;
            bool isThrown = thrownValue is bool && (bool)thrownValue;
            if (!isRanged && !isThrown)
                return false;

            // WeaponDescription is populated for physical weapon attacks. Requiring
            // it avoids granting the missile bonus against ranged spell attacks.
            object weaponDescription = ReadMemberObject(attackMode, "WeaponDescription") ??
                                       ReadMemberObject(attackMode, "weaponDescription");
            if (weaponDescription != null)
                return true;

            // Some thrown-weapon attack modes expose their source definition directly
            // rather than carrying WeaponDescription. Accept those only when the mode
            // identifies a weapon definition, never merely an effect description.
            object weaponDefinition = ReadMemberObject(attackMode, "WeaponDefinition") ??
                                     ReadMemberObject(attackMode, "weaponDefinition");
            return weaponDefinition != null;
        }

        private static bool ItemIs(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool FamilyIs(string actualFamily, string expectedFamily)
        {
            return NormalizeFamily(actualFamily) == NormalizeFamily(expectedFamily);
        }

        private static string NormalizeFamily(string family)
        {
            if (string.IsNullOrEmpty(family)) return string.Empty;

            string value = family.Trim()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .ToLowerInvariant();

            // Family names in UI labels are plural, while CharacterFamily is
            // normally singular. Accept both forms without broad substring matches.
            switch (value)
            {
                case "giants": return "giant";
                case "aberrations": return "aberration";
                case "artificerconstructs": return "artificerconstruct";
                case "beasts": return "beast";
                case "celestials": return "celestial";
                case "constructs": return "construct";
                case "elementals": return "elemental";
                case "humanoids": return "humanoid";
                case "monstrosities": return "monstrosity";
                case "plants": return "plant";
                case "dragons": return "dragon";
                case "sorrakkaths": return "sorrakkath";
                default: return value;
            }
        }

        private static bool IsArtificerConstruct(object attacker, string attackerFamily)
        {
            if (FamilyIs(attackerFamily, "ArtificerConstruct")) return true;
            if (!FamilyIs(attackerFamily, "Construct")) return false;

            // Keep this separate from the ordinary Constructs shield. Within the
            // broad Construct family, use the monster definition's identity to pick
            // out artificer-built types such as Steel Defenders and Homunculi.
            return AttackerIdentityContains(attacker,
                "artificer", "steeldefender", "homunculus", "dancingweapon");
        }

        private static bool IsSorrAkkath(object attacker, string attackerFamily)
        {
            if (FamilyIs(attackerFamily, "Sorr-Akkath")) return true;

            // Sorr-Akkath are also referred to as Soraks in Solasta's creature names.
            return AttackerIdentityContains(attacker,
                "sorrakkath", "sorak", "sessroth", "sorrtarr");
        }

        private static bool AttackerIdentityContains(object attacker, params string[] tokens)
        {
            if (attacker == null || tokens == null || tokens.Length == 0) return false;

            if (ObjectIdentityContains(attacker, tokens)) return true;

            string[] nestedMembers =
            {
                "MonsterDefinition", "monsterDefinition",
                "RaceDefinition", "raceDefinition",
                "OriginalFormCharacter", "originalFormCharacter"
            };

            foreach (string memberName in nestedMembers)
            {
                object nested = ReadMemberObject(attacker, memberName);
                if (nested != null && ObjectIdentityContains(nested, tokens)) return true;
            }

            return false;
        }

        private static bool ObjectIdentityContains(object value, string[] tokens)
        {
            if (value == null) return false;

            string[] textMembers = { "Name", "name", "SystemName", "systemName", "Title", "title" };
            foreach (string memberName in textMembers)
            {
                string candidate = ReadMember(value, memberName);
                if (TextContainsAnyToken(candidate, tokens)) return true;
            }

            object gui = ReadMemberObject(value, "GuiPresentation") ??
                         ReadMemberObject(value, "guiPresentation");
            if (gui != null)
            {
                foreach (string memberName in new[] { "Title", "title", "Description", "description" })
                {
                    string candidate = ReadMember(gui, memberName);
                    if (TextContainsAnyToken(candidate, tokens)) return true;
                }
            }

            return false;
        }

        private static bool TextContainsAnyToken(string text, string[] tokens)
        {
            if (string.IsNullOrEmpty(text)) return false;

            string normalized = NormalizeIdentity(text);
            foreach (string token in tokens)
            {
                string normalizedToken = NormalizeIdentity(token);
                if (normalizedToken.Length > 0 && normalized.IndexOf(
                    normalizedToken, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string NormalizeIdentity(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var chars = new List<char>(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c)) chars.Add(char.ToLowerInvariant(c));
            }
            return new string(chars.ToArray());
        }

        private static int GetCreatureFamilySlayerLongswordBonus(
            object attackMode,
            object defender,
            string defenderFamily)
        {
            // Find the equipped weapon ID once. This postfix runs for every attack,
            // so avoid repeating a full reflection scan for every possible family.
            string weaponName = FindCreatureFamilySlayerLongswordName(attackMode);
            if (string.IsNullOrEmpty(weaponName))
                return 0;

            // Each longsword adds +1 attack and +1 damage against its matching family.
            // The weapon's own feature already supplies its universal +1/+1 bonus.
            if (WeaponNameIs(weaponName, "LongswordVsGiantsPlus1")) return FamilyIs(defenderFamily, "Giant") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsAberrationsPlus1")) return FamilyIs(defenderFamily, "Aberration") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsArtificerConstructsPlus1")) return IsArtificerConstruct(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsCelestialsPlus1")) return FamilyIs(defenderFamily, "Celestial") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsConstructsPlus1")) return FamilyIs(defenderFamily, "Construct") && !IsArtificerConstruct(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsElementalsPlus1")) return FamilyIs(defenderFamily, "Elemental") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsFeyPlus1")) return FamilyIs(defenderFamily, "Fey") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsHumanoidsPlus1")) return FamilyIs(defenderFamily, "Humanoid") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsMonstrositiesPlus1")) return FamilyIs(defenderFamily, "Monstrosity") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsPlantsPlus1")) return FamilyIs(defenderFamily, "Plant") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsUndeadPlus1")) return FamilyIs(defenderFamily, "Undead") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsSorrAkkathPlus1")) return IsSorrAkkath(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordVsDragonsPlus1")) return FamilyIs(defenderFamily, "Dragon") ? 1 : 0;

            if (WeaponNameIs(weaponName, "LongswordPlus2VsDragons")) return FamilyIs(defenderFamily, "Dragon") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsSorrAkkathPlus3")) return IsSorrAkkath(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsGoblinoidsPlus3")) return MatchesTarget(defender, defenderFamily, "Goblin") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsGiantsPlus3FeyPlus4"))
            {
                if (FamilyIs(defenderFamily, "Fey")) return 2;
                return FamilyIs(defenderFamily, "Giant") ? 1 : 0;
            }
            if (WeaponNameIs(weaponName, "LongswordPlus2VsDwarvesPlus3")) return MatchesTarget(defender, defenderFamily, "Dwarf") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsElvesPlus3")) return MatchesTarget(defender, defenderFamily, "Elf") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsHalflingsPlus3")) return MatchesTarget(defender, defenderFamily, "Halfling") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsBeastsPlus3")) return FamilyIs(defenderFamily, "Beast") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsHumansPlus3")) return MatchesTarget(defender, defenderFamily, "Human") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsUndeadPlus3")) return FamilyIs(defenderFamily, "Undead") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsAbominationsPlus3")) return FamilyIs(defenderFamily, "Abomination") ? 1 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus2VsDragonsPlus3")) return FamilyIs(defenderFamily, "Dragon") ? 1 : 0;

            // These three weapons are +3 only against their named creature family.
            // They carry no universal attack/damage modifier on the item itself.
            if (WeaponNameIs(weaponName, "LongswordPlus3VsSorrAkkath")) return IsSorrAkkath(defender, defenderFamily) ? 3 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus3VsDragons")) return FamilyIs(defenderFamily, "Dragon") ? 3 : 0;
            if (WeaponNameIs(weaponName, "LongswordPlus3VsUndead")) return FamilyIs(defenderFamily, "Undead") ? 3 : 0;

            if (WeaponNameIs(weaponName, "ShortswordVsGiantsPlus1")) return FamilyIs(defenderFamily, "Giant") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsAberrationsPlus1")) return FamilyIs(defenderFamily, "Aberration") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsArtificerConstructsPlus1")) return IsArtificerConstruct(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsBeastsPlus1")) return FamilyIs(defenderFamily, "Beast") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsCelestialsPlus1")) return FamilyIs(defenderFamily, "Celestial") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsConstructsPlus1")) return FamilyIs(defenderFamily, "Construct") && !IsArtificerConstruct(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsElementalsPlus1")) return FamilyIs(defenderFamily, "Elemental") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsFeyPlus1")) return FamilyIs(defenderFamily, "Fey") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsHumanoidsPlus1")) return FamilyIs(defenderFamily, "Humanoid") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsMonstrositiesPlus1")) return FamilyIs(defenderFamily, "Monstrosity") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsPlantsPlus1")) return FamilyIs(defenderFamily, "Plant") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsUndeadPlus1")) return FamilyIs(defenderFamily, "Undead") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsSorrAkkathPlus1")) return IsSorrAkkath(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordVsDragonsPlus1")) return FamilyIs(defenderFamily, "Dragon") ? 1 : 0;

            // Dagger +1 weapons gain an additional +1 attack and +1 damage against
            // their designated creature family, for +2/+2 total against that family.
            if (WeaponNameIs(weaponName, "DaggerVsGiantsPlus1")) return FamilyIs(defenderFamily, "Giant") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsAberrationsPlus1")) return FamilyIs(defenderFamily, "Aberration") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsArtificerConstructsPlus1")) return IsArtificerConstruct(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsBeastsPlus1")) return FamilyIs(defenderFamily, "Beast") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsCelestialsPlus1")) return FamilyIs(defenderFamily, "Celestial") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsConstructsPlus1")) return FamilyIs(defenderFamily, "Construct") && !IsArtificerConstruct(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsElementalsPlus1")) return FamilyIs(defenderFamily, "Elemental") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsFeyPlus1")) return FamilyIs(defenderFamily, "Fey") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsHumanoidsPlus1")) return FamilyIs(defenderFamily, "Humanoid") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsMonstrositiesPlus1")) return FamilyIs(defenderFamily, "Monstrosity") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsPlantsPlus1")) return FamilyIs(defenderFamily, "Plant") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsUndeadPlus1")) return FamilyIs(defenderFamily, "Undead") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsSorrAkkathPlus1")) return IsSorrAkkath(defender, defenderFamily) ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerVsDragonsPlus1")) return FamilyIs(defenderFamily, "Dragon") ? 1 : 0;

            // Dagger +2 slayers gain +1 attack and damage against their target.
            // The combined Giants/Fey dagger grants +1 vs Giants and +2 vs Fey.
            if (WeaponNameIs(weaponName, "DaggerPlus2VsGoblinoidsPlus3")) return MatchesTarget(defender, defenderFamily, "Goblin") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerPlus2VsGiantsPlus3FeyPlus4"))
            {
                if (FamilyIs(defenderFamily, "Fey")) return 2;
                return FamilyIs(defenderFamily, "Giant") ? 1 : 0;
            }
            if (WeaponNameIs(weaponName, "DaggerPlus2VsDwarvesPlus3")) return MatchesTarget(defender, defenderFamily, "Dwarf") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerPlus2VsElvesPlus3")) return MatchesTarget(defender, defenderFamily, "Elf") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerPlus2VsHalflingsPlus3")) return MatchesTarget(defender, defenderFamily, "Halfling") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerPlus2VsBeastsPlus3")) return FamilyIs(defenderFamily, "Beast") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerPlus2VsHumansPlus3")) return MatchesTarget(defender, defenderFamily, "Human") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerPlus2VsUndeadPlus3")) return FamilyIs(defenderFamily, "Undead") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerPlus2VsAbominationsPlus3")) return FamilyIs(defenderFamily, "Abomination") ? 1 : 0;
            if (WeaponNameIs(weaponName, "DaggerPlus3VsDragons")) return FamilyIs(defenderFamily, "Dragon") ? 3 : 0;

            if (WeaponNameIs(weaponName, "ShortswordPlus2VsGoblinoidsPlus3")) return MatchesTarget(defender, defenderFamily, "Goblin") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordPlus2VsGiantsPlus3FeyPlus4"))
            {
                if (FamilyIs(defenderFamily, "Fey")) return 2;
                return FamilyIs(defenderFamily, "Giant") ? 1 : 0;
            }
            if (WeaponNameIs(weaponName, "ShortswordPlus2VsDwarvesPlus3")) return MatchesTarget(defender, defenderFamily, "Dwarf") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordPlus2VsElvesPlus3")) return MatchesTarget(defender, defenderFamily, "Elf") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordPlus2VsHalflingsPlus3")) return MatchesTarget(defender, defenderFamily, "Halfling") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordPlus2VsBeastsPlus3")) return FamilyIs(defenderFamily, "Beast") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordPlus2VsHumansPlus3")) return MatchesTarget(defender, defenderFamily, "Human") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordPlus2VsUndeadPlus3")) return FamilyIs(defenderFamily, "Undead") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordPlus2VsAbominationsPlus3")) return FamilyIs(defenderFamily, "Abomination") ? 1 : 0;
            if (WeaponNameIs(weaponName, "ShortswordPlus3VsDragons")) return FamilyIs(defenderFamily, "Dragon") ? 3 : 0;

            return 0;
        }

        private static bool MatchesTarget(object defender, string defenderFamily, string token)
        {
            if (FamilyIs(defenderFamily, token)) return true;
            foreach (string memberName in new[] { "Name", "CharacterName", "OriginalFormCharacterName",
                "MonsterDefinition", "CharacterDefinition", "RaceDefinition", "AncestryDefinition" })
            {
                object value = ReadMemberObject(defender, memberName);
                string text = value == null ? null : ReadMember(value, "Name") ?? SafeToString(value);
                if (!string.IsNullOrEmpty(text) &&
                    text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string FindCreatureFamilySlayerLongswordName(object attackMode)
        {
            if (attackMode == null)
                return null;

            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition",
                "sourceDefinition", "SourceDefinition",
                "itemDefinition", "ItemDefinition",
                "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                string name = GetCreatureFamilyLongswordNameFromValue(
                    ReadMemberObject(attackMode, memberName));
                if (!string.IsNullOrEmpty(name))
                    return name;
            }

            // Some game versions nest the definition under a member whose exact name
            // differs. Inspect only weapon/item members, matching the original Slayer
            // weapon reflection approach.
            foreach (MemberInfo member in GetMembers(attackMode.GetType()))
            {
                if (member.Name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) < 0 &&
                    member.Name.IndexOf("item", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                string name = GetCreatureFamilyLongswordNameFromValue(
                    GetValue(attackMode, member));
                if (!string.IsNullOrEmpty(name))
                    return name;
            }

            return null;
        }

        private static string GetCreatureFamilyLongswordNameFromValue(object value)
        {
            if (value == null)
                return null;

            string name = ReadMember(value, "Name");
            if (IsCreatureFamilyLongswordName(name))
                return name;

            string text = SafeToString(value);
            return IsCreatureFamilyLongswordName(text) ? text : null;
        }

        private static bool IsCreatureFamilyLongswordName(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                (value.IndexOf("LongswordVs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("LongswordPlus2Vs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("LongswordPlus3Vs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("ShortswordVs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("ShortswordPlus2Vs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("ShortswordPlus3Vs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("DaggerVs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("DaggerPlus2Vs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("DaggerPlus3Vs", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool WeaponNameIs(string actual, string expected)
        {
            return !string.IsNullOrEmpty(actual) &&
                actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsBeastSlayerAttack(object attackMode)
        {
            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition",
                "sourceDefinition", "SourceDefinition",
                "itemDefinition", "ItemDefinition",
                "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                object value = ReadMemberObject(attackMode, memberName);
                if (value == null) continue;

                string name = ReadMember(value, "Name");
                if (IsBeastSlayerWeaponName(name))
                    return true;

                if (IsBeastSlayerWeaponName(SafeToString(value)))
                    return true;
            }

            // Some game versions nest the weapon definition under another member.
            foreach (MemberInfo member in GetMembers(attackMode.GetType()))
            {
                if (member.Name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) < 0 &&
                    member.Name.IndexOf("item", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                object value = GetValue(attackMode, member);
                if (value == null) continue;

                if (IsBeastSlayerWeaponName(ReadMember(value, "Name")))
                    return true;
            }

            return false;
        }

        private static bool IsBeastSlayerWeaponName(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            return value.IndexOf("BeastSlayerLongsword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("BeastSlayerShortsword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("BeastSlayerGreatsword", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsFeySlayerAttack(object attackMode)
        {
            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition",
                "sourceDefinition", "SourceDefinition",
                "itemDefinition", "ItemDefinition",
                "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                object value = ReadMemberObject(attackMode, memberName);
                if (value == null) continue;

                string name = ReadMember(value, "Name");
                if (IsFeySlayerWeaponName(name))
                    return true;

                if (IsFeySlayerWeaponName(SafeToString(value)))
                    return true;
            }

            // Some game versions nest the weapon definition under another member.
            foreach (MemberInfo member in GetMembers(attackMode.GetType()))
            {
                if (member.Name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) < 0 &&
                    member.Name.IndexOf("item", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                object value = GetValue(attackMode, member);
                if (value == null) continue;

                if (IsFeySlayerWeaponName(ReadMember(value, "Name")))
                    return true;
            }

            return false;
        }

        private static bool IsFeySlayerWeaponName(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            return value.IndexOf("FeySlayerShortsword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("FeySlayerDagger", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMagicalSlayerAttack(object attackMode)
        {
            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition",
                "sourceDefinition", "SourceDefinition",
                "itemDefinition", "ItemDefinition",
                "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                object value = ReadMemberObject(attackMode, memberName);
                if (value == null) continue;

                if (IsMagicalSlayerWeaponName(ReadMember(value, "Name")) ||
                    IsMagicalSlayerWeaponName(SafeToString(value)))
                    return true;
            }

            // Some game versions nest the weapon definition under another member.
            foreach (MemberInfo member in GetMembers(attackMode.GetType()))
            {
                if (member.Name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) < 0 &&
                    member.Name.IndexOf("item", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                object value = GetValue(attackMode, member);
                if (value != null && IsMagicalSlayerWeaponName(ReadMember(value, "Name")))
                    return true;
            }

            return false;
        }

        private static bool IsMagicalSlayerWeaponName(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf("MagicalSlayerBow", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMagicalCreatureFamily(string family)
        {
            if (string.IsNullOrEmpty(family)) return false;

            // Solasta's ordinary bestiary uses creature families such as
            // Celestial, Construct, Dragon, Elemental, Fey, Fiend,
            // Monstrosity, Plant and Undead rather than one universal "Magical" family.
            // Accept the literal family too, in case another mod defines it.
            string[] magicalFamilies =
            {
                "Magical",
                "Celestial",
                "Construct",
                "Dragon",
                "Elemental",
                "Fey",
                "Fiend",
                "Monstrosity",
                "Plant",
                "Undead"
            };

            foreach (string magicalFamily in magicalFamilies)
            {
                if (string.Equals(family, magicalFamily, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool AddToNumericMember(object target, string[] names, int amount)
        {
            if (target == null) return false;
            Type type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (string name in names)
            {
                for (Type current = type; current != null; current = current.BaseType)
                {
                    FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                    if (field != null && !field.IsInitOnly &&
                        (field.FieldType == typeof(int) || field.FieldType == typeof(float)))
                    {
                        if (field.FieldType == typeof(int))
                            field.SetValue(target, (int)field.GetValue(target) + amount);
                        else
                            field.SetValue(target, (float)field.GetValue(target) + amount);
                        return true;
                    }

                    PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                    if (property == null || !property.CanRead || !property.CanWrite ||
                        property.GetIndexParameters().Length != 0) continue;

                    if (property.PropertyType == typeof(int))
                    {
                        property.SetValue(target, (int)property.GetValue(target, null) + amount, null);
                        return true;
                    }
                    if (property.PropertyType == typeof(float))
                    {
                        property.SetValue(target, (float)property.GetValue(target, null) + amount, null);
                        return true;
                    }
                }
            }
            return false;
        }

        private static object ReadNumericMember(object target, string[] names)
        {
            if (target == null) return null;
            foreach (string name in names)
            {
                object value = ReadMemberObject(target, name);
                if (value is int || value is float) return value;
            }
            return null;
        }

        private static bool ValuesEqual(object left, object right)
        {
            if (left == null || right == null) return left == right;
            return left.Equals(right);
        }

        private static string DisplayValue(object value)
        {
            return value == null ? "<not found>" : SafeToString(value);
        }

        private static string DescribeCharacter(object value)
        {
            if (value == null) return "<null>";
            return SafeReadMember(value, "Name") + "/" + SafeReadMember(value, "SystemName") +
                "/Family=" + SafeReadMember(value, "CharacterFamily");
        }

        private static string DescribeAttackMode(object attackMode)
        {
            foreach (string memberName in new[]
            {
                "weaponDefinition", "WeaponDefinition", "weaponDescription", "WeaponDescription",
                "sourceDefinition", "SourceDefinition", "itemDefinition", "ItemDefinition",
                "weapon", "Weapon", "sourceObject", "SourceObject"
            })
            {
                object value = ReadMemberObject(attackMode, memberName);
                if (value != null)
                    return memberName + "=" + value.GetType().Name + "(" + SafeReadMember(value, "Name") + ")";
            }
            return attackMode == null ? "<null>" : attackMode.GetType().Name;
        }

        private static string SafeReadMember(object target, string name)
        {
            try
            {
                object value = ReadMemberObject(target, name);
                return value == null ? "<null>" : SafeToString(value);
            }
            catch { return "<read failed>"; }
        }

        private static object ReadMemberObject(object target, string name)
        {
            if (target == null) return null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
                PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }
            return null;
        }

        private static string ReadMember(object target, string name)
        {
            object value = ReadMemberObject(target, name);
            return value == null ? null : SafeToString(value);
        }

        private static MemberInfo[] GetMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var all = new List<MemberInfo>();
            for (Type current = type; current != null; current = current.BaseType)
            {
                all.AddRange(current.GetFields(flags));
                all.AddRange(current.GetProperties(flags));
            }
            return all.ToArray();
        }

        private static object GetValue(object target, MemberInfo member)
        {
            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null) return field.GetValue(target);
                PropertyInfo property = member as PropertyInfo;
                if (property != null && property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(true) != null)
                    return property.GetValue(target, null);
            }
            catch { }
            return null;
        }

        private static string SafeToString(object value)
        {
            try { return value == null ? "<null>" : value.ToString(); }
            catch { return "<ToString failed>"; }
        }
    }
}
