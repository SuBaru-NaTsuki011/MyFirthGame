using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AndijonSaltanatlar.Economy;

namespace AndijonSaltanatlar.Transportation
{
    /// <summary>
    /// Shahar ichidagi tarixiy transport vositasi. Marshrut yo'l grafi bo'yicha quriladi;
    /// transport binolarning ichidan o'tmaydi — faqat yo'l kataklari bo'ylab yuradi.
    /// </summary>
    public sealed class TransportEntity
    {
        public int Id { get; set; }
        public TransportType Type { get; set; }
        /// <summary>Dunyo koordinatasi (piksel). Anchor — g'ildirak yerga tegadigan nuqta.</summary>
        public Vector2 Position { get; set; }
        public Vector2 Destination { get; set; }
        public int? AssignedRouteId { get; set; }
        public float CurrentSpeed { get; set; }
        public float MaxSpeed { get; set; }
        /// <summary>Yuk miqdori (birlik).</summary>
        public float Cargo { get; set; }
        public float CargoCapacity { get; set; }
        public ResourceType CargoType { get; set; } = ResourceType.Food;
        /// <summary>Yuk tashlanadigan yetkazib berish vazifasi (0 = yo'q).</summary>
        public int DeliveryJobId { get; set; }
        public TransportAnimationState AnimationState { get; set; }
        public int OwnerId { get; set; }
        public bool IsBlocked { get; set; }
        public float FacingAngle { get; set; }
        public float WaitTimer { get; set; }
        public string LabelUz { get; set; } = "";

        // --- Marshrut ---
        public List<Point> RouteTiles { get; set; } = new List<Point>();
        public int RouteIndex { get; set; }
        /// <summary>Shu transport hozir turgan yo'l katagi.</summary>
        public Point CurrentTile { get; set; }
        /// <summary>Manba va manzil kataklari (yangi marshrut / qayta hisoblash uchun).</summary>
        public Point RouteFrom { get; set; }
        public Point RouteTo { get; set; }
        /// <summary>Marshrut oxiriga yetganda bir marta ko'tariladigan hodisa uchun belgi.</summary>
        public bool Arrived { get; set; }

        // --- Animatsiya ---
        public float WheelRotation { get; set; }
        public float BobPhase { get; set; }
        public float LoadTimer { get; set; }

        public TransportEntity() { }

        public TransportEntity(int id, TransportType type, Vector2 position, float maxSpeed = 0f)
        {
            Id = id;
            Type = type;
            Position = position;
            MaxSpeed = maxSpeed > 0 ? maxSpeed : 1f;
            CurrentSpeed = 0f;
            Cargo = 0f;
            CargoCapacity = type switch
            {
                TransportType.Wagon => 6f,
                TransportType.Caravan => 10f,
                TransportType.Mule => 3f,
                TransportType.CitizenCarrier => 1f,
                _ => 4f
            };
            AnimationState = TransportAnimationState.Idle;
            LabelUz = DefaultLabel(type);
            CurrentTile = new Point((int)position.X, (int)position.Y);
        }

        public static string DefaultLabel(TransportType t)
        {
            switch (t)
            {
                case TransportType.Wagon: return "Yuk aravasi";
                case TransportType.Caravan: return "Savdo karvoni";
                case TransportType.Mule: return "Otli xabarchi";
                case TransportType.CitizenCarrier: return "Yuk tashuvchi fuqaro";
                default: return "Transport";
            }
        }

        public bool CanAcceptCargo(float amount) => Cargo + amount <= CargoCapacity + 0.001f;

        public bool HasRoute => RouteTiles != null && RouteTiles.Count > 1 && RouteIndex < RouteTiles.Count;

        /// <summary>Keyingi marshrut nuqtasi (tile).</summary>
        public Point? NextTile
            => HasRoute ? RouteTiles[Math.Min(RouteIndex, RouteTiles.Count - 1)] : (Point?)null;

        public void ClearRoute()
        {
            RouteTiles.Clear();
            RouteIndex = 0;
            Arrived = false;
            AnimationState = TransportAnimationState.Idle;
        }
    }

    public enum TransportType
    {
        None = 0,
        Wagon = 1,
        Caravan = 2,
        Mule = 3,
        CitizenCarrier = 4,
        Boat = 10
    }

    public enum TransportAnimationState
    {
        Idle = 0,
        Moving = 1,
        Loading = 2,
        Unloading = 3,
        Dock = 4,
        Turn = 5
    }
}
