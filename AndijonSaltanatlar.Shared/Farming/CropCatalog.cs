using System;
using System.Collections.Generic;

namespace AndijonSaltanatlar.Shared.Farming
{
    /// <summary>Ekin / mahsulot turlari. Rice = 0 — PROMPT 19A dagi guruch bilan mos (eski ma'lumot buzilmaydi).</summary>
    public enum CropType
    {
        Rice = 0,
        Wheat = 1,
        Cotton = 2,
        Fruit = 3,
        Vegetable = 4
    }

    public sealed class CropDef
    {
        public CropType Type;
        public string Key = "";
        public string NameUz = "";
        public long GrowingAtMinutes;
        public long MatureAtMinutes;
        public int SeedCost;
        public int Yield;
        /// <summary>Bozor uchun tavsiya narxi (1 birlik, tanga) — taklif yaratishda ko'rsatiladi.</summary>
        public int ReferencePrice;
        /// <summary>Suv talabi: necha "sug'orish birligi" oladi (guruch eng ko'p suv ichadi).</summary>
        public int WaterUnits = 1;
    }

    /// <summary>
    /// Umumiy CropSystem (PROMPT 19B): barcha ekinlar bitta qoidalar to'plami orqali ishlaydi.
    /// Guruch qiymatlari RiceFarmingRules bilan bir xil — 19A natijalari o'zgarmaydi.
    /// </summary>
    public static class CropCatalog
    {
        public static readonly IReadOnlyList<CropDef> All = new List<CropDef>
        {
            new CropDef { Type = CropType.Rice, Key = "Rice", NameUz = "Guruch",
                GrowingAtMinutes = RiceFarmingRules.PlantedToGrowingMinutes, MatureAtMinutes = RiceFarmingRules.PlantedToMatureMinutes,
                SeedCost = RiceFarmingRules.SeedCost, Yield = RiceFarmingRules.HarvestYield, ReferencePrice = 5, WaterUnits = 1 },
            new CropDef { Type = CropType.Wheat, Key = "Wheat", NameUz = "Bug'doy",
                GrowingAtMinutes = 50, MatureAtMinutes = 150, SeedCost = 8, Yield = 100, ReferencePrice = 4, WaterUnits = 1 },
            new CropDef { Type = CropType.Cotton, Key = "Cotton", NameUz = "Paxta",
                GrowingAtMinutes = 90, MatureAtMinutes = 240, SeedCost = 15, Yield = 80, ReferencePrice = 9, WaterUnits = 1 },
            new CropDef { Type = CropType.Fruit, Key = "Fruit", NameUz = "Mevalar",
                GrowingAtMinutes = 120, MatureAtMinutes = 300, SeedCost = 20, Yield = 90, ReferencePrice = 8, WaterUnits = 1 },
            new CropDef { Type = CropType.Vegetable, Key = "Vegetable", NameUz = "Sabzavotlar",
                GrowingAtMinutes = 40, MatureAtMinutes = 120, SeedCost = 6, Yield = 90, ReferencePrice = 3, WaterUnits = 1 },
        };

        public static CropDef Get(CropType t)
        {
            foreach (CropDef d in All) if (d.Type == t) return d;
            return All[0];
        }

        public static bool TryParse(string key, out CropType type)
        {
            foreach (CropDef d in All)
                if (string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase)) { type = d.Type; return true; }
            type = CropType.Rice;
            return false;
        }

        public static string NameOf(string productKey) => TryParse(productKey, out CropType t) ? Get(t).NameUz : productKey;

        public static RiceFieldState Evaluate(CropType crop, RiceFieldState stored, long plantedAt, long now)
        {
            if (stored == RiceFieldState.Empty || stored == RiceFieldState.Harvested) return stored;
            CropDef d = Get(crop);
            long age = now - plantedAt;
            if (age >= d.MatureAtMinutes) return RiceFieldState.Mature;
            if (age >= d.GrowingAtMinutes) return RiceFieldState.Growing;
            return RiceFieldState.Planted;
        }

        public static float Progress(CropType crop, RiceFieldState stored, long plantedAt, long now)
        {
            if (stored == RiceFieldState.Empty) return 0f;
            if (stored == RiceFieldState.Harvested) return 1f;
            return (float)Math.Clamp((now - plantedAt) / (double)Get(crop).MatureAtMinutes, 0.0, 1.0);
        }

        public static int SecondsToMature(CropType crop, long plantedAt, long now)
        {
            long left = plantedAt + Get(crop).MatureAtMinutes - now;
            return left <= 0 ? 0 : (int)Math.Ceiling(left / RiceFarmingRules.GameMinutesPerRealSecond);
        }
    }

    /// <summary>Karvon qoidalari (PROMPT 19B).</summary>
    public static class CaravanRules
    {
        /// <summary>Bir xil hududdagi shaharlar orasida minimal yo'l vaqti (real soniya).</summary>
        public const int MinTravelSeconds = 20;
        /// <summary>Har bir "masofa birligi" (xarita koordinatasi farqi) uchun qo'shimcha soniya.</summary>
        public const double SecondsPerDistanceUnit = 1.0;
        public const int MaxTravelSeconds = 180;

        public static int TravelSeconds(double distance)
            => (int)Math.Clamp(MinTravelSeconds + distance * SecondsPerDistanceUnit, MinTravelSeconds, MaxTravelSeconds);
    }

    /// <summary>Hudud qo'shish (Kingdom expansion) qoidalari — abstrakt siyosiy ko'rsatkichlar.</summary>
    public static class TerritoryRules
    {
        /// <summary>Diplomatik birlashishning asosiy narxi + hudud aholisiga bog'liq qism.</summary>
        public static int AnnexCost(int regionPopulation, int development) => 200 + regionPopulation / 2 + development * 3;

        /// <summary>Hudud rozi bo'lishi uchun kerakli minimal munosabat (-100..100).</summary>
        public const int RequiredRelation = 20;
        /// <summary>Elchi yuborish: munosabatni oshiradi.</summary>
        public const int EnvoyCost = 60;
        public const int EnvoyRelationGain = 15;
        /// <summary>AI saltanatning so'nggi (poytaxt) hududini birlashtirib bo'lmaydi.</summary>
        public const bool ProtectLastRegion = true;
    }
}
