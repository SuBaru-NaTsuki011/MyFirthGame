using System;
using System.Collections.Generic;
using AndijonSaltanatlar.RoadNetwork;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Transportation
{
    /// <summary>
    /// Yo'l tirbandligi statistikasi — HAQIQIY yuklamalardan (RoadSegment.CurrentLoad) hisoblanadi.
    /// Birinchi versiyada model statistik: har bir NPC uchun fizik to'qnashuv simulyatsiyasi yo'q.
    /// </summary>
    public sealed class TrafficStatistics
    {
        public float TotalTransportLoad { get; private set; }
        public float TotalPedestrianLoad { get; private set; }
        public int CongestedSegments { get; private set; }
        public int HighSegments { get; private set; }
        public int RoadSegments { get; private set; }
        public float AverageSpeed { get; private set; }
        public float AverageMovementCost { get; private set; }
        public int Vehicles { get; private set; }
        public DateTime LastUpdated { get; private set; }

        private readonly List<RoadSegment> _hot = new List<RoadSegment>();

        /// <summary>Eng gavjum yo'l kataklari (UI ro'yxati uchun).</summary>
        public IReadOnlyList<RoadSegment> Hotspots => _hot;

        public CongestionBand CityBand
        {
            get
            {
                if (CongestedSegments > 0) return CongestionBand.Congested;
                if (HighSegments > 2) return CongestionBand.High;
                if (HighSegments > 0 || TotalTransportLoad > RoadSegments * 0.35f) return CongestionBand.Moderate;
                return CongestionBand.Low;
            }
        }

        public void Sample(RoadNetworkManager network, IEnumerable<TransportEntity> transports)
        {
            TotalTransportLoad = 0f;
            TotalPedestrianLoad = 0f;
            CongestedSegments = 0;
            HighSegments = 0;
            RoadSegments = 0;
            AverageSpeed = 0f;
            AverageMovementCost = 0f;

            float speedSum = 0f, costSum = 0f;
            int moving = 0;
            _hot.Clear();

            if (network != null)
            {
                foreach (RoadSegment s in network.Segments)
                {
                    RoadSegments++;
                    TotalTransportLoad += s.CurrentLoad;
                    TotalPedestrianLoad += s.PedestrianLoad;
                    costSum += s.EffectiveMovementCost;
                    if (s.Band == CongestionBand.Congested) CongestedSegments++;
                    else if (s.Band == CongestionBand.High) HighSegments++;
                    if (s.CongestionLevel > 0.35f) _hot.Add(s);
                }
                if (RoadSegments > 0) AverageMovementCost = costSum / RoadSegments;
            }

            Vehicles = 0;
            if (transports != null)
                foreach (TransportEntity t in transports)
                {
                    Vehicles++;
                    if (t.CurrentSpeed > 0.01f) { speedSum += t.CurrentSpeed; moving++; }
                }
            if (moving > 0) AverageSpeed = speedSum / moving;

            _hot.Sort((a, b) => b.CongestionLevel.CompareTo(a.CongestionLevel));
            if (_hot.Count > 8) _hot.RemoveRange(8, _hot.Count - 8);
            LastUpdated = DateTime.UtcNow;
        }

        public void Clear()
        {
            TotalTransportLoad = 0f;
            TotalPedestrianLoad = 0f;
            CongestedSegments = 0;
            HighSegments = 0;
            RoadSegments = 0;
            AverageSpeed = 0f;
            AverageMovementCost = 0f;
            Vehicles = 0;
            _hot.Clear();
        }
    }
}
