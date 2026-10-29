
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
                    CreateAttackModifier(itemId, displayName, 1);

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
                    CreateAttackModifier(itemId, displayName, 1);
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
                    CreateAttackModifier(itemId, displayName, 1);
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
                    CreateAttackModifier(itemId, displayName, 1);
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
                    CreateAttackModifier(itemId, displayName, 1);
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
                    CreateAttackModifier(itemId, displayName, bonus);

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

        private static FeatureDefinitionAttackModifier CreateAttackModifier(
            string itemId,
            string displayName,
            int bonus)
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

        private static ItemPropertyDescription CreateFeatureProperty(
            Database<ItemDefinition> itemDb,
            ItemDefinition baseItem,
            FeatureDefinitionAttackModifier modifier)
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
                property, "featureDefinition", modifier);

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

        private static bool SetField(
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