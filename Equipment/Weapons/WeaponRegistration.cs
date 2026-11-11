namespace SolastaExpandedEquipmentMod
{
    public static class WeaponRegistration
    {
        public static bool Register()
        {
            // TEMPORARY STARTUP ISOLATION: set to true to re-enable dagger registration.
            const bool ENABLE_DAGGER_REGISTRATION = false;

            bool dartsRegistered = WeaponFactory.RegisterDarts();
            bool aspNestRegistered = WeaponFactory.RegisterAspNestDart();
            bool longswordRegistered =
                WeaponFactory.RegisterBeastSlayerLongsword();
            bool creatureFamilyLongswordsRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerLongswords();
            bool creatureFamilyShortswordsRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerShortswords();
            bool creatureFamilyDaggersRegistered =
                !ENABLE_DAGGER_REGISTRATION ||
                WeaponFactory.RegisterCreatureFamilySlayerDaggers();
            bool magicalLongswordsRegistered =
                WeaponFactory.RegisterMagicalLongswords();
            bool magicalShortswordsRegistered =
                WeaponFactory.RegisterMagicalShortswords();
            bool advancedLongswordsRegistered =
                WeaponFactory.RegisterAdvancedLongswords();
            bool advancedShortswordsRegistered =
                WeaponFactory.RegisterAdvancedShortswords();
            bool shortswordRegistered =
                WeaponFactory.RegisterBeastSlayerShortsword();
            bool greatswordRegistered =
                WeaponFactory.RegisterBeastSlayerGreatsword();
            bool feyShortswordRegistered =
                WeaponFactory.RegisterFeySlayerShortsword();
            bool feyDaggerRegistered =
                !ENABLE_DAGGER_REGISTRATION ||
                WeaponFactory.RegisterFeySlayerDagger();
            bool magicalBowRegistered =
                WeaponFactory.RegisterMagicalSlayerBow();
            bool magicalDaggersRegistered =
                !ENABLE_DAGGER_REGISTRATION ||
                WeaponFactory.RegisterMagicalDaggers();
            bool quicknessDaggerRegistered =
                !ENABLE_DAGGER_REGISTRATION ||
                WeaponFactory.RegisterQuicknessDagger();

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
                shortswordRegistered &&
                greatswordRegistered &&
                feyShortswordRegistered &&
                feyDaggerRegistered &&
                magicalBowRegistered &&
                magicalDaggersRegistered &&
                quicknessDaggerRegistered;
        }
    }
}
