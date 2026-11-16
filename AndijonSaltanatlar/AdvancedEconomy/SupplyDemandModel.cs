using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AndijonSaltanatlar.AdvancedEconomy
{
    /// <summary>Bozorda aylanadigan mahsulot turlari. Food = davlat omboridagi oziq-ovqat (ResourceType.Food).</summary>
    public enum ProductType
    {
        Food,   // oziq-ovqat (don, non)
        Fruit,  // meva
        Cloth,  // mato
        Crafts  // hunarmandchilik buyumlari
    }

    /// <summary>Bitta mahsulotning balans ta'rifi (konfiguratsiyadan).</summary>
    public sealed class ProductDef
    {
        public ProductType Type { get; set; }
        public string NameUz { get; set; } = "";
        /// <summary>Asosiy narx (tanga / birlik).</summary>
        public float BasePrice { get; set; } = 1f;
        /// <summary>Bir kishining kunlik ehtiyoji (birlik).</summary>
        public float PerCapitaDemand { get; set; }
    }

    /// <summary>
    /// Iqtisodiyot balansi — barcha formulalar koeffitsiyentlari shu yerda. Content/economy.json mavjud
    /// bo'lsa o'sha fayldan o'qiladi (kodga tegmasdan balanslash uchun), aks holda standart qiymatlar.
    /// </summary>
    public sealed class EconomyBalanceConfig
    {
        public List<ProductDef> Products { get; set; } = new List<ProductDef>
        {
            new ProductDef { Type = ProductType.Food,   NameUz = "Oziq-ovqat", BasePrice = 1.5f, PerCapitaDemand = 1.0f },
            new ProductDef { Type = ProductType.Fruit,  NameUz = "Meva",       BasePrice = 3.0f, PerCapitaDemand = 0.25f },
            new ProductDef { Type = ProductType.Cloth,  NameUz = "Mato",       BasePrice = 8.0f, PerCapitaDemand = 0.06f },
            new ProductDef { Type = ProductType.Crafts, NameUz = "Buyumlar",   BasePrice = 6.0f, PerCapitaDemand = 0.05f },
        };

        // --- Narx modeli ---
        /// <summary>Eng past narx = BasePrice * shu koeffitsiyent.</summary>
        public float PriceFloorFactor { get; set; } = 0.5f;
        /// <summary>Eng yuqori narx = BasePrice * shu koeffitsiyent.</summary>
        public float PriceCeilingFactor { get; set; } = 3.0f;
        /// <summary>Talab/taklif nisbatining narxga ta'sir kuchi.</summary>
        public float Elasticity { get; set; } = 0.6f;
        /// <summary>Narx maqsadga bir kunda qancha yaqinlashadi (0..1).</summary>
        public float PriceSmoothing { get; set; } = 0.25f;
        /// <summary>Bir kunda narx eng ko'pi bilan necha foizga o'zgaradi (0.08 = 8%).</summary>
        public float MaxDailyPriceChange { get; set; } = 0.08f;
        /// <summary>Zaxira necha kunga yetishi "me'yor" hisoblanadi (taklifni hisoblashda).</summary>
        public float StockCoverDays { get; set; } = 8f;

        // --- Bozor ---
        /// <summary>Bozor ombordan molni chakana narxning shu ulushiga oladi (ulgurji narx).</summary>
        public float WholesaleShare { get; set; } = 0.6f;
        /// <summary>Bitta bozorning kunlik savdo sig'imi (savdogarsiz).</summary>
        public float MarketBaseThroughput { get; set; } = 30f;
        /// <summary>Har bir savdogar qo'shadigan kunlik savdo sig'imi.</summary>
        public float ThroughputPerMerchant { get; set; } = 10f;
        /// <summary>Bozor necha kunlik molni zaxirada ushlaydi.</summary>
        public float MarketStockDays { get; set; } = 2f;
        /// <summary>Bozorning eng kam aylanma mablag'i (tanga).</summary>
        public float MarketWorkingCapital { get; set; } = 60f;

        // --- Ishlab chiqarish ---
        public float FruitPerGardener { get; set; } = 1.2f;
        public float ClothPerCarrier { get; set; } = 1.0f;
        public float CraftsPerCarrier { get; set; } = 0.8f;

        // --- Oilalar ---
        /// <summary>Bir kishidan kunlik aholi solig'i bazasi (tanga) — stavka foiziga ko'paytiriladi.</summary>
        public float PopTaxBasePerPerson { get; set; } = 2f;
        /// <summary>Puli yetmagan oilaga ombordan beriladigan bepul ulush (yetishmagan qismning ulushi).</summary>
        public float RationShare { get; set; } = 0.5f;
        /// <summary>Muallimning kunlik maoshi (ijtimoiy xarajat hisobidan).</summary>
        public float TeacherWage { get; set; } = 2f;

        // --- Voqealar ---
        /// <summary>Narx shu nisbatdan oshsa "narx baland" deb hisoblanadi (Current / Base).</summary>
        public float HighPriceRatio { get; set; } = 1.35f;
        /// <summary>Ishsizlik shu ulushdan oshsa muammo hisoblanadi.</summary>
        public float UnemploymentConcernShare { get; set; } = 0.2f;

        public ProductDef Get(ProductType t)
        {
            foreach (ProductDef p in Products) if (p.Type == t) return p;
            return Products[0];
        }

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public static EconomyBalanceConfig Load(string contentRoot)
        {
            try
            {
                string path = Path.Combine(contentRoot ?? "", "economy.json");
                if (File.Exists(path))
                {
                    var cfg = JsonSerializer.Deserialize<EconomyBalanceConfig>(File.ReadAllText(path), Options);
                    if (cfg != null && cfg.IsSane()) return cfg;
                }
            }
            catch
            {
                // buzuq fayl — standart balansga qaytamiz
            }
            return new EconomyBalanceConfig();
        }

        public static string ToJson(EconomyBalanceConfig cfg) => JsonSerializer.Serialize(cfg, Options);

        /// <summary>Xavfli (manfiy narx, cheksiz inflyatsiya) sozlamalarni rad etadi.</summary>
        public bool IsSane()
        {
            if (Products == null || Products.Count != Enum.GetValues(typeof(ProductType)).Length) return false;
            foreach (ProductDef p in Products)
                if (p.BasePrice <= 0f || p.PerCapitaDemand < 0f) return false;
            return PriceFloorFactor > 0f && PriceCeilingFactor >= PriceFloorFactor && PriceSmoothing > 0f && PriceSmoothing <= 1f
                   && MaxDailyPriceChange > 0f && MaxDailyPriceChange <= 0.5f && WholesaleShare > 0f && WholesaleShare < 1f
                   && StockCoverDays >= 1f;
        }
    }

    /// <summary>
    /// Bitta mahsulotning bozor holati: narx, talab, taklif, zaxira va narx chegaralari.
    /// </summary>
    public sealed class SupplyDemandModel
    {
        public const int HistoryLength = 30;

        public ProductType Product { get; set; }
        public float BasePrice { get; set; }
        public float CurrentPrice { get; set; }
        /// <summary>Bugungi taklif ko'rsatkichi (ishlab chiqarish + zaxiraning kunlik ulushi).</summary>
        public float Supply { get; set; }
        /// <summary>Bugungi talab (aholi ehtiyoji).</summary>
        public float Demand { get; set; }
        /// <summary>Shahardagi jami zaxira (ombor + bozorlar).</summary>
        public float Stock { get; set; }
        public float PriceFloor { get; set; }
        public float PriceCeiling { get; set; }
        /// <summary>Oxirgi kunlardagi narxlar (grafik uchun; eng eskisi boshida).</summary>
        public List<float> History { get; set; } = new List<float>();

        public float PriceRatio => BasePrice > 0f ? CurrentPrice / BasePrice : 1f;

        /// <summary>Kecha bilan solishtirganda narx o'zgarishi (+1 qimmatladi, -1 arzonladi, 0 o'zgarmadi).</summary>
        public int Trend
        {
            get
            {
                if (History.Count < 2) return 0;
                float d = History[History.Count - 1] - History[History.Count - 2];
                return d > 0.005f ? 1 : (d < -0.005f ? -1 : 0);
            }
        }

        public static SupplyDemandModel Create(ProductDef def, EconomyBalanceConfig cfg)
        {
            return new SupplyDemandModel
            {
                Product = def.Type,
                BasePrice = def.BasePrice,
                CurrentPrice = def.BasePrice,
                PriceFloor = def.BasePrice * cfg.PriceFloorFactor,
                PriceCeiling = def.BasePrice * cfg.PriceCeilingFactor
            };
        }

        /// <summary>
        /// Kunlik narx yangilanishi. Narx talab/taklif nisbatiga qarab maqsad narxga SEKIN yaqinlashadi:
        /// bir kunda MaxDailyPriceChange dan ko'p o'zgarmaydi va [PriceFloor, PriceCeiling] dan chiqmaydi.
        /// Shu sababli narx manfiy bo'lmaydi, keskin sakramaydi va cheksiz o'smaydi.
        /// </summary>
        public void UpdateDaily(float supply, float demand, float stock, EconomyBalanceConfig cfg)
        {
            Supply = Math.Max(0f, supply);
            Demand = Math.Max(0f, demand);
            Stock = Math.Max(0f, stock);

            float ratio;
            if (Demand <= 0.0001f) ratio = 0.5f;                 // talab yo'q — narx tushadi
            else if (Supply <= 0.0001f) ratio = 4f;              // mol yo'q — narx ko'tariladi
            else ratio = Demand / Supply;
            ratio = Math.Clamp(ratio, 0.25f, 4f);

            float target = BasePrice * MathF.Pow(ratio, cfg.Elasticity);
            target = Math.Clamp(target, PriceFloor, PriceCeiling);

            float next = CurrentPrice + (target - CurrentPrice) * cfg.PriceSmoothing;
            float maxStep = CurrentPrice * cfg.MaxDailyPriceChange;
            next = Math.Clamp(next, CurrentPrice - maxStep, CurrentPrice + maxStep);
            CurrentPrice = Math.Clamp(next, PriceFloor, PriceCeiling);

            History.Add(CurrentPrice);
            while (History.Count > HistoryLength) History.RemoveAt(0);
        }
    }

    /// <summary>
    /// Shahar bozor narxlari: har bir mahsulot uchun SupplyDemandModel. Narx shahar bo'yicha yagona
    /// (har bir bozorda alohida narx yuritilmaydi — soddalashtirilgan model).
    /// </summary>
    public sealed class MarketPricingSystem
    {
        private readonly Dictionary<ProductType, SupplyDemandModel> _models = new Dictionary<ProductType, SupplyDemandModel>();
        private readonly EconomyBalanceConfig _cfg;

        public MarketPricingSystem(EconomyBalanceConfig cfg)
        {
            _cfg = cfg;
            foreach (ProductDef def in cfg.Products) _models[def.Type] = SupplyDemandModel.Create(def, cfg);
        }

        public IEnumerable<SupplyDemandModel> All => _models.Values;
        public SupplyDemandModel Get(ProductType t) => _models[t];
        public float Price(ProductType t) => _models[t].CurrentPrice;
        public float Wholesale(ProductType t) => _models[t].CurrentPrice * _cfg.WholesaleShare;

        public void UpdateDaily(ProductType t, float supply, float demand, float stock)
            => _models[t].UpdateDaily(supply, demand, stock, _cfg);

        public List<SupplyDemandModel> Snapshot() => new List<SupplyDemandModel>(_models.Values);

        public void Restore(List<SupplyDemandModel> saved)
        {
            if (saved == null) return;
            foreach (SupplyDemandModel s in saved)
            {
                if (!_models.TryGetValue(s.Product, out SupplyDemandModel m)) continue;
                // Chegaralar har doim joriy konfiguratsiyadan olinadi; narx shu chegaraga qisiladi.
                m.CurrentPrice = Math.Clamp(s.CurrentPrice, m.PriceFloor, m.PriceCeiling);
                m.Supply = s.Supply; m.Demand = s.Demand; m.Stock = s.Stock;
                m.History = s.History ?? new List<float>();
            }
        }
    }
}
