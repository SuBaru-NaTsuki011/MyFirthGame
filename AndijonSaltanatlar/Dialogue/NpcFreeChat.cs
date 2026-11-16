using System;
using AndijonSaltanatlar.AdvancedEconomy;

namespace AndijonSaltanatlar.Dialogue
{
    public enum ChatIntent { Greeting, Problem, Work, Prices, Family, Home, Tax, City, Thanks, Unknown }

    /// <summary>
    /// Fuqaro bilan o'zbek tilida erkin savol-javob (PROMPT 17, 16-bo'lim — ixtiyoriy qism).
    /// Bu LLM EMAS: kalit so'zlar bo'yicha niyat aniqlanadi (intent classification) va javob shablonlardan,
    /// faqat suhbat kontekstidagi HAQIQIY qiymatlar bilan tuziladi. Hech qanday raqam to'qib chiqarilmaydi;
    /// tushunilmagan savolga xavfsiz umumiy javob qaytadi. Javob o'yin holatini o'zgartirmaydi.
    ///
    /// Kelajakda LLM ulansa: modelga faqat shu kontekst beriladi, javobi esa shu sinfdagi kabi faqat MATN
    /// bo'lib qoladi — iqtisodiyotga ta'sir qiladigan harakatlar DialogueActionExecutor buyruqlari orqali.
    /// </summary>
    public static class NpcFreeChat
    {
        public const int MaxQuestionLength = 60;

        public static ChatIntent Classify(string question)
        {
            string q = Normalize(question);
            if (q.Length == 0) return ChatIntent.Unknown;
            if (Has(q, "muammo", "qiyin", "tashvish", "shikoyat", "murojaat", "yetishm", "kerak", "ahvol")) return ChatIntent.Problem;
            if (Has(q, "narx", "qimmat", "arzon", "bozor", "savdo", "mol")) return ChatIntent.Prices;
            if (Has(q, "soliq")) return ChatIntent.Tax;
            if (Has(q, "ish", "kasb", "hunar", "maosh", "daromad")) return ChatIntent.Work;
            if (Has(q, "oila", "farzand", "bola", "xonadon", "jamg'arma", "pul")) return ChatIntent.Family;
            if (Has(q, "uy", "mahalla", "qayer", "yasha")) return ChatIntent.Home;
            if (Has(q, "shahar", "asaka", "aholi", "xalq")) return ChatIntent.City;
            if (Has(q, "rahmat", "tashakkur", "barakalla")) return ChatIntent.Thanks;
            if (Has(q, "salom", "assalom", "yaxshimi", "qalay")) return ChatIntent.Greeting;
            return ChatIntent.Unknown;
        }

        public static string Answer(string question, NpcDialogueSession s)
        {
            if (s == null) return "";
            DialogueContext c = s.Context;
            switch (Classify(question))
            {
                case ChatIntent.Greeting:
                    return c.Resolve("Va alaykum assalom, hazratim. Men {fullName}, {district}da yashayman.");
                case ChatIntent.Problem:
                    if (s.Profile.ActiveConcernIds.Count == 0)
                        return "Hozircha jiddiy tashvishimiz yo'q, hazratim. Shukr, kunimiz o'tib turibdi.";
                    return "Eng katta tashvishimiz - " + ConcernKeys.LabelUz(s.Profile.TopConcernId).ToLowerInvariant() + ". "
                           + ConcernDetail(s.Profile.TopConcernId, c);
                case ChatIntent.Work:
                    return s.Profile.IsEmployed
                        ? c.Resolve("Men {profession} bo'lib ishlayman, ish joyim - {workplace}.")
                        : c.Resolve("Hozir ishsizman, hazratim. Shaharda {unemployed} kishi ishsiz, bo'sh ish o'rni {freeJobs} ta.");
                case ChatIntent.Prices:
                    return c.Resolve("Bozorda oziq-ovqat hozir {foodPrice} tanga, hazratim. Bugun bozorlarda {customers} xaridor bo'ldi.");
                case ChatIntent.Tax:
                    return c.Resolve("Aholi solig'i hozir {tax} foiz, hazratim.");
                case ChatIntent.Family:
                    return c.Resolve("Oilamiz {members} kishi, ishlaydiganimiz {working} nafar. Jamg'armamiz {savings} tanga atrofida.");
                case ChatIntent.Home:
                    return c.Resolve("{district}da yashaymiz, hazratim.");
                case ChatIntent.City:
                    return c.Resolve("Shahrimizda {population} kishi yashaydi, {unemployed} nafari ishsiz. Omborda {foodStock} birlik oziq-ovqat bor.");
                case ChatIntent.Thanks:
                    return "Sizga ham rahmat, hazratim. Xalq g'amini yeganingiz uchun.";
                default:
                    // Xavfsiz zaxira javob: tushunilmagan savolga fakt aytilmaydi.
                    return "Uzr, hazratim, savolingizni yaxshi anglamadim. Ish, oila, narxlar yoki mahalla muammosi haqida so'rasangiz, bilganimni aytaman.";
            }
        }

        private static string ConcernDetail(string concern, DialogueContext c)
        {
            switch (concern)
            {
                case ConcernKeys.Unemployed: return c.Resolve("Shaharda {unemployed} kishi ishsiz, bo'sh ish o'rni {freeJobs} ta.");
                case ConcernKeys.Poverty: return c.Resolve("Oilamizda {members} jon, ishlaydiganimiz {working} nafar - nonga pul yetmayapti.");
                case ConcernKeys.FoodPrice: return c.Resolve("Oziq-ovqat {foodPrice} tangaga chiqdi.");
                case ConcernKeys.HighTax: return c.Resolve("Aholi solig'i {tax} foiz.");
                case ConcernKeys.NoRoad: return "Uyimiz ko'chaga ulanmagan.";
                case ConcernKeys.NoMarket: return "Mahallamizga yaqin bozor yo'q.";
                case ConcernKeys.Housing: return "Uyimiz tor, bo'sh uy yo'q.";
                case ConcernKeys.NoSchool: return "Shaharda madrasa yo'q.";
                case ConcernKeys.Security: return "Mahallada soqchi kam.";
                default: return "";
            }
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().ToLowerInvariant();
            if (s.Length > MaxQuestionLength) s = s.Substring(0, MaxQuestionLength);
            return s.Replace('ʻ', '\'').Replace('‘', '\'').Replace('’', '\'').Replace('`', '\'');
        }

        private static bool Has(string q, params string[] words)
        {
            foreach (string w in words) if (q.IndexOf(w, StringComparison.Ordinal) >= 0) return true;
            return false;
        }
    }
}
