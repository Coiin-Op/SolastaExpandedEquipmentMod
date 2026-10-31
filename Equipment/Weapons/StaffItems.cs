using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SolastaExpandedEquipmentMod
{
    /// <summary>Registers custom charge-based and daily-use staves.</summary>
    internal static class StaffItems
    {
        private const string LogPrefix = "[Solasta Expanded Equipment] ";

        private sealed class SpellCost
        {
            internal readonly string[] Ids;
            internal readonly string TitleFragment;
            internal readonly int Cost;

            internal SpellCost(string titleFragment, int cost, params string[] ids)
            {
                Ids = ids;
                TitleFragment = titleFragment;
                Cost = cost;
            }
        }

        private sealed class DailySpell
        {
            internal readonly string[] Ids;
            internal readonly string TitleFragment;
            internal readonly int Uses;

            internal DailySpell(string titleFragment, int uses, params string[] ids)
            {
                Ids = ids;
                TitleFragment = titleFragment;
                Uses = uses;
            }
        }

        internal static bool Register()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            if (itemDb == null)
                return false;

            bool success = true;
            success &= RegisterChargedStaff(itemDb, "StaffOfWinter", "Staff of Winter", "VeryRare", 5,
                "A staff with 5 charges, recharging at dawn. Each spell costs 1 charge.", false,
                new SpellCost("Ice Knife", 1, "IceKnife"),
                new SpellCost("Sleet Storm", 1, "SleetStorm"),
                new SpellCost("Snowball Swarm", 1, "SnillocsSnowballSwarm", "SnillocsSnowballSwarm"),
                new SpellCost("Cone of Cold", 1, "ConeOfCold"));

            success &= RegisterDailyStaff(itemDb, "StaffOfLight", "Staff of Light", "Rare",
                new[]
                {
                    new DailySpell("Light", 3, "Light"),
                    new DailySpell("Moonbeam", 2, "Moonbeam"),
                    new DailySpell("Daylight", 1, "Daylight")
                });
            success &= RegisterDailyStaff(itemDb, "StaffOfGreaterLight", "Staff of Greater Light", "VeryRare",
                new[]
                {
                    new DailySpell("Light", 3, "Light"),
                    new DailySpell("Moonbeam", 3, "Moonbeam"),
                    new DailySpell("Daylight", 2, "Daylight"),
                    new DailySpell("Sunbeam", 1, "Sunbeam")
                });
            success &= RegisterDailyStaff(itemDb, "StaffOfHighestLight", "Staff of Highest Light", "Legendary",
                new[]
                {
                    new DailySpell("Light", 3, "Light"),
                    new DailySpell("Moonbeam", 3, "Moonbeam"),
                    new DailySpell("Daylight", 3, "Daylight"),
                    new DailySpell("Sunbeam", 2, "Sunbeam")
                });

            success &= RegisterChargedStaff(itemDb, "StaffOfLife", "Staff of Life", "Legendary", 5,
                "A staff with 5 charges, recharging at dawn. Each spell costs 1 charge.", false,
                new SpellCost("Revivify", 1, "Revivify"),
                new SpellCost("Raise Dead", 1, "RaiseDead"),
                new SpellCost("Resurrection", 1, "Resurrection"));
            success &= RegisterChargedStaff(itemDb, "StaffOfStorms", "Staff of Storms", "VeryRare", 5,
                "A staff with 5 charges, recharging at dawn. Each spell costs 1 charge.", false,
                new SpellCost("Sleet Storm", 1, "SleetStorm"),
                new SpellCost("Thunderwave", 1, "Thunderwave"),
                new SpellCost("Lightning Bolt", 1, "LightningBolt"),
                new SpellCost("Thunderstorm", 1, "ThunderStorm", "Thunderstorm"));

            FeatureDefinitionDamageAffinity greaterFireStaffResistance =
                ArmorEnchantmentFactory.CreateDamageResistance(
                    "GreaterStaffOfFireResistance", "DamageFire");
            success &= RegisterChargedStaffWithFeatures(itemDb,
                "GreaterStaffOfFire", "Greater Staff of Fire", "VeryRare", 10,
                "Grants resistance to fire damage while held. A staff with 10 charges, recharging at dawn. " +
                "Burning Hands costs 1 charge, Fireball 3 charges, and Wall of Fire 4 charges.", false,
                new FeatureDefinition[] { greaterFireStaffResistance },
                new SpellCost("Burning Hands", 1, "BurningHands"),
                new SpellCost("Fireball", 3, "Fireball"),
                new SpellCost("Wall of Fire", 4, "WallOfFire"));
            // Greater Staff of Fire is registered as an item definition above.
            // The optional treasure-table helper is not part of this project, so
            // do not call TreasureTableItemRegistrar here. Loot-table integration
            // can be added separately once the project has a supported registrar.

            success &= RegisterChargedStaff(itemDb, "StaffOfDefense", "Staff of Defense", "VeryRare", 5,
                "Grants +2 AC. A staff with 5 charges, recharging at dawn. Each spell costs 1 charge.", true,
                new SpellCost("Haste", 1, "Haste"),
                new SpellCost("Expeditious Retreat", 1, "ExpeditiousRetreat"),
                new SpellCost("Jump", 1, "Jump"),
                new SpellCost("Fly", 1, "Fly"));

            // The user listed two different staves of defense. Keep both and distinguish the later entry.
            success &= RegisterChargedStaff(itemDb, "GreaterStaffOfDefense", "Greater Staff of Defense", "Legendary", 5,
                "Grants +2 AC. A staff with 5 charges, recharging at dawn. Each spell costs 1 charge.", true,
                new SpellCost("Shield", 1, "Shield"),
                new SpellCost("Shield of Faith", 1, "ShieldOfFaith"),
                new SpellCost("Blur", 1, "Blur"),
                new SpellCost("Protection from Evil and Good", 1, "ProtectionFromEvilAndGood"),
                new SpellCost("Counterspell", 1, "Counterspell"));

            success &= RegisterChargedStaff(itemDb, "ExpandedStaffOfPower", "Staff of Power", "Legendary", 10,
                "Grants +2 AC. A staff with 10 charges, recharging at dawn. Magic Missile costs 1 charge; " +
                "Lightning Bolt 2; Fireball 3; Cone of Cold 4; Globe of Invulnerability and Hold Monster 5 charges each.", true,
                new SpellCost("Globe of Invulnerability", 5, "GlobeOfInvulnerability"),
                new SpellCost("Magic Missile", 1, "MagicMissile"),
                new SpellCost("Lightning Bolt", 2, "LightningBolt"),
                new SpellCost("Fireball", 3, "Fireball"),
                new SpellCost("Cone of Cold", 4, "ConeOfCold"),
                new SpellCost("Hold Monster", 5, "HoldMonster"));

            success &= RegisterChargedStaff(itemDb, "StaffOfTravel", "Staff of Travel", "Rare", 5,
                "A staff with 5 charges, recharging at dawn. Each spell costs 1 charge.", false,
                new SpellCost("Freedom of Movement", 1, "FreedomOfMovement"),
                new SpellCost("Jump", 1, "Jump"),
                new SpellCost("Levitate", 1, "Levitate"),
                new SpellCost("Fly", 1, "Fly"),
                new SpellCost("Longstrider", 1, "Longstrider"));
            success &= RegisterChargedStaff(itemDb, "GreaterStaffOfTravel", "Greater Staff of Travel", "VeryRare", 5,
                "A staff with 5 charges, recharging at dawn. Each spell costs 1 charge.", false,
                new SpellCost("Freedom of Movement", 1, "FreedomOfMovement"),
                new SpellCost("Jump", 1, "Jump"),
                new SpellCost("Levitate", 1, "Levitate"),
                new SpellCost("Fly", 1, "Fly"),
                new SpellCost("Longstrider", 1, "Longstrider"),
                new SpellCost("Dimension Door", 1, "DimensionDoor"));
            success &= RegisterChargedStaff(itemDb, "MasterStaffOfTravel", "Master Staff of Travel", "Legendary", 5,
                "A staff with 5 charges, recharging at dawn. Each spell costs 1 charge.", false,
                new SpellCost("Freedom of Movement", 1, "FreedomOfMovement"),
                new SpellCost("Jump", 1, "Jump"),
                new SpellCost("Levitate", 1, "Levitate"),
                new SpellCost("Fly", 1, "Fly"),
                new SpellCost("Longstrider", 1, "Longstrider"),
                new SpellCost("Dimension Door", 1, "DimensionDoor"),
                new SpellCost("Misty Step", 1, "MistyStep"));

            Debug.Log(LogPrefix + "Staff registration completed: " + success + ".");
            return success;
        }

        private static bool RegisterChargedStaff(
            Database<ItemDefinition> itemDb, string itemId, string title, string rarity,
            int chargePool, string description, bool grantsAcBonus, params SpellCost[] spellCosts)
        {
            return RegisterChargedStaffCore(itemDb, itemId, title, rarity, chargePool,
                description, grantsAcBonus, new FeatureDefinition[0], spellCosts);
        }

        private static bool RegisterChargedStaffWithFeatures(
            Database<ItemDefinition> itemDb, string itemId, string title, string rarity,
            int chargePool, string description, bool grantsAcBonus,
            FeatureDefinition[] additionalFeatures, params SpellCost[] spellCosts)
        {
            return RegisterChargedStaffCore(itemDb, itemId, title, rarity, chargePool,
                description, grantsAcBonus, additionalFeatures, spellCosts);
        }

        private static bool RegisterChargedStaffCore(
            Database<ItemDefinition> itemDb, string itemId, string title, string rarity,
            int chargePool, string description, bool grantsAcBonus,
            FeatureDefinition[] additionalFeatures, SpellCost[] spellCosts)
        {
            if (itemDb.GetElement(itemId, true) != null)
                return true;

            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            List<KeyValuePair<SpellDefinition, int>> functions = new List<KeyValuePair<SpellDefinition, int>>();
            foreach (SpellCost cost in spellCosts)
            {
                SpellDefinition spell = FindSpell(spellDb, cost.Ids, cost.TitleFragment);
                if (spell == null)
                {
                    Debug.LogError(LogPrefix + "Could not find " + cost.TitleFragment + " for " + title + ".");
                    return false;
                }
                functions.Add(new KeyValuePair<SpellDefinition, int>(spell, cost.Cost));
            }

            UsableDeviceDescription device = RingItems.CreateSpellFunctionsByCharges(itemDb, functions, chargePool);
            return RegisterStaff(itemDb, itemId, title, rarity, description,
                device, grantsAcBonus, additionalFeatures);
        }

        private static bool RegisterDailyStaff(
            Database<ItemDefinition> itemDb, string itemId, string title, string rarity, DailySpell[] dailySpells)
        {
            if (itemDb.GetElement(itemId, true) != null)
                return true;

            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            List<KeyValuePair<SpellDefinition, int>> functions = new List<KeyValuePair<SpellDefinition, int>>();
            List<string> details = new List<string>();
            foreach (DailySpell dailySpell in dailySpells)
            {
                SpellDefinition spell = FindSpell(spellDb, dailySpell.Ids, dailySpell.TitleFragment);
                if (spell == null)
                {
                    Debug.LogError(LogPrefix + "Could not find " + dailySpell.TitleFragment + " for " + title + ".");
                    return false;
                }
                functions.Add(new KeyValuePair<SpellDefinition, int>(spell, dailySpell.Uses));
                details.Add(dailySpell.TitleFragment + " " + dailySpell.Uses + " times per day");
            }

            UsableDeviceDescription device = RingItems.CreateSpellFunctionsByRecharge(itemDb, functions);
            string description = string.Join("; ", details.ToArray()) + ". Each spell's uses recharge at dawn.";
            return RegisterStaff(itemDb, itemId, title, rarity, description, device, false);
        }

        private static SpellDefinition FindSpell(Database<SpellDefinition> spellDb, string[] ids, string titleFragment)
        {
            if (spellDb == null)
                return null;
            foreach (string id in ids)
            {
                SpellDefinition spell = spellDb.GetElement(id, true);
                if (spell != null)
                    return spell;
            }
            foreach (SpellDefinition candidate in spellDb.GetAllElements())
            {
                string name = candidate == null ? string.Empty : candidate.Name ?? string.Empty;
                string title = candidate == null || candidate.GuiPresentation == null
                    ? string.Empty : candidate.GuiPresentation.Title ?? string.Empty;
                if (name.IndexOf(titleFragment, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    title.IndexOf(titleFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return candidate;
            }
            return null;
        }

        private static bool RegisterStaff(
            Database<ItemDefinition> itemDb, string itemId, string title, string rarity,
            string description, UsableDeviceDescription device, bool grantsAcBonus,
            params FeatureDefinition[] additionalFeatures)
        {
            if (itemDb.GetElement(itemId, true) != null)
                return true;
            ItemDefinition template = FindStaffTemplate(itemDb);
            if (template == null || device == null)
                return false;

            ItemDefinition item = UnityEngine.Object.Instantiate(template);
            if (item == null)
                return false;
            item.ForceName(itemId);
            item.SetUserContentGUID(Guid.NewGuid().ToString());
            item.IsUserContent = true;

            GuiPresentation presentation = new GuiPresentation(template.GuiPresentation);
            presentation.Title = title;
            presentation.Description = description;
            SetMember(item, "guiPresentation", presentation);
            SetMember(item, "magical", true);
            SetMember(item, "requiresAttunement", true);
            SetEnumMember(item, "itemRarity", rarity);
            SetMember(item, "isUsableDevice", true);
            SetMember(item, "usableDeviceDescription", device);

            List<FeatureDefinition> staticFeatures = new List<FeatureDefinition>();
            if (grantsAcBonus)
            {
                FeatureDefinitionAttributeModifier armorClass = ArmorEnchantmentFactory.CreateAttributeModifier(
                    itemId + "ArmorClassBonus", AttributeDefinitions.ArmorClass, "Additive", 2);
                if (armorClass == null)
                    return false;
                staticFeatures.Add(armorClass);
            }
            if (additionalFeatures != null)
                foreach (FeatureDefinition feature in additionalFeatures)
                    if (feature != null)
                        staticFeatures.Add(feature);

            List<ItemPropertyDescription> properties = new List<ItemPropertyDescription>();
            if (staticFeatures.Count > 0)
            {
                ItemPropertyDescription propertyTemplate = FindPropertyTemplate(itemDb);
                if (propertyTemplate == null)
                    return false;
                foreach (FeatureDefinition feature in staticFeatures)
                {
                    ItemPropertyDescription property = new ItemPropertyDescription(propertyTemplate);
                    SetMember(property, "appliesOnItemOnly", false);
                    SetEnumMember(property, "type", "Feature");
                    SetMember(property, "featureDefinition", feature);
                    SetMember(property, "conditionDefinition", null);
                    SetEnumMember(property, "knowledgeAffinity", "ActiveAndVisible");
                    properties.Add(property);
                }
            }
            SetMember(item, "staticProperties", properties);

            itemDb.Add(item);
            bool registered = itemDb.GetElement(itemId, true) != null;
            if (registered)
                Debug.Log(LogPrefix + "Registered " + title + ".");
            return registered;
        }

        private static ItemDefinition FindStaffTemplate(Database<ItemDefinition> itemDb)
        {
            string[] preferredIds = { "StaffOfFire", "StaffOfHealing", "StaffOfPower", "Quarterstaff" };
            foreach (string id in preferredIds)
            {
                ItemDefinition preferred = itemDb.GetElement(id, true);
                if (IsStaffTemplate(preferred))
                    return preferred;
            }
            foreach (ItemDefinition candidate in itemDb.GetAllElements())
                if (IsStaffTemplate(candidate))
                    return candidate;
            return null;
        }

        private static bool IsStaffTemplate(ItemDefinition item)
        {
            if (item == null || item.GuiPresentation == null || item.ItemPresentation == null ||
                item.WeaponDescription == null)
                return false;
            string title = item.GuiPresentation.Title ?? string.Empty;
            string name = item.Name ?? string.Empty;
            return title.IndexOf("Staff", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Staff", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(name, "Quarterstaff", StringComparison.OrdinalIgnoreCase);
        }

        private static ItemPropertyDescription FindPropertyTemplate(Database<ItemDefinition> itemDb)
        {
            foreach (ItemDefinition item in itemDb.GetAllElements())
                if (item != null && item.StaticProperties != null && item.StaticProperties.Count > 0)
                    return item.StaticProperties[0];
            return null;
        }

        private static bool SetMember(object target, string name, object value)
        {
            if (target == null)
                return false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    if (field.IsInitOnly)
                        return false;
                    field.SetValue(target, value);
                    return true;
                }
                PropertyInfo property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0 && property.GetSetMethod(true) != null)
                {
                    property.SetValue(target, value, null);
                    return true;
                }
            }
            return false;
        }

        private static bool SetEnumMember(object target, string name, string value)
        {
            Type memberType = GetMemberType(target == null ? null : target.GetType(), name);
            if (memberType == null || !memberType.IsEnum)
                return false;
            try { return SetMember(target, name, Enum.Parse(memberType, value, true)); }
            catch { return false; }
        }

        private static Type GetMemberType(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.FieldType;
                PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.PropertyType;
            }
            return null;
        }
    }
}
