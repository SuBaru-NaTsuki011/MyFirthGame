using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Interiors
{
    /// <summary>Bino turiga mos ichki ko'rinish / ochiq hudud shabloni.</summary>
    public enum InteriorKind
    {
        House,
        Palace,
        Market,
        Farm,
        Garden,
        Caravanserai,
        Barracks,
        Arena,
        Madrasa,
        Mosque,
        Tower,
        Gate
    }

    public enum InteriorAction
    {
        None,
        Sit,
        Stand,
        Eat,
        Lie,
        OpenChest,
        Buy,
        Sell,
        Talk,
        Plant,
        Water,
        Harvest,
        Train,
        Lessons,
        Research,
        Library,
        Pray,
        Climb,
        Descend,
        PassGate,
        Inspect,
        Caravan,
        WallInfo
    }

    /// <summary>Ichki sahnadagi bitta interaktiv obyekt (mebel, do'kon, daraxt...).</summary>
    public sealed class InteriorHotspot
    {
        public string Id;
        public string Label;
        public Vector2 Tile;
        public int Size = 1;
        public float Radius = 1.75f;
        public InteriorAction Primary;
        public string PrimaryLabel = "";
        public InteriorAction Secondary;
        public string SecondaryLabel = "";
        /// <summary>Tugallangan obyekt (masalan, yig'ib olingan dala) — qo'shimcha holat matni.</summary>
        public string Status = "";
    }

    /// <summary>SaqLanadigan interyer holati — har bir qurilgan bino uchun alohida (BuildingId).</summary>
    public sealed class InteriorState
    {
        public int BuildingId;
        public int Bread = 2;
        public int ChestFood;
        public int ChestSeeds;
        public int ChestProduce;
        public int MarketStock = 40;
        public int MarketSales;
        public int MarketPurchases;
        public int GardenCare;
        public bool Visited;
        public bool LampOn = true;

        public InteriorState Clone() => (InteriorState)MemberwiseClone();
    }

    /// <summary>Save/Load uchun yassi yozuv (JSON).</summary>
    public sealed class SavedInterior
    {
        public int BuildingId { get; set; }
        public int Bread { get; set; } = 2;
        public int ChestFood { get; set; }
        public int ChestSeeds { get; set; }
        public int ChestProduce { get; set; }
        public int MarketStock { get; set; } = 40;
        public int MarketSales { get; set; }
        public int MarketPurchases { get; set; }
        public int GardenCare { get; set; }
        public bool Visited { get; set; }
        public bool LampOn { get; set; } = true;
    }

    /// <summary>
    /// Barcha interyer holatlarini BuildingId bo'yicha saqlaydi. Bino ko'chirilsa ham
    /// Id o'zgarmaydi — mebel, sandiq va savdo holati saqlanib qoladi.
    /// </summary>
    public static class InteriorStore
    {
        private static readonly Dictionary<int, InteriorState> States = new Dictionary<int, InteriorState>();

        public static InteriorState For(int buildingId)
        {
            if (!States.TryGetValue(buildingId, out InteriorState s))
            {
                s = new InteriorState { BuildingId = buildingId };
                States[buildingId] = s;
            }
            return s;
        }

        public static void Clear() => States.Clear();

        public static IEnumerable<InteriorState> All => States.Values;

        public static List<SavedInterior> Snapshot()
        {
            var list = new List<SavedInterior>();
            foreach (InteriorState s in States.Values)
                list.Add(new SavedInterior
                {
                    BuildingId = s.BuildingId,
                    Bread = s.Bread,
                    ChestFood = s.ChestFood,
                    ChestSeeds = s.ChestSeeds,
                    ChestProduce = s.ChestProduce,
                    MarketStock = s.MarketStock,
                    MarketSales = s.MarketSales,
                    MarketPurchases = s.MarketPurchases,
                    GardenCare = s.GardenCare,
                    Visited = s.Visited,
                    LampOn = s.LampOn
                });
            return list;
        }

        public static void Restore(List<SavedInterior> saved)
        {
            States.Clear();
            if (saved == null) return;
            foreach (SavedInterior d in saved)
            {
                if (d == null) continue;
                States[d.BuildingId] = new InteriorState
                {
                    BuildingId = d.BuildingId,
                    Bread = Math.Clamp(d.Bread, 0, 4),
                    ChestFood = Math.Max(0, d.ChestFood),
                    ChestSeeds = Math.Max(0, d.ChestSeeds),
                    ChestProduce = Math.Max(0, d.ChestProduce),
                    MarketStock = Math.Max(0, d.MarketStock),
                    MarketSales = Math.Max(0, d.MarketSales),
                    MarketPurchases = Math.Max(0, d.MarketPurchases),
                    GardenCare = Math.Max(0, d.GardenCare),
                    Visited = d.Visited,
                    LampOn = d.LampOn
                };
            }
        }

        public static InteriorKind KindOf(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.MainHall: return InteriorKind.Palace;
                case BuildingType.Market: return InteriorKind.Market;
                case BuildingType.Farm: return InteriorKind.Farm;
                case BuildingType.Garden: return InteriorKind.Garden;
                case BuildingType.Caravanserai: return InteriorKind.Caravanserai;
                case BuildingType.Barracks: return InteriorKind.Barracks;
                case BuildingType.GladiatorArena: return InteriorKind.Arena;
                case BuildingType.Madrasa: return InteriorKind.Madrasa;
                case BuildingType.Mosque: return InteriorKind.Mosque;
                case BuildingType.Tower: return InteriorKind.Tower;
                case BuildingType.Gate: return InteriorKind.Gate;
                default: return InteriorKind.House;
            }
        }
    }
}
