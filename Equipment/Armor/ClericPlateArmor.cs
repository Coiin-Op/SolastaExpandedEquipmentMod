using System.Collections.Generic;
using UnityModManagerNet;

namespace SolastaExpandedEquipmentMod
{
    public static class ClericPlateArmor
    {
        private sealed class DailySpell
        {
            internal readonly string SpellId;
            internal readonly string SpellTitle;
            internal readonly int Uses;

            internal DailySpell(string spellId, string spellTitle, int uses)
            {
                SpellId = spellId;
                SpellTitle = spellTitle;
                Uses = uses;
            }
        }

        public static bool Register(Database<ItemDefinition> itemDb, UnityModManager.ModEntry mod)
        {
            ItemDefinition plate = PlateArmor.FindPlateTemplate(itemDb);
            if (plate == null || plate.ArmorDescription == null || plate.GuiPresentation == null ||
                plate.ItemPresentation == null)
            {
                if (mod != null)
                    mod.Logger.Warning("[SolastaExpandedEquipmentMod] Could not find the base Plate armor template for Cleric's Plate.");
                return false;
            }

            string sourceId = plate.Name;
            int baseArmorClass = plate.ArmorDescription.ArmorClassValue;
            bool success = true;

            success &= RegisterItem(itemDb, sourceId, baseArmorClass,
                "ClericsPlatePlus1OfDefence", "Cleric's Plate +1 of Defense", 1, "Rare",
                new[] { new DailySpell("BladeBarrier", "Blade Barrier", 1) });
            success &= RegisterItem(itemDb, sourceId, baseArmorClass,
                "ClericsPlatePlus1OfHealing", "Cleric's Plate +1 of Healing", 1, "Rare",
                new[]
                {
                    new DailySpell("HealingWord", "Healing Word", 3),
                    new DailySpell("PrayerOfHealing", "Prayer of Healing", 2),
                    new DailySpell("LesserRestoration", "Lesser Restoration", 1)
                });
            success &= RegisterItem(itemDb, sourceId, baseArmorClass,
                "ClericsPlatePlus2OfHealing", "Cleric's Plate +2 of Healing", 2, "VeryRare",
                new[]
                {
                    new DailySpell("HealingWord", "Healing Word", 3),
                    new DailySpell("PrayerOfHealing", "Prayer of Healing", 3),
                    new DailySpell("LesserRestoration", "Lesser Restoration", 2)
                });
            success &= RegisterItem(itemDb, sourceId, baseArmorClass,
                "ClericsPlatePlus3OfHealing", "Cleric's Plate +3 of Healing", 3, "VeryRare",
                new[]
                {
                    new DailySpell("HealingWord", "Healing Word", 3),
                    new DailySpell("PrayerOfHealing", "Prayer of Healing", 3),
                    new DailySpell("LesserRestoration", "Lesser Restoration", 2),
                    new DailySpell("Heal", "Heal", 1)
                });
            success &= RegisterItem(itemDb, sourceId, baseArmorClass,
                "ClericsPlatePlus1OfRestoration", "Cleric's Plate +1 of Restoration", 1, "Rare",
                new[] { new DailySpell("LesserRestoration", "Lesser Restoration", 2) });
            success &= RegisterItem(itemDb, sourceId, baseArmorClass,
                "ClericsPlatePlus2OfRestoration", "Cleric's Plate +2 of Restoration", 2, "VeryRare",
                new[]
                {
                    new DailySpell("LesserRestoration", "Lesser Restoration", 3),
                    new DailySpell("GreaterRestoration", "Greater Restoration", 2)
                });

            return success;
        }

        private static bool RegisterItem(
            Database<ItemDefinition> itemDb,
            string sourceId,
            int baseArmorClass,
            string itemId,
            string title,
            int tier,
            string rarity,
            DailySpell[] spells)
        {
            Database<SpellDefinition> spellDb = DatabaseRepository.GetDatabase<SpellDefinition>();
            if (spellDb == null)
                return false;

            List<KeyValuePair<SpellDefinition, int>> spellUses =
                new List<KeyValuePair<SpellDefinition, int>>();
            List<string> spellDescriptions = new List<string>();

            foreach (DailySpell spell in spells)
            {
                SpellDefinition spellDefinition = spellDb.GetElement(spell.SpellId, true);
                if (spellDefinition == null)
                    return false;

                spellUses.Add(new KeyValuePair<SpellDefinition, int>(spellDefinition, spell.Uses));
                spellDescriptions.Add(spell.SpellTitle + " " + spell.Uses + " times per day");
            }

            UsableDeviceDescription deviceDescription = RingItems.CreateSpellFunctionsByRecharge(
                itemDb, spellUses);
            if (deviceDescription == null)
                return false;

            string description = title + ". Grants +" + tier + " AC. " +
                string.Join("; ", spellDescriptions.ToArray()) + ". Uses recharge at dawn.";

            return ArmorEnchantmentFactory.RegisterVariant(
                itemDb, itemId, title, tier, rarity, description, new List<FeatureDefinition>(),
                null, null, 0, sourceId, sourceId, baseArmorClass, deviceDescription);
        }
    }
}
