using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.RoadNetwork
{
    /// <summary>
    /// Yo'l tarmog'ining bitta katagi. Id — segmentning doimiy raqami; graf tugunlari esa
    /// katak kaliti bo'yicha quriladi (RoadGraphBuilder), shuning uchun yo'l o'zgarsa graf
    /// butunlay qayta quriladi va marshrut keshi bekor qilinadi.
    /// </summary>
    public sealed class RoadSegment
    {
        public int Id { get; set; }
        public RoadType Type { get; set; }
        public Point GridPosition { get; set; }

        public float Capacity { get; set; }
        /// <summary>Pathfinding narxi (1 / SpeedFactor).</summary>
        public float MovementCost { get; set; }
        /// <summary>Yo'l holati 0..1 (1 = yangi). Vaqt o'tishi bilan pasayadi, ta'mirlash mumkin.</summary>
        public float Condition { get; set; } = 1f;
        public float MaintenanceCost { get; set; }
        /// <summary>Yo'l qaysi shahar/hududga tegishli (0 = markaziy shahar).</summary>
        public int OwnerCityId { get; set; }
        public bool IsBlocked { get; set; }
        /// <summary>Suv ustidagi ko'prikmi.</summary>
        public bool IsBridge { get; set; }
        /// <summary>Atrofdagi yo'l ulanishlari bitmask: 1=shimol(-y), 2=sharq(+x), 4=janub(+y), 8=g'arb(-x).</summary>
        public int ConnectionMask { get; set; }
        /// <summary>O'yinchi qurganmi (statistika va ta'mirlash uchun).</summary>
        public bool BuiltByPlayer { get; set; }
        public int BuiltDay { get; set; }

        /// <summary>Shu katakdagi transport yuklamasi (transport birliklari).</summary>
        public float CurrentLoad { get; set; }
        /// <summary>Shu katakdagi piyodalar yuklamasi.</summary>
        public float PedestrianLoad { get; set; }

        public float CongestionLevel
        {
            get
            {
                float cap = Math.Max(1f, Capacity);
                return Math.Clamp(CurrentLoad / cap, 0f, 2f);
            }
        }

        /// <summary>Tirbandlikka ko'ra haqiqiy harakat narxi (marshrutlashda ishlatiladi).</summary>
        public float EffectiveMovementCost
        {
            get
            {
                float c = CongestionLevel;
                float slow = c <= 1f ? 1f + c * 0.55f : 1.55f + (c - 1f) * 1.6f;
                return MovementCost * slow / Math.Max(0.35f, Condition);
            }
        }

        public RoadSegment() { }

        public RoadSegment(int id, RoadType type, Point grid)
        {
            Id = id;
            Type = type;
            GridPosition = grid;
            Capacity = RoadTypeInfo.Capacity(type);
            MovementCost = RoadTypeInfo.MovementCost(type);
            MaintenanceCost = RoadTypeInfo.Maintenance(type);
            IsBridge = RoadTypeInfo.IsBridge(type);
        }

        public void SetType(RoadType type)
        {
            Type = type;
            Capacity = RoadTypeInfo.Capacity(type);
            MovementCost = RoadTypeInfo.MovementCost(type);
            MaintenanceCost = RoadTypeInfo.Maintenance(type);
            IsBridge = RoadTypeInfo.IsBridge(type);
        }

        /// <summary>Tirbandlik darajasi (PROMPT 18): LOW / MODERATE / HIGH / CONGESTED.</summary>
        public CongestionBand Band
        {
            get
            {
                float c = CongestionLevel;
                if (c < 0.35f) return CongestionBand.Low;
                if (c < 0.7f) return CongestionBand.Moderate;
                if (c < 1.0f) return CongestionBand.High;
                return CongestionBand.Congested;
            }
        }
    }

    public enum CongestionBand
    {
        Low = 0,
        Moderate = 1,
        High = 2,
        Congested = 3
    }

    public static class CongestionBandInfo
    {
        public static string NameUz(CongestionBand b)
        {
            switch (b)
            {
                case CongestionBand.Low: return "Bo'sh";
                case CongestionBand.Moderate: return "O'rtacha";
                case CongestionBand.High: return "Gavjum";
                default: return "Tirband";
            }
        }
    }

    public interface IDrawableInfrastructure
    {
        IReadOnlyList<int> GetConnectionIds();
    }
}
