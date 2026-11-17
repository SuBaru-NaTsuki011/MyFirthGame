using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Environment;

namespace AndijonSaltanatlar.Economy
{
    /// <summary>Fermerning xarid so'rovi (FERMER MUROJAATI).</summary>
    public sealed class FarmRequest
    {
        public string Id = "";
        public string Title = "";
        public string Body = "";
        public string Item = "";
        public int Amount;
        public int Cost;
    }

    /// <summary>
    /// Dehqonchilik ombori va fermerlar so'rovlari. Fermerlar ekish uchun urug' sarflaydi; urug', asbob, o'g'it yoki
    /// ariq kerak bo'lsa hukmdordan RUXSAT so'raydi — o'yin hech qachon o'zi sotib olmaydi.
    /// Bir vaqtda bitta so'rov; "Keyinroq" bosilsa shu so'rov 3 kun so'ralmaydi (spam yo'q).
    /// </summary>
    public sealed class FarmingManager
    {
        public const int SnoozeDays = 3;
        public const int MaxToolsLevel = 2;

        public int Seeds { get; set; } = 24;
        public int ToolsLevel { get; set; }
        /// <summary>O'g'itlangan hosillar soni (har hosil bittasini sarflaydi, +30%).</summary>
        public int FertilizerCharges { get; set; }
        public bool Irrigation { get; set; }
        /// <summary>Urug' yo'qligi sabab bugun ekilmay qolgan dalalar.</summary>
        public int IdleFields { get; private set; }
        public int HarvestedTotal { get; private set; }

        public FarmRequest Active { get; private set; }
        /// <summary>Yangi so'rov paydo bo'ldi — UI bir marta oyna ochadi.</summary>
        public bool PopupPending { get; set; }

        private readonly Dictionary<string, int> _snoozeUntil = new Dictionary<string, int>();
        private int _nextAskDay;

        public bool TryConsumeSeed()
        {
            if (Seeds <= 0) { IdleFields++; return false; }
            Seeds--;
            return true;
        }

        /// <summary>Hosil miqdori asbob, o'g'it va ariq bilan oshadi.</summary>
        public int ApplyYieldBonus(int harvest)
        {
            if (harvest <= 0) return 0;
            float k = 1f + 0.25f * ToolsLevel + (Irrigation ? 0.1f : 0f);
            if (FertilizerCharges > 0) { FertilizerCharges--; k += 0.3f; }
            int v = (int)Math.Round(harvest * k);
            HarvestedTotal += v;
            return v;
        }

        public void BeginDay() => IdleFields = 0;

        /// <summary>Kun oxirida: kerak bo'lsa yangi so'rov tayyorlaydi (bittadan ortiq emas).</summary>
        public void EndDay(int day, int farms, Season season)
        {
            if (Active != null || farms <= 0 || day < _nextAskDay) return;
            FarmRequest r = null;
            int seedNeed = Math.Max(10, farms * 3);
            if (Seeds < Math.Max(3, farms) && Allowed("seeds", day))
                r = new FarmRequest
                {
                    Id = "seeds", Item = "Urug'", Amount = seedNeed, Cost = seedNeed * 3,
                    Title = "URUG' TUGAYAPTI",
                    Body = "Hukmdorim, omborda " + Seeds + " qop urug' qoldi" + (IdleFields > 0 ? ", " + IdleFields + " ta dala bo'sh turibdi" : "") +
                           ". " + seedNeed + " qop urug' sotib olsak, barcha dalalarni eka olamiz.",
                };
            else if (season == Season.Bahor && FertilizerCharges == 0 && Allowed("fert", day))
                r = new FarmRequest
                {
                    Id = "fert", Item = "O'g'it", Amount = farms, Cost = Math.Max(60, farms * 15),
                    Title = "BAHORGI O'G'IT",
                    Body = "Bahor keldi. Dalalarga o'g'it solsak, keyingi " + farms + " ta hosil 30% ko'p bo'ladi.",
                };
            else if (ToolsLevel < MaxToolsLevel && farms >= 2 && Allowed("tools", day))
                r = new FarmRequest
                {
                    Id = "tools", Item = "Ketmon va o'roq", Amount = 1, Cost = 150 * (ToolsLevel + 1),
                    Title = "YANGI ASBOBLAR",
                    Body = "Ketmon va o'roqlarimiz eskirdi. Yangi asboblar bilan har bir hosil 25% ko'proq bo'ladi.",
                };
            else if (season == Season.Yoz && !Irrigation && Allowed("water", day))
                r = new FarmRequest
                {
                    Id = "water", Item = "Ariq tozalash", Amount = 1, Cost = 250,
                    Title = "ARIQLARNI TOZALASH",
                    Body = "Yoz issig'ida suv yetmayapti. Ariqlarni tozalasak, hosil doimiy 10% ko'payadi.",
                };
            if (r == null) return;
            Active = r;
            PopupPending = true;
        }

        private bool Allowed(string id, int day) => !_snoozeUntil.TryGetValue(id, out int until) || day >= until;

        /// <summary>SOTIB OLISH: tanga yetarli bo'lsa xarid qilinadi. Natija matni qaytariladi; tanga yetmasa so'rov qoladi.</summary>
        public bool Buy(ref int coins, int day, out string message)
        {
            FarmRequest r = Active;
            if (r == null) { message = "So'rov yo'q"; return false; }
            if (coins < r.Cost) { message = "Tanga yetarli emas: " + r.Cost + " kerak"; return false; }
            coins -= r.Cost;
            switch (r.Id)
            {
                case "seeds": Seeds += r.Amount; break;
                case "fert": FertilizerCharges += r.Amount; break;
                case "tools": ToolsLevel = Math.Min(MaxToolsLevel, ToolsLevel + 1); break;
                case "water": Irrigation = true; break;
            }
            message = r.Item + " sotib olindi (-" + r.Cost + "). Fermerlar minnatdor!";
            Active = null;
            PopupPending = false;
            _nextAskDay = day + 1;
            return true;
        }

        /// <summary>KEYINROQ: so'rov yopiladi va shu narsa 3 kun so'ralmaydi.</summary>
        public void Later(int day)
        {
            if (Active == null) return;
            _snoozeUntil[Active.Id] = day + SnoozeDays;
            Active = null;
            PopupPending = false;
            _nextAskDay = day + 1;
        }
    }
}
