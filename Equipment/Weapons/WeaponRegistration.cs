namespace SolastaExpandedEquipmentMod
{
    public static class WeaponRegistration
    {
        public static bool Register()
        {
            bool dartsRegistered = WeaponFactory.RegisterDarts();
            bool aspNestRegistered = WeaponFactory.RegisterAspNestDart();
            bool longswordRegistered =
                WeaponFactory.RegisterBeastSlayerLongsword();
            bool creatureFamilyLongswordsRegistered =
                WeaponFactory.RegisterCreatureFamilySlayerLongswords();
            bool shortswordRegistered =
                WeaponFactory.RegisterBeastSlayerShortsword();
            bool greatswordRegistered =
                WeaponFactory.RegisterBeastSlayerGreatsword();
            bool feyShortswordRegistered =
                WeaponFactory.RegisterFeySlayerShortsword();
            bool feyDaggerRegistered =
                WeaponFactory.RegisterFeySlayerDagger();
            bool magicalBowRegistered =
                WeaponFactory.RegisterMagicalSlayerBow();

            return dartsRegistered &&
                aspNestRegistered &&
                longswordRegistered &&
                creatureFamilyLongswordsRegistered &&
                shortswordRegistered &&
                greatswordRegistered &&
                feyShortswordRegistered &&
                feyDaggerRegistered &&
                magicalBowRegistered;
        }
    }
}