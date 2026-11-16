using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AndijonSaltanatlar.World;

namespace AndijonSaltanatlar.RoadNetwork
{
    /// <summary>
    /// Yo'l kataklaridan grafi quradi. Tugun Id = katak kaliti + 1 (barqaror va takrorlanmas).
    /// Shahar maydonlari (Plaza) ham piyoda o'tish mumkin bo'lgan tugun sifatida qo'shiladi.
    /// Graf faqat yo'l tarmog'i o'zgarganda quriladi — har kadrda emas.
    /// </summary>
    public sealed class RoadGraphBuilder
    {
        /// <summary>Maydon (Plaza) ustidagi harakat narxi.</summary>
        public const float PlazaCost = 0.9f;

        private static readonly Point[] Dirs =
        {
            new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0)
        };

        /// <summary>Grafni to'liq qayta quradi. Qaytaradi: qo'shilgan tugunlar soni.</summary>
        public int Rebuild(RoadGraph graph, IEnumerable<RoadSegment> segments, MapManager map)
        {
            if (graph == null) return 0;
            graph.Clear();
            if (map == null) return 0;

            int width = map.Width;
            var byTile = new Dictionary<long, RoadSegment>();

            foreach (RoadSegment s in segments)
            {
                if (s == null) continue;
                long key = (long)s.GridPosition.Y * width + s.GridPosition.X;
                byTile[key] = s;
                int nodeId = (int)(key + 1);
                graph.TryAddNode(nodeId, s.Type, s.GridPosition, s.Capacity, width);
            }

            // Maydonlar: yo'l bo'lmasa ham piyoda harakati uchun tugun.
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.GetTile(x, y) != TileType.Plaza) continue;
                    long key = (long)y * width + x;
                    if (byTile.ContainsKey(key)) continue;
                    graph.TryAddNode((int)(key + 1), RoadType.MainStreet, new Point(x, y), 20f, width);
                }

            int added = 0;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    long key = (long)y * width + x;
                    bool hasSegment = byTile.TryGetValue(key, out RoadSegment seg);
                    bool plaza = map.GetTile(x, y) == TileType.Plaza;
                    if (!hasSegment && !plaza) continue;

                    int a = (int)(key + 1);
                    foreach (Point d in Dirs)
                    {
                        int nx = x + d.X, ny = y + d.Y;
                        if (!map.InBounds(nx, ny)) continue;
                        long nkey = (long)ny * width + nx;
                        bool nSegment = byTile.TryGetValue(nkey, out RoadSegment nseg);
                        bool nPlaza = map.GetTile(nx, ny) == TileType.Plaza;
                        if (!nSegment && !nPlaza) continue;

                        int b = (int)(nkey + 1);
                        float cost = SegmentCost(seg, hasSegment) + SegmentCost(nseg, nSegment);
                        cost *= 0.5f;
                        if (graph.TryAddEdge(a, b, cost, out int edgeId))
                        {
                            added++;
                            if (graph.TryGetEdge(edgeId, out RoadEdge edge) && hasSegment && nSegment &&
                                (seg.IsBlocked || nseg.IsBlocked))
                                edge.Blocked = true;
                        }
                    }
                }
            graph.Bump();
            return added;
        }

        private static float SegmentCost(RoadSegment s, bool exists)
            => exists && s != null ? Math.Max(0.15f, s.MovementCost) : PlazaCost;
    }
}
