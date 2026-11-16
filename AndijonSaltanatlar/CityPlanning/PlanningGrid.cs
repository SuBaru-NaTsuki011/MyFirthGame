using System;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.World;

namespace AndijonSaltanatlar.CityPlanning
{
    /// <summary>
    /// Rejalashtirish uchun faqat-o'qish grid interfeysi. AI shahar holatini o'zgartirmasdan
    /// joylashuvni tekshiradi. Testlarda xotira grid'i, o'yinda MapManager adapteri ishlatiladi.
    /// </summary>
    public interface ICityPlanningGrid
    {
        int Width { get; }
        int Height { get; }

        bool InBounds(int x, int y);
        /// <summary>Yer turi bino qurishga mosmi (suv/yo'l emas).</summary>
        bool IsBuildableTerrain(int x, int y);
        /// <summary>Yer o'yinchiga ochiqmi (qulflangan/boshqa davlat yeri emas).</summary>
        bool IsOpenLand(int x, int y);
        /// <summary>Bino uchun to'siq (bino yoki olib tashlanmaydigan bezak).</summary>
        bool IsBlockedForBuilding(int x, int y);
        /// <summary>Yo'l uchun to'siq (suv, bino, bezak).</summary>
        bool IsBlockedForRoad(int x, int y);
        bool IsRoad(int x, int y);
        /// <summary>Muhofaza qilinadigan maydon (saroy/masjid/madrasa atrofi, markaziy maydon).</summary>
        bool IsProtected(int x, int y);
        BuildingType? BuildingAt(int x, int y);
    }

    /// <summary>Haqiqiy MapManager + GameStateManager ustidagi adapter.</summary>
    public sealed class MapPlanningGrid : ICityPlanningGrid
    {
        private readonly GameStateManager _state;
        private readonly MapManager _map;

        public MapPlanningGrid(GameStateManager state, MapManager map)
        {
            _state = state; _map = map;
        }

        public int Width => _map.Width;
        public int Height => _map.Height;

        public bool InBounds(int x, int y) => _map.InBounds(x, y);

        public bool IsBuildableTerrain(int x, int y)
        {
            if (!_map.IsBuildableTerrain(x, y)) return false;
            // Markaziy maydon (Plaza) — jamoat maydoni, AI uni yangi mahalla bilan band qilmaydi.
            return _map.GetTile(x, y) != TileType.Plaza;
        }

        public bool IsOpenLand(int x, int y) => _state.Land.IsBuildable(x, y);

        public bool IsBlockedForBuilding(int x, int y)
        {
            object o = _map.GetOccupant(x, y);
            if (o is Building) return true;
            if (o is Decoration d && !d.Removable) return true;
            return false;
        }

        public bool IsBlockedForRoad(int x, int y)
        {
            if (_map.GetTile(x, y) == TileType.Water) return true;
            object o = _map.GetOccupant(x, y);
            return o is Building || o is Decoration;
        }

        public bool IsRoad(int x, int y) => _map.InBounds(x, y) && _map.GetTile(x, y) == TileType.Road;

        public bool IsProtected(int x, int y)
        {
            if (!_map.InBounds(x, y)) return true;
            if (_map.GetTile(x, y) == TileType.Plaza) return true;
            // Muhofaza zonasi: hukmdor va madaniyat binolarining atrofi (1 katak).
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (!_map.InBounds(nx, ny)) continue;
                    if (_map.GetOccupant(nx, ny) is Building b &&
                        (b.Type == BuildingType.MainHall || b.Type == BuildingType.Mosque ||
                         b.Type == BuildingType.Madrasa || b.Type == BuildingType.Gate ||
                         b.Type == BuildingType.Wall || b.Type == BuildingType.Tower))
                        return true;
                }
            return false;
        }

        public BuildingType? BuildingAt(int x, int y)
            => _map.GetOccupant(x, y) is Building b ? b.Type : (BuildingType?)null;
    }
}
