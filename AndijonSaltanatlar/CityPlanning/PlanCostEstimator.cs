using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;

namespace AndijonSaltanatlar.CityPlanning
{
    public sealed class CostLine
    {
        public string NameUz { get; set; } = "";
        public int Count { get; set; }
        public int UnitCost { get; set; }
        public int Total => Count * UnitCost;
    }

    /// <summary>Reja xarajat smetasi — PROMPT 15, 11-bo'lim. Narxlar BuildingFactory/Catalog'dan olinadi.</summary>
    public sealed class PlanCostEstimate
    {
        public List<CostLine> Lines { get; set; } = new();
        public int BuildingsCost { get; set; }
        public int RoadsCost { get; set; }
        public int TotalCost => BuildingsCost + RoadsCost;
        public int Treasury { get; set; }
        public int MissingCoins => Math.Max(0, TotalCost - Treasury);
        public bool CanAffordNow => MissingCoins == 0;
        public int EstimatedDays { get; set; }
        public string SummaryUz { get; set; } = "";
    }

    /// <summary>
    /// Xarajat hisoblagichi. Haqiqiy bino prototipi (o'lcham, narx, sig'im) va yo'l tarifi asosida
    /// ishlaydi — AI narxni o'ylab topmaydi.
    /// </summary>
    public static class PlanCostEstimator
    {
        public static PlanCostEstimate Estimate(CityPlan plan, int treasury, int roadTileCost)
        {
            var est = new PlanCostEstimate { Treasury = treasury };
            var byKind = new Dictionary<BuildingType, CostLine>();
            int days = 0;
            if (plan != null)
            {
                // Narxlar har doim haqiqiy prototip/katalogdan olinadi (AI taxmini emas).
                ApplyCosts(plan, roadTileCost);
                foreach (var b in plan.Buildings)
                {
                    if (b.Status == BlueprintStatus.Blocked) continue;
                    if (!byKind.TryGetValue(b.Kind, out CostLine line))
                    {
                        line = new CostLine { NameUz = b.NameUz, UnitCost = b.Cost };
                        byKind[b.Kind] = line;
                    }
                    line.Count++;
                    days += Math.Max(1, b.BuildTimeDays);
                }
                foreach (var line in byKind.Values)
                {
                    est.Lines.Add(line);
                    est.BuildingsCost += line.Total;
                }
                int roads = 0;
                foreach (var r in plan.Roads) if (r.Status != BlueprintStatus.Blocked) roads++;
                if (roads > 0)
                {
                    est.Lines.Add(new CostLine { NameUz = "Yo'llar (katak)", Count = roads, UnitCost = roadTileCost });
                    est.RoadsCost = roads * roadTileCost;
                    days += Math.Max(1, roads / 3);
                }
            }
            est.EstimatedDays = days;
            est.SummaryUz = $"Jami {est.TotalCost} tanga" +
                            (est.MissingCoins > 0 ? $", yetishmaydi {est.MissingCoins} tanga" : ", xazina yetarli") +
                            $". Taxminiy muddat: {est.EstimatedDays} kun.";
            return est;
        }

        /// <summary>Haqiqiy narxlarni blueprint elementlariga yozadi (AI taxminini emas).</summary>
        public static void ApplyCosts(CityPlan plan, int roadTileCost)
        {
            if (plan == null) return;
            int buildings = 0;
            foreach (var b in plan.Buildings)
            {
                var proto = BuildingFactory.Create(b.Kind, PointZ, 0);
                b.Size = proto.Size.X;
                b.Cost = proto.Cost;
                b.PopulationCapacity = proto.PopulationCapacity;
                b.BuildTimeDays = Math.Max(1, 1 + proto.Size.X / 2);
                if (b.Status != BlueprintStatus.Blocked) buildings += b.Cost;
            }
            int roads = 0;
            foreach (var r in plan.Roads)
            {
                r.Cost = roadTileCost;
                if (r.Status != BlueprintStatus.Blocked) roads += roadTileCost;
            }
            plan.BuildingsCost = buildings;
            plan.RoadsCost = roads;
            plan.TotalBuildDays = plan.Buildings.Count + (plan.Roads.Count + 2) / 3;
        }

        private static Microsoft.Xna.Framework.Point PointZ => new(0, 0);

        public static string BenefitOf(BuildingType k) => k switch
        {
            BuildingType.House => "Turar joy sig'imi +5",
            BuildingType.Market => "Savdo va ish o'rinlari +",
            BuildingType.Garden => "Oziq-ovqat va baxt +",
            BuildingType.Farm => "Dehqonlar, oziq-ovqat va hosil savdosi +",
            BuildingType.GladiatorArena => "Gladiatorlar va tomosha +",
            BuildingType.Madrasa => "Ta'lim +",
            BuildingType.Mosque => "Madaniyat va baxt +",
            BuildingType.Caravanserai => "Savdo daromadi +, turar joy +8",
            BuildingType.Barracks => "Mudofaa va ish o'rinlari +",
            _ => ""
        };
    }
}
