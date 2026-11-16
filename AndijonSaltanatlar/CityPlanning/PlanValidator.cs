using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.SmartConstruction;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.CityPlanning
{
    /// <summary>
    /// Blueprint validatori — PROMPT 15, 7-10-bo'limlar. Har bir bino va yo'l qayta tekshiriladi:
    /// yer ochiqmi, o'yinchiga tegishlimi, terrain mosmi, to'qnashuv yo'qmi, texnologiya ochiqmi,
    /// muhofaza zonasi saqlanadimi va yo'l tarmog'i ulanganmi. GameState o'zgartirilmaydi.
    /// </summary>
    public static class PlanValidator
    {
        public static bool Validate(CityPlan plan, ICityPlanningGrid grid, int treasury,
            Func<BuildingType, bool> isUnlocked, int roadTileCost)
        {
            if (plan == null || grid == null) return false;

            var reserved = new HashSet<Point>();
            var roadSet = new HashSet<Point>();
            var plannedRoadTiles = new HashSet<Point>();
            foreach (var r in plan.Roads) plannedRoadTiles.Add(r.Tile);

            // 1) Binolar geometriyasi va texnologiya talablari.
            foreach (var b in plan.Buildings)
            {
                b.Status = BlueprintStatus.Buildable;
                b.StatusReasonUz = "";
                b.RequiredTech = BuildingCatalog.RequiredTech(b.Kind);
                b.RequirementUz = string.IsNullOrEmpty(b.RequiredTech) ? "Texnologiya talab qilinmaydi" : "Texnologiya: " + b.RequiredTech;
                b.BenefitUz = PlanCostEstimator.BenefitOf(b.Kind);

                if (b.Tile.X < 0 || b.Tile.Y < 0)
                {
                    b.Status = BlueprintStatus.Blocked;
                    b.StatusReasonUz = "Rejada joy belgilanmagan.";
                    continue;
                }
                if (isUnlocked != null && !isUnlocked(b.Kind))
                {
                    b.Status = BlueprintStatus.Blocked;
                    b.StatusReasonUz = "Texnologiya hali ochilmagan.";
                }

                bool geometryOk = true;
                for (int y = b.Tile.Y; y < b.Tile.Y + b.Size && geometryOk; y++)
                    for (int x = b.Tile.X; x < b.Tile.X + b.Size; x++)
                    {
                        var p = new Point(x, y);
                        if (!grid.InBounds(x, y) || !grid.IsOpenLand(x, y) ||
                            !grid.IsBuildableTerrain(x, y) || grid.IsBlockedForBuilding(x, y) ||
                            grid.IsProtected(x, y) || reserved.Contains(p) || plannedRoadTiles.Contains(p))
                        {
                            geometryOk = false;
                            break;
                        }
                    }
                if (!geometryOk)
                {
                    b.Status = BlueprintStatus.Blocked;
                    if (string.IsNullOrEmpty(b.StatusReasonUz))
                        b.StatusReasonUz = "Yer ochilmagan, band, yo'l bilan to'qnashadi yoki boshqa bino bilan ustma-ust tushadi.";
                    continue;
                }

                for (int y = b.Tile.Y; y < b.Tile.Y + b.Size; y++)
                    for (int x = b.Tile.X; x < b.Tile.X + b.Size; x++)
                        reserved.Add(new Point(x, y));
            }

            // 2) Yo'llar.
            var seenRoads = new HashSet<Point>();
            foreach (var r in plan.Roads)
            {
                r.Status = BlueprintStatus.Buildable;
                r.StatusReasonUz = "";
                var p = r.Tile;
                if (!grid.InBounds(p.X, p.Y) || !grid.IsOpenLand(p.X, p.Y) ||
                    !grid.IsBuildableTerrain(p.X, p.Y) || grid.IsProtected(p.X, p.Y) ||
                    grid.IsBlockedForRoad(p.X, p.Y) || reserved.Contains(p) || !seenRoads.Add(p))
                {
                    r.Status = BlueprintStatus.Blocked;
                    r.StatusReasonUz = "Yo'l katagi mos emas (band, suv, muhofaza zonasi yoki takror).";
                }
            }

            // 3) Tarmoq: taklif qilingan yo'llar mavjud yo'l tarmog'iga ulangan bo'lishi shart.
            foreach (var r in plan.Roads) if (r.Status != BlueprintStatus.Blocked) roadSet.Add(r.Tile);
            var disconnected = DisconnectedRoads(grid, roadSet);
            foreach (var r in plan.Roads)
                if (disconnected.Contains(r.Tile))
                {
                    r.Status = BlueprintStatus.Blocked;
                    r.StatusReasonUz = "Yo'l mavjud ko'cha tarmog'iga ulanmagan.";
                }

            roadSet.Clear();
            foreach (var r in plan.Roads) if (r.Status != BlueprintStatus.Blocked) roadSet.Add(r.Tile);

            // 4) Har bir bino yo'lga ulangan bo'lishi kerak — mavjud yoki rejalashtirilgan.
            foreach (var b in plan.Buildings)
            {
                if (b.Status == BlueprintStatus.Blocked) continue;
                bool connected = false;
                foreach (var p in SmartRoadPlanner.Perimeter(grid, b, reserved))
                    if (grid.IsRoad(p.X, p.Y) || roadSet.Contains(p)) { connected = true; break; }
                if (!connected)
                {
                    b.Status = BlueprintStatus.Blocked;
                    b.StatusReasonUz = "Bino yo'l tarmog'iga ulanmagan.";
                }
            }

            // 5) Narx va resurs holati (qurilish tartibi: avval yo'llar, keyin binolar).
            PlanCostEstimator.ApplyCosts(plan, roadTileCost);
            int cumulative = 0;
            foreach (var r in plan.Roads)
            {
                if (r.Status == BlueprintStatus.Blocked) continue;
                cumulative += r.Cost;
                if (cumulative > treasury) r.Status = BlueprintStatus.NeedsResource;
            }
            foreach (var b in plan.Buildings)
            {
                if (b.Status == BlueprintStatus.Blocked) continue;
                cumulative += b.Cost;
                if (cumulative > treasury)
                {
                    b.Status = BlueprintStatus.NeedsResource;
                    b.StatusReasonUz = "Xazina hozircha yetmaydi — reja qabul qilinishi mumkin, qurilish pauzaga tushadi.";
                }
            }

            // 6) Yakuniy yaroqlilik.
            int validBuildings = 0;
            foreach (var b in plan.Buildings)
            {
                if (b.Status == BlueprintStatus.Blocked) continue;
                validBuildings++;
            }
            bool blockedRoad = false;
            foreach (var r in plan.Roads) if (r.Status == BlueprintStatus.Blocked) blockedRoad = true;
            plan.IsValid = validBuildings > 0 && !blockedRoad;
            plan.State = plan.IsValid ? PlanExecState.ReadyForApproval : PlanExecState.Draft;
            PlanCostEstimator.ApplyCosts(plan, roadTileCost);
            return plan.IsValid;
        }

        /// <summary>Mavjud yo'lni umuman o'z ichiga olmaydigan komponentdagi reja yo'llarini topadi.</summary>
        private static HashSet<Point> DisconnectedRoads(ICityPlanningGrid grid, HashSet<Point> planned)
        {
            var bad = new HashSet<Point>();
            var visited = new HashSet<Point>();
            foreach (var start in planned)
            {
                if (visited.Contains(start)) continue;
                var component = new List<Point>();
                var q = new Queue<Point>();
                bool touchesExisting = false;
                q.Enqueue(start);
                visited.Add(start);
                while (q.Count > 0)
                {
                    Point cur = q.Dequeue();
                    component.Add(cur);
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = cur.X + (i == 0 ? 1 : i == 1 ? -1 : 0);
                        int ny = cur.Y + (i == 2 ? 1 : i == 3 ? -1 : 0);
                        var n = new Point(nx, ny);
                        if (!grid.InBounds(nx, ny)) continue;
                        if (grid.IsRoad(nx, ny)) { touchesExisting = true; continue; }
                        if (planned.Contains(n) && visited.Add(n)) q.Enqueue(n);
                    }
                }
                if (!touchesExisting) foreach (var p in component) bad.Add(p);
            }
            return bad;
        }
    }
}
