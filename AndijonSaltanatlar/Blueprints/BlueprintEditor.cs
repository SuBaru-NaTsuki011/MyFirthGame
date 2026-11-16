using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityPlanning;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Blueprints
{
    /// <summary>
    /// Blueprint muharriri — PROMPT 15, 10-bo'lim. Har bir o'zgarishdan so'ng reja qayta
    /// tekshiriladi (narx, yo'l ulanishi, to'qnashuv). Preview hech qachon GameState'ni o'zgartirmaydi.
    /// </summary>
    public static class BlueprintEditor
    {
        /// <summary>
        /// Rejaga yangi bino qo'shadi. Joy geometriyasi, mavjud binolar va rejalashtirilgan yo'llar
        /// bilan to'qnashuv tekshiriladi; yaroqsiz bo'lsa o'zgarish qaytariladi.
        /// </summary>
        public static bool AddBuilding(CityPlan plan, BuildingType kind, Point origin, ICityPlanningGrid grid,
            Func<BuildingType, bool> isUnlocked, int treasury, int roadTileCost)
        {
            if (plan == null || grid == null) return false;
            if (grid.Width <= 0 || grid.Height <= 0) return false;

            Building proto = BuildingFactory.Create(kind, origin, 0);
            int size = Math.Max(1, proto.Size.X);
            var reserved = Reserved(plan);
            foreach (var r in plan.Roads) reserved.Add(r.Tile);
            if (!SmartBuildingPlacement.IsValidSpot(grid, origin, size, reserved)) return false;

            int nextId = 1;
            foreach (var b in plan.Buildings) if (b.Id >= nextId) nextId = b.Id + 1;
            var item = new BlueprintItem
            {
                Id = nextId,
                Kind = kind,
                Tile = origin,
                Size = size,
                Cost = proto.Cost,
                PopulationCapacity = proto.PopulationCapacity,
                BuildTimeDays = Math.Max(1, 1 + size / 2),
                BenefitUz = PlanCostEstimator.BenefitOf(kind),
                RequiredTech = BuildingCatalog.RequiredTech(kind),
                Status = BlueprintStatus.Buildable
            };
            plan.Buildings.Add(item);
            Revalidate(plan, grid, isUnlocked, treasury, roadTileCost);
            if (item.Status == BlueprintStatus.Blocked)
            {
                plan.Buildings.Remove(item);
                Revalidate(plan, grid, isUnlocked, treasury, roadTileCost);
                return false;
            }
            return true;
        }

        public static bool RemoveBuilding(CityPlan plan, int itemId, ICityPlanningGrid grid,
            Func<BuildingType, bool> isUnlocked, int treasury, int roadTileCost)
        {
            if (plan == null) return false;
            int index = plan.Buildings.FindIndex(b => b.Id == itemId);
            if (index < 0) return false;
            plan.Buildings.RemoveAt(index);
            Revalidate(plan, grid, isUnlocked, treasury, roadTileCost);
            return true;
        }

        /// <summary>Binoni yangi origin'ga ko'chiradi. Yangi joy yaroqsiz bo'lsa eski holat tiklanadi.</summary>
        public static bool MoveBuilding(CityPlan plan, int itemId, Point newOrigin, ICityPlanningGrid grid,
            Func<BuildingType, bool> isUnlocked, int treasury, int roadTileCost)
        {
            if (plan == null || grid == null) return false;
            BlueprintItem item = plan.Buildings.Find(b => b.Id == itemId);
            if (item == null) return false;
            if (item.Tile == newOrigin) return true;

            var reserved = ReservedExcept(plan, item);
            if (!SmartBuildingPlacement.IsValidSpot(grid, newOrigin, item.Size, reserved)) return false;

            Point old = item.Tile;
            item.Tile = newOrigin;
            Revalidate(plan, grid, isUnlocked, treasury, roadTileCost);
            if (item.Status == BlueprintStatus.Blocked || !plan.IsValid)
            {
                item.Tile = old;
                Revalidate(plan, grid, isUnlocked, treasury, roadTileCost);
                return false;
            }
            return true;
        }

        /// <summary>Yo'l katagini qo'shadi yoki olib tashlaydi; natija yaroqsiz bo'lsa o'zgarish qaytariladi.</summary>
        public static bool ToggleRoad(CityPlan plan, Point tile, ICityPlanningGrid grid,
            Func<BuildingType, bool> isUnlocked, int treasury, int roadTileCost)
        {
            if (plan == null || grid == null) return false;
            int index = plan.Roads.FindIndex(r => r.Tile == tile);
            if (index >= 0)
            {
                plan.Roads.RemoveAt(index);
                Revalidate(plan, grid, isUnlocked, treasury, roadTileCost);
                if (!plan.IsValid)
                {
                    plan.Roads.Insert(index, new BlueprintRoad { Tile = tile, Cost = roadTileCost });
                    Revalidate(plan, grid, isUnlocked, treasury, roadTileCost);
                    return false;
                }
                return true;
            }

            if (plan.Roads.Exists(r => r.Tile == tile)) return false;
            plan.Roads.Add(new BlueprintRoad { Tile = tile, Cost = roadTileCost });
            Revalidate(plan, grid, isUnlocked, treasury, roadTileCost);
            if (!plan.IsValid)
            {
                plan.Roads.RemoveAll(r => r.Tile == tile);
                Revalidate(plan, grid, isUnlocked, treasury, roadTileCost);
                return false;
            }
            return true;
        }

        public static void Revalidate(CityPlan plan, ICityPlanningGrid grid,
            Func<BuildingType, bool> isUnlocked, int treasury, int roadTileCost)
        {
            if (plan == null || grid == null) return;
            PlanValidator.Validate(plan, grid, treasury, isUnlocked, roadTileCost);
        }

        private static HashSet<Point> Reserved(CityPlan plan)
        {
            var reserved = new HashSet<Point>();
            foreach (var b in plan.Buildings)
                for (int y = b.Tile.Y; y < b.Tile.Y + b.Size; y++)
                    for (int x = b.Tile.X; x < b.Tile.X + b.Size; x++)
                        reserved.Add(new Point(x, y));
            return reserved;
        }

        private static HashSet<Point> ReservedExcept(CityPlan plan, BlueprintItem except)
        {
            var reserved = new HashSet<Point>();
            foreach (var b in plan.Buildings)
            {
                if (ReferenceEquals(b, except)) continue;
                for (int y = b.Tile.Y; y < b.Tile.Y + b.Size; y++)
                    for (int x = b.Tile.X; x < b.Tile.X + b.Size; x++)
                        reserved.Add(new Point(x, y));
            }
            return reserved;
        }
    }
}
