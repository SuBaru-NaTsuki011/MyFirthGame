using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Economy;
using AndijonSaltanatlar.Government;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.AdvancedEconomy
{
    /// <summary>Iqtisodiy voqea — har doim aniq sababga ega (matnda ko'rsatiladi).</summary>
    public sealed class EconomicEvent
    {
        public int Day { get; set; }
        public string Key { get; set; } = "";
        public string TextUz { get; set; } = "";
        public bool Positive { get; set; }
    }

    /// <summary>AdvancedEconomyManager'ning saqlanadigan holati.</summary>
    public sealed class AdvancedEconomySave
    {
        public int LastProcessedDay { get; set; }
        public float[] Warehouse { get; set; }
        public float[] Carry { get; set; }
        public float FoodDrawCarry { get; set; }
        public float TaxCarry { get; set; }
        public float PopTaxCarry { get; set; }
        public float FoodProductionAvg { get; set; }
        public int LastMarketCount { get; set; }
        public int LastJobCapacity { get; set; }
        public float ImportSinceCaravan { get; set; }
        public int StateTradeCounter { get; set; }
        public List<SupplyDemandModel> Prices { get; set; }
        public List<MarketState> Markets { get; set; }
        public List<EconomicEvent> Events { get; set; }
        public Dictionary<string, int> EventCooldowns { get; set; }
        public List<string> ProcessedTransactionIds { get; set; }
        public List<EconomyTransaction> TradeHistory { get; set; }
    }

    /// <summary>
    /// PROMPT 17 — chuqur shahar iqtisodiyoti. Har o'yin kunida BIR MARTA (kun raqami bo'yicha himoyalangan)
    /// quyidagi zanjirni yuritadi:
    ///
    ///   ISHLAB CHIQARISH → OMBOR → BOZOR (ulgurji) → XARIDOR (chakana) → SOLIQ → XAZINA
    ///
    ///  1. Bog'bonlar oziq-ovqat va meva yetishtiradi, karvonchilar mato va buyum keltiradi — hammasi omborga tushadi.
    ///  2. Bozorlar ombordan ulgurji narxda mol oladi; pul ishlab chiqaruvchilarga (ularning oilasiga) o'tadi.
    ///  3. Oilalar ehtiyojiga qarab bozordan xarid qiladi; puli yetmasa kam oladi.
    ///  4. Har bir xariddan savdo solig'i, har bir oiladan aholi solig'i hisoblanadi.
    ///  5. Soliqlar EconomyManager orqali xazinaga BIR MARTA qo'shiladi (RetailDayResult).
    ///
    /// Pul va mol hech qayerdan paydo bo'lmaydi: har bir harakat bir joydan ayirilib, boshqa joyga qo'shiladi.
    /// Oziq-ovqat zaxirasi — mavjud ResourceType.Food (davlat ombori); bu tizim uni takrorlamaydi.
    /// </summary>
    public sealed class AdvancedEconomyManager
    {
        public const int ProductCount = 4;
        private static readonly ProductType[] BuyOrder = { ProductType.Food, ProductType.Fruit, ProductType.Cloth, ProductType.Crafts };

        private readonly float[] _warehouse = new float[ProductCount];   // Food indeksi ishlatilmaydi (ResourceType.Food)
        private readonly float[] _carry = new float[ProductCount];       // kasr qoldiqlar (ishlab chiqarish)
        private readonly Dictionary<string, int> _eventCooldown = new Dictionary<string, int>();
        private float _foodDrawCarry, _taxCarry, _popTaxCarry, _stateSalesCarry;
        private float _foodProductionAvg;
        private int _lastMarketCount = -1, _lastJobCapacity = -1;
        private float _importSinceCaravan;
        private int _stateTradeCounter;

        public EconomyBalanceConfig Config { get; }
        public MarketPricingSystem Prices { get; }
        public EconomyTransactionService Transactions { get; } = new EconomyTransactionService();
        public MerchantManager Merchants { get; }
        public List<EconomicEvent> Events { get; } = new List<EconomicEvent>();
        public int LastProcessedDay { get; private set; }
        public RetailDayResult LastResult { get; private set; } = new RetailDayResult();

        // --- Oxirgi kun ko'rsatkichlari (panel va tahlil uchun) ---
        public float[] ProducedToday { get; } = new float[ProductCount];
        public float[] DemandToday { get; } = new float[ProductCount];
        public float[] SoldToday { get; } = new float[ProductCount];
        /// <summary>Bozorda mol yoki sig'im yetmagani uchun qondirilmagan talab.</summary>
        public float[] MarketShortageToday { get; } = new float[ProductCount];
        public float RetailRevenueToday { get; private set; }
        public float WagesPaidToday { get; private set; }
        public float StateWagesPaidToday { get; private set; }
        public int HouseholdsUnderfed { get; private set; }
        public int HouseholdsCouldNotAfford { get; private set; }
        /// <summary>Hali ko'rsatilmagan yangi voqea (UI toast uchun; o'qilgach null qilinadi).</summary>
        public EconomicEvent PendingEvent { get; set; }

        public AdvancedEconomyManager(EconomyBalanceConfig cfg = null)
        {
            Config = cfg ?? new EconomyBalanceConfig();
            Prices = new MarketPricingSystem(Config);
            Merchants = new MerchantManager(Config);
        }

        /// <summary>Ombordagi zaxira. Oziq-ovqat uchun haqiqiy ResourceType.Food qaytadi.</summary>
        public float WarehouseStock(ProductType p, ResourceManager res)
            => p == ProductType.Food ? res.Get(ResourceType.Food) : _warehouse[(int)p];

        public float MarketStock(ProductType p)
        {
            float s = 0;
            foreach (MarketState m in Merchants.Markets) s += m.Stock[(int)p];
            return s;
        }

        /// <summary>Bozor sig'imi yetishmasligi ulushi (0..1) — "yangi bozor kerak" belgisi.</summary>
        public float MarketShortageShare
        {
            get
            {
                float d = DemandToday[(int)ProductType.Food];
                return d <= 0.01f ? 0f : Math.Min(1f, MarketShortageToday[(int)ProductType.Food] / d);
            }
        }

        // ==================================================================
        //  KUNLIK ZANJIR
        // ==================================================================
        public RetailDayResult RunDay(int day, BuildingManager b, PopulationManager pop, ResourceManager res,
                                      GovernmentPolicy policy, float foodBonus)
        {
            // Bir kun faqat bir marta hisoblanadi (Save/Load yoki takroriy chaqiruvda qayta bajarilmaydi).
            if (day <= LastProcessedDay || !Transactions.TryBegin("day:" + day))
                return new RetailDayResult();
            LastProcessedDay = day;
            Transactions.BeginDay();
            Merchants.Sync(b, pop);
            ResetDayStats();

            int coinsBefore = res.Get(ResourceType.Coin);
            int gardeners = pop.CountProfession(CitizenProfession.Bogbon) + pop.CountProfession(CitizenProfession.Dehqon);
            int carriers = pop.CountProfession(CitizenProfession.Tashuvchi);

            // ---- 1. ISHLAB CHIQARISH → OMBOR ----
            int foodProduced = EconomyManager.ComputeFoodProduction(gardeners, b.Count(BuildingType.Garden), b.Count(BuildingType.Farm), foodBonus);
            res.Add(ResourceType.Food, foodProduced);
            ProducedToday[(int)ProductType.Food] = foodProduced;
            Transactions.Add(TransactionType.Production, foodProduced, 0);

            float season = Math.Max(0.2f, 1f + foodBonus);
            Produce(ProductType.Fruit, gardeners * Config.FruitPerGardener * season, pop.Count, TransactionType.Production);
            float cloth = Produce(ProductType.Cloth, carriers * Config.ClothPerCarrier, pop.Count, TransactionType.Import);
            float crafts = Produce(ProductType.Crafts, carriers * Config.CraftsPerCarrier, pop.Count, TransactionType.Import);
            _importSinceCaravan += cloth + crafts;
            // Meva tez buziladi: ombordagi qoldiqning bir qismi har kuni yo'qoladi.
            _warehouse[(int)ProductType.Fruit] *= 0.85f;

            // ---- Oilalar: kun boshi ----
            foreach (Household hh in pop.Households)
            {
                hh.LastIncome = 0; hh.LastExpense = 0; hh.CouldNotAffordFood = false;
                hh.WorkingMembers = 0;
                foreach (int id in hh.MemberIds) { Citizen c = pop.GetById(id); if (c != null && c.IsEmployed) hh.WorkingMembers++; }
                hh.FoodNeed = hh.MemberIds.Count * Config.Get(ProductType.Food).PerCapitaDemand;
                foreach (ProductType p in BuyOrder) DemandToday[(int)p] += hh.MemberIds.Count * Config.Get(p).PerCapitaDemand;
            }

            // ---- 2. OMBOR → BOZOR (ulgurji) ----
            float gardenerPool = 0, carrierPool = 0, orphanPool = 0;
            Restock(res, gardeners, carriers, ref gardenerPool, ref carrierPool, ref orphanPool);

            // ---- 3-4. BOZOR → XARIDOR (chakana) + savdo solig'i ----
            float tradeTaxRate = policy.TradeTax / 100f;
            float retailTax = 0f;
            RetailPass(b, pop, res, tradeTaxRate, ref retailTax);

            // ---- Aholi solig'i (oilalardan haqiqatan undiriladi) ----
            float popTax = 0f;
            float perPerson = Config.PopTaxBasePerPerson * policy.PopulationTax / 100f;
            foreach (Household hh in pop.Households)
            {
                float due = hh.MemberIds.Count * perPerson;
                float paid = Math.Min(due, Math.Max(0f, hh.SharedMoney));
                if (paid <= 0f) continue;
                hh.SharedMoney -= paid; hh.LastExpense += paid; popTax += paid;
            }
            Transactions.Add(TransactionType.PopulationTax, 0, popTax);

            // ---- 5. DAROMADLAR: ishlab chiqaruvchi, karvonchi, savdogar, davlat maoshi ----
            PayPool(pop, CitizenProfession.Bogbon, gardenerPool);
            PayPool(pop, CitizenProfession.Tashuvchi, carrierPool);
            PayMerchants(pop, ref orphanPool);
            PayStateWages(pop, coinsBefore);

            // ---- Soliq va xazina tushumlari (butun tangalarga aylantiriladi, kasr keyingi kunga o'tadi) ----
            var result = new RetailDayResult
            {
                FoodProduced = foodProduced,
                // Ombordan chiqqan jami oziq-ovqat: bozorlar olgani + to'g'ridan-to'g'ri ta'minot.
                FoodConsumed = _foodLeftWarehouseToday,
                PopTaxCollected = TakeWhole(ref _popTaxCarry, popTax),
                RetailTax = TakeWhole(ref _taxCarry, retailTax) + TakeWhole(ref _stateSalesCarry, orphanPool)
            };
            Transactions.Add(TransactionType.RetailTax, 0, retailTax);
            if (orphanPool > 0f) Transactions.Add(TransactionType.MarketFee, 0, orphanPool);

            // ---- Narxlar: talab va taklifga qarab ----
            UpdatePrices(res);
            RaiseEvents(day, b, pop, cloth, crafts);

            LastResult = result;
            return result;
        }

        private int _foodLeftWarehouseToday;

        private void ResetDayStats()
        {
            Array.Clear(ProducedToday, 0, ProductCount);
            Array.Clear(DemandToday, 0, ProductCount);
            Array.Clear(SoldToday, 0, ProductCount);
            Array.Clear(MarketShortageToday, 0, ProductCount);
            RetailRevenueToday = 0; WagesPaidToday = 0; StateWagesPaidToday = 0;
            HouseholdsUnderfed = 0; HouseholdsCouldNotAfford = 0;
            _foodLeftWarehouseToday = 0;
            foreach (MarketState m in Merchants.Markets)
            {
                m.CustomersToday = 0; m.UnitsSoldToday = 0; m.RevenueToday = 0; m.TaxToday = 0;
                m.WholesaleCostToday = 0; m.ThroughputLeft = m.Throughput;
            }
        }

        /// <summary>Oziq-ovqatdan boshqa molni omborga qo'shadi (ombor sig'imi bilan cheklangan).</summary>
        private float Produce(ProductType p, float amount, int population, TransactionType type)
        {
            if (amount <= 0f) return 0f;
            float cap = Math.Max(20f, population * Config.Get(p).PerCapitaDemand * 40f);
            float room = Math.Max(0f, cap - _warehouse[(int)p]);
            float made = Math.Min(amount, room);
            _warehouse[(int)p] += made;
            ProducedToday[(int)p] = made;
            Transactions.Add(type, made, 0);
            return made;
        }

        // ------------------------------------------------------------------
        // Ombor → bozor
        // ------------------------------------------------------------------
        private void Restock(ResourceManager res, int gardeners, int carriers,
                             ref float gardenerPool, ref float carrierPool, ref float orphanPool)
        {
            int n = Merchants.Markets.Count;
            if (n == 0) return;
            foreach (MarketState m in Merchants.Markets)
            {
                foreach (ProductType p in BuyOrder)
                {
                    int i = (int)p;
                    // Bozor shahar talabining o'z ulushini bir necha kunlik zaxira bilan ushlaydi.
                    float target = Math.Min(DemandToday[i] / n * Config.MarketStockDays, m.Throughput * Config.MarketStockDays);
                    float want = target - m.Stock[i];
                    if (want <= 0.01f) continue;

                    float wholesale = Prices.Wholesale(p);
                    float available = p == ProductType.Food ? res.Get(ResourceType.Food) : _warehouse[i];
                    float qty = Math.Min(want, available);
                    if (wholesale > 0f) qty = Math.Min(qty, Math.Max(0f, m.Cash) / wholesale);
                    if (p == ProductType.Food) qty = MathF.Floor(qty); // ombor oziq-ovqati butun birliklarda
                    if (qty <= 0.01f) continue;

                    float cost = qty * wholesale;
                    if (p == ProductType.Food) { res.Add(ResourceType.Food, -(int)qty); _foodLeftWarehouseToday += (int)qty; }
                    else _warehouse[i] -= qty;
                    m.Stock[i] += qty;
                    m.Cash -= cost;
                    m.WholesaleCostToday += cost;

                    bool farmGoods = p == ProductType.Food || p == ProductType.Fruit;
                    if (farmGoods && gardeners > 0) gardenerPool += cost;
                    else if (!farmGoods && carriers > 0) carrierPool += cost;
                    else orphanPool += cost; // ishlab chiqaruvchisi yo'q mol — davlat ombori sotuvi
                    Transactions.Add(TransactionType.Wholesale, qty, cost);
                }
            }
        }

        // ------------------------------------------------------------------
        // Bozor → oilalar
        // ------------------------------------------------------------------
        private void RetailPass(BuildingManager b, PopulationManager pop, ResourceManager res, float tradeTaxRate, ref float retailTax)
        {
            var homePos = new Dictionary<int, (int x, int y)>();
            foreach (Building x in b.Buildings) homePos[x.Id] = (x.Position.X, x.Position.Y);

            var markets = new List<MarketState>(Merchants.Markets);
            var comparer = new MarketDistanceComparer();
            foreach (Household hh in pop.Households)
            {
                int members = hh.MemberIds.Count;
                if (members == 0) { hh.FedRatio = 1f; hh.Comfort = 0f; continue; }

                (int hx, int hy) = hh.HomeBuildingId.HasValue && homePos.TryGetValue(hh.HomeBuildingId.Value, out var hp) ? hp : (GameConfig.CityCenter, GameConfig.CityCenter);
                comparer.X = hx; comparer.Y = hy;
                markets.Sort(comparer);

                bool visited = false;
                float comfortSum = 0f;
                float foodBought = 0f, foodNeed = 0f;
                bool foodMoneyShort = false;

                foreach (ProductType p in BuyOrder)
                {
                    int i = (int)p;
                    float need = members * Config.Get(p).PerCapitaDemand;
                    if (need <= 0f) continue;
                    float remaining = need;
                    float price = Prices.Price(p);
                    bool moneyShort = false;

                    foreach (MarketState m in markets)
                    {
                        if (remaining <= 0.0001f) break;
                        float q = Math.Min(remaining, Math.Min(m.Stock[i], m.ThroughputLeft));
                        if (q <= 0.0001f) continue;
                        float affordable = price > 0f ? Math.Max(0f, hh.SharedMoney) / price : q;
                        if (q > affordable) { q = affordable; moneyShort = true; }
                        if (q <= 0.0001f) break;

                        float pay = q * price;
                        float tax = pay * tradeTaxRate;
                        hh.SharedMoney -= pay; hh.LastExpense += pay;
                        m.Stock[i] -= q; m.ThroughputLeft -= q;
                        m.Cash += pay - tax;
                        m.UnitsSoldToday += q; m.RevenueToday += pay; m.TaxToday += tax;
                        if (!visited) { m.CustomersToday += members; visited = true; }
                        retailTax += tax;
                        RetailRevenueToday += pay;
                        SoldToday[i] += q;
                        remaining -= q;
                        Transactions.Add(TransactionType.Retail, q, pay);
                        if (moneyShort) break;
                    }

                    if (p == ProductType.Food)
                    {
                        foodNeed = need; foodBought = need - remaining; foodMoneyShort = moneyShort;
                    }
                    else
                    {
                        comfortSum += (need - remaining) / need;
                    }
                    if (remaining > 0.0001f && !moneyShort) MarketShortageToday[i] += remaining;
                }

                // Oziq-ovqat yetmagan bo'lsa: pul yetmagan oila — yarim bepul ulush; bozor yetmagan bo'lsa —
                // ombordan to'g'ridan-to'g'ri (to'liq) ta'minot. Ikkalasi ham haqiqiy ombor zaxirasidan olinadi.
                float fed = foodBought;
                float missing = foodNeed - foodBought;
                if (missing > 0.0001f)
                {
                    float grant = foodMoneyShort ? missing * Config.RationShare : missing;
                    float got = DrawFood(res, grant);
                    fed += got;
                    Transactions.Add(foodMoneyShort ? TransactionType.Ration : TransactionType.Subsistence, got, 0);
                    if (foodMoneyShort) { hh.CouldNotAffordFood = true; HouseholdsCouldNotAfford++; }
                }
                hh.FedRatio = foodNeed > 0f ? Math.Clamp(fed / foodNeed, 0f, 1f) : 1f;
                hh.Comfort = Math.Clamp(comfortSum / 3f, 0f, 1f);
                if (hh.FedRatio < 0.99f) HouseholdsUnderfed++;
            }
        }

        private sealed class MarketDistanceComparer : IComparer<MarketState>
        {
            public int X, Y;
            public int Compare(MarketState a, MarketState b)
                => (Math.Abs(a.X - X) + Math.Abs(a.Y - Y)).CompareTo(Math.Abs(b.X - X) + Math.Abs(b.Y - Y));
        }

        /// <summary>Ombordan oziq-ovqat oladi (kasr qismi yig'ilib boradi). Haqiqatan olingan miqdorni qaytaradi.</summary>
        private float DrawFood(ResourceManager res, float amount)
        {
            if (amount <= 0f) return 0f;
            float have = res.Get(ResourceType.Food) - _foodDrawCarry;
            float got = Math.Min(amount, Math.Max(0f, have));
            if (got <= 0f) return 0f;
            _foodDrawCarry += got;
            int whole = (int)_foodDrawCarry;
            if (whole > 0)
            {
                res.Add(ResourceType.Food, -whole);
                _foodDrawCarry -= whole;
                _foodLeftWarehouseToday += whole;
            }
            return got;
        }

        // ------------------------------------------------------------------
        // Daromadlar
        // ------------------------------------------------------------------
        private void PayHousehold(PopulationManager pop, Citizen c, float amount, TransactionType type)
        {
            if (amount <= 0f || c == null) return;
            Household hh = pop.GetHousehold(c.HouseholdId);
            if (hh == null) return;
            hh.SharedMoney += amount; hh.LastIncome += amount;
            if (type == TransactionType.Wage) WagesPaidToday += amount; else StateWagesPaidToday += amount;
            Transactions.Add(type, 0, amount);
        }

        private void PayPool(PopulationManager pop, CitizenProfession profession, float pool)
        {
            if (pool <= 0f) return;
            int n = pop.CountProfession(profession);
            if (n <= 0) return;
            float each = pool / n;
            foreach (Citizen c in pop.Citizens)
                if (c.IsEmployed && c.Profession == profession) PayHousehold(pop, c, each, TransactionType.Wage);
        }

        private void PayMerchants(PopulationManager pop, ref float orphanPool)
        {
            foreach (MarketState m in Merchants.Markets)
            {
                float keep = Math.Max(Config.MarketWorkingCapital, m.WholesaleCostToday * 1.5f);
                float profit = m.Cash - keep;
                if (profit <= 0f) continue;
                if (m.MerchantIds.Count > 0)
                {
                    float each = profit / m.MerchantIds.Count;
                    foreach (int id in m.MerchantIds) PayHousehold(pop, pop.GetById(id), each, TransactionType.Wage);
                    m.Cash -= profit;
                }
                else if (m.Cash > keep * 3f)
                {
                    // Savdogarsiz bozorning ortiqcha foydasi — bozor haqi sifatida xazinaga.
                    float fee = m.Cash - keep * 3f;
                    m.Cash -= fee;
                    orphanPool += fee;
                }
            }
        }

        /// <summary>
        /// Davlat maoshlari. Summalar EconomyManager.ComputeExpense bilan AYNAN bir xil — xazinadan
        /// EconomyManager ayiradi, bu yerda esa o'sha pul oilalarga beriladi (ikki marta hisoblanmaydi).
        /// </summary>
        private void PayStateWages(PopulationManager pop, int treasury)
        {
            int soldiers = pop.CountProfession(CitizenProfession.Soqchi);
            int officials = pop.CountProfession(CitizenProfession.Amaldor);
            int teachers = pop.CountProfession(CitizenProfession.Muallim);
            int expense = EconomyManager.ComputeExpense(soldiers, officials, pop.Count);
            // Xazina bo'sh bo'lsa maosh ham shunga yarasha kamayadi (yo'q puldan maosh berilmaydi).
            float scale = expense <= 0 ? 0f : Math.Min(1f, treasury / (float)expense);
            if (scale <= 0f) return;

            float social = EconomyManager.ComputeSocialExpense(pop.Count) * scale;
            foreach (Citizen c in pop.Citizens)
            {
                if (!c.IsEmployed) continue;
                if (c.Profession == CitizenProfession.Soqchi) PayHousehold(pop, c, EconomyManager.SoldierWage * scale, TransactionType.StateWage);
                else if (c.Profession == CitizenProfession.Amaldor) PayHousehold(pop, c, EconomyManager.OfficialWage * scale, TransactionType.StateWage);
            }
            if (teachers > 0)
            {
                float each = Math.Min(Config.TeacherWage * scale, social / teachers);
                foreach (Citizen c in pop.Citizens)
                    if (c.IsEmployed && c.Profession == CitizenProfession.Muallim) { PayHousehold(pop, c, each, TransactionType.StateWage); social -= each; }
            }
            if (social <= 0.001f) return;

            // Qolgani — muhtoj (ishlovchisi yo'q) oilalarga yordam; bunday oila bo'lmasa hammaga teng.
            int needy = 0, members = 0;
            foreach (Household hh in pop.Households) { if (hh.MemberIds.Count > 0 && hh.WorkingMembers == 0) needy++; members += hh.MemberIds.Count; }
            foreach (Household hh in pop.Households)
            {
                if (hh.MemberIds.Count == 0) continue;
                float share;
                if (needy > 0) share = hh.WorkingMembers == 0 ? social / needy : 0f;
                else share = members > 0 ? social * hh.MemberIds.Count / members : 0f;
                if (share <= 0f) continue;
                hh.SharedMoney += share; hh.LastIncome += share;
                StateWagesPaidToday += share;
                Transactions.Add(TransactionType.Welfare, 0, share);
            }
        }

        private static int TakeWhole(ref float carry, float add)
        {
            carry += add;
            int whole = (int)carry;
            if (whole < 0) whole = 0;
            carry -= whole;
            return whole;
        }

        // ------------------------------------------------------------------
        // Narxlar
        // ------------------------------------------------------------------
        private void UpdatePrices(ResourceManager res)
        {
            foreach (ProductType p in BuyOrder)
            {
                int i = (int)p;
                float stock = WarehouseStock(p, res) + MarketStock(p);
                float supply = ProducedToday[i] + stock / Config.StockCoverDays;
                Prices.UpdateDaily(p, supply, DemandToday[i], stock);
            }
        }

        // ------------------------------------------------------------------
        // Voqealar — har biri haqiqiy sababdan kelib chiqadi
        // ------------------------------------------------------------------
        private void RaiseEvents(int day, BuildingManager b, PopulationManager pop, float cloth, float crafts)
        {
            SupplyDemandModel food = Prices.Get(ProductType.Food);
            float produced = ProducedToday[(int)ProductType.Food];

            if (_foodProductionAvg > 0f && produced >= _foodProductionAvg * 1.3f && produced >= pop.Count)
                Raise(day, "harvest", 15, true, "Hosil ko'paydi: bugun " + (int)produced + " birlik oziq-ovqat yetishtirildi (o'rtacha " + (int)_foodProductionAvg + ").");
            _foodProductionAvg = _foodProductionAvg <= 0f ? produced : _foodProductionAvg * 0.85f + produced * 0.15f;

            if (food.PriceRatio >= Config.HighPriceRatio && food.Trend > 0)
                Raise(day, "food_price_up", 10, false, "Oziq-ovqat narxi ko'tarildi: talab " + (int)food.Demand + ", taklif " + (int)food.Supply + ". Narx " + food.CurrentPrice.ToString("0.0") + " tanga.");
            else if (food.PriceRatio <= 0.7f && food.Trend < 0)
                Raise(day, "food_price_down", 15, true, "Oziq-ovqat arzonladi: omborlar to'la, narx " + food.CurrentPrice.ToString("0.0") + " tanga.");

            foreach (ProductType p in BuyOrder)
            {
                int i = (int)p;
                if (DemandToday[i] > 1f && MarketShortageToday[i] > DemandToday[i] * 0.25f)
                {
                    string name = Config.Get(p).NameUz;
                    string cause = Merchants.Markets.Count == 0 ? "shaharda bozor yo'q" : (MarketStock(p) < 0.5f ? "bozorlarda mol tugadi" : "bozorlar sig'imi yetmayapti");
                    Raise(day, "shortage_" + p, 8, false, name + " tanqisligi: " + cause + " (" + (int)MarketShortageToday[i] + " birlik talab qondirilmadi).");
                }
            }

            int marketCount = Merchants.Markets.Count;
            if (_lastMarketCount >= 0 && marketCount > _lastMarketCount)
                Raise(day, "market_opened", 0, true, "Yangi bozor ochildi. Shaharda endi " + marketCount + " ta bozor ishlaydi.");
            _lastMarketCount = marketCount;

            if (_lastJobCapacity >= 0 && pop.JobCapacity >= _lastJobCapacity + 3)
                Raise(day, "jobs_up", 0, true, "Ish o'rinlari ko'paydi: +" + (pop.JobCapacity - _lastJobCapacity) + " (jami " + pop.JobCapacity + ").");
            _lastJobCapacity = pop.JobCapacity;

            if (_importSinceCaravan >= 12f)
            {
                Raise(day, "caravan", 6, true, "Savdo karvoni yetib keldi: omborga mato va buyumlar tushirildi (" + (int)_importSinceCaravan + " birlik).");
                _importSinceCaravan = 0f;
            }
        }

        private void Raise(int day, string key, int cooldownDays, bool positive, string textUz)
        {
            if (_eventCooldown.TryGetValue(key, out int until) && day < until) return;
            _eventCooldown[key] = day + Math.Max(1, cooldownDays);
            var e = new EconomicEvent { Day = day, Key = key, TextUz = textUz, Positive = positive };
            Events.Add(e);
            while (Events.Count > 24) Events.RemoveAt(0);
            PendingEvent = e;
        }

        // ==================================================================
        //  DAVLAT SAVDOSI (hukmdor qarori bilan; chakana savdodan alohida tur)
        // ==================================================================

        /// <summary>Keyingi davlat savdosi uchun takrorlanmas operatsiya Id'si.</summary>
        public string NextStateTradeId() => "st:" + (++_stateTradeCounter);

        /// <summary>
        /// Davlat bozordan mol sotib oladi: xazina → bozor kassasi, mol bozor zaxirasidan omborga o'tadi.
        /// Bir xil transactionId ikki marta bajarilmaydi; mol yoki tanga yetmasa hech narsa o'zgarmaydi.
        /// </summary>
        public bool StateBuy(string transactionId, int day, MarketState market, ProductType product, int quantity,
                             ResourceManager res, out string messageUz)
        {
            messageUz = "";
            if (market == null || quantity <= 0) { messageUz = "Noto'g'ri savdo so'rovi"; return false; }
            int i = (int)product;
            float price = Prices.Price(product);
            int cost = (int)MathF.Ceiling(quantity * price);
            if (market.Stock[i] < quantity) { messageUz = "Bozorda yetarli mol yo'q"; return false; }
            if (!res.CanAfford(ResourceType.Coin, cost)) { messageUz = "Xazinada tanga yetarli emas (" + cost + ")"; return false; }
            if (!Transactions.TryBegin(transactionId)) { messageUz = "Bu savdo allaqachon bajarilgan"; return false; }

            res.Spend(ResourceType.Coin, cost);
            market.Stock[i] -= quantity;
            market.Cash += cost;
            if (product == ProductType.Food) res.Add(ResourceType.Food, quantity);
            else _warehouse[i] += quantity;
            string name = Config.Get(product).NameUz;
            Transactions.Log(transactionId, day, TransactionType.StateBuy, product, quantity, cost, "Davlat xaridi: " + quantity + " " + name);
            messageUz = "Sotib olindi: " + quantity + " " + name + " (-" + cost + " tanga)";
            return true;
        }

        /// <summary>
        /// Davlat ombordan bozorga mol sotadi: mol ombordan bozor zaxirasiga, ulgurji narx bozor kassasidan
        /// xazinaga o'tadi. Bozor kassasida pul yetmasa yoki omborda mol bo'lmasa bajarilmaydi.
        /// </summary>
        public bool StateSell(string transactionId, int day, MarketState market, ProductType product, int quantity,
                              ResourceManager res, out string messageUz)
        {
            messageUz = "";
            if (market == null || quantity <= 0) { messageUz = "Noto'g'ri savdo so'rovi"; return false; }
            int i = (int)product;
            int income = (int)MathF.Floor(quantity * Prices.Wholesale(product));
            if (WarehouseStock(product, res) < quantity) { messageUz = "Omborda yetarli mol yo'q"; return false; }
            if (market.Cash < income) { messageUz = "Bozor kassasida pul yetarli emas"; return false; }
            if (!Transactions.TryBegin(transactionId)) { messageUz = "Bu savdo allaqachon bajarilgan"; return false; }

            if (product == ProductType.Food) res.Add(ResourceType.Food, -quantity);
            else _warehouse[i] -= quantity;
            market.Stock[i] += quantity;
            market.Cash -= income;
            res.Add(ResourceType.Coin, income);
            string name = Config.Get(product).NameUz;
            Transactions.Log(transactionId, day, TransactionType.StateSell, product, quantity, income, "Davlat sotuvi: " + quantity + " " + name);
            messageUz = "Sotildi: " + quantity + " " + name + " (+" + income + " tanga)";
            return true;
        }

        // ==================================================================
        //  SAVE / LOAD
        // ==================================================================
        public AdvancedEconomySave Snapshot()
        {
            return new AdvancedEconomySave
            {
                LastProcessedDay = LastProcessedDay,
                Warehouse = (float[])_warehouse.Clone(),
                Carry = (float[])_carry.Clone(),
                FoodDrawCarry = _foodDrawCarry, TaxCarry = _taxCarry, PopTaxCarry = _popTaxCarry,
                FoodProductionAvg = _foodProductionAvg,
                LastMarketCount = _lastMarketCount, LastJobCapacity = _lastJobCapacity,
                ImportSinceCaravan = _importSinceCaravan,
                StateTradeCounter = _stateTradeCounter,
                Prices = Prices.Snapshot(),
                Markets = Merchants.Snapshot(),
                Events = new List<EconomicEvent>(Events),
                EventCooldowns = new Dictionary<string, int>(_eventCooldown),
                ProcessedTransactionIds = Transactions.ProcessedIds(),
                TradeHistory = new List<EconomyTransaction>(Transactions.History)
            };
        }

        public void Restore(AdvancedEconomySave s, BuildingManager b, PopulationManager pop)
        {
            if (s == null) return;
            LastProcessedDay = s.LastProcessedDay;
            if (s.Warehouse != null) Array.Copy(s.Warehouse, _warehouse, Math.Min(ProductCount, s.Warehouse.Length));
            if (s.Carry != null) Array.Copy(s.Carry, _carry, Math.Min(ProductCount, s.Carry.Length));
            _foodDrawCarry = s.FoodDrawCarry; _taxCarry = s.TaxCarry; _popTaxCarry = s.PopTaxCarry;
            _foodProductionAvg = s.FoodProductionAvg;
            _lastMarketCount = s.LastMarketCount; _lastJobCapacity = s.LastJobCapacity;
            _importSinceCaravan = s.ImportSinceCaravan;
            _stateTradeCounter = s.StateTradeCounter;
            Prices.Restore(s.Prices);
            Merchants.Restore(s.Markets);
            Merchants.Sync(b, pop);
            Events.Clear();
            if (s.Events != null) Events.AddRange(s.Events);
            _eventCooldown.Clear();
            if (s.EventCooldowns != null) foreach (var kv in s.EventCooldowns) _eventCooldown[kv.Key] = kv.Value;
            Transactions.Restore(s.ProcessedTransactionIds, s.TradeHistory);
            PendingEvent = null;
        }
    }
}
