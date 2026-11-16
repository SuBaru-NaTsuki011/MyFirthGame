using System;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>
    /// Yer tile'lari atlasi. Hamma tile bitta teksturada — butun xarita bitta draw-call bilan chiziladi.
    ///
    /// Atlas tuzilishi (katak 68x36, ichida 66x34 "semiz" romb — tikuv ko'rinmasligi uchun 1px ortiqcha):
    ///   qator 0: oddiy yer      qator 1: ko'cha        qator 2: maydon (g'isht)
    ///   qator 3: yashil hudud   qator 4-5: suv (2 kadr)
    ///   qator 6: ariq qirg'oqlari (4 tomon)
    ///   qator 7: [0] oq to'la romb, [1] romb konturi (highlight / ghost uchun)
    /// Content/Sprites/tiles.png fayli bo'lsa, shu tuzilishdagi haqiqiy atlas o'qiladi.
    /// </summary>
    public static class TileArt
    {
        public const int CellW = 68, CellH = 36;
        public const int Columns = 4, Rows = 8;
        public const int RowBanks = 6, RowOverlay = 7;
        public const int DrawOffsetX = -33, DrawOffsetY = -1;

        public static Rectangle Cell(int row, int col)
        {
            return new Rectangle(col * CellW + 1, row * CellH + 1, CellW - 2, CellH - 2);
        }

        public static Canvas BuildAtlas()
        {
            var c = new Canvas(CellW * Columns, CellH * Rows);
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Columns; col++)
                    DrawCell(c, row, col);
            return c;
        }

        private static float Frac(float v) => v - MathF.Floor(v);

        private static void DrawCell(Canvas c, int row, int col)
        {
            int ox = col * CellW + 1, oy = row * CellH + 1;
            const float e = 0.045f;
            for (int py = 0; py < 34; py++)
                for (int px = 0; px < 66; px++)
                {
                    float dx = (px + 0.5f - 33f) / 32f;
                    float dy = (py + 0.5f - 17f) / 16f;
                    float u = (dx + dy) * 0.5f + 0.5f;
                    float v = (dy - dx) * 0.5f + 0.5f;
                    if (u < -e || u > 1 + e || v < -e || v > 1 + e) continue;

                    float n = Canvas.Hash(px + col * 71 + row * 13, py + col * 37);
                    float n2 = Canvas.Hash(px * 3 + col * 19, py * 5 + row * 7 + 101);
                    Color? pix = Pixel(row, col, u, v, px, py, n, n2);
                    if (pix.HasValue) c.Blend(ox + px, oy + py, pix.Value);
                }
        }

        private static Color? Pixel(int row, int col, float u, float v, int px, int py, float n, float n2)
        {
            switch (row)
            {
                case 0: // oddiy yer (soz tuproq)
                {
                    Color b = Canvas.Shade(new Color(214, 188, 140), 0.95f + n * 0.10f);
                    if (n2 > 0.985f) b = Canvas.Shade(b, 0.84f);
                    if (col == 3 && n2 < 0.012f) b = new Color(150, 160, 96);
                    return b;
                }
                case 1: // ko'cha (shibbalangan yo'l)
                {
                    Color b = Canvas.Shade(new Color(170, 144, 110), 0.93f + n * 0.14f);
                    if (n2 > 0.968f) b = new Color(198, 186, 166);
                    else if (n2 < 0.03f) b = Canvas.Shade(b, 0.82f);
                    return b;
                }
                case 2: // markaziy maydon (pishiq g'isht to'shama)
                {
                    Color b = Canvas.Shade(new Color(208, 164, 118), 0.96f + n * 0.08f);
                    float fu = Frac(u * 4f), fv = Frac(v * 4f);
                    if ((((int)MathF.Floor(u * 4f + 8) + (int)MathF.Floor(v * 4f + 8)) & 1) == 0) b = Canvas.Shade(b, 0.93f);
                    if (fu < 0.08f || fv < 0.08f) b = Canvas.Shade(b, 0.80f);
                    if (col == 3)
                    {
                        // Feruza koshinli girih yulduzcha
                        float d = MathF.Abs(u - 0.5f) + MathF.Abs(v - 0.5f);
                        float d2 = MathF.Max(MathF.Abs(u - 0.5f), MathF.Abs(v - 0.5f));
                        if (d < 0.30f || d2 < 0.21f) b = d2 < 0.09f ? Pal.Gold : Canvas.Shade(Pal.Turquoise, 0.92f + n * 0.12f);
                    }
                    return b;
                }
                case 3: // yashil hudud
                {
                    Color b = Canvas.Shade(new Color(120, 160, 84), 0.92f + n * 0.16f);
                    if (n2 > 0.90f) b = Canvas.Shade(new Color(90, 136, 68), 0.95f + n * 0.1f);
                    if (col == 3 && n2 < 0.012f) b = new Color(240, 224, 124);
                    if (col == 2 && n2 < 0.008f) b = new Color(232, 150, 160);
                    return b;
                }
                case 4:
                case 5: // suv / ariq
                {
                    float phase = row == 4 ? 0f : 2.1f;
                    Color b = Canvas.Shade(Pal.Water, 0.96f + n * 0.08f);
                    float w = MathF.Sin(px * 0.33f + col * 1.9f + phase) * MathF.Sin(py * 0.85f + col + phase * 0.7f);
                    if (w > 0.72f) b = new Color(132, 198, 220);
                    else if (w < -0.80f) b = Canvas.Shade(b, 0.90f);
                    return b;
                }
                case RowBanks: // ariq qirg'og'i: 0: y-1 tomon, 1: x-1 tomon, 2: x+1 tomon, 3: y+1 tomon
                {
                    const float t = 0.17f;
                    float d = col switch { 0 => v, 1 => u, 2 => 1f - u, _ => 1f - v };
                    if (d > t) return null;
                    Color b = Canvas.Shade(new Color(160, 130, 92), 0.92f + n * 0.14f);
                    if (d > t - 0.045f) b = Canvas.Shade(b, 0.72f);
                    else if (n2 > 0.93f) b = new Color(104, 140, 76);
                    return b;
                }
                case RowOverlay:
                {
                    if (col == 0) return Color.White;
                    if (col == 1)
                    {
                        float d = MathF.Min(MathF.Min(u, v), MathF.Min(1f - u, 1f - v));
                        return d < 0.075f ? Color.White : (Color?)null;
                    }
                    return null;
                }
            }
            return null;
        }
    }
}
