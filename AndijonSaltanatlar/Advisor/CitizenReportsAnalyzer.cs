using System;
using System.Collections.Generic;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.CityPlanning;

namespace AndijonSaltanatlar.Advisor
{
    public enum CitizenReportStatus { Open, PlanProposed, Resolved }

    /// <summary>
    /// Bitta umumlashtirilgan masala: mahalla + muammo. Bir necha fuqaro bir xil muammoni aytsa,
    /// ular ALOHIDA murojaat emas, bitta masala sifatida yuritiladi.
    /// </summary>
    public sealed class CitizenReport
    {
        public string ConcernKey { get; set; } = "";
        public int DistrictId { get; set; }
        /// <summary>O'yinchi suhbatda Vazirga yuborgan fuqarolar.</summary>
        public List<int> ForwardedCitizenIds { get; set; } = new List<int>();
        /// <summary>Shu muammodan xavotirdagi fuqarolar soni (butun mahalla bo'yicha statistik hisob).</summary>
        public int AffectedCount { get; set; }
        public int DistrictPopulation { get; set; }
        public int FirstDay { get; set; }
        public int LastDay { get; set; }
        public CitizenReportStatus Status { get; set; } = CitizenReportStatus.Open;
        public int ResolvedDay { get; set; }

        public bool IsForwarded => ForwardedCitizenIds.Count > 0;

        /// <summary>
        /// Masalaning og'irligi (0..100). Faqat fuqarolar soniga emas, haqiqiy ehtiyojga qaraydi:
        /// muammo turi og'irligi * mahalla aholisining qancha ulushiga tegishi.
        /// </summary>
        public int Severity
        {
            get
            {
                float share = DistrictPopulation <= 0 ? 0.3f : Math.Clamp(AffectedCount / (float)DistrictPopulation, 0.05f, 1f);
                float s = ConcernKeys.Weight(ConcernKey) * (0.45f + 0.55f * share);
                if (IsForwarded) s += 6f; // hukmdor shaxsan eshitgan masala biroz muhimroq
                return (int)Math.Clamp(s, 0f, 100f);
            }
        }
    }

    /// <summary>
    /// Fuqarolar murojaatlarini umumlashtiradi. Manbalar:
    ///  1) mahallalar statistikasi — haqiqiy ehtiyojlar (DistrictStats.Needs), barcha fuqarolar bo'yicha;
    ///  2) o'yinchi suhbatda "Vazirga yetkaz" degan aniq murojaatlar.
    /// Muammo haqiqatan hal bo'lganda masala "hal qilindi" deb belgilanadi (bir necha kundan keyin ro'yxatdan chiqadi).
    /// </summary>
    public sealed class CitizenReportsAnalyzer
    {
        public const int KeepResolvedDays = 6;

        public List<CitizenReport> Reports { get; } = new List<CitizenReport>();

        public CitizenReport Find(string concern, int districtId)
        {
            foreach (CitizenReport r in Reports)
                if (r.ConcernKey == concern && r.DistrictId == districtId && r.Status != CitizenReportStatus.Resolved) return r;
            return null;
        }

        /// <summary>O'yinchi fuqaro murojaatini Vazirga yubordi. Bir fuqaro bitta masalada bir marta sanaladi.</summary>
        public CitizenReport Forward(int citizenId, string concern, int districtId, int day, int districtPopulation, int affected)
        {
            if (string.IsNullOrEmpty(concern)) return null;
            CitizenReport r = Find(concern, districtId);
            if (r == null)
            {
                r = new CitizenReport { ConcernKey = concern, DistrictId = districtId, FirstDay = day };
                Reports.Add(r);
            }
            if (!r.ForwardedCitizenIds.Contains(citizenId)) r.ForwardedCitizenIds.Add(citizenId);
            r.LastDay = day;
            r.DistrictPopulation = Math.Max(r.DistrictPopulation, districtPopulation);
            r.AffectedCount = Math.Max(Math.Max(r.AffectedCount, affected), r.ForwardedCitizenIds.Count);
            return r;
        }

