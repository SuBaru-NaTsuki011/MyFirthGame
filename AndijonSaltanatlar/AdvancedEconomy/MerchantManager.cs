using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.AdvancedEconomy
{
    /// <summary>
    /// Bitta bozorning iqtisodiy holati: zaxira, kassa, savdogarlar va bugungi savdo ko'rsatkichlari.
    /// Bino koordinatasi bo'yicha saqlanadi (bino Id'si qayta yuklashda o'zgarishi mumkin).
    /// </summary>
    public sealed class MarketState
    {
        public int X { get; set; }
        public int Y { get; set; }
        /// <summary>Mahsulot zaxirasi (ProductType indeksi bo'yicha).</summary>
        public float[] Stock { get; set; } = new float[4];
        /// <summary>Bozor kassasi — ombordan mol olishga ishlatiladigan aylanma mablag'.</summary>
        public float Cash { get; set; }

        // --- Bugungi ko'rsatkichlar ---
        public int CustomersToday { get; set; }
        public float UnitsSoldToday { get; set; }
        public float RevenueToday { get; set; }
        public float TaxToday { get; set; }
        public float WholesaleCostToday { get; set; }
        /// <summary>Kunlik savdo sig'imi (birlik) va undan qolgani.</summary>
        public float Throughput { get; set; }
        public float ThroughputLeft { get; set; }

        // --- Ish vaqtida hisoblanadi (saqlanmaydi) ---
        [System.Text.Json.Serialization.JsonIgnore] public int BuildingId;
        [System.Text.Json.Serialization.JsonIgnore] public string Name = "Bozor";
        [System.Text.Json.Serialization.JsonIgnore] public int JobSlots;
        [System.Text.Json.Serialization.JsonIgnore] public List<int> MerchantIds = new List<int>();

        public float TotalStock
        {
            get { float s = 0; foreach (float v in Stock) s += v; return s; }
        }
    }

    /// <summary>Savdogar paneli uchun ma'lumot (haqiqiy fuqaro + uning bozori).</summary>
    public sealed class MerchantInfo
    {
        public Citizen Citizen;
        public MarketState Market;
        /// <summary>Rasta raqami (1 dan).</summary>
        public int Stall;
        /// <summary>Savdogar ixtisoslashgan mahsulot.</summary>
        public ProductType Product;
        public float Available;
        public float Price;
        public string StatusUz = "";
    }

    /// <summary>
    /// Bozorlar va savdogarlarni boshqaradi. Har bir Market binosi uchun MarketState yuritadi; savdogarlar —
    /// PopulationManager'dagi shu bozorga biriktirilgan haqiqiy Savdogar kasbidagi fuqarolar.
    /// </summary>
    public sealed class MerchantManager
    {
        private readonly List<MarketState> _markets = new List<MarketState>();
        private readonly EconomyBalanceConfig _cfg;

        public MerchantManager(EconomyBalanceConfig cfg)
        {
            _cfg = cfg;
        }

        public IReadOnlyList<MarketState> Markets => _markets;

        /// <summary>Bozorlar ro'yxatini binolar bilan, savdogarlarni esa fuqarolar bilan sinxronlaydi.</summary>
        public void Sync(BuildingManager b, PopulationManager pop)
        {
            var seen = new HashSet<MarketState>();
            int index = 0;
            foreach (Building x in b.Buildings)
            {
                if (x.Type != BuildingType.Market || x.BuildProgress < 1f) continue;
                index++;
                MarketState m = FindAt(x.Position.X, x.Position.Y);
                if (m == null)
                {
                    m = new MarketState { X = x.Position.X, Y = x.Position.Y, Cash = _cfg.MarketWorkingCapital };
                    _markets.Add(m);
                }
                if (m.Stock == null || m.Stock.Length != 4) m.Stock = new float[4];
                m.BuildingId = x.Id;
                m.Name = index == 1 ? "Markaziy bozor" : index + "-bozor";
                m.JobSlots = PopulationManager.JobSlots(BuildingType.Market, out _);
                m.MerchantIds.Clear();
                seen.Add(m);
            }
            _markets.RemoveAll(m => !seen.Contains(m));

            foreach (Citizen c in pop.Citizens)
            {
                if (c.Profession != CitizenProfession.Savdogar || !c.WorkplaceBuildingId.HasValue) continue;
                MarketState m = GetByBuilding(c.WorkplaceBuildingId.Value);
                if (m != null) m.MerchantIds.Add(c.Id);
            }
            foreach (MarketState m in _markets)
                m.Throughput = _cfg.MarketBaseThroughput + m.MerchantIds.Count * _cfg.ThroughputPerMerchant;
        }

        public MarketState FindAt(int x, int y)
        {
            foreach (MarketState m in _markets) if (m.X == x && m.Y == y) return m;
            return null;
        }

        public MarketState GetByBuilding(int buildingId)
        {
            foreach (MarketState m in _markets) if (m.BuildingId == buildingId) return m;
            return null;
        }

        /// <summary>Berilgan nuqtaga eng yaqin bozor (yo'q bo'lsa null).</summary>
        public MarketState Nearest(int x, int y)
        {
            MarketState best = null;
            int bestD = int.MaxValue;
            foreach (MarketState m in _markets)
            {
                int d = Math.Abs(m.X - x) + Math.Abs(m.Y - y);
                if (d < bestD) { bestD = d; best = m; }
            }
            return best;
        }

        /// <summary>Fuqaro savdogar bo'lsa — uning rastasi, moli va savdo holati.</summary>
        public MerchantInfo GetMerchant(Citizen c, MarketPricingSystem prices)
        {
            if (c == null || c.Profession != CitizenProfession.Savdogar || !c.WorkplaceBuildingId.HasValue) return null;
            MarketState m = GetByBuilding(c.WorkplaceBuildingId.Value);
            if (m == null) return null;
            int idx = Math.Max(0, m.MerchantIds.IndexOf(c.Id));
            var product = (ProductType)(idx % 4);
            var info = new MerchantInfo
            {
                Citizen = c, Market = m, Stall = idx + 1, Product = product,
                Available = m.Stock[(int)product], Price = prices.Price(product)
            };
            SupplyDemandModel model = prices.Get(product);
            if (info.Available < 0.5f) info.StatusUz = "Mol tugagan";
            else if (m.ThroughputLeft <= 0.5f && m.UnitsSoldToday > 0) info.StatusUz = "Savdo juda qizg'in";
            else if (model.PriceRatio >= _cfg.HighPriceRatio) info.StatusUz = "Narx baland, xaridor kam";
            else if (m.UnitsSoldToday >= m.Throughput * 0.5f) info.StatusUz = "Savdo qizg'in";
            else if (m.UnitsSoldToday > 0) info.StatusUz = "Savdo o'rtacha";
            else info.StatusUz = "Savdo sust";
            return info;
        }

        public List<MarketState> Snapshot() => new List<MarketState>(_markets);

        public void Restore(List<MarketState> saved)
        {
            _markets.Clear();
            if (saved == null) return;
            foreach (MarketState m in saved)
            {
                if (m == null) continue;
                if (m.Stock == null || m.Stock.Length != 4) m.Stock = new float[4];
                m.MerchantIds = new List<int>();
                _markets.Add(m);
            }
        }
    }
}
