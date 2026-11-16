using System;

namespace AndijonSaltanatlar.Shared.Farming
{
    /// <summary>Guruch maydonining holati: Empty → Planted → Growing → Mature → Harvested (→ yana Planted).</summary>
    public enum RiceFieldState
    {
        Empty = 0,
        Planted = 1,
        Growing = 2,
        Mature = 3,
        Harvested = 4
    }

    /// <summary>
    /// Guruch yetishtirish qoidalari — server ham, client (Windows/Android) ham AYNAN shu kodni ishlatadi.
    /// O'sish faqat SERVER vaqtiga bog'liq: maydon ekilgan paytdagi server o'yin vaqti (PlantedAtGameMinute)
    /// saqlanadi, joriy holat esa server bergan joriy o'yin vaqtidan hisoblanadi. Client o'z soatiga tayanmaydi.
    ///
    /// O'yin vaqti: 1 real soniya = <see cref="GameMinutesPerRealSecond"/> o'yin daqiqasi.
    /// </summary>
    public static class RiceFarmingRules
    {
        /// <summary>1 real soniya necha o'yin daqiqasi (1.0 => 1 real daqiqa = 1 o'yin soati).</summary>
        public const double GameMinutesPerRealSecond = 1.0;

        /// <summary>Ekilgandan to "o'sish" bosqichigacha (o'yin daqiqasi).</summary>
        public const long PlantedToGrowingMinutes = 60;          // 1 o'yin soati  (~1 real daqiqa)
        /// <summary>Ekilgandan to to'liq yetilishgacha (o'yin daqiqasi).</summary>
        public const long PlantedToMatureMinutes = 180;          // 3 o'yin soati  (~3 real daqiqa)

        public const int FieldBuildCost = 100;     // ekin maydoni qurish (tanga)
        public const int SeedCost = 10;            // urug' (tanga)
        public const int HarvestYield = 120;       // bitta hosil (birlik guruch)
        public const int MaxFieldsPerKingdom = 6;
        /// <summary>Suv manbai (ariq) sig'imi: nechta maydonni sug'ora oladi.</summary>
        public const int FieldsPerWaterSource = 4;
        public const int StartingWaterSources = 1;

        public static long GameMinutesSince(DateTime epochUtc, DateTime nowUtc)
        {
            double sec = (nowUtc - epochUtc).TotalSeconds;
            if (sec < 0) sec = 0;
            return (long)(sec * GameMinutesPerRealSecond);
        }

        /// <summary>Saqlangan holat + ekilgan vaqt + joriy server vaqtidan haqiqiy holatni hisoblaydi.</summary>
        public static RiceFieldState Evaluate(RiceFieldState stored, long plantedAtGameMinute, long nowGameMinute)
        {
            if (stored == RiceFieldState.Empty || stored == RiceFieldState.Harvested) return stored;
            long age = nowGameMinute - plantedAtGameMinute;
            if (age >= PlantedToMatureMinutes) return RiceFieldState.Mature;
            if (age >= PlantedToGrowingMinutes) return RiceFieldState.Growing;
            return RiceFieldState.Planted;
        }

        /// <summary>Yetilish darajasi 0..1 (animatsiya va progress bar uchun).</summary>
        public static float Progress(RiceFieldState stored, long plantedAtGameMinute, long nowGameMinute)
        {
            if (stored == RiceFieldState.Empty) return 0f;
            if (stored == RiceFieldState.Harvested) return 1f;
            double p = (nowGameMinute - plantedAtGameMinute) / (double)PlantedToMatureMinutes;
            return (float)Math.Clamp(p, 0.0, 1.0);
        }

        /// <summary>Yetilishigacha qolgan real soniyalar (UI uchun taxminiy).</summary>
        public static int SecondsToMature(long plantedAtGameMinute, long nowGameMinute)
        {
            long left = plantedAtGameMinute + PlantedToMatureMinutes - nowGameMinute;
            return left <= 0 ? 0 : (int)Math.Ceiling(left / GameMinutesPerRealSecond);
        }

        public static bool CanPlant(RiceFieldState evaluated) => evaluated == RiceFieldState.Empty || evaluated == RiceFieldState.Harvested;
        public static bool CanHarvest(RiceFieldState evaluated) => evaluated == RiceFieldState.Mature;

        /// <summary>Suv yetarlimi: sug'orilayotgan (ekilgan) maydonlar soni suv sig'imidan oshmasin.</summary>
        public static bool HasWaterFor(int activeFields, int waterSources) => activeFields < waterSources * FieldsPerWaterSource;

        public static string StateNameUz(RiceFieldState s)
        {
            switch (s)
            {
                case RiceFieldState.Planted: return "Ekildi";
                case RiceFieldState.Growing: return "O'smoqda";
                case RiceFieldState.Mature: return "Yetildi";
                case RiceFieldState.Harvested: return "Yig'ib olindi";
                default: return "Bo'sh";
            }
        }
    }

    /// <summary>Saltanatlar bozori qoidalari (server va client uchun umumiy validatsiya).</summary>
    public static class MarketRules
    {
        public const int MinQuantity = 1;
        public const int MaxQuantity = 100000;
        public const int MinPrice = 1;
        public const int MaxPrice = 10000000;
        public const int MaxActiveOffersPerKingdom = 10;

        public static string Validate(int quantity, int totalPrice)
        {
            if (quantity < MinQuantity || quantity > MaxQuantity) return "Miqdor noto'g'ri";
            if (totalPrice < MinPrice || totalPrice > MaxPrice) return "Narx noto'g'ri";
            return null;
        }
    }
}
