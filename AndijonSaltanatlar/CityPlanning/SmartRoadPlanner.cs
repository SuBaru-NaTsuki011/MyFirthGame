using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.CityPlanning
{
    /// <summary>Yo'l rejasining natijasi — yo'llar, ulanish holati va xato sababi.</summary>
    public sealed class RoadPlanResult
    {
        public List<Point> Tiles { get; set; } = new();
        public bool ConnectedToExistingNetwork { get; set; }
        public int UnconnectedBuildings { get; set; }
        public string ErrorUz { get; set; } = "";
        public int Cost { get; set; }
    }

    /// <summary>
    /// Yo'l rejalashtiruvchi — PROMPT 15, 8-bo'lim. Yangi mahallani mavjud yo'l tarmog'iga ulaydi:
    /// A*/uniform-cost graph search orqali eng qisqa yo'l topiladi, reja binolari va suvdan o'tmaydi,
    /// muhofaza hududlari chetlab o'tiladi. Blueprint faqat preview — xarita o'zgarmaydi.
    /// </summary>
    public static class SmartRoadPlanner
    {
        private static readonly int[] Dx = { 1, -1, 0, 0 };
        private static readonly int[] Dy = { 0, 0, 1, -1 };

        /// <summary>
        /// Anchor'dan eng yaqin mavjud yo'lga trunk yo'l, so'ng har bir bino uchun yo'lga ulanish
        /// tarmog'ini rejalashtiradi. Bino uchun yo'l topilmasa UnconnectedBuildings oshadi.
        /// </summary>
        public static RoadPlanResult PlanRoads(ICityPlanningGrid grid, Point anchor,
            IReadOnlyList<BlueprintItem> buildings, HashSet<Point> reserved, int roadTileCost,
            int maxExpansionsPerSearch = 6000)
        {
            var result = new RoadPlanResult();
            var planned = new HashSet<Point>();

            // 1) Trunk: anchor'dan mavjud yo'lga.
            if (!grid.IsRoad(anchor.X, anchor.Y))
            {
                List<Point> trunk = FindPath(grid, new List<Point> { anchor }, reserved, planned,
                    p => grid.IsRoad(p.X, p.Y), maxExpansionsPerSearch, out bool foundRoad);
                if (trunk != null && foundRoad && trunk.Count > 0)
                {
                    foreach (var p in trunk) planned.Add(p);
                    result.ConnectedToExistingNetwork = true;
                }
                else
                {
                    result.ErrorUz = "Yangi hududdan mavjud yo'lga xavfsiz yo'l topilmadi.";
                }
            }
            else
            {
                result.ConnectedToExistingNetwork = true;
            }

            // 2) Har bir bino uchun yo'lga ulanish.
            if (buildings != null)
            {
                foreach (var b in buildings)
                {
                    if (b.Status == BlueprintStatus.Blocked) continue;
                    List<Point> perimeter = Perimeter(grid, b, reserved);
                    bool connected = false;
                    foreach (var p in perimeter)
                        if (grid.IsRoad(p.X, p.Y) || planned.Contains(p)) { connected = true; break; }
                    if (connected) continue;

                    List<Point> path = FindPath(grid, perimeter, reserved, planned,
                        p => grid.IsRoad(p.X, p.Y) || planned.Contains(p),
                        maxExpansionsPerSearch, out bool found);
                    if (path != null && found && path.Count > 0)
                    {
                        foreach (var p in path) planned.Add(p);
                        result.ConnectedToExistingNetwork = true;
                    }
                    else
                    {
                        result.UnconnectedBuildings++;
                    }
                }
            }

            if (result.UnconnectedBuildings > 0)
                result.ErrorUz = result.UnconnectedBuildings + " bino yo'l tarmog'iga ulanmadi.";

            foreach (var p in planned) result.Tiles.Add(p);
            result.Cost = result.Tiles.Count * roadTileCost;
            return result;
        }

        /// <summary>Bitta anchor uchun eng yaqin mavjud yo'lga yo'l (eski API saqlangan).</summary>
        public static List<Point> ConnectToRoad(ICityPlanningGrid grid, Point anchor, HashSet<Point> reserved,
            int maxExpansions = 4000)
        {
            var planned = new HashSet<Point>();
            List<Point> path = FindPath(grid, new List<Point> { anchor }, reserved, planned,
                p => grid.IsRoad(p.X, p.Y), maxExpansions, out _);
            return path ?? new List<Point>();
        }

        /// <summary>Bino atrofidagi (izdan tashqari) quriladigan yo'l kataklari.</summary>
        public static List<Point> Perimeter(ICityPlanningGrid grid, BlueprintItem b, HashSet<Point> reserved)
        {
            var list = new List<Point>();
            int x0 = b.Tile.X - 1, y0 = b.Tile.Y - 1;
            int x1 = b.Tile.X + b.Size, y1 = b.Tile.Y + b.Size;
            for (int x = x0; x <= x1; x++)
            {
                AddRoadCandidate(grid, list, reserved, new Point(x, y0));
                AddRoadCandidate(grid, list, reserved, new Point(x, y1));
            }
            for (int y = y0 + 1; y < y1; y++)
            {
                AddRoadCandidate(grid, list, reserved, new Point(x0, y));
                AddRoadCandidate(grid, list, reserved, new Point(x1, y));
            }
            return list;
        }

        private static void AddRoadCandidate(ICityPlanningGrid grid, List<Point> list, HashSet<Point> reserved, Point p)
        {
            if (!grid.InBounds(p.X, p.Y)) return;
            if (reserved != null && reserved.Contains(p)) return;
            if (grid.IsBlockedForRoad(p.X, p.Y)) return;
            if (!grid.IsOpenLand(p.X, p.Y)) return;
            list.Add(p);
        }

        /// <summary>
        /// Uniform-cost graph search (A* g + Manhattan evristika; to'siqlar tufayli evristika
        /// qisqartirilgan). Manba bir yoki bir nechta katak; maqsad predikat bilan aniqlanadi.
        /// Qaytgan yo'l manba va maqsadni o'z ichiga olmaydi (faqat yangi quriladigan kataklar).
        /// </summary>
        private static List<Point> FindPath(ICityPlanningGrid grid, List<Point> sources, HashSet<Point> reserved,
            HashSet<Point> planned, Func<Point, bool> isGoal, int maxExpansions, out bool reachedGoal)
        {
            reachedGoal = false;
            var open = new PriorityQueue<Point, int>();
            var g = new Dictionary<Point, int>();
            var came = new Dictionary<Point, Point>();
            var closed = new HashSet<Point>();

            foreach (var s in sources)
            {
                if (!grid.InBounds(s.X, s.Y)) continue;
                if (isGoal(s)) { reachedGoal = true; return new List<Point>(); }
                if (!Passable(grid, s, reserved, planned, sources)) continue;
                g[s] = 0;
                came[s] = s;
                open.Enqueue(s, 0);
            }

            int expansions = 0;
            Point hit = default;
            while (open.Count > 0 && expansions++ < maxExpansions)
            {
                Point cur = open.Dequeue();
                if (!closed.Add(cur)) continue;
                if (isGoal(cur)) { hit = cur; reachedGoal = true; break; }

                int cg = g[cur];
                for (int i = 0; i < 4; i++)
                {
                    var n = new Point(cur.X + Dx[i], cur.Y + Dy[i]);
                    if (closed.Contains(n) || !grid.InBounds(n.X, n.Y)) continue;
                    if (!Passable(grid, n, reserved, planned, sources)) continue;
                    int ng = cg + 1;
                    if (g.TryGetValue(n, out int old) && old <= ng) continue;
                    g[n] = ng;
                    came[n] = cur;
                    // Maqsadga Manhattan masofasi — evristika (to'siqlar bilan ham admissible).
                    int h = isGoal(n) ? 0 : 1;
                    open.Enqueue(n, ng + h);
                }
            }

            if (!reachedGoal) return null;

            var path = new List<Point>();
            Point node = hit;
            while (true)
            {
                if (!grid.IsRoad(node.X, node.Y) && !planned.Contains(node)) path.Add(node);
                if (!came.TryGetValue(node, out Point prev) || prev == node) break;
                node = prev;
            }
            path.Reverse();
            return path;
        }

        private static bool Passable(ICityPlanningGrid grid, Point p, HashSet<Point> reserved,
            HashSet<Point> planned, List<Point> sources)
        {
            if (!grid.IsOpenLand(p.X, p.Y)) return false;
            if (reserved != null && reserved.Contains(p)) return false;
            if (grid.IsProtected(p.X, p.Y) && !grid.IsRoad(p.X, p.Y)) return false;
            if (grid.IsBlockedForRoad(p.X, p.Y) && !grid.IsRoad(p.X, p.Y) && !planned.Contains(p)) return false;
            if (planned.Contains(p)) return true;
            // Manba katagi band bo'lishi mumkin emas (anchor bo'sh bo'lishi kerak).
            return true;
        }
    }
}
