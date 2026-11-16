using System.Collections.Generic;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Units
{
    /// <summary>
    /// Patrul askarlarining qo'lda belgilangan yo'nalish nuqtalarini A* yo'li bo'ylab
    /// zich tile yo'liga aylantiradi — shunda ular devor va binolardan o'tib ketmasdan,
    /// ko'chalar bo'ylab yuradi. Yo'l topilmasa, asl nuqta saqlanadi (xavfsiz zaxira).
    ///
    /// Shahar generatsiyasidan keyin bir marta chaqiriladi.
    /// </summary>
    public static class PatrolController
    {
        public static void RouteAll(MapManager map, UnitManager units)
        {
            foreach (Unit u in units.Units)
                if (u is PatrolUnit pu && pu.PatrolPoints.Count >= 2)
                    RouteUnit(map, pu);
        }

        private static void RouteUnit(MapManager map, PatrolUnit pu)
        {
            var src = new List<Vector2>(pu.PatrolPoints);
            // Yopiq halqa qilamiz: oxirgi nuqtadan birinchisiga qaytish ham yo'l bo'yicha bo'lsin.
            var cycle = new List<Vector2>(src) { src[0] };

            var dense = new List<Vector2>();
            for (int i = 0; i + 1 < cycle.Count; i++)
            {
                List<Point> path = Pathfinder.FindPath(map, ToTile(cycle[i]), ToTile(cycle[i + 1]));
                if (path == null || path.Count < 1)
                {
                    dense.Add(cycle[i]);        // yo'l topilmadi — asl nuqtani qoldiramiz
                    continue;
                }
                // Oxirgi tugundan tashqari barcha tugunlarni qo'shamiz (keyingi segment uni qaytaradi).
                for (int k = 0; k < path.Count - 1; k++)
                    dense.Add(Center(path[k]));
            }

            if (dense.Count >= 2)
            {
                pu.PatrolPoints.Clear();
                pu.PatrolPoints.AddRange(dense);
                pu.PauseTime = 0f; // zich yo'lda har katakda to'xtamasin — silliq yursin
            }
        }

        private static Point ToTile(Vector2 v) => new Point((int)v.X, (int)v.Y);
        private static Vector2 Center(Point p) => new Vector2(p.X + 0.5f, p.Y + 0.5f);
    }
}
