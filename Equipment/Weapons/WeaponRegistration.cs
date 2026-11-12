namespace SolastaExpandedEquipmentMod
{
    public static class WeaponRegistration
    {
        public static bool Register()
        {
            // DAGGER ISOLATION: only Fey Slayer Dagger has a confirmed clean startup.
            // Keep creature-family, magical, and Quickness dagger groups disabled for now.
            const bool ENABLE_CREATURE_FAMILY_DAGGERS = true;
            const bool ENABLE_FEY_SLAYER_DAGGER = true;
            const bool ENABLE_MAGICAL_DAGGERS = true;
            const bool ENABLE_QUICKNESS_DAGGER = true;

            const bool ENABLE_SCIMITAR_FAMILY_SLAYER = true;
            const bool ENABLE_MAGICAL_SCIMITARS = true;
            const bool ENABLE_ADVANCED_SCIMITARS = true;

            bool dartsRegistered = WeaponFactory.RegisterDarts();
            bool aspNestRegistered = WeaponFactory.RegisterAspNestDart();
            bool longswordRegistered =
                WeaponFactory.RegisterBeastSlayerLongsword();
            bool creatureFamilyLongswordsRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerLongswords();
            bool creatureFamilyShortswordsRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerShortswords();
            bool creatureFamilyDaggersRegistered =
                !ENABLE_CREATURE_FAMILY_DAGGERS ||
                WeaponFactory.RegisterCreatureFamilySlayerDaggers();
            bool magicalLongswordsRegistered =
                WeaponFactory.RegisterMagicalLongswords();
            bool magicalShortswordsRegistered =
                WeaponFactory.RegisterMagicalShortswords();
            bool advancedLongswordsRegistered =
                WeaponFactory.RegisterAdvancedLongswords();
            bool advancedShortswordsRegistered =
                WeaponFactory.RegisterAdvancedShortswords();
            bool scimitarFamilyRegistered =
                !ENABLE_SCIMITAR_FAMILY_SLAYER ||
                WeaponFactory.RegisterCreatureFamilySlayerScimitars();
            bool magicalScimitarsRegistered =
                !ENABLE_MAGICAL_SCIMITARS ||
                WeaponFactory.RegisterMagicalScimitars();
            bool advancedScimitarsRegistered =
                !ENABLE_ADVANCED_SCIMITARS ||
                WeaponFactory.RegisterAdvancedScimitars();
            bool creatureFamilyRapiersRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerRapiers();
            bool magicalRapiersRegistered =
                WeaponFactory.RegisterMagicalRapiers();
            bool advancedRapiersRegistered =
                WeaponFactory.RegisterAdvancedRapiers();
            bool creatureFamilyGreatswordsRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerGreatswords();
            bool magicalGreatswordsRegistered =
                WeaponFactory.RegisterMagicalGreatswords();
            bool advancedGreatswordsRegistered =
                WeaponFactory.RegisterAdvancedGreatswords();
            bool creatureFamilyKatanasRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerKatanas();
            bool magicalKatanasRegistered =
                WeaponFactory.RegisterMagicalKatanas();
            bool advancedKatanasRegistered =
                WeaponFactory.RegisterAdvancedKatanas();
            bool creatureFamilyLongbowsRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerLongbows();
            bool magicalLongbowsRegistered =
                WeaponFactory.RegisterMagicalLongbows();
            bool advancedLongbowsRegistered =
                WeaponFactory.RegisterAdvancedLongbows();
            bool creatureFamilyShortbowsRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerShortbows();
            bool creatureFamilyMacesRegistered = WeaponFactory.RegisterCreatureFamilySlayerMaces();
            bool magicalMacesRegistered = WeaponFactory.RegisterMagicalMaces();
            bool advancedMacesRegistered = WeaponFactory.RegisterAdvancedMaces();
            bool magicalShortbowsRegistered =
                WeaponFactory.RegisterMagicalShortbows();
            bool advancedShortbowsRegistered =
                WeaponFactory.RegisterAdvancedShortbows();
            bool creatureFamilyWarhammersRegistered = WeaponFactory.RegisterCreatureFamilySlayerWarhammers();
            bool magicalWarhammersRegistered = WeaponFactory.RegisterMagicalWarhammers();
            bool advancedWarhammersRegistered = WeaponFactory.RegisterAdvancedWarhammers();
            bool additionalWeaponBatchesRegistered = WeaponFactory.RegisterAdditionalWeaponBatches();
            bool shortswordRegistered =
                WeaponFactory.RegisterBeastSlayerShortsword();
            bool greatswordRegistered =
                WeaponFactory.RegisterBeastSlayerGreatsword();
            bool feyShortswordRegistered =
                WeaponFactory.RegisterFeySlayerShortsword();
            bool feyDaggerRegistered =
                !ENABLE_FEY_SLAYER_DAGGER ||
                WeaponFactory.RegisterFeySlayerDagger();
            bool magicalBowRegistered =
                WeaponFactory.RegisterMagicalSlayerBow();
            bool magicalDaggersRegistered =
                !ENABLE_MAGICAL_DAGGERS ||
                WeaponFactory.RegisterMagicalDaggers();
            bool quicknessDaggerRegistered =
                !ENABLE_QUICKNESS_DAGGER ||
                WeaponFactory.RegisterQuicknessDagger();

            ApplyGeneratedWeaponPrices();

            return dartsRegistered &&
                aspNestRegistered &&
                longswordRegistered &&
                creatureFamilyLongswordsRegistered &&
                creatureFamilyShortswordsRegistered &&
                creatureFamilyDaggersRegistered &&
                magicalLongswordsRegistered &&
                magicalShortswordsRegistered &&
                advancedLongswordsRegistered &&
                advancedShortswordsRegistered &&
                scimitarFamilyRegistered &&
                magicalScimitarsRegistered &&
                advancedScimitarsRegistered &&
                creatureFamilyRapiersRegistered &&
                magicalRapiersRegistered &&
                advancedRapiersRegistered &&
                creatureFamilyGreatswordsRegistered &&
                magicalGreatswordsRegistered &&
                advancedGreatswordsRegistered &&
                creatureFamilyKatanasRegistered &&
                magicalKatanasRegistered &&
                advancedKatanasRegistered &&
                creatureFamilyLongbowsRegistered &&
                magicalLongbowsRegistered &&
                advancedLongbowsRegistered &&
                creatureFamilyShortbowsRegistered &&
                magicalShortbowsRegistered &&
                advancedShortbowsRegistered &&
                creatureFamilyMacesRegistered &&
                magicalMacesRegistered &&
                advancedMacesRegistered &&
                creatureFamilyWarhammersRegistered &&
                magicalWarhammersRegistered &&
                advancedWarhammersRegistered &&
                additionalWeaponBatchesRegistered &&
                shortswordRegistered &&
                greatswordRegistered &&
                feyShortswordRegistered &&
                feyDaggerRegistered &&
                magicalBowRegistered &&
                magicalDaggersRegistered &&
                quicknessDaggerRegistered;
        }

        private static void ApplyGeneratedWeaponPrices()
        {
            Database<ItemDefinition> itemDb = DatabaseRepository.GetDatabase<ItemDefinition>();
            if (itemDb == null)
                return;

            string[] weaponPrefixes =
            {
                "Longsword", "Shortsword", "Scimitar", "Rapier", "Greatsword", "Katana",
                "Longbow", "Shortbow", "Dagger", "Mace", "Warhammer", "Greataxe", "Maul", "Battleaxe",
                "Handaxe", "Halberd", "Pike", "LongMace", "Morningstar", "Quarterstaff",
                "Javelin", "Spear", "Arrow", "Bolt", "Dart", "BeastSlayer", "FeySlayer",
                "MagicalSlayer", "Quickness"
            };

            foreach (ItemDefinition item in itemDb.GetAllElements())
            {
                if (item == null || !item.IsUserContent || !item.IsWeapon ||
                    string.IsNullOrEmpty(item.Name) || !IsGeneratedWeaponId(item.Name, weaponPrefixes))
                    continue;

                int priceGp = GetGeneratedWeaponPriceGp(item.Name);
                int[] costs = item.Costs;
                if (costs == null || costs.Length < 5)
                    continue;

                // Costs are ordered Platinum, Gold, Electrum, Silver, Copper.
                // Keep the full value in gold so merchant display rounds predictably.
                for (int i = 0; i < costs.Length; i++)
                    costs[i] = 0;
                costs[1] = priceGp;
            }
        }

        private static bool IsGeneratedWeaponId(string itemId, string[] weaponPrefixes)
        {
            bool knownFamily = false;
            foreach (string prefix in weaponPrefixes)
            {
                if (itemId.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    knownFamily = true;
                    break;
                }
            }

            if (!knownFamily)
                return false;

            return itemId.IndexOf("Plus", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   itemId.IndexOf("Vs", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   itemId.IndexOf("Slayer", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   itemId.IndexOf("Quickness", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int GetGeneratedWeaponPriceGp(string itemId)
        {
            string id = itemId.ToLowerInvariant();

            // These are the strongest on-hit effects and should be the rarest,
            // most expensive items in the current weapon batches.
            if (id.Contains("curse")) return 2000;
            if (id.Contains("petrification")) return 1800;
            if (id.Contains("mindnumbing") || id.Contains("feeblemind")) return 1700;
            if (id.Contains("blindness")) return 1600;

            if (id.Contains("quickness")) return 1200;
            if (id.Contains("demonicinfluence")) return 1100;
            if (id.Contains("slow")) return 800;

            if (id.Contains("ofacid") || id.Contains("offire") || id.Contains("oflightning") ||
                id.Contains("ofcold") || id.Contains("ofpoison") || id.Contains("ofwilting") ||
                id.Contains("oflight") || id.Contains("ofboomin"))
                return 300;

            if (id.Contains("ofbane")) return 450;
            if (id.Contains("ofweakening") || id.Contains("ofpower")) return 500;

            if (id.Contains("vs") && id.Contains("plus3")) return 800;
            if (id.Contains("vs") && id.Contains("plus2")) return 500;
            if (id.Contains("vs") && id.Contains("plus1")) return 200;

            if (id.Contains("plus3")) return 1000;
            if (id.Contains("plus2")) return 600;
            if (id.Contains("plus1")) return 200;

            return 200;
        }
    }
}
