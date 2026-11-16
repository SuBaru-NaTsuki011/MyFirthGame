using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.LandExpansion
{
    /// <summary>
    /// Shahar qurilish maydonini kengaytirishni boshqaradi. Markaziy shahar doim ochiq; atrofdagi
    /// mahalla hududlari qulflangan va aholi+tanga sharti bajarilganda ochiladi. Bu KingdomExpansion
    /// (dunyo xaritasidagi hudud qo'shish) dan ALOHIDA tizim.
    /// </summary>
    public sealed class CityLandExpansionManager
    {
        public List<LandParcel> Parcels { get; } = new List<LandParcel>();

        public void Initialize()
        {
            Parcels.Clear();
            Add(1, "Shimoliy mahalla", new Rectangle(14, 2, 36, 10), 400, 60);
            Add(2, "Janubiy turar joy", new Rectangle(14, 53, 36, 9), 500, 90);
            Add(3, "G'arbiy hunarmandlar mahallasi", new Rectangle(2, 14, 10, 36), 600, 120);
            Add(4, "Sharqiy savdo hududi", new Rectangle(54, 14, 9, 36), 650, 140);
        }

        private void Add(int id, string name, Rectangle bounds, int cost, int reqPop)
            => Parcels.Add(new LandParcel { Id = id, NameUz = name, GridBounds = bounds, UnlockCost = cost, RequiredPopulation = reqPop });

        public LandParcel ParcelAt(int x, int y)
        {
            foreach (LandParcel p in Parcels) if (p.GridBounds.Contains(x, y)) return p;
            return null;
        }

        /// <summary>Tile'ga qurish mumkinmi (markaz — ha; qulflangan mahalla — yo'q).</summary>
        public bool IsBuildable(int x, int y)
        {
            LandParcel p = ParcelAt(x, y);
            return p == null || p.State == LandParcelState.Open;
        }

        /// <summary>Aholiga qarab Locked ↔ Unlockable holatini yangilaydi (ochilganlar o'zgarmaydi).</summary>
        public void RefreshStates(int population)
        {
            foreach (LandParcel p in Parcels)
            {
                if (p.State == LandParcelState.Open || p.IsProtected || p.State == LandParcelState.OtherKingdom) continue;
                p.State = population >= p.RequiredPopulation ? LandParcelState.Unlockable : LandParcelState.Locked;
            }
        }

        public bool Unlock(LandParcel p)
        {
            if (p == null || p.State == LandParcelState.Open) return false;
            p.State = LandParcelState.Open;
            return true;
        }

        public List<int> OpenParcelIds()
        {
            var list = new List<int>();
            foreach (LandParcel p in Parcels) if (p.State == LandParcelState.Open) list.Add(p.Id);
            return list;
        }

        public void ApplyOpen(List<int> openIds)
        {
            if (openIds == null) return;
            foreach (LandParcel p in Parcels) if (openIds.Contains(p.Id)) p.State = LandParcelState.Open;
        }
    }
}
