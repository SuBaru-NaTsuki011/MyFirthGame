using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// Tile asosidagi A* yo'l topuvchi. Yurish mumkin bo'lgan tile'lar — MapManager.IsWalkable
    /// (ko'cha, yer, maydon, yashillik; suv va binolar to'siq, darvoza ochiq). 4 yo'nalishli.
    /// .NET 8 ning PriorityQueue'sidan foydalanadi.
    /// </summary>
    public static class Pathfinder
    {
        private static readonly Point[] Dirs =
        {
            new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1)
        };

        /// <summary>start dan goal gacha tile yo'li (ikkalasi ham kiritiladi) yoki null.</summary>
        public static List<Point> FindPath(MapManager map, Point start, Point goal, int maxExpansions = 20000)
        {
            if (!map.IsWalkable(start.X, start.Y) || !map.IsWalkable(goal.X, goal.Y)) return null;
            if (start == goal) return new List<Point> { start };

            var open = new PriorityQueue<Point, int>();
            var cameFrom = new Dictionary<Point, Point>();
            var gScore = new Dictionary<Point, int> { [start] = 0 };
            var closed = new HashSet<Point>();

            open.Enqueue(start, Heuristic(start, goal));

            int expansions = 0;
            while (open.Count > 0 && expansions++ < maxExpansions)
            {
                Point current = open.Dequeue();
                if (current == goal) return Reconstruct(cameFrom, current);
                if (!closed.Add(current)) continue;

                int cg = gScore[current];
                foreach (Point dir in Dirs)
                {
                    var next = new Point(current.X + dir.X, current.Y + dir.Y);
                    if (!map.IsWalkable(next.X, next.Y) || closed.Contains(next)) continue;

                    int tentative = cg + 1;
                    if (!gScore.TryGetValue(next, out int known) || tentative < known)
                    {
                        gScore[next] = tentative;
                        cameFrom[next] = current;
                        open.Enqueue(next, tentative + Heuristic(next, goal));
                    }
                }
            }
            return null;
        }

        private static int Heuristic(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        private static List<Point> Reconstruct(Dictionary<Point, Point> cameFrom, Point current)
        {
            var path = new List<Point> { current };
            while (cameFrom.TryGetValue(current, out Point prev))
            {
                current = prev;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}
