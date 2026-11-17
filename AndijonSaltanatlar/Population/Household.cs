using System.Collections.Generic;

namespace AndijonSaltanatlar.Population
{
    /// <summary>
    /// Bir uyda yashaydigan oila. PROMPT 17: oila iqtisodiyoti maydonlari (daromad, xarajat, ta'minot)
    /// qo'shildi — ularni AdvancedEconomy.HouseholdEconomyManager har o'yin kunida yangilaydi.
    /// </summary>
    public sealed class Household
    {
        public int Id { get; set; }
        public string FamilyName { get; set; } = "";
        public List<int> MemberIds { get; } = new List<int>();
        public int? HomeBuildingId { get; set; }
        /// <summary>Oila jamg'armasi (tanga).</summary>
        public float SharedMoney { get; set; } = 40;
        public float FoodReserve { get; set; } = 10;
        public float Happiness { get; set; } = 70;
        public int HardshipDays { get; set; }
        public bool IsRevolting { get; set; }

        // --- Oila iqtisodiyoti (oxirgi o'yin kuni) ---
        /// <summary>Kunlik daromad: ish haqi, hosil haqi, savdo foydasi, davlat yordami.</summary>
        public float LastIncome { get; set; }
        /// <summary>Kunlik xarajat: bozordagi xarid va aholi solig'i.</summary>
        public float LastExpense { get; set; }
        /// <summary>Kunlik oziq-ovqat ehtiyoji (birlik).</summary>
        public float FoodNeed { get; set; }
        /// <summary>Oziq-ovqat bilan ta'minlanganlik: 1 = to'liq, 0.5 = yarim ulush, 0 = och.</summary>
        public float FedRatio { get; set; } = 1f;
        /// <summary>Oziq-ovqatdan boshqa mollar (meva, mato, buyum) ehtiyojining qondirilgan ulushi (0..1).</summary>
        public float Comfort { get; set; }
        /// <summary>Ishlaydigan a'zolar soni.</summary>
        public int WorkingMembers { get; set; }
        /// <summary>Bugun pul yetmagani uchun oziq-ovqatni to'liq sotib ololmadimi.</summary>
        public bool CouldNotAffordFood { get; set; }
    }
}
