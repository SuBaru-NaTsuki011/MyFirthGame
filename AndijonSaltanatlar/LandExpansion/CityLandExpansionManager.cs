using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.LandExpansion
{
    /// <summary>
    /// Shahar qurilish maydonini kengaytirishni boshqaradi.
    ///
    /// Yer halqalarga bo'lingan, har halqada 4 tomon (shimol, janub, g'arb, sharq):
    ///   1-halqa — shahar devori atrofidagi 4 mahalla (asl shahar bloki ichida);
    ///   2..N-halqa — shahar blokidan tashqaridagi yerlar: yaylov (chorvachilik), dalalar (dehqonchilik),
    ///   bog'lar va karvon yo'li (savdo). Keyingi halqa faqat oldingi halqaning 4 tomoni ham ochilgandan keyin ochiladi.
    /// Markaziy shahar (asl blok, uchastkalarga kirmagan joylar) doim ochiq; blokdan tashqaridagi hech bir uchastkaga
    /// tegishli bo'lmagan tile'lar (xarita cheti) qurilmaydi.
    /// Bu KingdomExpansion (dunyo xaritasidagi hudud qo'shish) dan ALOHIDA tizim. Yerni bundan ham kattalashtirish —
    /// keyingi bosqichda boshqa yerlarni bosib olish orqali.
    /// </summary>
    public sealed class CityLandExpansionManager
    {
        public List<LandParcel> Parcels { get; } = new List<LandParcel>();

        /// <summary>Oxirgi RefreshStates chaqiruvida to'liq ochilgan halqa yangi halqani ochdimi (UI xabari uchun).</summary>
        public int LastCompletedRing { get; private set; }

        private const int O = GameConfig.CityOffset;
        private const int R = GameConfig.RingWidth;
        private const int Block = GameConfig.CityBlockSize;

        public int RingCount => GameConfig.MaxRings;

        public void Initialize()
        {
            Parcels.Clear();
            // 1-halqa: asl 4 mahalla (Id 1..4 — eski saqlashlar bilan mos).
            Add(1, 1, LandSide.North, "Shimoliy mahalla", "Turar joy", new Rectangle(14 + O, 2 + O, 36, 10), 400, 60);
            Add(2, 1, LandSide.South, "Janubiy turar joy", "Turar joy", new Rectangle(14 + O, 53 + O, 36, 9), 500, 90);
            Add(3, 1, LandSide.West, "G'arbiy hunarmandlar mahallasi", "Hunarmandchilik", new Rectangle(2 + O, 14 + O, 10, 36), 600, 120);
            Add(4, 1, LandSide.East, "Sharqiy savdo hududi", "Savdo", new Rectangle(54 + O, 14 + O, 9, 36), 650, 140);

            // 2..N-halqa: shahar blokini o'rab turgan kvadrat halqalar. Shimol/janub burchaklarni ham qamraydi.
            string[] north = { "Shimoliy yaylov", "Shimoliy tog' etagi", "Shimoliy o'rmonzor", "Shimoliy dasht" };
            string[] south = { "Janubiy dalalar", "Soy bo'yi ekinzorlari", "Janubiy paxtazor", "Janubiy vodiy" };
            string[] west = { "G'arbiy bog'lar", "G'arbiy uzumzor", "G'arbiy yaylov", "G'arbiy adirlar" };
            string[] east = { "Sharqiy karvon yo'li", "Sharqiy bozor yerlari", "Sharqiy savdo qo'rg'oni", "Sharqiy chegara" };
            int id = 5;
            for (int ring = 2; ring <= GameConfig.MaxRings; ring++)
            {
                int k = ring - 1;
                int outerMin = O - k * R;                 // halqa tashqi chegarasi
                int outerSize = Block + 2 * k * R;
                int innerMin = outerMin + R;              // oldingi halqa chegarasi
                int innerSize = outerSize - 2 * R;
                int cost = 900 * k + 400 * k * k;
                int pop = 160 + 60 * k;
                int n = (ring - 2) % north.Length;
                Add(id++, ring, LandSide.North, Name(north[n], ring), "Chorvachilik",
                    new Rectangle(outerMin, outerMin, outerSize, R), cost, pop);
                Add(id++, ring, LandSide.South, Name(south[n], ring), "Dehqonchilik",
                    new Rectangle(outerMin, innerMin + innerSize, outerSize, R), cost + 100 * k, pop + 20);
                Add(id++, ring, LandSide.West, Name(west[n], ring), "Bog'dorchilik",
                    new Rectangle(outerMin, innerMin, R, innerSize), cost + 200 * k, pop + 40);
                Add(id++, ring, LandSide.East, Name(east[n], ring), "Savdo",
                    new Rectangle(innerMin + innerSize, innerMin, R, innerSize), cost + 300 * k, pop + 60);
            }
        }

        private static string Name(string baseName, int ring) => baseName;

        private void Add(int id, int ring, LandSide side, string name, string purpose, Rectangle bounds, int cost, int reqPop)
            => Parcels.Add(new LandParcel
            {
                Id = id, Ring = ring, Side = side, NameUz = name, PurposeUz = purpose,
                GridBounds = bounds, UnlockCost = cost, RequiredPopulation = reqPop
            });

        public LandParcel ParcelAt(int x, int y)
        {
            foreach (LandParcel p in Parcels) if (p.GridBounds.Contains(x, y)) return p;
            return null;
        }

        /// <summary>Tile asl shahar bloki ichidami (uchastkaga kirmagan joylari doim ochiq).</summary>
        public static bool InCityBlock(int x, int y) => x >= O && y >= O && x < O + Block && y < O + Block;

        /// <summary>Tile'ga qurish mumkinmi (markaz — ha; qulflangan uchastka yoki xarita cheti — yo'q).</summary>
        public bool IsBuildable(int x, int y)
        {
            LandParcel p = ParcelAt(x, y);
            if (p == null) return InCityBlock(x, y);
            return p.State == LandParcelState.Open;
        }

        /// <summary>Halqaning 4 tomoni ham ochiqmi.</summary>
        public bool IsRingComplete(int ring)
        {
            bool any = false;
            foreach (LandParcel p in Parcels)
            {
                if (p.Ring != ring) continue;
                any = true;
                if (p.State != LandParcelState.Open) return false;
            }
            return any;
        }

        /// <summary>Hozir ochilayotgan (eng ichki to'liq ochilmagan) halqa. Hammasi ochiq bo'lsa — RingCount + 1.</summary>
        public int CurrentRing
        {
            get
            {
                for (int r = 1; r <= RingCount; r++) if (!IsRingComplete(r)) return r;
                return RingCount + 1;
            }
        }

        /// <summary>
        /// Aholiga qarab Locked ↔ Unlockable holatini yangilaydi (ochilganlar o'zgarmaydi).
        /// Keyingi halqa uchastkalari oldingi halqa to'liq ochilguncha qulflangan qoladi.
        /// </summary>
        public void RefreshStates(int population)
        {
            int current = CurrentRing;
            foreach (LandParcel p in Parcels)
            {
                p.WaitingForPreviousRing = p.Ring > current;
                if (p.State == LandParcelState.Open || p.IsProtected || p.State == LandParcelState.OtherKingdom) continue;
                if (p.WaitingForPreviousRing) { p.State = LandParcelState.Locked; continue; }
                p.State = population >= p.RequiredPopulation ? LandParcelState.Unlockable : LandParcelState.Locked;
            }
        }

        /// <summary>Uchastkani ochish mumkinmi (oldingi halqa to'liq ochilgan bo'lishi shart).</summary>
        public bool CanUnlockRing(LandParcel p) => p != null && p.Ring <= CurrentRing;

        public bool Unlock(LandParcel p)
        {
            if (p == null || p.State == LandParcelState.Open) return false;
            p.State = LandParcelState.Open;
            LastCompletedRing = IsRingComplete(p.Ring) ? p.Ring : 0;
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

        /// <summary>Ochiq qurilish maydoni chegarasi (kamera chegarasi uchun): blok + ochilgan halqalar + bitta keyingi halqa.</summary>
        public Rectangle VisibleArea()
        {
            int k = System.Math.Min(CurrentRing, RingCount) - 1; // keyingi ochiladigan halqa ko'rinsin
            int min = O - k * R;
            int size = Block + 2 * k * R;
            return new Rectangle(min, min, size, size);
        }
    }
}
