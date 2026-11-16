using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;

namespace AndijonSaltanatlar.AIGovernor
{
    /// <summary>
    /// Shahar tahlili hisoboti — PROMPT 15, 4-bo'lim. Barcha qiymatlar haqiqiy GameState'dan olinadi
    /// (AI statistikani o'ylab topmaydi).
    /// </summary>
    public class CityAnalysisReport
    {
        // Xom ko'rsatkichlar
        public int Population { get; set; }
        public int HousingUsed { get; set; }
        public int HousingCapacity { get; set; }
        public int HousingShortage { get; set; }      // Population - HousingCapacity (manfiy bo'lsa zaxira bor)
        public int Treasury { get; set; }
        public int Food { get; set; }
        public int Happiness { get; set; }
        public int Education { get; set; }
        public int Health { get; set; }
        public int Security { get; set; }
        public int Workers { get; set; }
        public int Unemployed { get; set; }
        public int DailyIncome { get; set; }
        public int DailyExpense { get; set; }
        public int DailyNet => DailyIncome - DailyExpense;
        public int FoodProduced { get; set; }
        public int FoodConsumed { get; set; }
        public int BuildableTiles { get; set; }       // taxminiy bo'sh quriladigan katak
        public int OpenParcels { get; set; }
        public int LockedParcels { get; set; }
        public int RoadTiles { get; set; }
        public int TotalBuildings { get; set; }
        public int MilitaryStrength { get; set; }

        public Dictionary<BuildingType, int> BuildingCounts { get; set; } = new();

        // Tahlil natijalari
        public List<string> DetectedProblems { get; set; } = new();
        public List<BuildingType> RecommendedBuildings { get; set; } = new();
        public List<int> RecommendedLandParcels { get; set; } = new();
        public List<string> InfrastructureGaps { get; set; } = new();
        public Dictionary<string, int> EstimatedResourceNeeds { get; set; } = new();
        public List<string> DevelopmentOpportunities { get; set; } = new();

        /// <summary>Umumiy shahar salomatligi 0..100.</summary>
        public int OverallCityHealthScore { get; set; }

        public int CountOf(BuildingType t) => BuildingCounts.TryGetValue(t, out int v) ? v : 0;
    }
}