        /// <summary>Kunlik: statistik masalalarni yangilaydi va hal bo'lganlarini yopadi.</summary>
        public void Refresh(DistrictEconomyManager districts, int day)
        {
            foreach (DistrictStats d in districts.Districts)
            {
                if (d.Population == 0) continue;
                foreach (string need in d.Needs)
                {
                    CitizenReport r = Find(need, d.Id);
                    if (r == null)
                    {
                        r = new CitizenReport { ConcernKey = need, DistrictId = d.Id, FirstDay = day };
                        Reports.Add(r);
                    }
                    r.AffectedCount = Math.Max(d.CountOf(need), r.ForwardedCitizenIds.Count);
                    r.DistrictPopulation = d.Population;
                    r.LastDay = day;
                }
            }

            foreach (CitizenReport r in Reports)
            {
                if (r.Status == CitizenReportStatus.Resolved) continue;
                DistrictStats d = districts.Get(r.DistrictId);
                bool stillNeeded = d != null && d.Needs.Contains(r.ConcernKey);
                // Faqat yuborilgan (statistikada yo'q) masala ham bo'lishi mumkin — u fuqarolarning o'zi hal bo'lganda yopiladi.
                if (!stillNeeded && (!r.IsForwarded || r.LastDay < day))
                {
                    r.Status = CitizenReportStatus.Resolved;
                    r.ResolvedDay = day;
                }
            }
            Reports.RemoveAll(r => r.Status == CitizenReportStatus.Resolved && day - r.ResolvedDay > KeepResolvedDays);
        }

        /// <summary>Faqat yuborilgan masala hali dolzarbligini bildiradi (fuqaro muammosi davom etyapti).</summary>
        public void Touch(string concern, int districtId, int day)
        {
            CitizenReport r = Find(concern, districtId);
            if (r != null) r.LastDay = day;
        }

        /// <summary>Ochiq masalalar, og'irligi bo'yicha saralangan.</summary>
        public List<CitizenReport> OpenReports()
        {
            var list = new List<CitizenReport>();
            foreach (CitizenReport r in Reports) if (r.Status != CitizenReportStatus.Resolved) list.Add(r);
            list.Sort((a, b) => b.Severity.CompareTo(a.Severity));
            return list;
        }

        public CitizenReport Top()
        {
            List<CitizenReport> open = OpenReports();
            return open.Count > 0 ? open[0] : null;
        }

        public int OpenCount
        {
            get { int n = 0; foreach (CitizenReport r in Reports) if (r.Status != CitizenReportStatus.Resolved) n++; return n; }
        }
    }

    /// <summary>
    /// Donishmand Vazir ↔ fuqaro murojaatlari: umumlashtirilgan matn, tavsiya etiladigan reja turi va
    /// AdvisorManager uchun xabarlar. Reja faqat TAVSIYA — qurilish har doim o'yinchi tasdig'ini talab qiladi.
    /// </summary>
    public static class AdvisorNPCIntegration
    {
        public const int ActionOpenReports = 6;

        /// <summary>Muammoga mos mahalla rejasi turi (null = mavjud mahallani yaxshilash rejasi).</summary>
        public static DistrictType? RecommendedPlan(string concern)
        {
            switch (concern)
            {
                case ConcernKeys.Unemployed: return DistrictType.Craft;
                case ConcernKeys.Poverty: return DistrictType.Craft;
                case ConcernKeys.NoMarket: return DistrictType.Commercial;
                case ConcernKeys.Housing: return DistrictType.Residential;
                case ConcernKeys.FoodPrice: return DistrictType.Agricultural;
                case ConcernKeys.NoSchool: return DistrictType.Cultural;
                default: return null; // yo'l, xavfsizlik, soliq — mavjud mahallani yaxshilash yoki siyosat
            }
        }

