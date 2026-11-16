using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityPlanning;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.LandExpansion;
using AndijonSaltanatlar.World;

namespace AndijonSaltanatlar.AIGovernor
{
    /// <summary>
    /// Shahar holatini haqiqiy GameState'dan o'qib tahlil qiladi — PROMPT 15, 4-bo'lim.
    /// Har frame'da emas, so'rov yoki muhim hodisada ishga tushiriladi (22-bo'lim, performance).
    /// </summary>
    public class CityAnalysisEngine
    {
        private readonly GameStateManager _state;
        private readonly BuildingManager _buildings;
        private readonly MapManager _map;

        public CityAnalysisEngine(GameStateManager state, BuildingManager buildings, MapManager map)
        {
            _state = state; _buildings = buildings; _map = map;
        }

        public CityAnalysisReport Analyze()
        {
            var r = new CityAnalysisReport
            {
                Population = _state.Population,
                HousingUsed = SafeInt(() => _state.Pop.HousingUsed, _state.Population),
                HousingCapacity = _buildings.TotalPopulation,
                Treasury = _state.Coins,
                Food = _state.Food,
                Happiness = (int)Safe(() => _state.Pop.AvgHappiness),
                Education = (int)Safe(() => _state.Pop.AvgEducation),
                Health = (int)Safe(() => _state.Pop.AvgHealth),
                Security = (int)Safe(() => _state.Pop.AvgSecurity),
                Workers = SafeInt(() => _state.Pop.Workers, 0),
                Unemployed = SafeInt(() => _state.Pop.Unemployed, 0),
                DailyIncome = _state.Economy.LastIncome,
                DailyExpense = _state.Economy.LastExpense,
                FoodProduced = _state.Economy.LastFoodProduced,
                FoodConsumed = _state.Economy.LastFoodConsumed,
                TotalBuildings = _buildings.Buildings.Count,
                MilitaryStrength = SafeInt(() => _state.Military.TotalStrength, 0)
            };
            r.HousingShortage = r.Population - r.HousingCapacity;

            foreach (BuildingType t in Enum.GetValues(typeof(BuildingType)))
                r.BuildingCounts[t] = _buildings.Count(t);

            foreach (var p in _state.Land.Parcels)
            {
                if (p.State == LandParcelState.Open) r.OpenParcels++;
                else if (p.State == LandParcelState.Locked || p.State == LandParcelState.Unlockable)
                {
                    r.LockedParcels++;
                    r.RecommendedLandParcels.Add(p.Id);
                }
            }

            CountMapStats(r);

            // ---- Muammolarni aniqlash ----
            if (r.HousingShortage > 0)
            {
                r.DetectedProblems.Add($"Turar joy yetishmaydi: {r.HousingShortage} nafar fuqaro uchun joy yo'q.");
                r.RecommendedBuildings.Add(BuildingType.House);
            }
            if (r.Happiness < 55)
            {
                r.DetectedProblems.Add($"Fuqarolar baxti past ({r.Happiness}). Bog' va infratuzilma kerak.");
                r.RecommendedBuildings.Add(BuildingType.Garden);
            }
            if (r.Food < 80)
                r.DetectedProblems.Add($"Oziq-ovqat zaxirasi kam ({r.Food}). Bog' yoki savdo kerak.");
            if (r.Treasury < 150)
            {
                r.DetectedProblems.Add($"Xazina kamaymoqda ({r.Treasury} tanga). Savdo va soliqni ko'rib chiqing.");
                r.RecommendedBuildings.Add(BuildingType.Market);
            }
            if (r.Unemployed > Math.Max(3, r.Workers / 5))
            {
                r.DetectedProblems.Add($"Ishsizlar ko'p ({r.Unemployed}). Bozor va hunarmandchilik ish o'rni beradi.");
                if (!r.RecommendedBuildings.Contains(BuildingType.Market)) r.RecommendedBuildings.Add(BuildingType.Market);
            }
            if (r.Health < 50)
                r.DetectedProblems.Add($"Aholi sog'lig'i past ({r.Health}). Bog' va oziq-ovqat muhim.");
            if (r.Education < 30 && r.CountOf(BuildingType.Madrasa) == 0)
                r.DetectedProblems.Add("Ta'lim darajasi past — madrasa qurish kerak.");

            // ---- Infratuzilma bo'shliqlari ----
            if (r.CountOf(BuildingType.Market) == 0)
            {
                r.InfrastructureGaps.Add("Bozor yo'q — savdo va ish o'rinlari cheklangan.");
                if (!r.RecommendedBuildings.Contains(BuildingType.Market)) r.RecommendedBuildings.Add(BuildingType.Market);
            }
            if (r.CountOf(BuildingType.Madrasa) == 0)
            {
                r.InfrastructureGaps.Add("Madrasa (maktab) yo'q — ta'lim past qoladi.");
                r.RecommendedBuildings.Add(BuildingType.Madrasa);
            }
            if (r.CountOf(BuildingType.Garden) == 0)
                r.InfrastructureGaps.Add("Bog' yo'q — dam olish maydonlari kerak.");
            if (r.RoadTiles < 40)
                r.InfrastructureGaps.Add("Yo'l tarmog'i siyrak — yangi mahalla uchun ko'cha kerak.");

            // ---- Rivojlanish imkoniyatlari ----
            if (r.BuildableTiles < 20 && r.LockedParcels > 0)
                r.DevelopmentOpportunities.Add("Markazda joy kamaymoqda — yangi yer ochib mahalla qurish mumkin (faqat tasdiq bilan).");
            if (r.BuildableTiles >= 20)
                r.DevelopmentOpportunities.Add($"Bo'sh maydonlarda yangi mahalla rejalashtirish mumkin ({r.BuildableTiles} katak).");
            if (r.Treasury >= 900)
                r.DevelopmentOpportunities.Add("Xazina yangi mahalla qurishga yetadi — reja tuzish mumkin.");
            if (r.HousingShortage > 0)
                r.DevelopmentOpportunities.Add($"{Math.Max(1, (r.HousingShortage + 4) / 5)} yangi uy sig'imni yopadi.");

            // ---- Resurs ehtiyojlari (taxminiy) ----
            if (r.HousingShortage > 0)
                r.EstimatedResourceNeeds["uylar"] = Math.Max(1, (int)Math.Ceiling(r.HousingShortage / 5.0));
            if (r.CountOf(BuildingType.Market) == 0) r.EstimatedResourceNeeds["bozorlar"] = 1;
            if (r.CountOf(BuildingType.Garden) == 0) r.EstimatedResourceNeeds["bog'lar"] = 1;

            r.OverallCityHealthScore = ComputeHealth(r);
            return r;
        }

