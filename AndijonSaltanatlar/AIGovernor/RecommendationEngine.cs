using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityPlanning;

namespace AndijonSaltanatlar.AIGovernor
{
    public enum RecommendationAction
    {
        BuildHouse, BuildMarket, BuildGarden, BuildMadrasa, UnlockLand, PlanDistrict, ReviewTaxes
    }

    /// <summary>Bitta tavsiya — Utility AI ball bilan.</summary>
    public class Recommendation
    {
        public string TitleUz { get; set; } = "";
        public string ReasonUz { get; set; } = "";
        public string ActionLabelUz { get; set; } = "REJA TUZISH";
        public RecommendationAction Action { get; set; }
        public DistrictType? SuggestedDistrict { get; set; }
        public int PriorityScore { get; set; }
    }

    /// <summary>
    /// Tavsiya dvigateli — PROMPT 15, 5-bo'lim. Har bir muammo uchun foydali taklif va Utility AI ustuvorlik.
    /// PriorityScore = ProblemSeverity + ExpectedBenefit + Urgency - ResourcePressure - ConstructionRisk.
    /// Tavsiya faqat matn; xarajat/qurilish faqat foydalanuvchi tasdig'idan keyin bo'ladi.
    /// </summary>
    public class RecommendationEngine
    {
        public List<Recommendation> Recommend(CityAnalysisReport r)
        {
            var list = new List<Recommendation>();
            if (r == null) return list;

            if (r.HousingShortage > 0)
                list.Add(Score("Yangi turar joy mahallasi",
                    $"Shahrimizda {r.HousingShortage} nafar fuqaro uchun uy yetishmayapti. Bir mahalla 3-6 uy bilan buni yopadi.",
                    RecommendationAction.PlanDistrict, DistrictType.Residential,
                    severity: System.Math.Min(45, r.HousingShortage * 3), benefit: 28, urgency: 22,
                    resourcePressure: Pressure(r), risk: 6));

            if (r.CountOf(BuildingType.Market) == 0)
                list.Add(Score("Bozor qurish",
                    "Bozor yo'q — ishsizlik va savdo muammosi. Bozor ish o'rni va daromad beradi.",
                    RecommendationAction.BuildMarket, DistrictType.Commercial,
                    severity: 32, benefit: 28, urgency: 16, resourcePressure: Pressure(r), risk: 8));
            else if (r.Unemployed > System.Math.Max(3, r.Workers / 5))
                list.Add(Score("Savdo mahallasi",
                    $"{r.Unemployed} ishsiz fuqaro bor. Savdo mahallasi bozor va karvonsaroy bilan ish o'rni yaratadi.",
                    RecommendationAction.PlanDistrict, DistrictType.Commercial,
                    severity: 24, benefit: 24, urgency: 14, resourcePressure: Pressure(r), risk: 6));

            if (r.Happiness < 55 || r.CountOf(BuildingType.Garden) == 0)
                list.Add(Score("Bog' va dam olish hududi",
                    $"Fuqarolar baxti {r.Happiness}. Bog' oziq-ovqat va baxtni oshiradi.",
                    RecommendationAction.PlanDistrict, DistrictType.Garden,
                    severity: r.CountOf(BuildingType.Garden) == 0 ? 26 : 18, benefit: 20, urgency: 10,
                    resourcePressure: Pressure(r), risk: 3));

            if (r.CountOf(BuildingType.Madrasa) == 0)
                list.Add(Score("Madrasa qurish",
                    "Ta'lim maskani yo'q — madrasa fuqarolar ta'limini oshiradi.",
                    RecommendationAction.BuildMadrasa, DistrictType.Cultural,
                    severity: 18, benefit: 20, urgency: 8, resourcePressure: Pressure(r), risk: 6));

            if (r.BuildableTiles < 20 && r.LockedParcels > 0)
                list.Add(Score("Yangi yer ochish (tasdiq bilan)",
                    "Markazda joy kamaymoqda. Yangi yer faqat sizning roziligingiz va to'lovingiz bilan ochiladi.",
                    RecommendationAction.UnlockLand, null,
                    severity: 30, benefit: 26, urgency: 18, resourcePressure: Pressure(r), risk: 0));

            if (r.Treasury < 150)
                list.Add(Score("Soliq va xarajat siyosati",
                    $"Xazina kamaymoqda ({r.Treasury}). Soliq va xarajatlarni qayta ko'rib chiqing.",
                    RecommendationAction.ReviewTaxes, null,
                    severity: 26, benefit: 15, urgency: 18, resourcePressure: 0, risk: 2));

            if (r.OverallCityHealthScore >= 55 && r.BuildableTiles >= 20 && r.Treasury >= 450)
                list.Add(Score("Muvozanatli yangi mahalla",
                    $"Shahar barqaror (salomatlik {r.OverallCityHealthScore}/100). Yangi mahalla bilan rivojlanishni davom ettiring.",
                    RecommendationAction.PlanDistrict, DistrictType.Mixed,
                    severity: 8, benefit: 22, urgency: 8, resourcePressure: Pressure(r), risk: 5));

            if (list.Count == 0)
                list.Add(Score("Shaharni kuzatish",
                    "Hozircha shoshilinch muammo yo'q. Savdo va aholi o'sishini kuzatib turing.",
                    RecommendationAction.ReviewTaxes, null,
                    severity: 1, benefit: 1, urgency: 1, resourcePressure: 0, risk: 0));

            return list.OrderByDescending(x => x.PriorityScore).ToList();
        }

        private static int Pressure(CityAnalysisReport r) => r.Treasury < 100 ? 24 : r.Treasury < 250 ? 12 : 0;

        private static Recommendation Score(string title, string reason, RecommendationAction action,
            DistrictType? district, int severity, int benefit, int urgency, int resourcePressure, int risk)
            => new()
            {
                TitleUz = title, ReasonUz = reason, Action = action, SuggestedDistrict = district,
                ActionLabelUz = action == RecommendationAction.UnlockLand ? "YERNI KO'RSAT"
                    : action == RecommendationAction.ReviewTaxes ? "KO'RIB CHIQISH"
                    : "REJA TUZISH",
                PriorityScore = severity + benefit + urgency - resourcePressure - risk
            };
    }
}
