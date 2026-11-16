using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.SmartConstruction;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.CityPlanning
{
    /// <summary>
    /// AI shahar rejalashtiruvchi — PROMPT 15, 6-15-bo'lim. Mahalla turini haqiqiy quriladigan binolar va
    /// yo'llardan iborat blueprintga aylantiradi. Blueprint faqat preview — GameState'ni o'zgartirmaydi.
    /// Har bir bino uchun bir nechta nomzod baholanadi, yo'llar A*/graph search bilan ulanadi.
    /// </summary>
    public sealed class CityPlanningManager
    {
        public const int RoadTileCost = 10;

        private readonly ICityPlanningGrid _grid;
        private readonly Func<int> _treasury;
        private readonly Func<BuildingType, bool> _isUnlocked;
        private readonly Func<int> _day;

        public CityPlanningManager(ICityPlanningGrid grid, Func<int> treasury,
            Func<BuildingType, bool> isUnlocked, Func<int> day)
        {
            _grid = grid;
            _treasury = treasury ?? (() => 0);
            _isUnlocked = isUnlocked ?? (_ => true);
            _day = day ?? (() => 1);
        }

        public CityPlan GeneratePlan(DistrictType type, int housingShortage = 0, bool improveExisting = false)
        {
            var objective = improveExisting
                ? PlanningObjective.ForImprovement(housingShortage)
                : PlanningObjective.ForDistrict(type, housingShortage);
            var reserved = new HashSet<Point>();
            List<Point> anchors = improveExisting
                ? SmartBuildingPlacement.FindInfillAnchors(_grid, 5)
                : SmartBuildingPlacement.FindDistrictAnchors(_grid, reserved, 4);
            if (anchors.Count == 0)
            {
                return FailurePlan(type, improveExisting
                    ? "Mavjud mahalla ichida bo'shliq topilmadi — yangi hudud rejasini tanlang."
                    : "Bo'sh ochiq yer topilmadi. Avval yangi hudud ochish kerak.", objective);
            }

            CityPlan best = null;
            int bestScore = int.MinValue;
            foreach (var anchor in anchors)
            {
                CityPlan attempt = BuildAttempt(type, objective, anchor);
                if (attempt == null) continue;
                int placed = 0;
                foreach (var b in attempt.Buildings) if (b.Status != BlueprintStatus.Blocked) placed++;
                int score = placed * 140 - attempt.TotalCost / 4 - attempt.RoadCount;
                if (attempt.IsValid) score += 900;
                if (score > bestScore) { bestScore = score; best = attempt; }
                if (attempt.IsValid && placed >= objective.RequestedBuildings) break;
            }

            if (best == null)
                return FailurePlan(type, "Bu turdagi mahalla uchun mos joy topilmadi.", objective);

            if (improveExisting)
                best.RationaleUz = "Mavjud mahalla ichidagi bo'shliqlar uchun tejamkor reja. " + best.RationaleUz;
            if (!best.IsValid)
                best.RationaleUz += " Diqqat: reja hozircha to'liq yaroqli emas — tafsilotlar blueprintda.";
            else
                best.RationaleUz += " REJA TASDIQLANGUNCHA hech narsa qurilmaydi.";
            return best;
        }

        private CityPlan BuildAttempt(DistrictType type, PlanningObjective objective, Point anchor)
        {
            var plan = new CityPlan
            {
                Id = (System.Environment.TickCount & 0x7fffffff),
                Type = type,
                NameUz = objective.LabelUz,
                CreatedDay = _day(),
                Anchor = anchor
            };

            var reserved = new HashSet<Point>();
            var kinds = BuildKindList(objective);
            var skippedLocked = new List<string>();

            foreach (BuildingType kind in kinds)
            {
                if (plan.Buildings.Count >= objective.MaxBuildings) break;
                if (!_isUnlocked(kind))
                {
                    string name = DisplayName(kind);
                    if (!skippedLocked.Contains(name)) skippedLocked.Add(name);
                    continue;
                }
                Building proto = BuildingFactory.Create(kind, new Point(0, 0), 0);
                int size = proto.Size.X;
                Point? spot = SmartBuildingPlacement.FindSpot(_grid, anchor, size, reserved, 18, 220);
                if (!spot.HasValue) continue;

                var item = new BlueprintItem
                {
                    Id = plan.Buildings.Count + 1,
                    Kind = kind,
                    Tile = spot.Value,
                    Size = size,
                    Cost = proto.Cost,
                    PopulationCapacity = proto.PopulationCapacity,
                    Status = BlueprintStatus.Buildable,
                    BuildTimeDays = Math.Max(1, 1 + size / 2),
                    BenefitUz = PlanCostEstimator.BenefitOf(kind),
                    RequiredTech = BuildingCatalog.RequiredTech(kind)
                };
                plan.Buildings.Add(item);
                for (int y = item.Tile.Y; y < item.Tile.Y + size; y++)
                    for (int x = item.Tile.X; x < item.Tile.X + size; x++)
                        reserved.Add(new Point(x, y));
            }

            if (plan.Buildings.Count == 0)
            {
                plan.IsValid = false;
                plan.RationaleUz = "Bu hududda bino uchun yetarli bo'sh joy topilmadi.";
                return plan;
            }

            RoadPlanResult roads = SmartRoadPlanner.PlanRoads(_grid, anchor, plan.Buildings, reserved,
                RoadTileCost, 5500);
            foreach (var t in roads.Tiles)
                plan.Roads.Add(new BlueprintRoad { Tile = t, Cost = RoadTileCost });

            PlanValidator.Validate(plan, _grid, _treasury(), _isUnlocked, RoadTileCost);

            var notes = new List<string>();
            if (skippedLocked.Count > 0)
                notes.Add("Texnologiya yopiq bo'lgani uchun rejaga kiritilmadi: " + string.Join(", ", skippedLocked) + ".");
            if (roads.UnconnectedBuildings > 0)
                notes.Add(roads.UnconnectedBuildings + " bino yo'lga ulanmadi.");
            notes.Add($"Jami narx: {plan.TotalCost} tanga (xazina: {_treasury()} tanga).");
            plan.RationaleUz = string.Join(" ", notes);
            return plan;
        }

        private List<BuildingType> BuildKindList(PlanningObjective o)
        {
            var list = new List<BuildingType>();
            for (int i = 0; i < o.Houses; i++) list.Add(BuildingType.House);
            for (int i = 0; i < o.Markets; i++) list.Add(BuildingType.Market);
            for (int i = 0; i < o.Gardens; i++) list.Add(BuildingType.Garden);
            for (int i = 0; i < o.Madrasas; i++) list.Add(BuildingType.Madrasa);
            for (int i = 0; i < o.Mosques; i++) list.Add(BuildingType.Mosque);
            for (int i = 0; i < o.Caravanserais; i++)
            {
                if (_isUnlocked(BuildingType.Caravanserai)) list.Add(BuildingType.Caravanserai);
                else list.Add(BuildingType.Market);
            }
            return list;
        }

        private static string DisplayName(BuildingType k) => k switch
        {
            BuildingType.House => "uy",
            BuildingType.Market => "bozor",
            BuildingType.Garden => "bog'",
            BuildingType.Madrasa => "madrasa",
            BuildingType.Mosque => "masjid",
            BuildingType.Caravanserai => "karvonsaroy",
            _ => k.ToString().ToLowerInvariant()
        };

        private CityPlan FailurePlan(DistrictType type, string reason, PlanningObjective objective = null)
        {
            objective ??= PlanningObjective.ForDistrict(type, 0);
            return new CityPlan
            {
                Type = type, NameUz = objective.LabelUz, CreatedDay = _day(),
                IsValid = false, State = PlanExecState.Draft, RationaleUz = reason
            };
        }
    }
}
