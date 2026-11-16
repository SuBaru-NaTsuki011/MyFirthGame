using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.RoadNetwork;

namespace AndijonSaltanatlar.Infrastructure
{
    /// <summary>Xizmat ko'rsatish statistikasi (panel va Vazir uchun).</summary>
    public sealed class PublicServiceManager
    {
        private readonly BuildingManager _buildings;
        private readonly RoadNetworkManager _network;

        public ServiceCoverageSystem Coverage { get; }

        public PublicServiceManager(int mapWidth, int mapHeight, BuildingManager buildings, RoadNetworkManager network)
        {
            _buildings = buildings;
            _network = network;
            Coverage = new ServiceCoverageSystem(mapWidth, mapHeight);
        }

        public void Rebuild() => Coverage.Rebuild(_buildings, _network);

        /// <summary>Shahar bo'yicha xizmat qamrovi foizi (uylar o'rtacha qamrovi).</summary>
        public float CityCoverage(ServiceType type)
        {
            if (_buildings == null) return 0f;
            return Coverage.DistrictCoverage(type, _buildings.Buildings) * 100f;
        }

        public int ConnectedServiceBuildings
        {
            get
            {
                int n = 0;
                if (_buildings == null) return 0;
                foreach (Building b in _buildings.Buildings)
                {
                    if (b.BuildProgress < 1f) continue;
                    if (!ServiceCoverageSystem.ServiceOf(b.Type).HasValue) continue;
                    if (_network == null || _network.HasRoadAccess(b, 3)) n++;
                }
                return n;
            }
        }

        public int DisconnectedServiceBuildings
        {
            get
            {
                int n = 0;
                if (_buildings == null) return 0;
                foreach (Building b in _buildings.Buildings)
                {
                    if (b.BuildProgress < 1f) continue;
                    if (!ServiceCoverageSystem.ServiceOf(b.Type).HasValue) continue;
                    if (_network != null && !_network.HasRoadAccess(b, 3)) n++;
                }
                return n;
            }
        }

        public List<string> SummaryLines()
        {
            var lines = new List<string>();
            foreach (ServiceType t in new[] { ServiceType.Market, ServiceType.Education, ServiceType.Health, ServiceType.Security })
            {
                float pct = CityCoverage(t);
                lines.Add(ServiceCoverageSystem.NameUz(t) + ": " + (int)pct + "%");
            }
            return lines;
        }
    }
}
