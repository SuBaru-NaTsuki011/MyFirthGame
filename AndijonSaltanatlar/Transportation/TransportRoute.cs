using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Transportation
{
    /// <summary>Bitta transport marshruti: yo'l grafi bo'yicha hisoblangan kataklar zanjiri.</summary>
    public sealed class TransportRoute
    {
        public int Id { get; set; }
        public int TransportId { get; set; }
        public List<Point> Tiles { get; set; } = new List<Point>();
        /// <summary>Dunyo koordinatalaridagi nuqtalar (tile markazlari).</summary>
        public List<Vector2> Waypoints { get; set; } = new List<Vector2>();
        public float Distance { get; set; }
        public int GraphVersion { get; set; }
        public Point From { get; set; }
        public Point To { get; set; }
        public bool IsValid => Tiles != null && Tiles.Count > 1;

        public TransportRoute() { }

        public TransportRoute(int id, int transportId, Point from, Point to, List<Point> tiles, int graphVersion)
        {
            Id = id;
            TransportId = transportId;
            From = from;
            To = to;
            GraphVersion = graphVersion;
            Tiles = tiles ?? new List<Point>();
            Distance = Math.Max(1, Tiles.Count - 1);
            foreach (Point p in Tiles)
                Waypoints.Add(TileCenter(p));
        }

        /// <summary>Katak markazining dunyo koordinatasi (sprite anchor nuqtasi).</summary>
        public static Vector2 TileCenter(Point p)
            => IsoMath.TileToWorld(p.X + 0.5f, p.Y + 0.5f) + new Vector2(0f, GameConfig.HalfTileH);
    }
}
