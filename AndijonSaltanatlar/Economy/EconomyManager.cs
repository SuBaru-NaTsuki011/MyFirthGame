using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Government;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.Economy
{
    /// <summary>
    /// Bir o'yin kunidagi chakana (bozor) iqtisodiyoti natijasi — AdvancedEconomyManager hisoblaydi va
    /// EconomyManager'ga uzatadi. Shu orqali oziq-ovqat va soliq FAQAT bitta joyda hisobga olinadi.
    /// </summary>
    public sealed class RetailDayResult
    {
        /// <summary>Bugun ishlab chiqarilib omborga tushgan oziq-ovqat (allaqachon resursga qo'shilgan).</summary>
        public int FoodProduced;
        /// <summary>Bugun ombordan chiqib iste'mol qilingan oziq-ovqat (allaqachon resursdan ayirilgan).</summary>
        public int FoodConsumed;
        /// <summary>Oilalardan haqiqatan undirilgan aholi solig'i (tanga).</summary>
        public int PopTaxCollected;
        /// <summary>Chakana savdodan olingan savdo solig'i (tanga) — xazinaga shu yerda bir marta qo'shiladi.</summary>
        public int RetailTax;
    }

    /// <summary>
    /// Shahar iqtisodiyoti (davlat darajasi). Har o'yin kunida bir marta hisoblanadi (GameStateManager
    /// chaqiradi): tashqi savdo hajmi, soliqlar va davlat xarajatlari. Resurslar manfiy bo'lmaydi.
    /// Daromad/xarajat bitta joyda — ikki marta qo'shilmaydi.
    ///
    /// PROMPT 17: oziq-ovqat zanjiri (ishlab chiqarish → ombor → bozor → xaridor) va aholi solig'ini
    /// endi AdvancedEconomyManager yuritadi va natijani <see cref="RetailDayResult"/> orqali beradi.
    /// retail = null bo'lsa eski (soddalashtirilgan) hisob ishlaydi.
    /// </summary>
    public sealed class EconomyManager
    {
        public const int SoldierWage = 3;
        public const int OfficialWage = 2;

        private readonly ResourceManager _res;

        public int LastIncome { get; private set; }
        public int LastExpense { get; private set; }
        public int LastFoodProduced { get; private set; }
        public int LastFoodConsumed { get; private set; }
        public int LastProduction { get; private set; }

        // --- Daromad tarkibi (iqtisodiyot paneli uchun) ---
        public int LastPopTax { get; private set; }
        public int LastTradeTax { get; private set; }
        public int LastProdTax { get; private set; }
        public int LastRetailTax { get; private set; }
        public int LastTradeVolume { get; private set; }

        public EconomyManager(ResourceManager res)
        {
            _res = res;
        }

        /// <summary>Kunlik oziq-ovqat ishlab chiqarish formulasi (yagona manba).</summary>
        public static int ComputeFoodProduction(int farmers, int gardens, float foodBonus)
        {
            int v = (int)((farmers * 5 + gardens * 2 + 3) * (1f + foodBonus));
            return v < 0 ? 0 : v;
        }

        /// <summary>Davlatning ijtimoiy xarajati (muallimlar maoshi va muhtoj oilalarga yordam).</summary>
        public static int ComputeSocialExpense(int population) => population / 8 + 2;

        /// <summary>Kunlik davlat xarajati (yagona manba): soqchi va amaldor maoshi + ijtimoiy xarajat.</summary>
        public static int ComputeExpense(int soldiers, int officials, int population)
            => soldiers * SoldierWage + officials * OfficialWage + ComputeSocialExpense(population);

        public void DailyTick(BuildingManager b, PopulationManager pop, GovernmentPolicy policy,
                              float foodBonus, float tradeBonus, float taxBonus, RetailDayResult retail = null)
        {
            int population = pop.Count;
            int farmers = pop.CountProfession(CitizenProfession.Bogbon);
            int traders = pop.CountProfession(CitizenProfession.Savdogar) + pop.CountProfession(CitizenProfession.Tashuvchi);
            int soldiers = pop.CountProfession(CitizenProfession.Soqchi);
            int officials = pop.CountProfession(CitizenProfession.Amaldor);

            // --- Oziq-ovqat ---
            int foodProduced, foodConsumed;
            if (retail != null)
            {
                // Bozor zanjiri allaqachon omborga qo'shgan/ayirgan — bu yerda faqat hisobot.
                foodProduced = retail.FoodProduced;
                foodConsumed = retail.FoodConsumed;
            }
            else
            {
                foodProduced = ComputeFoodProduction(farmers, b.Count(BuildingType.Garden), foodBonus);
                foodConsumed = population;
                _res.Add(ResourceType.Food, foodProduced - foodConsumed);
            }
            LastFoodProduced = foodProduced;
            LastFoodConsumed = foodConsumed;

            // --- Tashqi (davlat darajasidagi) savdo hajmi va umumiy ishlab chiqarish ---
            int tradeVolume = (int)((traders * 8 + b.Count(BuildingType.Market) * 6 + b.Count(BuildingType.Caravanserai) * 5) * (1f + tradeBonus));
            LastTradeVolume = tradeVolume;
            LastProduction = foodProduced + tradeVolume;

            // --- Soliqlar (foizda) + boshqaruv texnologiyalari bonusi ---
            float pt = policy.PopulationTax / 100f, tt = policy.TradeTax / 100f, prt = policy.ProductionTax / 100f;
            int popTax = retail != null ? retail.PopTaxCollected : (int)(population * 2 * pt);
            int tradeTax = (int)(tradeVolume * tt);
            int prodTax = (int)(LastProduction * prt);
            int retailTax = retail != null ? retail.RetailTax : 0;
            LastPopTax = popTax;
            LastTradeTax = tradeTax;
            LastProdTax = prodTax;
            LastRetailTax = retailTax;

            int income = (int)((popTax + tradeTax + prodTax + tradeVolume / 3) * (1f + taxBonus)) + retailTax;
            int expense = ComputeExpense(soldiers, officials, population);

            LastIncome = income;
            LastExpense = expense;
            _res.Add(ResourceType.Coin, income - expense);
        }
    }
}
