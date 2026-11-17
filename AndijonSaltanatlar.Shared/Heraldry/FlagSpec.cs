using System;

namespace AndijonSaltanatlar.Shared.Heraldry
{
    /// <summary>
    /// Saltanat bayrog'i: naqsh (pattern), uchta rang va ramz (emblem). Matn ko'rinishida saqlanadi:
    /// "naqsh-rang1-rang2-rang3-ramz-ramzRangi" (masalan "1-2-0-0-2-6"). Server faqat to'g'ri qiymatlarni qabul qiladi.
    /// Rang namunasi (ColorIndexAt) shu yerda — o'yin, telefon va server bir xil bayroqni ko'radi.
    /// </summary>
    public sealed class FlagSpec
    {
        public const float Aspect = 1.5f; // eni / bo'yi

        public static readonly (string Name, byte R, byte G, byte B)[] Palette =
        {
            ("Oq", 236, 232, 220), ("Qora", 32, 28, 26), ("Qizil", 178, 36, 36), ("Ko'k", 36, 74, 150),
            ("Moviy", 70, 150, 205), ("Yashil", 38, 120, 62), ("Oltin", 214, 172, 62), ("Sariq", 236, 206, 80),
            ("To'q sariq", 214, 112, 40), ("Binafsha", 110, 52, 132), ("Jigarrang", 118, 72, 40), ("Kumush", 162, 162, 168),
            ("Pushti", 204, 96, 128), ("Zumrad", 28, 132, 120),
        };

        public static readonly string[] PatternNames =
        {
            "Yaxlit", "Ikki yo'l", "Uch yo'l", "Tik ikki", "Tik uch", "Qiya", "Xoch", "Hoshiya", "Uchburchak", "To'rt bo'lak", "Belbog'li"
        };

        public static readonly string[] EmblemNames =
        {
            "Ramzsiz", "Yulduz", "Hilol", "Hilol va yulduz", "Quyosh", "Qilich", "Qal'a", "Toj", "Olmos", "Doira"
        };

        public int Pattern { get; set; }
        public int C1 { get; set; }
        public int C2 { get; set; }
        public int C3 { get; set; }
        public int Emblem { get; set; }
        public int EmblemColor { get; set; }

        public FlagSpec Clone() => (FlagSpec)MemberwiseClone();

        public string Encode() => Pattern + "-" + C1 + "-" + C2 + "-" + C3 + "-" + Emblem + "-" + EmblemColor;
        public override string ToString() => Encode();

        public static bool TryParse(string s, out FlagSpec f)
        {
            f = null;
            if (string.IsNullOrWhiteSpace(s) || s.Length > 40) return false;
            string[] p = s.Trim().Split('-');
            if (p.Length != 6) return false;
            var v = new int[6];
            for (int i = 0; i < 6; i++) if (!int.TryParse(p[i], out v[i])) return false;
            int pc = Palette.Length;
            if (v[0] < 0 || v[0] >= PatternNames.Length || v[4] < 0 || v[4] >= EmblemNames.Length) return false;
            if (v[1] < 0 || v[1] >= pc || v[2] < 0 || v[2] >= pc || v[3] < 0 || v[3] >= pc || v[5] < 0 || v[5] >= pc) return false;
            f = new FlagSpec { Pattern = v[0], C1 = v[1], C2 = v[2], C3 = v[3], Emblem = v[4], EmblemColor = v[5] };
            return true;
        }

        /// <summary>Noto'g'ri/bo'sh bo'lsa — saltanat rangi asosidagi standart bayroq.</summary>
        public static FlagSpec ParseOr(string s, int kingdomId, int r, int g, int b)
            => TryParse(s, out FlagSpec f) ? f : ForKingdom(kingdomId, r, g, b);

        /// <summary>Yangi saltanat uchun standart bayroq: asosiy rang — saltanat rangiga eng yaqin rang.</summary>
        public static FlagSpec ForKingdom(int id, int r, int g, int b)
        {
            int main = Nearest(r, g, b);
            int[] patterns = { 1, 3, 5, 7, 9, 10, 2, 8 };
            int pattern = patterns[Math.Abs(id) % patterns.Length];
            int second = main == 0 ? 3 : 0;
            int emblem = 1 + Math.Abs(id * 7) % (EmblemNames.Length - 1);
            int ec = main == 6 || second == 6 ? 1 : 6;
            return new FlagSpec { Pattern = pattern, C1 = main, C2 = second, C3 = main, Emblem = emblem, EmblemColor = ec };
        }

