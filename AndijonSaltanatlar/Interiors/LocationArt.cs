using System;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Environment;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Interiors
{
    /// <summary>
    /// Ochiq hududlar (bog', ferma, bozor, mashg'ulot maydoni) va yangi mebel turlari uchun
    /// kodda chiziladigan sprite'lar. O'yinning qolgan grafikasi kabi — tashqi asset talab qilmaydi.
    /// </summary>
    public static class LocationArt
    {
        private static readonly Color OutlineCol = new Color(40, 28, 20);

        /// <summary>Ochiq hudud zamini: yo'l, chekka toshlar va (qishda) qor dog'lari.</summary>
        public static Canvas OpenShell(Color ground, out Vector2 origin, Color? path = null, bool snow = false)
        {
            int n = InteriorArt.N;
            var ic = new IsoCanvas(n, 24);
            origin = ic.P(0, 0, 0);
            ic.Top(0, 0, n, n, 0, ground, 0.08f);
            Color p = path ?? Canvas.Shade(ground, 0.86f);
            // markaziy yo'l (shimoldan janubga) va hovli
            ic.Top(3.4f, 0f, 4.6f, n, 0.2f, p, 0.03f);
            ic.Top(0f, 3.4f, n, 4.6f, 0.2f, p, 0.03f);
            // chekka toshlar
            Color edge = Canvas.Shade(ground, 0.65f);
            for (int i = 0; i < n; i++)
            {
                ic.Top(i, 0, i + 0.12f, n, 1.5f, edge, 0f);
                ic.Top(0, i, n, i + 0.12f, 1.5f, edge, 0f);
            }
            if (snow)
            {
                for (int i = 0; i < 14; i++)
                {
                    float x = 0.4f + (i * 1.37f) % (n - 0.8f), y = 0.4f + (i * 2.11f) % (n - 0.8f);
                    ic.Top(x, y, x + 0.7f, y + 0.5f, 1.6f, new Color(244, 248, 252), 0.02f);
                }
            }
            ic.C.Outline(OutlineCol, 0.25f);
            return ic.C;
        }

        public static Canvas Tree(Color leaf, bool snowy)
        {
            var ic = new IsoCanvas(1, 70);
            Color trunk = new Color(98, 70, 46);
            ic.Box(0.42f, 0.42f, 0.58f, 0.58f, 0, 26, trunk, 0, Canvas.Shade(trunk, 1.1f));
            Vector2 c = ic.P(0.5f, 0.5f, 44);
            ic.C.FillEllipse(c.X, c.Y, 22, 17, Canvas.Shade(leaf, 0.82f), 0.08f);
            ic.C.FillEllipse(c.X - 8, c.Y + 3, 14, 11, Canvas.Shade(leaf, 1.05f), 0.08f);
            ic.C.FillEllipse(c.X + 8, c.Y + 2, 14, 11, Canvas.Shade(leaf, 0.92f), 0.08f);
            ic.C.FillEllipse(c.X, c.Y - 9, 13, 10, Canvas.Shade(leaf, 1.15f), 0.08f);
            if (snowy)
            {
                ic.C.FillEllipse(c.X - 3, c.Y - 12, 14, 6, new Color(244, 248, 252), 0.02f);
                ic.C.FillEllipse(c.X + 10, c.Y + 1, 9, 4, new Color(244, 248, 252), 0.02f);
            }
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        public static Canvas Flowers(Color bloom)
        {
            var ic = new IsoCanvas(1, 24);
            Color stem = new Color(72, 120, 62);
            for (int i = 0; i < 5; i++)
            {
                float x = 0.2f + (i * 0.17f) % 0.6f, y = 0.3f + (i * 0.23f) % 0.5f;
                Vector2 b = ic.P(x, y, 12);
                ic.C.Line(ic.P(x, y, 0), b, stem);
                ic.C.FillEllipse(b.X, b.Y, 4, 3, bloom);
                ic.C.FillEllipse(b.X, b.Y, 1.6f, 1.3f, new Color(255, 240, 150));
            }
            return ic.C;
        }

        public static Canvas Well()
        {
            var ic = new IsoCanvas(1, 40);
            Color stone = new Color(150, 146, 138);
            ic.Cylinder(0.5f, 0.5f, 0, 16, 17, stone);
            ic.Cylinder(0.5f, 0.5f, 16, 18, 12, new Color(40, 52, 60));
            ic.Pole(0.2f, 0.2f, 0, 30, new Color(110, 76, 44));
            ic.Pole(0.8f, 0.8f, 0, 30, new Color(110, 76, 44));
            ic.QuadSW(0.2f, 0.8f, 0.2f, 28, 31, new Color(126, 88, 50));
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        public static Canvas Bench(Color wood)
        {
            var ic = new IsoCanvas(2, 30);
            Color body = Canvas.Shade(wood, 1.05f);
            ic.Box(0.1f, 0.55f, 1.9f, 1.45f, 0, 11, body, 0, Canvas.Shade(body, 1.15f));
            ic.Box(0.15f, 1.0f, 1.85f, 1.12f, 11, 22, body);
            for (int i = 0; i < 3; i++)
                ic.Top(0.12f, 0.6f + i * 0.26f, 1.88f, 0.7f + i * 0.26f, 11.2f, Canvas.Shade(wood, 0.8f));
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        /// <summary>Bozor rastasi: yog'och peshtaxta, rangli soyabon va mahsulotlar.</summary>
        public static Canvas Stall(Color awning, int variant)
        {
            var ic = new IsoCanvas(2, 74);
            Color wood = new Color(132, 88, 50);
            ic.Box(0.08f, 0.55f, 1.92f, 1.9f, 0, 20, wood, 0, Canvas.Shade(wood, 1.1f));
            ic.Pole(0.1f, 0.65f, 18, 62, wood);
            ic.Pole(1.9f, 0.65f, 18, 62, wood);
            ic.Pole(0.1f, 1.85f, 18, 56, wood);
            ic.Pole(1.9f, 1.85f, 18, 56, wood);
            // soyabon
            for (int s = 0; s < 4; s++)
                ic.Top(0.02f, 0.5f + s * 0.36f, 1.98f, 0.86f + s * 0.36f, 62, s % 2 == 0 ? awning : Canvas.Shade(awning, 0.82f));
            // peshtaxtadagi tovarlar
            for (int i = 0; i < 5; i++)
            {
                Vector2 p = ic.P(0.25f + i * 0.32f, 1.05f + (i % 2) * 0.12f, 24);
                Color c = variant switch
                {
                    1 => new Color(196, 132, 62),
                    2 => new Color(150, 60, 50),
                    _ => new Color(78, 136, 64)
                };
                ic.C.FillEllipse(p.X, p.Y, 5.5f, 4, c);
                ic.C.FillEllipse(p.X - 1, p.Y - 1, 2.2f, 1.6f, Canvas.Shade(c, 1.25f));
            }
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        public static Canvas Crate(Color wood)
        {
            var ic = new IsoCanvas(1, 26);
            ic.Box(0.12f, 0.12f, 0.88f, 0.88f, 0, 24, wood, 0, Canvas.Shade(wood, 1.12f));
            ic.QuadSW(0.12f, 0.88f, 0.88f, 8, 10, Canvas.Shade(wood, 0.7f));
            ic.QuadSW(0.12f, 0.88f, 0.88f, 16, 18, Canvas.Shade(wood, 0.7f));
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        /// <summary>Ekin qatori (1x1). growth 0..1: nihol -> ko'k -> oltin.</summary>
        public static Canvas CropRow(float growth, Season season)
        {
            var ic = new IsoCanvas(1, 26);
            ic.Top(0.05f, 0.35f, 0.95f, 0.65f, 1, new Color(120, 84, 56), 0.1f);
            Color stem = season == Season.Kuz ? new Color(196, 164, 66) : new Color(88, 146, 68);
            if (growth < 0.15f)
            {
                for (int i = 0; i < 5; i++)
                    ic.C.FillEllipse(ic.P(0.2f + i * 0.15f, 0.5f, 3).X, ic.P(0.2f + i * 0.15f, 0.5f, 3).Y, 1.8f, 1.4f, new Color(76, 120, 58));
            }
            else
            {
                int h = (int)(10 + growth * 20);
                for (int i = 0; i < 6; i++)
                {
                    float x = 0.15f + i * 0.14f;
                    ic.C.Line(ic.P(x, 0.5f, 1), ic.P(x, 0.5f, h), stem);
                    Vector2 tip = ic.P(x, 0.5f, h);
                    ic.C.FillEllipse(tip.X, tip.Y, 3, 4, growth >= 0.95f ? new Color(226, 190, 84) : Canvas.Shade(stem, 1.15f));
                }
            }
            ic.C.Outline(OutlineCol, 0.3f);
            return ic.C;
        }

        public static Canvas WaterChannel()
        {
            var ic = new IsoCanvas(2, 12);
            ic.Top(0.05f, 0.15f, 1.95f, 0.85f, 1.5f, new Color(96, 80, 62), 0.05f);
            ic.Top(0.1f, 0.25f, 1.9f, 0.75f, 2f, new Color(88, 156, 190), 0.04f);
            for (int i = 0; i < 4; i++)
                ic.Top(0.2f + i * 0.45f, 0.4f, 0.42f + i * 0.45f, 0.42f, 2.2f, new Color(160, 206, 226), 0f);
            ic.C.Outline(OutlineCol, 0.3f);
            return ic.C;
        }

        public static Canvas Bookshelf(Color wood)
        {
            var ic = new IsoCanvas(1, 70);
            ic.Box(0.08f, 0.08f, 0.92f, 0.4f, 0, 66, wood, 0, Canvas.Shade(wood, 1.05f));
            for (int r = 0; r < 3; r++)
            {
                float z = 8 + r * 20;
                ic.QuadSW(0.12f, 0.88f, 0.4f, z, z + 2, Canvas.Shade(wood, 0.85f));
                for (int k = 0; k < 4; k++)
                {
                    Color spine = k % 3 == 0 ? new Color(150, 52, 44) : k % 3 == 1 ? new Color(42, 84, 140) : new Color(64, 110, 62);
                    ic.Box(0.16f + k * 0.17f, 0.1f, 0.28f + k * 0.17f, 0.28f, z + 2, z + 16, spine);
                }
            }
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        public static Canvas LowDesk(Color wood)
        {
            var ic = new IsoCanvas(2, 24);
            ic.Box(0.1f, 0.1f, 1.9f, 1.4f, 0, 10, wood, 0, Canvas.Shade(wood, 1.15f));
            // ochiq kitob
            ic.Box(0.5f, 0.4f, 1.1f, 0.9f, 10, 12, new Color(236, 228, 206));
            ic.QuadSW(0.8f, 0.82f, 0.9f, 10, 12, new Color(170, 140, 90));
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        public static Canvas PrayerRug(Color accent)
        {
            var ic = new IsoCanvas(2, 8);
            ic.Top(0.15f, 0.1f, 1.85f, 1.9f, 0.5f, accent, 0.03f);
            ic.Top(0.3f, 0.25f, 1.7f, 1.75f, 0.7f, Canvas.Shade(accent, 1.2f), 0.02f);
            ic.C.FillPolygon(new Color(236, 220, 184), 0f,
                ic.P(1.0f, 0.5f, 0.8f), ic.P(1.25f, 0.95f, 0.8f), ic.P(0.75f, 0.95f, 0.8f));
            ic.C.Outline(OutlineCol, 0.3f);
            return ic.C;
        }

        public static Canvas TrainingMarker(Color banner)
        {
            var ic = new IsoCanvas(1, 60);
            Color wood = new Color(112, 78, 46);
            ic.Pole(0.5f, 0.5f, 0, 54, wood, 3);
            ic.Flag(0.5f, 0.5f, 54, banner, 16, 10);
            ic.Top(0.1f, 0.1f, 0.9f, 0.9f, 3, new Color(150, 120, 80), 0.1f);
            ic.C.Outline(OutlineCol, 0.35f);
            return ic.C;
        }

        public static Canvas PracticeDummy()
        {
            var ic = new IsoCanvas(1, 58);
            Color straw = new Color(196, 170, 106);
            ic.Pole(0.5f, 0.5f, 0, 20, new Color(110, 76, 44));
            ic.Cylinder(0.5f, 0.5f, 20, 50, 9, straw);
            ic.Band(0.5f, 0.5f, 32, 36, 9.2f, new Color(150, 60, 40));
            ic.C.FillEllipse(ic.P(0.5f, 0.5f, 54).X, ic.P(0.5f, 0.5f, 54).Y, 7, 6, Canvas.Shade(straw, 1.1f));
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        public static Canvas CaravanCargo(Color wrap)
        {
            var ic = new IsoCanvas(2, 46);
            ic.Box(0.1f, 0.1f, 1.1f, 1.1f, 0, 26, new Color(140, 100, 62), 0, new Color(158, 118, 74));
            ic.Box(0.6f, 0.5f, 1.7f, 1.5f, 0, 34, wrap, 0, Canvas.Shade(wrap, 1.1f));
            ic.QuadSW(0.6f, 1.7f, 1.5f, 12, 14, PalGold());
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        private static Color PalGold() => new Color(222, 182, 64);

        public static Canvas GateArch()
        {
            var ic = new IsoCanvas(3, 120);
            Color brick = new Color(176, 132, 92);
            ic.Box(0.05f, 0.6f, 2.95f, 1.05f, 0, 96, brick, 8);
            ic.Arch(true, 1.5f, 1.05f, 0.5f, 0, 62, new Color(40, 30, 22));
            ic.Top(0f, 0.5f, 3f, 1.1f, 96, Canvas.Shade(brick, 0.85f));
            for (int i = 0; i < 5; i++)
            {
                Vector2 t = ic.P(0.2f + i * 0.65f, 0.62f, 100);
                ic.C.FillRect((int)t.X - 3, (int)t.Y - 4, 6, 6, Canvas.Shade(brick, 0.7f));
            }
            ic.C.Outline(OutlineCol, 0.35f);
            return ic.C;
        }
    }
}
