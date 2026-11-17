using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Shared.Heraldry;
using AndijonSaltanatlar.UI.Design;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// Saltanat bayrog'ini piksel uslubida chizadi (FlagSpec namunasi kataklar bo'yicha). Hilpirash: har ustun
    /// sinus bo'ylab siljiydi (bayroq dastasiga yaqin tomoni qimirlamaydi). Bir xil rangli katakchalar birlashtiriladi.
    /// </summary>
    public static class FlagRenderer
    {
        private static readonly Dictionary<string, FlagSpec> Cache = new Dictionary<string, FlagSpec>();

        /// <summary>Matndan bayroq (kesh bilan). Bo'sh/yaroqsiz bo'lsa — rangga yaqin standart bayroq.</summary>
        public static FlagSpec Spec(string flag, Color fallback)
        {
            string key = flag ?? "";
            if (key.Length > 0 && Cache.TryGetValue(key, out FlagSpec f)) return f;
            if (!FlagSpec.TryParse(key, out f))
            {
                int c = FlagSpec.Nearest(fallback.R, fallback.G, fallback.B);
                f = new FlagSpec { Pattern = 0, C1 = c, C2 = c, C3 = c };
                return f;
            }
            if (Cache.Count > 256) Cache.Clear();
            Cache[key] = f;
            return f;
        }

        public static Color ToColor(int index)
        {
            var p = FlagSpec.Palette[Math.Clamp(index, 0, FlagSpec.Palette.Length - 1)];
            return new Color(p.R, p.G, p.B);
        }

        /// <summary>Bayroqni to'rtburchakka chizadi. cellsX — gorizontal kataklar soni (0 = o'lchamga qarab).</summary>
        public static void Draw(UiKit ui, Rectangle r, FlagSpec f, int cellsX = 0, float wave = 0f, float phase = 0f, float alpha = 1f, bool mirror = false)
        {
            if (f == null || r.Width < 2 || r.Height < 2) return;
            if (cellsX <= 0) cellsX = Math.Clamp(r.Width / 3, 6, 72);
            int cellsY = Math.Max(4, (int)Math.Round(cellsX / FlagSpec.Aspect));
            for (int ix = 0; ix < cellsX; ix++)
            {
                int x0 = r.X + ix * r.Width / cellsX, x1 = r.X + (ix + 1) * r.Width / cellsX;
                if (x1 <= x0) continue;
                // mirror: dasta o'ng tomonda (bayroq orqa tomondan ko'rinadi) — hilpirash dastadan uzoqlashgan sari kuchayadi
                float k = mirror ? 1f - (ix + 1) / (float)cellsX : ix / (float)cellsX;
                int dy = wave == 0f ? 0 : (int)Math.Round(MathF.Sin(phase - ix * 0.55f) * wave * k);
                float shade = wave == 0f ? 1f : 1f + 0.10f * MathF.Cos(phase - ix * 0.55f) * k;
                double u = mirror ? 1.0 - (ix + 0.5) / cellsX : (ix + 0.5) / cellsX;
                int runStart = 0, runColor = f.ColorIndexAt(u, 0.5 / cellsY);
                for (int iy = 1; iy <= cellsY; iy++)
                {
                    int c = iy < cellsY ? f.ColorIndexAt(u, (iy + 0.5) / cellsY) : -1;
                    if (c == runColor) continue;
                    int y0 = r.Y + runStart * r.Height / cellsY, y1 = r.Y + iy * r.Height / cellsY;
                    Color col = ToColor(runColor);
                    if (shade != 1f) col = new Color((int)Math.Min(255, col.R * shade), (int)Math.Min(255, col.G * shade), (int)Math.Min(255, col.B * shade));
                    ui.Fill(new Rectangle(x0, y0 + dy, x1 - x0, y1 - y0), col * alpha);
                    runStart = iy; runColor = c;
                }
            }
        }

        /// <summary>Ramkali bayroq (UI uchun): siyoh chegara + bayroq.</summary>
        public static void DrawFramed(UiKit ui, Rectangle r, FlagSpec f, Color border, int cellsX = 0)
        {
            ui.Fill(new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), border);
            Draw(ui, r, f, cellsX);
        }

        /// <summary>Dastali hilpirab turgan bayroqcha (xarita qasrlari, qo'shinlar, jang maydoni).</summary>
        public static void DrawOnPole(UiKit ui, Vector2 poleBase, int poleH, int flagW, FlagSpec f, float time, int seed, float alpha = 1f)
        {
            int flagH = Math.Max(4, (int)(flagW / FlagSpec.Aspect));
            int px = (int)poleBase.X, top = (int)poleBase.Y - poleH;
            ui.Fill(new Rectangle(px - 1, top, 2, poleH), new Color(70, 52, 34) * alpha);
            ui.Fill(new Rectangle(px - 2, top - 2, 4, 3), new Color(214, 172, 62) * alpha);
            int cells = Math.Clamp(flagW / 3, 5, 24);
            Draw(ui, new Rectangle(px + 1, top + 1, flagW, flagH), f, cells, Math.Max(1f, flagH * 0.12f), time * 4f + seed, alpha);
        }
    }
}