        private void CountMapStats(CityAnalysisReport r)
        {
            var grid = new MapPlanningGrid(_state, _map);
            int buildable = 0, roads = 0;
            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    if (_map.GetTile(x, y) == TileType.Road) roads++;
                    if (grid.IsOpenLand(x, y) && grid.IsBuildableTerrain(x, y) &&
                        !grid.IsBlockedForBuilding(x, y) && !grid.IsProtected(x, y))
                        buildable++;
                }
            r.BuildableTiles = buildable;
            r.RoadTiles = roads;
        }

        private static int ComputeHealth(CityAnalysisReport r)
        {
            int score = 50;
            score += r.HousingShortage <= 0 ? 15 : -Math.Min(25, r.HousingShortage);
            score += (r.Happiness - 50) / 3;
            score += r.Treasury > 300 ? 10 : (r.Treasury < 100 ? -10 : 0);
            score += r.Food > 150 ? 5 : (r.Food < 60 ? -10 : 0);
            score += r.CountOf(BuildingType.Market) > 0 ? 8 : -8;
            score += r.CountOf(BuildingType.Madrasa) > 0 ? 5 : 0;
            score += r.DailyNet >= 0 ? 4 : -6;
            return Math.Max(0, Math.Min(100, score));
        }

        private static float Safe(Func<float> f) { try { return f(); } catch { return 0; } }
        private static int SafeInt(Func<int> f, int fallback) { try { return f(); } catch { return fallback; } }
    }
}
