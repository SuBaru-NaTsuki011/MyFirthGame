using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.RoadNetwork;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Infrastructure
{
    public enum ServiceType
    {
        Market = 0,
        Education = 1,
        Health = 2,
        Security = 3
    }

    /// <summary>
    /// Xizmat binolarining haqiqiy qamrovi. Bino qurilishi bilan xizmat butun shaharga
    /// avtomatik tarqalmaydi: radius, sig'im va YO'LGA ULANISH hisobga olinadi.
    /// Yo'lga ulanmagan xizmat binosi qamrovining faqat kichik qismini beradi.
    /// </summary>
    public sealed class ServiceCoverageSystem
    {
        private const int GridCount = 4;
        private readonly int _width;
        private readonly int _height;
        private readonly float[][] _grids = new float[GridCount][];
        private readonly List<ServicePoint> _points = new List<ServicePoint>();

        public int Version { get; private set; }

        private sealed class ServicePoint
        {
            public ServiceType Type;
            public Point Position;
            public int Radius;
            public float Strength;
        }

        public ServiceCoverageSystem(int width, int height)
        {
            _width = width;
            _height = height;
            for (int i = 0; i < GridCount; i++) _grids[i] = new float[width * height];
        }

        public static string NameUz(ServiceType t)
        {
            switch (t)
            {
                case ServiceType.Market: return "Bozor";
                case ServiceType.Education: return "Ta'lim";
                case ServiceType.Health: return "Sog'liq";
                default: return "Xavfsizlik";
            }
        }

        /// <summary>Bino qaysi xizmatni beradi (null = xizmat binosi emas).</summary>
        public static ServiceType? ServiceOf(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Market: return ServiceType.Market;
                case BuildingType.Madrasa: return ServiceType.Education;
                case BuildingType.Mosque: return ServiceType.Health;
                case BuildingType.Tower: return ServiceType.Security;
                case BuildingType.Barracks: return ServiceType.Security;
                default: return null;
            }
        }

        /// <summary>Standart xizmat radiusi (katak).</summary>
        public static int RadiusOf(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Market: return 8;
                case BuildingType.Madrasa: return 10;
                case BuildingType.Mosque: return 8;
                case BuildingType.Tower: return 7;
                case BuildingType.Barracks: return 11;
                default: return 0;
            }
        }

        /// <summary>Binolardan xizmat qamrovini qayta hisoblaydi (qurilish o'zgarganda).</summary>
        public void Rebuild(BuildingManager buildings, RoadNetworkManager network)
        {
            for (int i = 0; i < GridCount; i++) Array.Clear(_grids[i], 0, _grids[i].Length);
            _points.Clear();
            if (buildings != null)
            {
                foreach (Building b in buildings.Buildings)
                {
                    ServiceType? st = ServiceOf(b.Type);
                    if (!st.HasValue || b.BuildProgress < 1f) continue;
                    int radius = RadiusOf(b.Type) + Math.Max(0, b.Level - 1);
                    bool connected = network == null || network.HasRoadAccess(b, 3);
                    // Yo'li yo'q xizmat binosi bor-yo'g'i 35% quvvat bilan ishlaydi.
                    float strength = connected ? 1f : 0.35f;
                    var p = new ServicePoint
                    {
                        Type = st.Value,
                        Position = new Point(b.Position.X + b.Size.X / 2, b.Position.Y + b.Size.Y / 2),
                        Radius = radius,
                        Strength = strength
                    };
                    _points.Add(p);
                    Paint(p);
                }
            }
            Version++;
        }

        private void Paint(ServicePoint p)
        {
            float[] grid = _grids[(int)p.Type];
            int r = p.Radius;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = p.Position.X + dx, y = p.Position.Y + dy;
                    if ((uint)x >= (uint)_width || (uint)y >= (uint)_height) continue;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d > r) continue;
                    float value = p.Strength * (1f - (d / r) * 0.8f);
                    int i = y * _width + x;
                    if (value > grid[i]) grid[i] = value;
                }
        }

        public float CoverageAt(ServiceType type, int x, int y)
        {
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height) return 0f;
            return _grids[(int)type][y * _width + x];
        }

        public float CoverageFor(ServiceType type, Building b)
        {
            if (b == null) return 0f;
            int cx = b.Position.X + b.Size.X / 2;
            int cy = b.Position.Y + b.Size.Y / 2;
            return CoverageAt(type, cx, cy);
        }

        public int PointCount(ServiceType type)
        {
            int n = 0;
            foreach (ServicePoint p in _points) if (p.Type == type) n++;
            return n;
        }

        /// <summary>Mahalla bo'yicha qamrov: mahalladagi binolarning o'rtacha qamrovi.</summary>
        public float DistrictCoverage(ServiceType type, IEnumerable<Building> buildings)
        {
            float sum = 0f;
            int n = 0;
            foreach (Building b in buildings)
            {
                if (b.Type != BuildingType.House || b.BuildProgress < 1f) continue;
                sum += CoverageFor(type, b);
                n++;
            }
            return n == 0 ? 0f : sum / n;
        }

        public void Clear()
        {
            for (int i = 0; i < GridCount; i++) Array.Clear(_grids[i], 0, _grids[i].Length);
            _points.Clear();
            Version++;
        }
    }
}
