using AndijonSaltanatlar.Buildings;

namespace AndijonSaltanatlar.Progression
{
    /// <summary>
    /// Binolarni darajaga ko'tarish: mustahkamlik va sig'im oshadi. Binoning xaritadagi joylashuvi
    /// o'zgarmaydi. Narx darajaga qarab oshadi.
    /// </summary>
    public static class BuildingUpgradeSystem
    {
        /// <summary>Har bir bino 5-darajagacha kattalashadi (ko'rinishi ham o'zgaradi).</summary>
        public const int MaxLevel = 5;

        public static bool CanUpgrade(Building b) => b != null && b.Level < MaxLevel && b.BuildProgress >= 1f;
        /// <summary>Narx bino narxi, darajasi va turiga bog'liq: devor arzon, saroy qimmat.</summary>
        public static int UpgradeCost(Building b) =>
            System.Math.Max(1, (int)(System.Math.Max(60, b.Cost / 2) * b.Level * BuildingUpgradeEffects.CostFactor(b.Type)));

        /// <summary>Bino turiga xos daraja nomi (ferma — Tomorqa/Dala..., bozor — Rasta/Chorsu...).</summary>
        public static string LevelName(Building b) => b == null ? "" : BuildingUpgradeEffects.LevelName(b.Type, b.Level);

        /// <summary>Eski umumiy nom (moslik uchun). Imkoni bo'lsa LevelName(Building) ishlating.</summary>
        public static string LevelName(int level)
        {
            switch (level)
            {
                case 1: return "Oddiy";
                case 2: return "Bezatilgan";
                case 3: return "Boy";
                case 4: return "Hashamatli";
                default: return "Saltanat";
            }
        }

        public static bool Apply(Building b)
        {
            if (!CanUpgrade(b)) return false;
            b.Upgrade();
            return true;
        }
    }
}
