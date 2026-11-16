using System;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Environment;

namespace AndijonSaltanatlar.Water
{
    /// <summary>
    /// Sug'orish mexanikasi. Dehqonchilik samaradorligi suv ta'minoti, mavsum va ob-havoga bog'liq.
    /// Bu yerda murakkab gidravlika yo'q — faqat balanslangan o'yin qoidasi:
    ///   suv yo'q       → 0.55 (hosil keskin tushadi)
    ///   qisman suv     → 0.55..0.95
    ///   to'liq suv     → 1.00
    /// Bu ko'paytiruvchi EconomyManager'ning kunlik ishlab chiqarish bonusiga qo'shiladi.
    /// </summary>
    public sealed class IrrigationSystem
    {
        private readonly WaterNetworkManager _water;

        public IrrigationSystem(WaterNetworkManager water) => _water = water;

        public float EfficiencyFor(Building garden)
        {
            if (garden == null) return 1f;
            float coverage = _water.CoverageOf(garden);
            return 0.55f + 0.45f * Math.Clamp(coverage, 0f, 1f);
        }

        /// <summary>Bog'lar bo'yicha o'rtacha sug'orish samaradorligi.</summary>
        public float AverageEfficiency(BuildingManager buildings)
        {
            if (buildings == null) return 1f;
            float sum = 0f;
            int n = 0;
            foreach (Building b in buildings.Buildings)
            {
                if (b.Type != BuildingType.Garden || b.BuildProgress < 1f) continue;
                sum += EfficiencyFor(b);
                n++;
            }
            return n == 0 ? 1f : sum / n;
        }

        /// <summary>
        /// Oziq-ovqat ishlab chiqarishga qo'shiladigan bonus. 0.02..0.20 oralig'ida —
        /// ya'ni suv to'liq bo'lsa hosil 20% gacha oshadi, suv bo'lmasa jarima yo'q
        /// (jarima allaqachon baza hosilning pastligida).
        /// </summary>
        public float FoodProductionBonus(BuildingManager buildings, Season season)
        {
            float avg = AverageEfficiency(buildings);
            float seasonal = SeasonManager.FoodModifier(season);
            return Math.Clamp((avg - 0.55f) / 0.45f * 0.18f * (0.6f + seasonal * 0.4f), 0f, 0.2f);
        }

        /// <summary>Sug'orilgan dehqonchilik maydonlari (bog'lar) soni.</summary>
        public int IrrigatedGardens(BuildingManager buildings)
        {
            if (buildings == null) return 0;
            int n = 0;
            foreach (Building b in buildings.Buildings)
                if (b.Type == BuildingType.Garden && b.BuildProgress >= 1f && _water.CoverageOf(b) >= 0.5f) n++;
            return n;
        }

        public int TotalGardens(BuildingManager buildings)
        {
            if (buildings == null) return 0;
            int n = 0;
            foreach (Building b in buildings.Buildings)
                if (b.Type == BuildingType.Garden && b.BuildProgress >= 1f) n++;
            return n;
        }
    }
}