        public static int Nearest(int r, int g, int b)
        {
            int best = 0, bd = int.MaxValue;
            for (int i = 0; i < Palette.Length; i++)
            {
                int dr = Palette[i].R - r, dg = Palette[i].G - g, db = Palette[i].B - b;
                int d = dr * dr * 3 + dg * dg * 4 + db * db * 2;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        // ------------------------------------------------------------------ Rang namunasi
        /// <summary>Bayroq nuqtasidagi rang (palitra indeksi). u — chapdan o'ngga 0..1, v — yuqoridan pastga 0..1.</summary>
        public int ColorIndexAt(double u, double v)
        {
            if (Emblem != 0)
            {
                (double cu, double cv) = EmblemCenter();
                double R = Pattern == 6 || Pattern == 9 ? 0.19 : Pattern == 8 ? 0.23 : 0.30;
                double ex = (u - cu) * Aspect / R, ey = (v - cv) / R;
                if (Math.Abs(ex) <= 1.05 && Math.Abs(ey) <= 1.05 && InEmblem(Emblem, ex, ey)) return EmblemColor;
            }
            return PatternAt(u, v);
        }

        private (double, double) EmblemCenter() => Pattern switch
        {
            8 => (0.17, 0.5),          // uchburchak ichida
            6 => (0.2, 0.25),          // xoch burchagida
            9 => (0.25, 0.25),         // yuqori chap bo'lak
            _ => (0.5, 0.5),
        };

        private int PatternAt(double u, double v)
        {
            switch (Pattern)
            {
                case 1: return v < 0.5 ? C1 : C2;
                case 2: return v < 1 / 3.0 ? C1 : v < 2 / 3.0 ? C2 : C3;
                case 3: return u < 0.5 ? C1 : C2;
                case 4: return u < 1 / 3.0 ? C1 : u < 2 / 3.0 ? C2 : C3;
                case 5: return u + v < 1.0 ? C1 : C2;
                case 6: return Math.Abs(v - 0.5) < 0.11 || Math.Abs(u - 0.4) < 0.075 ? C2 : C1;
                case 7: return u < 0.09 || u > 0.91 || v < 0.13 || v > 0.87 ? C2 : C1;
                case 8: return u < 0.42 * (1 - Math.Abs(2 * v - 1)) ? C2 : v < 0.5 ? C1 : C3;
                case 9: return (u < 0.5) ^ (v < 0.5) ? C2 : C1;
                case 10:
                    if (v < 0.27 || v > 0.73) return C1;
                    if (v < 0.32 || v > 0.68) return C3;
                    return C2;
                default: return C1;
            }
        }

        // ------------------------------------------------------------------ Ramzlar (ex, ey ∈ [-1, 1])
        private static readonly double[] StarX = new double[10], StarY = new double[10];

        static FlagSpec()
        {
            for (int k = 0; k < 10; k++)
            {
                double a = -Math.PI / 2 + k * Math.PI / 5, rad = k % 2 == 0 ? 1.0 : 0.42;
                StarX[k] = Math.Cos(a) * rad;
                StarY[k] = Math.Sin(a) * rad;
            }
        }

        private static bool InStar(double x, double y, double cx, double cy, double scale)
        {
            x = (x - cx) / scale; y = (y - cy) / scale;
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
                if ((StarY[i] > y) != (StarY[j] > y) && x < (StarX[j] - StarX[i]) * (y - StarY[i]) / (StarY[j] - StarY[i]) + StarX[i]) inside = !inside;
            return inside;
        }

        private static bool Circle(double x, double y, double cx, double cy, double r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) < r * r;

        private static bool Crescent(double x, double y, double cx, double s)
            => Circle(x, y, cx, 0, 0.9 * s) && !Circle(x, y, cx + 0.36 * s, 0, 0.76 * s);

        public static bool InEmblem(int emblem, double x, double y)
        {
            switch (emblem)
            {
                case 1: return InStar(x, y, 0, 0.08, 1.0);
                case 2: return Crescent(x, y, 0, 1.0);
                case 3: return Crescent(x, y, -0.3, 0.85) || InStar(x, y, 0.5, 0.05, 0.42);
                case 4:
                {
                    double r = Math.Sqrt(x * x + y * y);
                    if (r < 0.46) return true;
                    double th = Math.Atan2(y, x);
                    return r < 0.98 && r > 0.55 && Math.Cos(12 * th) > 0.45;
                }
                case 5:
                {
                    bool blade = Math.Abs(x) < 0.12 && y > -0.62 && y < 0.42;
                    bool tip = y >= -0.98 && y <= -0.62 && Math.Abs(x) < 0.12 * (y + 0.98) / 0.36;
                    bool guard = Math.Abs(y - 0.5) < 0.09 && Math.Abs(x) < 0.5;
                    bool grip = Math.Abs(x) < 0.08 && y > 0.5 && y < 0.82;
                    return blade || tip || guard || grip || Circle(x, y, 0, 0.9, 0.12);
                }
                case 6:
                {
                    bool body = Math.Abs(x) < 0.55 && y > -0.35 && y < 0.95;
                    bool merlon = y > -0.78 && y <= -0.35 && Math.Abs(x) < 0.65 && ((int)Math.Floor((x + 0.65) / 0.26)) % 2 == 0;
                    bool door = Math.Abs(x) < 0.17 && y > 0.42;
                    bool window = Math.Abs(x) < 0.08 && y > -0.15 && y < 0.1;
                    return (body || merlon) && !door && !window;
                }
                case 7:
                {
                    bool band = y > 0.25 && y < 0.65 && Math.Abs(x) < 0.8;
                    bool peaks = false;
                    foreach (double px in new[] { -0.6, 0.0, 0.6 })
                        if (y > -0.62 && y <= 0.25 && Math.Abs(x - px) < 0.26 * (y + 0.62) / 0.87 + 0.03) peaks = true;
                    bool balls = Circle(x, y, -0.6, -0.72, 0.12) || Circle(x, y, 0, -0.72, 0.12) || Circle(x, y, 0.6, -0.72, 0.12);
                    return band || peaks || balls;
                }
                case 8: return Math.Abs(x) * 1.15 + Math.Abs(y) < 0.95;
                case 9: return Circle(x, y, 0, 0, 0.72);
                default: return false;
            }
        }
    }
}