        public static string RecommendationUz(string concern)
        {
            switch (concern)
            {
                case ConcernKeys.Unemployed: return "Men ushbu hudud uchun yangi ustaxona va bozor rejasini tayyorlashni tavsiya qilaman.";
                case ConcernKeys.Poverty: return "Ish o'rinlari ko'paysa, oilalar daromadi tiklanadi. Hunarmandchilik rejasini tavsiya qilaman.";
                case ConcernKeys.NoMarket: return "Yangi bozor qurilsa, savdo jonlanadi va xazinaga tushum ortadi.";
                case ConcernKeys.Housing: return "Yangi turar joy mahallasi rejasini tayyorlashni tavsiya qilaman.";
                case ConcernKeys.FoodPrice: return "Bog'larni ko'paytirsak, taklif ortib narx pasayadi.";
                case ConcernKeys.NoSchool: return "Madrasa qurilishi ta'limni yo'lga qo'yadi. Avval tegishli texnologiya ochilishi kerak bo'lishi mumkin.";
                case ConcernKeys.NoRoad: return "Uylarni mavjud ko'chalarga ulaydigan yo'l rejasini tuzib beraman.";
                case ConcernKeys.HighTax: return "Aholi solig'ini kamaytirishni tavsiya qilaman (DAVLAT bo'limi).";
                case ConcernKeys.Security: return "Mahallaga qo'riqlash minorasi qurishni tavsiya qilaman.";
                default: return "Vaziyatni kuzatib boramiz.";
            }
        }

        private static string ProblemPhraseUz(string concern)
        {
            switch (concern)
            {
                case ConcernKeys.Unemployed: return "ish o'rinlari yetishmasligidan";
                case ConcernKeys.Poverty: return "ro'zg'origa pul yetmayotganidan";
                case ConcernKeys.NoMarket: return "bozor yo'qligidan";
                case ConcernKeys.Housing: return "uy-joy torligidan";
                case ConcernKeys.FoodPrice: return "oziq-ovqat qimmatlaganidan";
                case ConcernKeys.NoSchool: return "maktab yo'qligidan";
                case ConcernKeys.NoRoad: return "uylari ko'chaga ulanmaganidan";
                case ConcernKeys.HighTax: return "soliq og'irligidan";
                case ConcernKeys.Security: return "xavfsizlik sustligidan";
                default: return "ahvolidan";
            }
        }

        /// <summary>Vazirning umumlashtirilgan hisoboti (bitta masala bo'yicha).</summary>
        public static string SummaryUz(CitizenReport r, string districtName)
        {
            if (r == null) return "Hazratim, hozircha fuqarolardan jiddiy murojaat yo'q.";
            return "Hazratim, " + districtName + "dagi " + Math.Max(1, r.AffectedCount) + " fuqaro " + ProblemPhraseUz(r.ConcernKey)
                   + " xavotirda. " + RecommendationUz(r.ConcernKey);
        }

        /// <summary>AdvisorManager ro'yxatiga qo'shiladigan xabarlar (eng og'ir 2 ta masala).</summary>
        public static List<AdvisorMessage> BuildMessages(CitizenReportsAnalyzer reports, Func<int, string> districtName)
        {
            var list = new List<AdvisorMessage>();
            if (reports == null) return list;
            List<CitizenReport> open = reports.OpenReports();
            for (int i = 0; i < open.Count && i < 2; i++)
            {
                CitizenReport r = open[i];
                list.Add(new AdvisorMessage
                {
                    Category = r.Severity >= 70 ? AdvisorCategory.Ogohlantirish : AdvisorCategory.Tavsiya,
                    Title = "Fuqarolar murojaati: " + ConcernKeys.LabelUz(r.ConcernKey),
                    Body = SummaryUz(r, districtName != null ? districtName(r.DistrictId) : ""),
                    ActionLabel = "Murojaatlarni o'qish",
                    ActionId = ActionOpenReports,
                    // 90+ (shoshilinch) darajasiga chiqmaydi: mavjud favqulodda ogohlantirishlar ustun qoladi.
                    Priority = Math.Min(88, 40 + r.Severity / 2)
                });
            }
            return list;
        }
    }
}
