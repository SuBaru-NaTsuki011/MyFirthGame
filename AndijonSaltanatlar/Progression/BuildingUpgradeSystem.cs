using AndijonSaltanatlar.Buildings;

namespace AndijonSaltanatlar.Progression
{
    /// <summary>
    /// Binolarni darajaga ko'tarish: mustahkamlik va sig'im oshadi. Binoning xaritadagi joylashuvi
    /// o'zgarmaydi. Narx darajaga qarab oshadi.
    /// </summary>
    public static class BuildingUpgradeSystem
    {
        public const int MaxLevel = 3;

        public static bool CanUpgrade(Building b) => b != null && b.Level < MaxLevel;
        public static int UpgradeCost(Building b) => 60 * b.Level;

        public static bool Apply(Building b)
        {
            if (!CanUpgrade(b)) return false;
            b.Upgrade();
            return true;
        }
    }
}
