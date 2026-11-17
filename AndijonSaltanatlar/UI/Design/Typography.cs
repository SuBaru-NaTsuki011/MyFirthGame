using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AndijonSaltanatlar.Core.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI.Design
{
    /// <summary>Tipografiya uslublari (PROMPT 19B, 12-bo'lim) — desktop o'lchamlari pikselda.</summary>
    public enum TextStyle
    {
        MainTitle,   // 36
        ScreenTitle, // 28
        PanelTitle,  // 20
        Body,        // 16
        Button,      // 16 (semibold)
        Secondary,   // 14
        Caption      // 12
    }

    /// <summary>
    /// Yagona tipografiya tizimi: Noto Sans (SIL OFL) TTF shriftlari FontStashSharp orqali — Windows va Android'da
    /// bir xil, aniq (piksel-perfekt atlas) va o'zbekcha belgilar (Oʻ, Gʻ, ʼ) bilan. Mobil qurilmada UiScale bo'yicha
    /// kattalashadi. Shrift fayli topilmasa eski piksel shriftga qaytadi (o'yin to'xtamaydi).
    /// </summary>
    public sealed class Typography
    {
        private FontSystem _regular, _semibold, _bold;
        private readonly PixelFont _fallback;
        private readonly Dictionary<(TextStyle, int), SpriteFontBase> _cache = new Dictionary<(TextStyle, int), SpriteFontBase>();

        /// <summary>UI masshtabi: desktop 1.0; telefonda ekran balandligi va zichligiga qarab 1.0..1.8.</summary>
        public float UiScale { get; set; } = 1f;
        public bool HasTrueType => _regular != null;

        /// <summary>
        /// O'yin uslubi: barcha menyu va panellar shahar HUD'idagi kabi piksel shrift bilan chiziladi
        /// (bir xil ko'rinish). false — Noto Sans TTF (eski "zamonaviy" ko'rinish).
        /// </summary>
        public bool PixelMode { get; set; } = true;

        private bool UsePixel => PixelMode || !HasTrueType;

        /// <summary>Piksel shrift masshtabi (butun son — harflar aniq, xira emas): matn 2x, sarlavhalar 3–5x.</summary>
        public int PixelScale(TextStyle s)
        {
            int pc = s == TextStyle.MainTitle ? 4 : s == TextStyle.ScreenTitle || s == TextStyle.PanelTitle ? 3 : 2;
            // Telefonda kattaroq, lekin HUD'dan oshib ketmasin (matn kenglikka sig'sin): masalan matn 3x, sarlavha 5x.
            float k = UiScale > 1.01f ? UiScale * 0.85f : 1f;
            return Math.Max(2, (int)Math.Round(pc * k));
        }

        /// <summary>
        /// Matnni to'rtburchak ichiga sig'diradi: avval shrift bir-ikki pog'ona kichrayadi (2x gacha), shunda ham sig'masa
        /// oxiri "..." bilan qisqartiriladi. align: 0 — chap, 1 — markaz, 2 — o'ng. Matn hech qachon chegaradan chiqmaydi.
        /// </summary>
        public void DrawFitted(SpriteBatch sb, string text, Rectangle r, TextStyle style, Color color, int align = 1)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!UsePixel)
            {
                string t = Fit(text, r.Width, style);
                Vector2 m0 = Measure(t, style);
                float x0 = align == 1 ? r.X + (r.Width - m0.X) / 2f : align == 2 ? r.Right - m0.X : r.X;
                Draw(sb, t, new Vector2(x0, r.Y + (r.Height - LineHeight(style)) / 2f), style, color);
                return;
            }
            text = Uz(text);
            int sc = PixelScale(style);
            while (sc > 2 && _fallback.Measure(text, sc, 1).X > r.Width) sc--;
            if (_fallback.Measure(text, sc, 1).X > r.Width)
            {
                string t = text;
                while (t.Length > 1 && _fallback.Measure(t + "..", sc, 1).X > r.Width) t = t.Substring(0, t.Length - 1);
                text = t.TrimEnd() + "..";
            }
            Vector2 m = _fallback.Measure(text, sc, 1);
            float x = align == 1 ? r.X + (r.Width - m.X) / 2f : align == 2 ? r.Right - m.X : r.X;
            float y = r.Y + (r.Height - m.Y) / 2f;
            _fallback.Draw(sb, text, new Vector2(MathF.Round(x), MathF.Round(y)), color, sc, 1);
        }

        public Typography(PixelFont fallback)
        {
            _fallback = fallback;
            try
            {
                _regular = Load("NotoSans-Regular.ttf");
                _semibold = Load("NotoSans-SemiBold.ttf") ?? _regular;
                _bold = Load("NotoSans-Bold.ttf") ?? _semibold;
            }
            catch
            {
                _regular = _semibold = _bold = null;
            }
        }

        private static FontSystem Load(string file)
        {
            byte[] data = null;
            try
            {
                // TitleContainer: Windows'da ilova papkasi, Android'da APK asset'lari.
                using Stream s = Microsoft.Xna.Framework.TitleContainer.OpenStream(Path.Combine("Content", "Fonts", file));
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                data = ms.ToArray();
            }
            catch
            {
                string p = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", file);
                if (File.Exists(p)) data = File.ReadAllBytes(p);
            }
            if (data == null) return null;
            var fs = new FontSystem(new FontSystemSettings { FontResolutionFactor = 1f, KernelWidth = 1, KernelHeight = 1 });
            fs.AddFont(data);
            return fs;
        }

        public static int BaseSize(TextStyle s)
        {
            switch (s)
            {
                case TextStyle.MainTitle: return 36;
                case TextStyle.ScreenTitle: return 28;
                case TextStyle.PanelTitle: return 20;
                case TextStyle.Body: return 16;
                case TextStyle.Button: return 16;
                case TextStyle.Secondary: return 14;
                default: return 12;
            }
        }

        public int Size(TextStyle s) => Math.Max(10, (int)Math.Round(BaseSize(s) * UiScale));

        private SpriteFontBase Font(TextStyle s)
        {
            int size = Size(s);
            if (_cache.TryGetValue((s, size), out SpriteFontBase f)) return f;
            FontSystem sys = s == TextStyle.MainTitle || s == TextStyle.ScreenTitle ? _bold
                           : s == TextStyle.Button || s == TextStyle.PanelTitle ? _semibold : _regular;
            f = sys.GetFont(size);
            _cache[(s, size)] = f;
            return f;
        }

        /// <summary>
        /// O'zbek lotin imlosini to'g'ri belgilarga o'giradi: o'/g' → oʻ/gʻ (U+02BB), tutuq belgisi ' → ʼ (U+02BC).
        /// Matn katalogda oddiy apostrof bilan yozilishi mumkin — ekranda to'g'ri ko'rinadi.
        /// </summary>
        public static string Uz(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('\'') < 0 && text.IndexOf('‘') < 0 && text.IndexOf('’') < 0) return text ?? "";
            var sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\'' || c == '‘' || c == '’' || c == '`')
                {
                    char prev = i > 0 ? text[i - 1] : ' ';
                    char next = i + 1 < text.Length ? text[i + 1] : ' ';
                    if (prev == 'o' || prev == 'O' || prev == 'g' || prev == 'G')
                        sb.Append('ʻ');                                  // oʻ, gʻ
                    else if (char.IsLetter(prev) && char.IsLetter(next))
                        sb.Append('ʼ');
                    else
                        sb.Append(c == '\'' ? '’' : c);
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public Vector2 Measure(string text, TextStyle style)
        {
            text = Uz(text);
            if (UsePixel) return _fallback.Measure(text, PixelScale(style), 1);
            return Font(style).MeasureString(text);
        }

        public float LineHeight(TextStyle style) => UsePixel ? PixelScale(style) * 10 : Font(style).LineHeight;

        public void Draw(SpriteBatch sb, string text, Vector2 pos, TextStyle style, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = Uz(text);
            pos = new Vector2(MathF.Round(pos.X), MathF.Round(pos.Y));
            if (UsePixel)
            {
                int sc = PixelScale(style);
                // Glif (7 qator) qator balandligi (10 qator) ichida vertikal markazda.
                _fallback.Draw(sb, text, new Vector2(pos.X, pos.Y + (int)(sc * 1.5f)), color, sc, 1);
                return;
            }
            Font(style).DrawText(sb, text, pos, color);
        }

        public void DrawCentered(SpriteBatch sb, string text, Rectangle r, TextStyle style, Color color)
        {
            Vector2 m = Measure(text, style);
            Draw(sb, text, new Vector2(r.X + (r.Width - m.X) / 2f, r.Y + (r.Height - LineHeight(style)) / 2f), style, color);
        }

        public void DrawRight(SpriteBatch sb, string text, Vector2 rightTop, TextStyle style, Color color)
        {
            Vector2 m = Measure(text, style);
            Draw(sb, text, new Vector2(rightTop.X - m.X, rightTop.Y), style, color);
        }

        /// <summary>Matnni kenglikka sig'diradi: sig'masa oxiri "…" bilan qisqartiriladi (kesilib qolmaydi).</summary>
        public string Fit(string text, float maxWidth, TextStyle style)
        {
            if (string.IsNullOrEmpty(text) || Measure(text, style).X <= maxWidth) return text ?? "";
            string t = text;
            while (t.Length > 1 && Measure(t + "…", style).X > maxWidth) t = t.Substring(0, t.Length - 1);
            return t.TrimEnd() + "…";
        }

        /// <summary>So'zlar bo'yicha qatorlarga bo'ladi (kichik ekranlarda wrapping).</summary>
        public List<string> Wrap(string text, float maxWidth, TextStyle style)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            foreach (string para in text.Split('\n'))
            {
                string cur = "";
                foreach (string w in para.Split(' '))
                {
                    string test = cur.Length == 0 ? w : cur + " " + w;
                    if (Measure(test, style).X > maxWidth && cur.Length > 0) { lines.Add(cur); cur = w; }
                    else cur = test;
                }
                lines.Add(cur);
            }
            return lines;
        }

        /// <summary>Matnni to'rtburchak ichida qatorlarga bo'lib chizadi; ishlatilgan balandlikni qaytaradi.</summary>
        public float DrawWrapped(SpriteBatch sb, string text, Rectangle r, TextStyle style, Color color, int maxLines = 99)
        {
            float lh = LineHeight(style) + 2, y = r.Y;
            List<string> lines = Wrap(text, r.Width, style);
            for (int i = 0; i < lines.Count && i < maxLines; i++)
            {
                string line = i == maxLines - 1 && lines.Count > maxLines ? Fit(lines[i] + " …", r.Width, style) : lines[i];
                Draw(sb, line, new Vector2(r.X, y), style, color);
                y += lh;
            }
            return y - r.Y;
        }
    }
}
