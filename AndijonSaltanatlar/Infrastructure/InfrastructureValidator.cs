using System;
using System.Collections.Generic;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Logistics;
using AndijonSaltanatlar.RoadNetwork;
using AndijonSaltanatlar.Water;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Infrastructure
{
    /// <summary>Bitta mahallaning infratuzilma holati.</summary>
    public sealed class DistrictInfrastructureReport
    {
        public int DistrictId { get; set; }
        public string NameUz { get; set; } = "";
        public int Houses { get; set; }
        public int HousesWithoutRoad { get; set; }
        public int Population { get; set; }
        public float WaterCoverage { get; set; }
        public float MarketCoverage { get; set; }
        public float EducationCoverage { get; set; }
        public float HealthCoverage { get; set; }
        public float SecurityCoverage { get; set; }
        public bool HasWarehouse { get; set; }
        public bool HasDeliveryRoute { get; set; }
        public List<string> IssuesUz { get; } = new List<string>();

        public bool IsReady => IssuesUz.Count == 0;
        public bool NeedsRoad => HousesWithoutRoad > 0;
        public bool NeedsWater => WaterCoverage < 0.5f;
    }

    /// <summary>
    /// Mahalla infratuzilmasini tekshiradi: yo'l, suv, bozor, ombor, xizmatlar.
    /// Donishmand Vazir va "INFRATUZILMA REJASI" tugmasi shu hisobotga tayanadi.
    /// </summary>
    public sealed class InfrastructureValidator
    {
        private readonly RoadNetworkManager _network;
        private readonly WaterNetworkManager _water;
        private readonly PublicServiceManager _services;
        private readonly BuildingManager _buildings;
        private readonly LogisticsManager _logistics;
        private readonly GameStateManager _state;

        public InfrastructureValidator(RoadNetworkManager network, WaterNetworkManager water,
                                       PublicServiceManager services, BuildingManager buildings,
                                       LogisticsManager logistics, GameStateManager state)
        {
            _network = network;
            _water = water;
            _services = services;
            _buildings = buildings;
            _logistics = logistics;
            _state = state;
        }

        public List<DistrictInfrastructureReport> ValidateAll()
        {
            var list = new List<DistrictInfrastructureReport>();
            if (_state?.Life?.Districts == null) return list;
            foreach (DistrictStats d in _state.Life.Districts.Districts)
            {
                if (d.Population == 0 && d.Houses == 0) continue;
                list.Add(ValidateDistrict(d.Id));
            }
            return list;
        }

        public DistrictInfrastructureReport ValidateDistrict(int districtId)
        {
            var report = new DistrictInfrastructureReport { DistrictId = districtId };
            DistrictStats stats = _state?.Life?.Districts.Get(districtId);
            report.NameUz = stats?.NameUz ?? ("Mahalla " + districtId);
            report.Population = stats?.Population ?? 0;

            var houses = new List<Building>();
            var gardens = new List<Building>();
            var all = new List<Building>();
            if (_buildings != null && _state?.Life?.Districts != null)
            {
                foreach (Building b in _buildings.Buildings)
                {
                    if (b.BuildProgress < 1f) continue;
                    int did = _state.Life.Districts.DistrictOfBuilding(b.Id);
                    if (did != districtId) continue;
                    all.Add(b);
                    if (b.Type == BuildingType.House) houses.Add(b);
                    if (b.Type == BuildingType.Garden) gardens.Add(b);
                }
            }
            report.Houses = houses.Count;

            // --- Yo'l ulanishi ---
            foreach (Building h in houses)
                if (_network == null || !_network.HasRoadAccess(h, 3)) report.HousesWithoutRoad++;

            // --- Suv ---
            float waterSum = 0f;
            int waterN = 0;
            foreach (Building b in all)
            {
                if (WaterNetworkManager.DemandOf(b.Type, b.Level) <= 0f) continue;
                waterSum += _water != null ? _water.CoverageOf(b) : 0f;
                waterN++;
            }
            report.WaterCoverage = waterN == 0 ? 1f : waterSum / waterN;

            // --- Xizmatlar ---
            if (_services != null && houses.Count > 0)
            {
                report.MarketCoverage = _services.Coverage.DistrictCoverage(ServiceType.Market, houses);
                report.EducationCoverage = _services.Coverage.DistrictCoverage(ServiceType.Education, houses);
                report.HealthCoverage = _services.Coverage.DistrictCoverage(ServiceType.Health, houses);
                report.SecurityCoverage = _services.Coverage.DistrictCoverage(ServiceType.Security, houses);
            }

            // --- Ombor va yetkazib berish yo'li ---
            if (_logistics != null)
            {
                foreach (WarehouseState w in _logistics.Warehouses.All.Values)
                {
                    if (w.IsMarket || !w.RoadConnected) continue;
                    report.HasWarehouse = true;
                    break;
                }
                report.HasDeliveryRoute = HasMarketRoute(gardens);
            }

            // --- Muammolar (aniq, o'lchovli) ---
            if (report.HousesWithoutRoad > 0)
                report.IssuesUz.Add(report.HousesWithoutRoad + " uy ko'chaga ulanmagan");
            if (report.WaterCoverage < 0.5f)
                report.IssuesUz.Add("suv ta'minoti yetarli emas (" + (int)(report.WaterCoverage * 100) + "%)");
            if (houses.Count > 0 && report.MarketCoverage < 0.25f)
                report.IssuesUz.Add("bozor xizmati yetib bormaydi");
            if (houses.Count > 0 && report.EducationCoverage < 0.2f)
                report.IssuesUz.Add("maktab qamrovi yo'q");
            if (houses.Count > 0 && report.SecurityCoverage < 0.2f)
                report.IssuesUz.Add("xavfsizlik qamrovi past");
            if (!report.HasWarehouse && gardens.Count > 0)
                report.IssuesUz.Add("ombor (karvonsaroy) yo'q — hosil yo'lda qoladi");
            if (gardens.Count > 0 && !report.HasDeliveryRoute)
                report.IssuesUz.Add("dehqon xo'jaligidan bozorga yo'l yo'q");

            return report;
        }

        private bool HasMarketRoute(List<Building> gardens)
        {
            if (_network == null || _logistics == null || gardens.Count == 0) return false;
            foreach (WarehouseState m in _logistics.Warehouses.All.Values)
            {
                if (!m.IsMarket) continue;
                Point marketPos = _logistics.CenterOf(m.BuildingId);
                if (marketPos.X < 0) continue;
                foreach (Building g in gardens)
                {
                    if (RouteFinder.IsReachable(_network, LogisticsManager.Center(g), marketPos, 8)) return true;
                }
            }
            return false;
        }

        /// <summary>Bo'sh (qurilish mumkin) katak topadi — quduq/reja uchun.</summary>
        public static Point? FindFreeTileNear(MapManager map, int cx, int cy, int radius)
        {
            for (int r = 1; r <= radius; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                        int x = cx + dx, y = cy + dy;
                        if (!map.InBounds(x, y)) continue;
                        TileType t = map.GetTile(x, y);
                        if (t != TileType.Ground && t != TileType.Grass) continue;
                        if (map.GetOccupant(x, y) != null) continue;
                        return new Point(x, y);
                    }
            return null;
        }
    }
}
