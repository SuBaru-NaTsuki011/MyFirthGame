using System;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.NPC
{
    /// <summary>
    /// Fuqaroning fe'l-atvori (0..100). Fuqaro Id'si va kasbidan deterministik hisoblanadi — saqlash shart emas,
    /// har doim bir xil chiqadi. Shaxsiyat faqat MULOQOT USLUBIGA ta'sir qiladi; iqtisodiy faktlarni o'zgartirmaydi.
    /// </summary>
    public sealed class NPCPersonality
    {
        public float Sociability { get; private set; }
        public float Optimism { get; private set; }
        public float Patience { get; private set; }
        public float Ambition { get; private set; }
        public float CommunityFocus { get; private set; }

        private static float Hash01(int id, int salt)
        {
            unchecked
            {
                uint h = (uint)(id * 374761393 + salt * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }

        public static NPCPersonality For(Citizen c)
        {
            float V(int salt, float bias) => Math.Clamp(30f + Hash01(c.Id, salt) * 40f + bias, 0f, 100f);
            // Kasbga xos moyilliklar.
            float soc = 0, opt = 0, pat = 0, amb = 0, com = 0;
            switch (c.Profession)
            {
                case CitizenProfession.Savdogar: soc = 22; amb = 20; pat = -6; break;
                case CitizenProfession.Bogbon: case CitizenProfession.Dehqon: pat = 20; com = 12; break;
                case CitizenProfession.Tashuvchi: case CitizenProfession.Hunarmand: amb = 16; soc = 6; break;
                case CitizenProfession.Muallim: opt = 18; pat = 22; com = 20; break;
                case CitizenProfession.Soqchi: pat = 8; com = 10; soc = -6; break;
                case CitizenProfession.Amaldor: pat = 14; soc = -4; com = 8; break;
                default: opt = -10; break; // ishsiz — biroz tushkun
            }
            return new NPCPersonality
            {
                Sociability = V(1, soc), Optimism = V(2, opt), Patience = V(3, pat),
                Ambition = V(4, amb), CommunityFocus = V(5, com)
            };
        }

        /// <summary>Eng yorqin xususiyat (profil kartasi uchun).</summary>
        public string DominantTraitUz
        {
            get
            {
                float best = Sociability; string name = "Dilkash";
                if (Optimism > best) { best = Optimism; name = "Umidvor"; }
                if (Patience > best) { best = Patience; name = "Sabrli"; }
                if (Ambition > best) { best = Ambition; name = "G'ayratli"; }
                if (CommunityFocus > best) { name = "El-yurt g'amida"; }
                return name;
            }
        }

        /// <summary>
        /// Matnga shaxsiyatga mos murojaat va yakun qo'shadi. Faktlar (raqamlar, muammo) o'zgarmaydi —
        /// faqat ohang: dilkash fuqaro iliq salomlashadi, sabrsiz qisqa gapiradi, umidvor yaxshi tilak bildiradi.
        /// </summary>
        public string Stylize(string text, bool isProblem, int seed)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string prefix = "";
            if (!text.StartsWith("Hazratim", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("Assalomu", StringComparison.OrdinalIgnoreCase))
            {
                if (Sociability >= 62f) prefix = (seed & 1) == 0 ? "Assalomu alaykum, hazratim! " : "Xush kelibsiz, hazratim! ";
                else if (Sociability < 42f) prefix = "Hazratim. ";
                else prefix = "Assalomu alaykum. ";
            }
            else if (Sociability >= 62f && !text.StartsWith("Assalomu", StringComparison.OrdinalIgnoreCase))
            {
                prefix = "Assalomu alaykum! ";
            }

            string suffix = "";
            if (isProblem)
            {
                if (Patience < 42f) suffix = " Iltimos, bu ishni kechiktirmang.";
                else if (Optimism >= 62f) suffix = " Siz albatta chorasini topasiz, deb ishonamiz.";
                else if (CommunityFocus >= 62f) suffix = " Bu faqat menga emas, butun mahallaga tegishli.";
            }
            else
            {
                if (Optimism >= 66f) suffix = " Xudo xohlasa, bundan ham yaxshi bo'ladi.";
                else if (Ambition >= 66f) suffix = " Hali qiladigan ishlarimiz ko'p.";
            }
            return prefix + text + suffix;
        }
    }
}
