using System;
using System.Collections.Generic;
using AndijonSaltanatlar.AIGovernor;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityPlanning;

namespace AndijonSaltanatlar.Forecasting
{
    /// <summary>Prognoz natijasi — kafolat emas, faqat baholash.</summary>
    public class ForecastScenario
    {
        public string NameUz { get; set; } = "";
        public string SummaryUz { get; set; } = "";
        public string RiskUz { get; set; } = "";
        public int AddedHousing { get; set; }
        public int AddedJobs { get; set; }
        public int AddedFood { get; set; }
        public int AddedHappiness { get; set; }
        public int TotalCost { get; set; }
        public int TreasuryAfter { get; set; }
        public int HousingCapacityAfter { get; set; }
        public int HousingShortageAfter { get; set; }
        public int DailyNet { get; set; }
        public int PaybackDays { get; set; }
    }

    /// <summary>
    /// Iqtisodiy prognoz — PROMPT 15, 16-bo'lim. Reja oqibatlarini optimistik/odatiy/salbiy
    /// stsenariylarda baholaydi. Raqamlar haqiqiy bino sig'imi va katalog narxidan olinadi.
    /// </summary>
    public class EconomicForecastService
    {
        public List<ForecastScenario> Forecast(CityAnalysisReport report, CityPlan plan)
        {
            var list = new List<ForecastScenario>();
            if (report == null || plan == null) return list;

            int housing = 0, jobs = 0, food = 0, happiness = 0, cost = 0;
            foreach (var b in plan.Buildings)
            {
                if (b.Status == BlueprintStatus.Blocked) continue;
                cost += b.Cost;
                housing += b.PopulationCapacity;
                switch (b.Kind)
                {
                    case BuildingType.House:
                        jobs += 1;
                        break;
                    case BuildingType.Market:
                        jobs += 6; food += 1; happiness += 1;
                        break;
                    case BuildingType.Caravanserai:
                        jobs += 9; food += 2; happiness += 2;
                        break;
                    case BuildingType.Garden:
                        jobs += 2; food += 5; happiness += 4;
                        break;
                    case BuildingType.Farm:
                        jobs += 6; food += 8;
                        break;
                    case BuildingType.GladiatorArena:
                        happiness += 3;
                        break;
                    case BuildingType.Madrasa:
                        jobs += 3; happiness += 2;
                        break;
                    case BuildingType.Mosque:
                        jobs += 2; happiness += 5;
                        break;
                }
            }
            foreach (var r in plan.Roads)
                if (r.Status != BlueprintStatus.Blocked) cost += r.Cost;

            int treasuryAfter = report.Treasury - cost;
            int capacityAfter = report.HousingCapacity + housing;
            int shortageAfter = report.Population - capacityAfter;
            int dailyNet = report.DailyNet;
            int payback = dailyNet > 0 ? (int)Math.Ceiling(cost / (double)dailyNet) : 0;

            string effect = $"+{housing} turar joy sig'imi, +{jobs} ish o'rni, +{food} oziq-ovqat, " +
                            $"+{happiness} baxt. Xazina: {report.Treasury} → {treasuryAfter} tanga.";

            list.Add(new ForecastScenario
            {
                NameUz = "Odatiy",
                SummaryUz = effect + (shortageAfter > 0
                    ? $" Yangi sig'imdan keyin ham {shortageAfter} kishiga joy yetmaydi."
                    : " Yangi sig'im aholi ehtiyojini qoplaydi."),
                RiskUz = treasuryAfter < 100
                    ? "Qurilish davomida xazina juda kamayadi — navbat resurs uchun pauzaga tushishi mumkin."
                    : "Xavf past: tasdiqlangan budjet doirasida bajariladi.",
                AddedHousing = housing, AddedJobs = jobs, AddedFood = food, AddedHappiness = happiness,
                TotalCost = cost, TreasuryAfter = treasuryAfter, HousingCapacityAfter = capacityAfter,
                HousingShortageAfter = shortageAfter, DailyNet = dailyNet, PaybackDays = payback
            });

            int optimisticIncome = Math.Max(1, report.DailyIncome) + jobs / 2 + 2;
            int optimisticNet = optimisticIncome - report.DailyExpense;
            list.Add(new ForecastScenario
            {
                NameUz = "Optimistik",
                SummaryUz = $"Aholi tez ko'payadi, savdo jonlanadi. Kunlik sof oqim ~+{Math.Max(0, optimisticNet)} tanga; " +
                            $"xazina taxminan {Payback(cost, optimisticNet)} kunda tiklanadi.",
                RiskUz = "Bu prognoz bozor va aholi o'sishiga bog'liq — kafolat emas.",
                AddedHousing = housing, AddedJobs = jobs, AddedFood = food, AddedHappiness = happiness,
                TotalCost = cost, TreasuryAfter = treasuryAfter, HousingCapacityAfter = capacityAfter,
                HousingShortageAfter = shortageAfter, DailyNet = optimisticNet,
                PaybackDays = Payback(cost, optimisticNet)
            });

            int pessimisticNet = Math.Min(0, report.DailyNet) - Math.Max(1, jobs / 4);
            list.Add(new ForecastScenario
            {
                NameUz = "Salbiy",
                SummaryUz = treasuryAfter < 50
                    ? "Qurilish xazinani deyarli bo'shatadi; resurs yetmay navbat pauzaga tushishi mumkin."
                    : $"Aholi sekin ko'payadi, dastlab xarajat foydadan oshadi (kunlik ~{pessimisticNet} tanga).",
                RiskUz = "Reja baribir bekor qilinmaydi: pauza, qayta tasdiq yoki bekor qilish sizning qo'lingizda.",
                AddedHousing = housing, AddedJobs = jobs, AddedFood = food, AddedHappiness = happiness,
                TotalCost = cost, TreasuryAfter = treasuryAfter, HousingCapacityAfter = capacityAfter,
                HousingShortageAfter = shortageAfter, DailyNet = pessimisticNet,
                PaybackDays = Payback(cost, pessimisticNet)
            });

            return list;
        }

        private static int Payback(int cost, int net)
        {
            if (net <= 0) return 0;
            return (int)Math.Ceiling(cost / (double)net);
        }
    }
}
