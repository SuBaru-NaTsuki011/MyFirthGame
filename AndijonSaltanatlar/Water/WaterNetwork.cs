using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Environment;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Water
{
    /// <summary>
    /// Suv tarmog'i (PROMPT 18): WATER SOURCE → DISTRIBUTION → CONSUMER BUILDING.
    ///
    /// Manbalar: quduq va hovuz (bezak sifatida quriladi), ariq/daryo suv hududlari.
    /// Qamrov to'ri manbalar yoki mavsum o'zgarganda qayta hisoblanadi.
    /// Suv cheksiz resurs EMAS: manba sig'imi va radiusi haqiqiy cheklov beradi.
    /// </summary>
    public sealed class WaterNetworkManager
    {
        private readonly MapManager _map;
        private readonly BuildingManager _buildings;

        public List<WaterSource> Sources { get; } = new List<WaterSource>();
        public WaterCoverageCalculator Coverage { get; }
        public Season CurrentSeason { get; private set; } = Season.Bahor;

        /// <summary>Manbalar yoki mavsum o'zgardi (overlay yangilanishi uchun).</summary>
        public event Action Changed;

        public WaterNetworkManager(MapManager map, BuildingManager buildings)
        {
            _map = map;
            _buildings = buildings;
            Coverage = new WaterCoverageCalculator(map.Width, map.Height);
            Rebuild();
        }

        // ==================================================================
        //  MANBALARNI YIG'ISH
        // ==================================================================

        public void Rebuild()
        {
            Sources.Clear();
            int id = 1;

            // 1) Quduqlar va hovuzlar (bezaklar).
            foreach (Decoration d in _map.Decorations)
            {
                if (d.Type == DecorationType.Well)
                {
                    Sources.Add(new WaterSource
                    {
                        Id = id++, Type = WaterSourceType.Well, NameUz = "Mahalla qudug'i",
                        Position = d.Position, Capacity = 8f, ServiceRadius = 6,
                        Quality = 1f, SeasonalModifier = WellSeasonModifier(CurrentSeason),
                        DecorationId = d.Id
                    });
                }
                else if (d.Type == DecorationType.Hovuz)
                {
                    Sources.Add(new WaterSource
                    {
                        Id = id++, Type = WaterSourceType.Pond, NameUz = "Suv havzasi",
                        Position = d.Position, Capacity = 12f, ServiceRadius = 5,
                        Quality = 0.95f, SeasonalModifier = BodySeasonModifier(CurrentSeason),
                        DecorationId = d.Id
                    });
                }
            }

            // 2) Ariq/daryo suv hududlari: har bir uzluksiz suv bo'lagi — bitta manba.
            foreach (WaterBody body in FindWaterBodies())
            {
                Sources.Add(new WaterSource
                {
                    Id = id++, Type = WaterSourceType.Canal, NameUz = "Ariq",
                    Position = body.Center, Capacity = body.Capacity,
                    ServiceRadius = body.Radius, Quality = 0.7f,
                    SeasonalModifier = BodySeasonModifier(CurrentSeason),
                    TilesCovered = body.Area
                });
            }

            Coverage.Rebuild(Sources);
            Changed?.Invoke();
        }

        private sealed class WaterBody
        {
            public Point Center;
            public int Radius;
            public int Area;
            public float Capacity;
        }

        /// <summary>Suv kataklarini uzluksiz bo'laklarga ajratadi (ariq/daryo).</summary>
        private List<WaterBody> FindWaterBodies()
        {
            var result = new List<WaterBody>();
            var seen = new bool[_map.Width, _map.Height];
            var stack = new Stack<Point>();

            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    if (seen[x, y] || _map.GetTile(x, y) != TileType.Water) continue;

                    int minX = x, maxX = x, minY = y, maxY = y, area = 0;
                    long sumX = 0, sumY = 0;
                    stack.Clear();
                    stack.Push(new Point(x, y));
                    seen[x, y] = true;

                    while (stack.Count > 0)
                    {
                        Point p = stack.Pop();
                        area++;
                        sumX += p.X; sumY += p.Y;
                        if (p.X < minX) minX = p.X;
                        if (p.X > maxX) maxX = p.X;
                        if (p.Y < minY) minY = p.Y;
                        if (p.Y > maxY) maxY = p.Y;

                        TryPush(p.X + 1, p.Y); TryPush(p.X - 1, p.Y);
                        TryPush(p.X, p.Y + 1); TryPush(p.X, p.Y - 1);
                    }

                    if (area < 2) continue;
                    int w = maxX - minX + 1, h = maxY - minY + 1;
                    result.Add(new WaterBody
                    {
                        Center = new Point((int)(sumX / area), (int)(sumY / area)),
                        Radius = Math.Clamp(Math.Max(w, h) / 2 + 2, 3, 12),
                        Area = area,
                        Capacity = Math.Min(40f, 6f + area * 0.35f)
                    });

                    void TryPush(int nx, int ny)
                    {
                        if (!_map.InBounds(nx, ny) || seen[nx, ny]) return;
                        if (_map.GetTile(nx, ny) != TileType.Water) return;
                        seen[nx, ny] = true;
                        stack.Push(new Point(nx, ny));
                    }
                }
            return result;
        }

        private static float WellSeasonModifier(Season s)
        {
            switch (s)
            {
                case Season.Yoz: return 0.85f;
                case Season.Qish: return 0.9f;
                default: return 1f;
            }
        }

        private static float BodySeasonModifier(Season s)
        {
            switch (s)
            {
                case Season.Yoz: return 0.6f;   // yozda ariq sayozlanadi
                case Season.Qish: return 0.8f;
                case Season.Kuz: return 0.95f;
                default: return 1f;
            }
        }

        public void SetSeason(Season season)
        {
            if (season == CurrentSeason) return;
            CurrentSeason = season;
            Rebuild();
        }

        // ==================================================================
        //  ISTE'MOL
        // ==================================================================

        /// <summary>Bino kunlik suv ehtiyoji (birlik).</summary>
        public static float DemandOf(BuildingType type, int level)
        {
            float baseDemand;
            switch (type)
            {
                case BuildingType.House: baseDemand = 1f; break;
                case BuildingType.Garden: baseDemand = 2f; break;
                case BuildingType.Market: baseDemand = 1.2f; break;
                case BuildingType.Madrasa: baseDemand = 1f; break;
                case BuildingType.Mosque: baseDemand = 0.6f; break;
                case BuildingType.Caravanserai: baseDemand = 1.2f; break;
                case BuildingType.Barracks: baseDemand = 1.4f; break;
                default: return 0f;
            }
            return baseDemand * Math.Max(1, level);
        }

        public float CoverageOf(Building b)
            => b == null ? 0f : Coverage.ForBuilding(b.Position, b.Size.X);

        public float TotalSupply
        {
            get { float s = 0f; foreach (WaterSource w in Sources) s += w.EffectiveCapacity; return s; }
        }

        public float TotalDemand
        {
            get
            {
                if (_buildings == null) return 0f;
                float s = 0f;
                foreach (Building b in _buildings.Buildings)
                {
                    if (b.BuildProgress < 1f) continue;
                    s += DemandOf(b.Type, b.Level);
                }
                return s;
            }
        }

        public int ConsumingBuildings
        {
            get
            {
                int n = 0;
                if (_buildings == null) return 0;
                foreach (Building b in _buildings.Buildings)
                    if (b.BuildProgress >= 1f && DemandOf(b.Type, b.Level) > 0f) n++;
                return n;
            }
        }

        public int ServedBuildings
        {
            get
            {
                int n = 0;
                if (_buildings == null) return 0;
                foreach (Building b in _buildings.Buildings)
                {
                    if (b.BuildProgress < 1f || DemandOf(b.Type, b.Level) <= 0f) continue;
                    if (CoverageOf(b) >= 0.5f) n++;
                }
                return n;
            }
        }

        public int UnservedBuildings => Math.Max(0, ConsumingBuildings - ServedBuildings);

        /// <summary>Ta'minlangan aholi soni (uy suv qamroviga qarab).</summary>
        public int ServedPopulation(Population.PopulationManager pop)
        {
            if (pop == null) return 0;
            int n = 0;
            foreach (Population.Citizen c in pop.Citizens)
            {
                if (!c.HomeBuildingId.HasValue) continue;
                Building home = FindBuilding(c.HomeBuildingId.Value);
                if (home != null && CoverageOf(home) >= 0.5f) n++;
            }
            return n;
        }

        public float AverageCoverage
        {
            get
            {
                if (_buildings == null) return 0f;
                float sum = 0f;
                int n = 0;
                foreach (Building b in _buildings.Buildings)
                {
                    if (b.BuildProgress < 1f || DemandOf(b.Type, b.Level) <= 0f) continue;
                    sum += CoverageOf(b);
                    n++;
                }
                return n == 0 ? 1f : sum / n;
            }
        }

        public bool HasShortage => UnservedBuildings > 0 || AverageCoverage < 0.55f;

        public Building FindBuilding(int id)
        {
            if (_buildings == null) return null;
            foreach (Building b in _buildings.Buildings) if (b.Id == id) return b;
            return null;
        }

        public WaterSource NearestSource(Point p)
        {
            WaterSource best = null;
            float bestD = float.MaxValue;
            foreach (WaterSource s in Sources)
            {
                float d = Math.Abs(s.Position.X - p.X) + Math.Abs(s.Position.Y - p.Y);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        public void Clear()
        {
            Sources.Clear();
            Coverage.Clear();
        }
    }
}
