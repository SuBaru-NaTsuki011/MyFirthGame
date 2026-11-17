using System;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>
    /// Har qanday binoning yuqori daraja ko'rinishi (asosiy rasm ustiga qo'shiladi), shuning uchun har bir bino
    /// 5-darajagacha kattalashib, hashamatlashib boradi:
    ///   2 — old tomonda chiroqlar; 3 — burchaklarda bayroqlar;
    ///   4 — tosh poydevor (bino balandroq ko'tariladi) va ikki burchak minorasi;
    ///   5 — oq-oltin poydevor, oltin gumbazli minoralar va saltanat bayroqlari.
    /// 1x1 binolar (devor, darvoza) uchun — tepasida bayroqcha.
    /// </summary>
    public static class LevelArt
    {
        private static readonly Color Outline = new Color(44, 28, 20);
        private static readonly Color LampLight = new Color(255, 214, 120);

        public static Canvas Compose(Canvas baseArt, int size, int level)
        {
            if (level <= 1 || baseArt == null) return baseArt;
            int tileH = size * GameConfig.TileHeight;
            int baseH = Math.Max(0, baseArt.Height - tileH);
            bool big = size >= 2;
            int podium = big ? (level >= 5 ? 10 : level >= 4 ? 7 : 0) : 0;
            float towerH = big && level >= 4 ? Math.Max(baseH * 0.85f, 46f) + (level >= 5 ? 16f : 6f) : 0f;
            float top = Math.Max(baseH, towerH + (level >= 5 ? 26f : 10f)) + podium;
            int H = (int)Math.Ceiling(top) + (level >= 3 ? 24 : 10);
            int dy = H - podium - baseH; // asosiy rasm poydevor ustiga ko'tariladi

            // 1) Poydevor (4-5 daraja)
            var back = new IsoCanvas(size, H);
            if (podium > 0)
            {
                Color stone = level >= 5 ? Pal.Plaster : Pal.Stone;
                Color trim = level >= 5 ? Pal.Gold : Pal.Turquoise;
                back.Box(0.02f, 0.02f, size - 0.02f, size - 0.02f, 0, podium, stone, 0, Canvas.Shade(stone, 1.06f));
                back.QuadSW(0.02f, size - 0.02f, size - 0.02f, podium - 2, podium, trim);
                back.QuadSE(size - 0.02f, 0.02f, size - 0.02f, podium - 2, podium, Canvas.Shade(trim, 0.78f));
                back.C.Outline(Outline);
            }
            Canvas result = back.C;
            Blit(result, baseArt, 0, dy);

            // 2) Old bezaklar (bino ustidan chiziladi)
            var front = new IsoCanvas(size, H);
            float z0 = podium;
            if (!big)
            {
                if (level >= 4)
                {
                    front.Pole(0.5f, 0.5f, baseH - 6, baseH + 12, Pal.Wood, 1);
                    front.Flag(0.5f, 0.5f, baseH + 12, level >= 5 ? Pal.Gold : Pal.Red, 8, 5);
                }
                else if (level >= 2)
                {
                    front.Box(0.42f, 0.42f, 0.58f, 0.58f, baseH - 4, baseH, level >= 3 ? Pal.Gold : Pal.Turquoise);
                }
            }
            else
            {
                float e = 0.17f;
                var corners = new[] { new Vector2(e, size - e), new Vector2(size - e, e) };
                if (level >= 4)
                {
                    float r = size >= 4 ? 7f : 5f;
                    float t = z0 + towerH;
                    foreach (Vector2 c in corners)
                    {
                        front.Cylinder(c.X, c.Y, z0, t - 10, r, level >= 5 ? Pal.Plaster : Pal.BrickPale, 2);
                        front.Band(c.X, c.Y, z0 + towerH * 0.45f, z0 + towerH * 0.45f + 3, r + 0.5f, Pal.Turquoise);
                        front.Band(c.X, c.Y, t - 14, t - 10, r + 1.5f, level >= 5 ? Pal.Gold : Pal.Turquoise);
                        if (level >= 5) front.Dome(c.X, c.Y, t - 10, r + 1f, 11, Pal.Gold, 0, true);
                        else front.Cone(c.X, c.Y, t - 10, r + 1f, 11, Pal.Turquoise);
                    }
                    if (level >= 5)
                        for (int i = 0; i < corners.Length; i++)
                        {
                            Vector2 c = corners[i];
                            front.Pole(c.X, c.Y, t + 1, t + 16, Pal.Wood, 1);
                            front.Flag(c.X, c.Y, t + 16, i == 0 ? Pal.Red : Pal.Gold, 11, 7);
                        }
                }
                else if (level >= 3)
                {
                    foreach (Vector2 c in corners)
                    {
                        front.Pole(c.X, c.Y, z0, z0 + 30, Pal.Wood, 1);
                        front.Flag(c.X, c.Y, z0 + 30, Pal.Red, 10, 7);
                    }
                }
                // Chiroqlar (2+ daraja): old tomonda, ikki yonda
                var lamps = new[] { new Vector2(size * 0.3f, size - 0.07f), new Vector2(size - 0.07f, size * 0.3f) };
                foreach (Vector2 c in lamps)
                {
                    front.Pole(c.X, c.Y, z0, z0 + 12, Pal.Wood, 1);
                    front.Box(c.X - 0.07f, c.Y - 0.07f, c.X + 0.07f, c.Y + 0.07f, z0 + 12, z0 + 18, LampLight, 0, Pal.Gold, 0f);
                }
            }
            front.C.Outline(Outline);
            Blit(result, front.C, 0, 0);
            return result;
        }

        /// <summary>src ni dst ga (ox, oy) nuqtadan alfa bilan qo'yadi.</summary>
        public static void Blit(Canvas dst, Canvas src, int ox, int oy)
        {
            for (int y = 0; y < src.Height; y++)
            {
                int ty = y + oy;
                if (ty < 0 || ty >= dst.Height) continue;
                for (int x = 0; x < src.Width; x++)
                {
                    int tx = x + ox;
                    if (tx < 0 || tx >= dst.Width) continue;
                    Color c = src.Pixels[y * src.Width + x];
                    if (c.A == 0) continue;
                    dst.Blend(tx, ty, c);
                }
            }
        }
    }
}
