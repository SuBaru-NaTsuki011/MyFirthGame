using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.CityPlanning
{
    /// <summary>
    /// Bino uchun mos joy tanlash — PROMPT 15, 7-bo'lim. Tasodifiy emas: yer ochilgan, egallanmagan,
    /// terrain mos, boshqa reja binolari bilan to'qnashmaydigan va muhofaza zonasi saqlanadigan
    /// kataklar baholanadi. Nomzodlar Accessibility / Efficiency / Compatibility / Expansion /
    /// VisualHarmony / Cost mezonlari bo'yicha solishtiriladi.
    /// </summary>
    public static class SmartBuildingPlacement
    {
        public const int DefaultSearchRadius = 18;

        /// <summary>
        /// Anchor atrofidan halqalar bo'ylab qidirib, eng yuqori ballli yaroqli origin'ni qaytaradi.
        /// Qidiruv maydoni cheklangan (performance — PROMPT 15, 22-bo'lim).
        /// </summary>
        public static Point? FindSpot(ICityPlanningGrid grid, Point anchor, int size, HashSet<Point> reserved,
            int maxRadius = DefaultSearchRadius, int maxCandidates = 260)
        {
            var candidates = FindCandidates(grid, anchor, size, reserved, maxRadius, maxCandidates, 1);
            return candidates.Count > 0 ? candidates[0].Origin : (Point?)null;
        }

        /// <summary>Eng yaxshi N nomzadni qaytaradi (solishtirish/tashxis uchun).</summary>
        public static List<PlacementCandidate> FindCandidates(ICityPlanningGrid grid, Point anchor, int size,
            HashSet<Point> reserved, int maxRadius = DefaultSearchRadius, int maxCandidates = 260, int topN = 5)
        {
            var best = new List<PlacementCandidate>();
            int inspected = 0;
            for (int r = 0; r <= maxRadius && inspected < maxCandidates; r++)
                for (int dy = -r; dy <= r && inspected < maxCandidates; dy++)
                    for (int dx = -r; dx <= r && inspected < maxCandidates; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                        var origin = new Point(anchor.X + dx, anchor.Y + dy);
                        inspected++;
                        if (!IsValidSpot(grid, origin, size, reserved)) continue;

                        var c = Evaluate(grid, origin, size, reserved);
                        if (!c.IsValid) continue;
                        InsertSorted(best, c, topN);
                    }
            return best;
        }

        /// <summary>Nomzodni barcha mezonlar bo'yicha baholaydi.</summary>
        public static PlacementCandidate Evaluate(ICityPlanningGrid grid, Point origin, int size, HashSet<Point> reserved)
        {
            var c = new PlacementCandidate { Origin = origin, Size = size, IsValid = true };
            if (!IsValidSpot(grid, origin, size, reserved))
            {
                c.IsValid = false;
                c.RejectReasonUz = "Joy ochiq emas yoki band.";
                return c;
            }

            int roadDistance = DistanceToNearestRoad(grid, origin, size, 14);
            c.AccessibilityScore = PlacementCandidate.Clamp(100 - roadDistance * 11);

            // Anchor markazidan uzoqlik va ixchamlik: yaqin joylar birinchi.
            int openAround = CountOpenAround(grid, origin, size, 4);
            c.EfficiencyScore = PlacementCandidate.Clamp(45 + openAround * 2 - roadDistance * 2);

            // Qo'shnichilik: atrofda bo'sh joy ko'p bo'lsa yangi mahalla uyg'un joylashadi.
            int blockedAround = CountBlockedAround(grid, origin, size, 3);
            c.NeighborhoodCompatibilityScore = PlacementCandidate.Clamp(85 - blockedAround * 9);

            // Kelajakda kengayish: 5 katak radiusda ochiq maydon.
            c.ExpansionPotentialScore = PlacementCandidate.Clamp(openAround * 12);

            // Vizual uyg'unlik: yo'lga yaqin va muhofaza zonasidan uzoq.
            c.VisualHarmonyScore = PlacementCandidate.Clamp(70 - roadDistance * 3 + (openAround > 12 ? 12 : 0));

            // Qurilish narxi: yo'l ulanishining qisqaligi qurilish xarajatini kamaytiradi.
            c.ConstructionCostScore = PlacementCandidate.Clamp(100 - roadDistance * 10);

            c.TotalScore = (int)MathF.Round(
                c.AccessibilityScore * 0.24f +
                c.EfficiencyScore * 0.18f +
                c.NeighborhoodCompatibilityScore * 0.20f +
                c.ExpansionPotentialScore * 0.16f +
                c.VisualHarmonyScore * 0.10f +
                c.ConstructionCostScore * 0.12f);
            return c;
        }

        /// <summary>
        /// Yer ochilgan, o'yinchiga tegishli, terrain mos, band emas, muhofaza qilinmagan va
        /// blueprint binolari bilan to'qnashmaydi.
        /// </summary>
        public static bool IsValidSpot(ICityPlanningGrid grid, Point origin, int size, HashSet<Point> reserved)
        {
            if (size <= 0) return false;
            for (int y = origin.Y; y < origin.Y + size; y++)
                for (int x = origin.X; x < origin.X + size; x++)
                {
                    if (!grid.InBounds(x, y)) return false;
                    var p = new Point(x, y);
                    if (reserved != null && reserved.Contains(p)) return false;
                    if (!grid.IsOpenLand(x, y)) return false;
                    if (!grid.IsBuildableTerrain(x, y)) return false;
                    if (grid.IsBlockedForBuilding(x, y)) return false;
                    if (grid.IsProtected(x, y)) return false;
                }
            return true;
        }

        /// <summary>Reja uchun mahalla anchor nomzodlarini ochiq maydon va yo'l yaqinligi bo'yicha saralaydi.</summary>
        public static List<Point> FindDistrictAnchors(ICityPlanningGrid grid, HashSet<Point> reserved, int count = 4)
        {
            var scored = new List<(Point p, int score)>();
            int cx = grid.Width / 2, cy = grid.Height / 2;
            for (int y = 2; y < grid.Height - 2; y += 2)
                for (int x = 2; x < grid.Width - 2; x += 2)
                {
                    if (!IsValidSpot(grid, new Point(x, y), 2, reserved)) continue;
                    int open = CountOpenAround(grid, new Point(x, y), 2, 5);
                    if (open < 8) continue;
                    int roadDistance = DistanceToNearestRoad(grid, new Point(x, y), 2, 20);
                    int centerPenalty = (Math.Abs(x - cx) + Math.Abs(y - cy)) / 6;
                    int score = open * 4 - roadDistance * 2 - centerPenalty;
                    scored.Add((new Point(x, y), score));
                }

            scored.Sort((a, b) => b.score.CompareTo(a.score));
            var result = new List<Point>();
            foreach (var s in scored)
            {
                bool farEnough = true;
                foreach (var r in result)
                    if (Math.Abs(r.X - s.p.X) + Math.Abs(r.Y - s.p.Y) < 9) { farEnough = false; break; }
                if (!farEnough) continue;
                result.Add(s.p);
                if (result.Count >= count) break;
            }
            return result;
        }

        /// <summary>
        /// Mavjud mahalla ichidagi bo'shliqlar uchun anchorlar — yaqin atrofda uy/bozor va mavjud ko'cha
        /// bo'lgan joylar ustuvor. Yangi chekka hudud ochish o'rniga infill rejalashtirish uchun.
        /// </summary>
        public static List<Point> FindInfillAnchors(ICityPlanningGrid grid, int count = 5)
        {
            var scored = new List<(Point p, int score)>();
            for (int y = 1; y < grid.Height - 2; y++)
                for (int x = 1; x < grid.Width - 2; x++)
                {
                    var origin = new Point(x, y);
                    if (!IsValidSpot(grid, origin, 2, null)) continue;

                    int roadDistance = DistanceToNearestRoad(grid, origin, 2, 8);
                    int neighbours = CountBuildingsAround(grid, origin, 2, 5);
                    if (roadDistance > 4 || neighbours == 0) continue;

                    int open = CountOpenAround(grid, origin, 2, 3);
                    int score = neighbours * 9 + open * 2 - roadDistance * 6;
                    scored.Add((origin, score));
                }

            scored.Sort((a, b) => b.score.CompareTo(a.score));
            var result = new List<Point>();
            foreach (var s in scored)
            {
                bool farEnough = true;
                foreach (var r in result)
                    if (Math.Abs(r.X - s.p.X) + Math.Abs(r.Y - s.p.Y) < 7) { farEnough = false; break; }
                if (!farEnough) continue;
                result.Add(s.p);
                if (result.Count >= count) break;
            }
            return result;
        }

        private static int CountBuildingsAround(ICityPlanningGrid grid, Point origin, int size, int radius)
        {
            int cx = origin.X + size / 2, cy = origin.Y + size / 2, n = 0;
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                    if (grid.InBounds(x, y) && grid.BuildingAt(x, y).HasValue) n++;
            return n;
        }

        /// <summary>Anchor binoning shimoliy chap burchagi; qidiruvni binoning o'rtasidan boshlaymiz.</summary>
        public static Point AnchorForSize(Point origin, int size)
            => new Point(origin.X + (size - 1) / 2, origin.Y + (size - 1) / 2);

        private static void InsertSorted(List<PlacementCandidate> list, PlacementCandidate c, int max)
        {
            int i = 0;
            while (i < list.Count && list[i].TotalScore >= c.TotalScore) i++;
            list.Insert(i, c);
            if (list.Count > max) list.RemoveAt(list.Count - 1);
        }

        private static int CountOpenAround(ICityPlanningGrid grid, Point origin, int size, int radius)
        {
            int cx = origin.X + size / 2, cy = origin.Y + size / 2, n = 0;
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                    if (grid.InBounds(x, y) && grid.IsOpenLand(x, y) && grid.IsBuildableTerrain(x, y) &&
                        !grid.IsBlockedForBuilding(x, y) && !grid.IsProtected(x, y)) n++;
            return n;
        }

        private static int CountBlockedAround(ICityPlanningGrid grid, Point origin, int size, int radius)
        {
            int cx = origin.X + size / 2, cy = origin.Y + size / 2, n = 0;
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (!grid.InBounds(x, y)) { n++; continue; }
                    if (grid.IsBlockedForBuilding(x, y) || !grid.IsOpenLand(x, y)) n++;
                }
            return n;
        }

        /// <summary>Eng yaqin mavjud yo'lgacha masofa (Manhattan halqalari, limit bilan).</summary>
        public static int DistanceToNearestRoad(ICityPlanningGrid grid, Point origin, int size, int limit)
        {
            int cx = origin.X + size / 2, cy = origin.Y + size / 2;
            for (int r = 0; r <= limit; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                        int x = cx + dx, y = cy + dy;
                        if (grid.InBounds(x, y) && grid.IsRoad(x, y)) return r;
                    }
            return limit;
        }
    }
}
