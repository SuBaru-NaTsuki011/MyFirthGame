using System;
using AndijonSaltanatlar.Core.Graphics;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Interiors
{
    /// <summary>
    /// Uy ichki ko'rinishi uchun kodda chizilgan sprite'lar (o'yinning qolgan grafikasi kabi — asset fayl shart emas).
    /// Har bir uy turi (BuildingArt.House variantlari 0..3) o'z uslubiga ega:
    ///   0 — paxsa uy: tuproq pol, qizil gilam;
    ///   1 — pishiq g'isht uy (bolaxonali): g'isht pol, ko'k bezaklar, bolaxonaga zinapoya;
    ///   2 — suvoqli boy uy (bolaxonali): yog'och pol, feruza-zarhal ganch naqsh, zinapoya;
    ///   3 — gumbazchali to'q paxsa uy: tuproq pol, yashil bezaklar, beshik.
    /// Xona 8x8 tile: chapda (x 0..3) YOTOQXONA, o'rtada past to'siq devor (x = 4), o'ngda (x 5..7) OSHXONA.
    /// </summary>
    public static class InteriorArt
    {
        public const int N = 8;          // xona o'lchami (tile)
        public const int WallH = 92;     // orqa devor balandligi (piksel)

        private static readonly Color OutlineCol = new Color(40, 28, 20);

        public struct Style
        {
            public int Variant;
            public Color Wall;    // ichki suvoq
            public Color Floor;
            public Color Accent;  // gilam, ko'rpa
            public Color Accent2; // ikkinchi bezak rangi
        }

        public static Style For(int v)
        {
            Color outer = v switch { 0 => Pal.Mud, 1 => Pal.BrickPale, 2 => Pal.Plaster, _ => Pal.MudDark };
            return new Style
            {
                Variant = v,
                Wall = Canvas.Mix(outer, Pal.Plaster, 0.45f),
                Floor = v switch { 1 => new Color(184, 128, 88), 2 => new Color(156, 108, 66), 3 => new Color(170, 136, 96), _ => new Color(190, 158, 114) },
                Accent = v switch { 1 => new Color(46, 84, 150), 2 => new Color(34, 140, 146), 3 => new Color(64, 118, 70), _ => new Color(158, 42, 40) },
                Accent2 = v switch { 1 => new Color(214, 170, 70), 2 => Pal.Gold, 3 => new Color(200, 120, 60), _ => new Color(222, 176, 72) }
            };
        }

        // ==================================================================
        // XONA QOBIG'I: pol, ikki orqa devor, deraza, taxmon, so'zana, gilam
        // ==================================================================

        /// <param name="origin">Kanvasdagi (0,0) tile shimoliy burchagi — chizishda shu nuqta world (0,0) ga to'g'ri keladi.</param>
        public static Canvas Shell(Style s, out Vector2 origin)
        {
            var ic = new IsoCanvas(N, WallH + 24, 16);
            origin = ic.P(0, 0, 0);
            Color wall = s.Wall;

            // --- Pol ---
            ic.Top(0, 0, N, N, 0, s.Floor, s.Variant == 2 ? 0.03f : 0.07f);
            Color seam = Canvas.Alpha(Canvas.Shade(s.Floor, 0.72f), 120);
            if (s.Variant == 1)
            {
                for (float i = 0.5f; i < N; i += 0.5f)
                {
                    ic.C.Line(ic.P(i, 0, 0), ic.P(i, N, 0), seam);
                    ic.C.Line(ic.P(0, i, 0), ic.P(N, i, 0), seam);
                }
            }
            else if (s.Variant == 2)
            {
                for (float i = 0.34f; i < N; i += 0.34f) ic.C.Line(ic.P(i, 0, 0), ic.P(i, N, 0), seam); // taxta qatorlari
            }

            // --- Orqa devorlar (y = 0 — o'ng-orqa, x = 0 — chap-orqa) ---
            ic.QuadSW(0, N, 0, 0, WallH, Canvas.Shade(wall, 0.97f), 0.03f);
            ic.QuadSE(0, 0, N, 0, WallH, Canvas.Shade(wall, 0.82f), 0.03f);
            // devor tepasidagi qalinlik
            ic.Top(-0.3f, -0.3f, N, 0, WallH, Canvas.Shade(wall, 0.7f), 0.02f);
            ic.Top(-0.3f, 0, 0, N, WallH, Canvas.Shade(wall, 0.66f), 0.02f);
            // poldan devorga o'tish (pastki hoshiya)
            ic.QuadSW(0, N, 0, 0, 7, Canvas.Shade(wall, 0.68f));
            ic.QuadSE(0, 0, N, 0, 7, Canvas.Shade(wall, 0.58f));

            // --- Devor naqshi (belbog'). Boy uyda — ganch naqsh ---
            ic.QuadSW(0, N, 0, 70, 76, s.Accent);
            ic.QuadSE(0, 0, N, 70, 76, Canvas.Shade(s.Accent, 0.8f));
            if (s.Variant == 2)
            {
                for (float x = 0.15f; x < N; x += 0.4f)
                    ic.C.FillPolygon(Pal.Gold, 0f, ic.P(x, 0, 77), ic.P(x + 0.2f, 0, 82), ic.P(x + 0.4f, 0, 77));
                for (float y = 0.15f; y < N; y += 0.4f)
                    ic.C.FillPolygon(Canvas.Shade(Pal.Gold, 0.8f), 0f, ic.P(0, y, 77), ic.P(0, y + 0.2f, 82), ic.P(0, y + 0.4f, 77));
            }

            // --- Derazalar (y = 0 devorida): yotoqxona va oshxona ---
            Window(ic, 2.2f);
            Window(ic, 6.45f);

            // --- Taxmon: devor tokchasida taxlangan ko'rpalar (x = 0 devorida) ---
            ic.Arch(false, 2.2f, 0f, 0.62f, 6, 80, Canvas.Shade(wall, 0.5f));
            Color[] quilts = { s.Accent, s.Accent2, new Color(226, 214, 190), Canvas.Shade(s.Accent, 1.25f), new Color(170, 60, 60), s.Accent2, s.Accent };
            for (int k = 0; k < quilts.Length; k++)
                ic.QuadSE(0.01f, 1.68f, 2.72f, 8 + k * 7, 14 + k * 7, Canvas.Shade(quilts[k], 0.85f), 0.04f);
            ic.QuadSE(0.01f, 1.75f, 2.65f, 57, 64, new Color(236, 230, 216)); // yostiqlar

            // --- So'zana (devor gilami) ---
            ic.QuadSE(0.01f, 3.35f, 4.85f, 26, 72, Canvas.Shade(new Color(232, 220, 196), 0.85f), 0.03f);
            ic.QuadSE(0.01f, 3.42f, 4.78f, 29, 69, Canvas.Shade(s.Accent, 0.7f));
            ic.QuadSE(0.01f, 3.52f, 4.68f, 33, 65, Canvas.Shade(new Color(232, 220, 196), 0.82f));
            for (int i = 0; i < 3; i++)
            {
                Vector2 c = ic.P(0.01f, 3.75f + i * 0.4f, 49);
                ic.C.FillEllipse(c.X, c.Y, 5.5f, 9, Canvas.Shade(i == 1 ? s.Accent2 : s.Accent, 0.8f));
                ic.C.FillEllipse(c.X, c.Y, 2.2f, 3.6f, Canvas.Shade(new Color(232, 220, 196), 0.8f));
            }

            // --- Oshxona devorida: osilgan lagan va qalampir shodasi ---
            Vector2 lagan = ic.P(5.95f, 0, 62);
            ic.C.FillEllipse(lagan.X, lagan.Y, 9, 9, new Color(232, 236, 240));
            ic.C.FillEllipse(lagan.X, lagan.Y, 6, 6, Pal.Blue);
            ic.C.FillEllipse(lagan.X, lagan.Y, 2.5f, 2.5f, new Color(232, 236, 240));
            for (int i = 0; i < 6; i++)
            {
                Vector2 p = ic.P(7.15f, 0, 72 - i * 5);
                ic.C.FillEllipse(p.X, p.Y, 2, 2.6f, new Color(196, 40, 30));
            }

            // --- Gilam (yotoqxona) ---
            ic.Top(1.3f, 1.4f, 3.75f, 4.7f, 0.3f, Canvas.Shade(s.Accent, 0.75f), 0.04f);
            ic.Top(1.45f, 1.55f, 3.6f, 4.55f, 0.4f, s.Accent, 0.05f);
            ic.C.FillPolygon(s.Accent2, 0.04f, ic.P(2.52f, 2.0f, 0.5f), ic.P(3.15f, 3.05f, 0.5f), ic.P(2.52f, 4.1f, 0.5f), ic.P(1.9f, 3.05f, 0.5f));
            ic.C.FillPolygon(Canvas.Shade(s.Accent, 0.6f), 0f, ic.P(2.52f, 2.55f, 0.6f), ic.P(2.8f, 3.05f, 0.6f), ic.P(2.52f, 3.55f, 0.6f), ic.P(2.24f, 3.05f, 0.6f));
            for (float t = 1.7f; t < 3.5f; t += 0.35f)
            {
                Vector2 a = ic.P(t, 1.7f, 0.5f), b = ic.P(t, 4.4f, 0.5f);
                ic.C.FillEllipse(a.X, a.Y, 1.6f, 0.9f, s.Accent2);
                ic.C.FillEllipse(b.X, b.Y, 1.6f, 0.9f, s.Accent2);
            }

            // --- Oshxona: ko'rpachalar (xontaxta atrofida o'tirish joyi) ---
            ic.Top(7.12f, 4.1f, 7.86f, 5.9f, 0.6f, Canvas.Shade(s.Accent, 0.9f), 0.05f);
            ic.Top(7.12f, 4.8f, 7.86f, 5.0f, 0.8f, s.Accent2);
            ic.Top(5.1f, 3.18f, 6.9f, 3.86f, 0.6f, Canvas.Shade(s.Accent2, 0.85f), 0.05f);
            ic.Top(5.8f, 3.18f, 6.0f, 3.86f, 0.8f, s.Accent);

            // --- Deraza nuri polda ---
            ic.Top(1.85f, 0.15f, 2.55f, 2.9f, 0.2f, new Color(255, 240, 200, 34), 0f);
            ic.Top(6.1f, 0.15f, 6.8f, 2.4f, 0.2f, new Color(255, 240, 200, 30), 0f);

            // --- Eshik: poyandoz va ostonadan tushgan yorug'lik ---
            ic.Top(6.1f, 7.1f, 6.9f, 7.92f, 0.5f, new Color(120, 84, 52), 0.05f);
            ic.Top(6.2f, 7.2f, 6.8f, 7.82f, 0.6f, new Color(150, 60, 44));
            ic.Top(6.02f, 7.7f, 6.98f, 8.0f, 0.7f, new Color(255, 232, 170, 110), 0f);

            // --- Old devorlarning kesimi (qo'g'irchoq uyi ko'rinishi) — eshik joyi ochiq ---
            Color cut = Canvas.Shade(wall, 0.9f);
            ic.Box(0, 7.86f, 6.0f, 8.0f, 0, 12, cut, 0, Canvas.Shade(s.Accent, 0.6f));
            ic.Box(7.0f, 7.86f, 8.0f, 8.0f, 0, 12, cut, 0, Canvas.Shade(s.Accent, 0.6f));
            ic.Box(7.86f, 0, 8.0f, 7.86f, 0, 12, cut, 0, Canvas.Shade(s.Accent, 0.6f));

            return ic.C;
        }

        private static void Window(IsoCanvas ic, float x)
        {
            ic.FramedArch(true, x, 0f, 0.42f, 32, 78, Pal.Wood, new Color(176, 212, 236));
            // panjara (yog'och to'r)
            Color lattice = Canvas.Shade(Pal.Wood, 0.9f);
            for (float dx = -0.22f; dx <= 0.23f; dx += 0.11f)
                ic.C.Line(ic.P(x + dx, 0, 33), ic.P(x + dx, 0, 70), lattice);
            for (float z = 40; z < 72; z += 8)
                ic.C.Line(ic.P(x - 0.3f, 0, z), ic.P(x + 0.3f, 0, z), lattice);
            ic.QuadSW(x - 0.5f, x + 0.5f, 0.02f, 29, 33, Canvas.Shade(Pal.Wood, 1.1f)); // tokcha
        }

        // ==================================================================
        // JIHOZLAR (har biri n x n izli sprite, langari — pastki-o'rta nuqta)
        // ==================================================================

        /// <summary>Yotoqxona va oshxona orasidagi past devor bo'lagi.</summary>
        public static Canvas Partition(Style s)
        {
            var ic = new IsoCanvas(1, 56);
            ic.Box(0.36f, 0, 0.64f, 1, 0, 42, s.Wall, 0, Canvas.Shade(s.Accent, 0.75f));
            ic.QuadSW(0.36f, 0.64f, 1f, 30, 35, s.Accent);
            ic.QuadSE(0.64f, 0, 1, 30, 35, Canvas.Shade(s.Accent, 0.8f));
            ic.C.Outline(OutlineCol, 0.35f);
            return ic.C;
        }

        /// <summary>Yotoq: yog'och supa, to'shak, ko'rpa va yostiq (2x2).</summary>
        public static Canvas Bed(Style s)
        {
            var ic = new IsoCanvas(2, 40);
            Color wood = new Color(128, 84, 48);
            ic.Box(0.08f, 0.15f, 1.92f, 1.85f, 0, 13, wood, 0, Canvas.Shade(wood, 1.1f));
            ic.Box(0.15f, 0.22f, 1.85f, 1.78f, 13, 20, new Color(232, 222, 200));
            ic.Box(0.15f, 0.72f, 1.85f, 1.78f, 20, 24, s.Accent, 0, s.Accent);
            ic.Top(0.15f, 1.15f, 1.85f, 1.3f, 24.2f, s.Accent2);
            ic.Box(0.28f, 0.28f, 1.72f, 0.66f, 20, 28, new Color(244, 240, 230));
            ic.C.Outline(OutlineCol, 0.45f);
            return ic.C;
        }

        /// <summary>Sandiq: bo'yalgan, temir tasmali kelinlik sandig'i (1x1).</summary>
        public static Canvas Chest(Style s)
        {
            var ic = new IsoCanvas(1, 40);
            Color body = s.Variant == 2 ? new Color(120, 40, 34) : new Color(148, 62, 40);
            ic.Box(0.14f, 0.22f, 0.86f, 0.8f, 0, 22, body, 0, Canvas.Shade(body, 1.15f));
            ic.QuadSW(0.14f, 0.86f, 0.8f, 9, 11, Pal.Gold);
            ic.QuadSW(0.3f, 0.36f, 0.8f, 0, 22, Pal.Gold);
            ic.QuadSW(0.64f, 0.7f, 0.8f, 0, 22, Pal.Gold);
            ic.QuadSE(0.86f, 0.22f, 0.8f, 9, 11, Canvas.Shade(Pal.Gold, 0.8f));
            // ustida taxlangan ko'rpacha
            ic.Box(0.2f, 0.28f, 0.8f, 0.74f, 22, 26, s.Accent);
            ic.Box(0.22f, 0.3f, 0.78f, 0.72f, 26, 29, s.Accent2);
            ic.C.Outline(OutlineCol, 0.45f);
            return ic.C;
        }

        /// <summary>O'choq: loydan qurilgan, qozonli, mo'rili (1x1).</summary>
        public static Canvas Stove()
        {
            var ic = new IsoCanvas(1, 104);
            Color clay = new Color(170, 122, 82);
            ic.Box(0.3f, 0.0f, 0.7f, 0.24f, 28, 98, Canvas.Shade(clay, 0.95f));     // mo'ri
            ic.Box(0.06f, 0.08f, 0.94f, 0.94f, 0, 28, clay, 6, Canvas.Shade(clay, 1.05f));
            ic.Arch(true, 0.5f, 0.94f, 0.22f, 2, 19, new Color(40, 24, 18));          // olov og'zi
            ic.Arch(true, 0.5f, 0.94f, 0.14f, 2, 12, new Color(255, 150, 50));
            ic.Arch(true, 0.5f, 0.94f, 0.07f, 2, 8, new Color(255, 226, 120));
            // qozon va ichidagi osh
            Vector2 q = ic.P(0.5f, 0.52f, 28);
            ic.C.FillEllipse(q.X, q.Y, 15, 7.5f, new Color(36, 32, 30));
            ic.C.FillEllipse(q.X, q.Y - 0.5f, 12, 5.8f, new Color(214, 150, 60), 0.12f);
            for (int i = 0; i < 6; i++)
                ic.C.FillEllipse(q.X - 7 + i * 3, q.Y - 1 + (i % 2), 1.2f, 0.8f, new Color(230, 110, 40));
            // bug'
            for (int i = 0; i < 3; i++)
                ic.C.FillBlob(q.X - 3 + i * 3, q.Y - 12 - i * 9, 4 + i, 3 + i, new Color(255, 255, 255, 70), 0f);
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        /// <summary>Javon: kosa, lagan va choynaklar terilgan yog'och tokcha (1x1).</summary>
        public static Canvas Shelf()
        {
            var ic = new IsoCanvas(1, 84);
            Color wood = new Color(122, 80, 46);
            ic.Box(0.06f, 0.04f, 0.94f, 0.5f, 0, 66, wood, 0, Canvas.Shade(wood, 1.1f));
            ic.QuadSW(0.12f, 0.88f, 0.5f, 4, 62, Canvas.Shade(wood, 0.55f));   // ichki qismi
            for (int r = 0; r < 3; r++)
            {
                float z = 6 + r * 19;
                ic.QuadSW(0.1f, 0.9f, 0.5f, z - 2, z, Canvas.Shade(wood, 1.15f));
                for (int k = 0; k < 3; k++)
                {
                    Vector2 p = ic.P(0.22f + k * 0.28f, 0.5f, z + 5);
                    if ((r + k) % 3 == 0) { ic.C.FillEllipse(p.X, p.Y - 3, 5, 5, new Color(236, 238, 242)); ic.C.FillEllipse(p.X, p.Y - 3, 3, 3, Pal.Blue); }
                    else if ((r + k) % 3 == 1) { ic.C.FillEllipse(p.X, p.Y + 1, 4.5f, 3.5f, Pal.Blue); ic.C.FillRect((int)p.X - 4, (int)p.Y - 2, 9, 2, new Color(236, 238, 242)); }
                    else { ic.C.FillEllipse(p.X, p.Y, 3.5f, 4.5f, new Color(60, 120, 170)); ic.C.FillRect((int)p.X + 3, (int)p.Y - 2, 3, 1, new Color(60, 120, 170)); }
                }
            }
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        /// <summary>Suv xumchasi va ko'za (1x1).</summary>
        public static Canvas Jars()
        {
            var ic = new IsoCanvas(1, 48);
            Color clay = new Color(178, 102, 62);
            ic.Cylinder(0.42f, 0.42f, 0, 26, 9.5f, clay);
            ic.Cylinder(0.42f, 0.42f, 26, 31, 5f, Canvas.Shade(clay, 0.9f));
            ic.Band(0.42f, 0.42f, 14, 16, 9.6f, new Color(90, 50, 30));
            ic.Cylinder(0.75f, 0.72f, 0, 13, 5.5f, Canvas.Shade(clay, 1.12f));
            ic.Cylinder(0.75f, 0.72f, 13, 17, 2.8f, Canvas.Shade(clay, 1.0f));
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        /// <summary>Un va guruch qoplari (1x1).</summary>
        public static Canvas Sacks()
        {
            var ic = new IsoCanvas(1, 40);
            Color burlap = new Color(206, 186, 146);
            Vector2 a = ic.P(0.4f, 0.45f, 0), b = ic.P(0.7f, 0.72f, 0);
            ic.C.FillBlob(a.X, a.Y - 12, 10, 13, burlap, 0.06f);
            ic.C.FillRect((int)a.X - 4, (int)a.Y - 26, 8, 3, Canvas.Shade(burlap, 0.7f));
            ic.C.FillBlob(b.X, b.Y - 8, 8, 9, Canvas.Shade(burlap, 0.92f), 0.06f);
            ic.C.FillRect((int)b.X - 3, (int)b.Y - 18, 6, 2, Canvas.Shade(burlap, 0.65f));
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        /// <summary>Xontaxta va dasturxon: non, osh, choynak, piyolalar, uzum (2x2).</summary>
        public static Canvas Table(Style s)
        {
            var ic = new IsoCanvas(2, 40);
            Color wood = new Color(126, 82, 46);
            foreach (var (lx, ly) in new[] { (0.32f, 0.32f), (1.6f, 0.32f), (0.32f, 1.6f), (1.6f, 1.6f) })
                ic.Box(lx, ly, lx + 0.1f, ly + 0.1f, 0, 10, Canvas.Shade(wood, 0.8f));
            ic.Box(0.25f, 0.25f, 1.75f, 1.75f, 10, 13, wood);
            // dasturxon
            ic.Top(0.16f, 0.16f, 1.84f, 1.84f, 13.5f, new Color(238, 228, 206), 0.02f);
            ic.Top(0.16f, 0.16f, 1.84f, 0.26f, 13.6f, s.Accent);
            ic.Top(0.16f, 0.16f, 0.26f, 1.84f, 13.6f, s.Accent);
            ic.Top(0.16f, 1.74f, 1.84f, 1.84f, 13.6f, s.Accent);
            ic.Top(1.74f, 0.16f, 1.84f, 1.84f, 13.6f, s.Accent);
            ic.QuadSW(0.16f, 1.84f, 1.84f, 9, 13.5f, Canvas.Shade(new Color(238, 228, 206), 0.85f)); // osilib turgan chet
            ic.QuadSE(1.84f, 0.16f, 1.84f, 9, 13.5f, Canvas.Shade(new Color(238, 228, 206), 0.7f));

            // non (patir)
            foreach (var (nx, ny) in new[] { (0.55f, 0.6f), (0.6f, 1.35f) })
            {
                Vector2 n = ic.P(nx, ny, 14);
                ic.C.FillEllipse(n.X, n.Y, 8, 4, new Color(206, 140, 62));
                ic.C.FillEllipse(n.X, n.Y, 4.5f, 2.2f, new Color(232, 186, 110));
                ic.C.FillEllipse(n.X, n.Y, 1.2f, 0.7f, new Color(150, 96, 40));
            }
            // lagandagi osh
            Vector2 o = ic.P(1.15f, 0.85f, 14);
            ic.C.FillEllipse(o.X, o.Y, 11, 5.5f, new Color(236, 238, 242));
            ic.C.FillEllipse(o.X, o.Y, 9.5f, 4.6f, Pal.Blue);
            ic.C.FillEllipse(o.X, o.Y - 1, 7.5f, 3.6f, new Color(226, 172, 70), 0.12f);
            for (int i = 0; i < 5; i++) ic.C.FillEllipse(o.X - 5 + i * 2.5f, o.Y - 1.5f + (i % 2), 1.1f, 0.7f, new Color(232, 106, 36));
            ic.C.FillEllipse(o.X, o.Y - 2.5f, 2.2f, 1.2f, new Color(160, 80, 50)); // go'sht
            // choynak va piyolalar
            ic.Cylinder(1.45f, 1.42f, 14, 20, 3.4f, Pal.Blue);
            ic.Cylinder(1.45f, 1.42f, 20, 22, 1.6f, new Color(236, 238, 242));
            Vector2 sp = ic.P(1.45f, 1.42f, 18);
            ic.C.Line(sp + new Vector2(3, 0), sp + new Vector2(7, -3), Pal.Blue, 2);
            foreach (var (px, py) in new[] { (1.05f, 1.5f), (1.25f, 1.7f), (1.7f, 1.15f) })
            {
                Vector2 p = ic.P(px, py, 14);
                ic.C.FillEllipse(p.X, p.Y - 1, 2.6f, 2.2f, new Color(236, 238, 242));
                ic.C.FillEllipse(p.X, p.Y - 2, 1.8f, 0.8f, Pal.Blue);
            }
            // uzum
            Vector2 g = ic.P(0.95f, 1.45f, 14);
            for (int i = 0; i < 7; i++) ic.C.FillEllipse(g.X - 3 + (i % 3) * 2.2f, g.Y - 2 + (i / 3) * 1.6f, 1.3f, 1.1f, new Color(110, 50, 110));
            ic.C.Outline(OutlineCol, 0.35f);
            return ic.C;
        }

        /// <summary>Bolaxonaga ko'tariladigan yog'och zinapoya (1x1).</summary>
        public static Canvas Stairs(Style s)
        {
            var ic = new IsoCanvas(1, 100);
            Color wood = new Color(140, 94, 54);
            for (int i = 0; i < 6; i++)
                ic.Box(0.12f, 0f, 0.88f, 1f - i * 0.16f, i * 13, (i + 1) * 13, Canvas.Shade(wood, 0.92f + i * 0.03f));
            ic.Pole(0.86f, 0.98f, 0, 28, Canvas.Shade(wood, 0.8f));
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        /// <summary>Beshik: o'yma yog'och beshik, ustiga yopilgan rangli yopinchiq (1x1).</summary>
        public static Canvas Cradle(Style s)
        {
            var ic = new IsoCanvas(1, 50);
            Color wood = new Color(150, 98, 52);
            // tagidagi egilgan oyoqlar
            ic.Box(0.15f, 0.25f, 0.85f, 0.35f, 0, 4, Canvas.Shade(wood, 0.8f));
            ic.Box(0.15f, 0.65f, 0.85f, 0.75f, 0, 4, Canvas.Shade(wood, 0.8f));
            ic.Box(0.2f, 0.3f, 0.8f, 0.7f, 6, 16, wood);
            ic.Pole(0.22f, 0.5f, 6, 30, wood, 3);
            ic.Pole(0.78f, 0.5f, 6, 30, wood, 3);
            ic.C.Line(ic.P(0.22f, 0.5f, 30), ic.P(0.78f, 0.5f, 30), wood, 2);
            // yopinchiq
            ic.Box(0.22f, 0.32f, 0.78f, 0.68f, 16, 20, s.Accent, 0, s.Accent);
            ic.Top(0.22f, 0.45f, 0.78f, 0.55f, 20.3f, s.Accent2);
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        // ==================================================================
        // SHOH SAROYI — TAXT ZALI
        // ==================================================================

        /// <summary>Saroy uslubi: oq marmar, qizil-zarhal bezaklar (dasturxon va hokazo uchun).</summary>
        public static Style PalaceStyle => new Style
        {
            Variant = 9,
            Wall = new Color(238, 228, 206),
            Floor = new Color(228, 222, 208),
            Accent = new Color(150, 28, 40),
            Accent2 = Pal.Gold
        };

        /// <summary>Saroy eshigi — old devordagi ikki tile kenglikdagi o'tish joyi (x = 3..4, y = 7).</summary>
        public const int PalaceDoorX0 = 3, PalaceDoorX1 = 4;

        public static Canvas PalaceShell(out Vector2 origin)
        {
            const int H = WallH;
            var ic = new IsoCanvas(N, H + 24, 16);
            origin = ic.P(0, 0, 0);
            Color marble = new Color(228, 222, 208), plaster = new Color(238, 228, 206);
            Color koshin = new Color(34, 140, 160), deep = new Color(24, 52, 120);

            // --- Marmar pol, koshin naqsh ---
            ic.Top(0, 0, N, N, 0, marble, 0.03f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float cx = x + 0.5f, cy = y + 0.5f;
                    ic.C.FillPolygon((x + y) % 2 == 0 ? koshin : deep, 0f,
                        ic.P(cx, cy - 0.22f, 0.2f), ic.P(cx + 0.22f, cy, 0.2f), ic.P(cx, cy + 0.22f, 0.2f), ic.P(cx - 0.22f, cy, 0.2f));
                    ic.C.Line(ic.P(x, y, 0), ic.P(x + 1, y, 0), Canvas.Alpha(Canvas.Shade(marble, 0.8f), 120));
                    ic.C.Line(ic.P(x, y, 0), ic.P(x, y + 1, 0), Canvas.Alpha(Canvas.Shade(marble, 0.8f), 120));
                }
            // chetdagi to'q ko'k hoshiya
            ic.Top(0, 0, N, 0.25f, 0.3f, deep);
            ic.Top(0, 0, 0.25f, N, 0.3f, deep);

            // --- Devorlar: pastda koshin panel, tepada oq suvoq, zarhal ganch belbog' ---
            ic.QuadSW(0, N, 0, 0, H, Canvas.Shade(plaster, 0.98f), 0.02f);
            ic.QuadSE(0, 0, N, 0, H, Canvas.Shade(plaster, 0.84f), 0.02f);
            ic.QuadSW(0, N, 0, 0, 34, koshin, 0.03f);
            ic.QuadSE(0, 0, N, 0, 34, Canvas.Shade(koshin, 0.8f), 0.03f);
            for (float x = 0.25f; x < N; x += 0.5f)
            {
                Vector2 a = ic.P(x, 0, 17), b = ic.P(0, x, 17);
                ic.C.FillPolygon(new Color(240, 236, 220), 0f, a + new Vector2(0, -6), a + new Vector2(5, 0), a + new Vector2(0, 6), a + new Vector2(-5, 0));
                ic.C.FillPolygon(new Color(214, 210, 196), 0f, b + new Vector2(0, -6), b + new Vector2(5, 0), b + new Vector2(0, 6), b + new Vector2(-5, 0));
                ic.C.FillEllipse(a.X, a.Y, 1.5f, 1.5f, deep);
                ic.C.FillEllipse(b.X, b.Y, 1.5f, 1.5f, deep);
            }
            ic.QuadSW(0, N, 0, 34, 37, Pal.Gold);
            ic.QuadSE(0, 0, N, 34, 37, Canvas.Shade(Pal.Gold, 0.8f));
            ic.QuadSW(0, N, 0, 68, 74, Pal.Gold);
            ic.QuadSE(0, 0, N, 68, 74, Canvas.Shade(Pal.Gold, 0.8f));
            for (float x = 0.1f; x < N; x += 0.3f)
            {
                ic.C.FillPolygon(Pal.Gold, 0f, ic.P(x, 0, 75), ic.P(x + 0.15f, 0, 80), ic.P(x + 0.3f, 0, 75));
                ic.C.FillPolygon(Canvas.Shade(Pal.Gold, 0.8f), 0f, ic.P(0, x, 75), ic.P(0, x + 0.15f, 80), ic.P(0, x + 0.3f, 75));
            }
            ic.QuadSW(0, N, 0, 84, H, deep);
            ic.QuadSE(0, 0, N, 84, H, Canvas.Shade(deep, 0.8f));
            ic.Top(-0.3f, -0.3f, N, 0, H, Canvas.Shade(plaster, 0.7f), 0.02f);
            ic.Top(-0.3f, 0, 0, N, H, Canvas.Shade(plaster, 0.66f), 0.02f);

            // --- Taxt ortidagi peshtoq (katta bezakli ravoq) va quyosh timsoli ---
            ic.Arch(true, 4.0f, 0f, 1.25f, 34, 90, Pal.Gold);
            ic.Arch(true, 4.0f, 0f, 1.1f, 36, 86, deep);
            ic.Arch(true, 4.0f, 0f, 0.85f, 38, 80, koshin);
            Vector2 sun = ic.P(4.0f, 0f, 62);
            ic.C.FillEllipse(sun.X, sun.Y, 9, 9, Pal.Gold);
            for (int i = 0; i < 12; i++)
            {
                float ang = i * MathF.PI / 6f;
                ic.C.Line(sun + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 10f, sun + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 15f, Pal.Gold, 2);
            }
            ic.C.FillEllipse(sun.X, sun.Y, 5, 5, new Color(250, 224, 120));

            // --- Yon ravoqlar (y = 0 devorida) va baland derazalar (x = 0 devorida) ---
            ic.FramedArch(true, 1.2f, 0f, 0.45f, 37, 82, Pal.Gold, Canvas.Shade(deep, 1.2f));
            ic.FramedArch(true, 6.8f, 0f, 0.45f, 37, 82, Pal.Gold, Canvas.Shade(deep, 1.2f));
            foreach (float y in new[] { 2.0f, 6.0f })
            {
                ic.FramedArch(false, y, 0f, 0.5f, 30, 84, Pal.Gold, new Color(120, 170, 230));
                for (float dy = -0.3f; dy <= 0.31f; dy += 0.15f) ic.C.Line(ic.P(0, y + dy, 32), ic.P(0, y + dy, 72), Canvas.Shade(Pal.Gold, 0.8f));
                ic.Top(0.05f, y - 0.5f, 1.6f, y + 0.5f, 0.4f, new Color(255, 240, 200, 30), 0f); // nur
            }
            // --- Osilgan bayroqlar (x = 0 devorida) ---
            foreach (float y in new[] { 3.6f, 4.4f })
            {
                ic.QuadSE(0.02f, y - 0.25f, y + 0.25f, 30, 82, new Color(150, 28, 40));
                ic.QuadSE(0.02f, y - 0.25f, y + 0.25f, 78, 82, Pal.Gold);
                Vector2 e = ic.P(0.02f, y, 56);
                ic.C.FillEllipse(e.X, e.Y, 4, 6, Pal.Gold);
            }

            // --- Eshikdan taxtgacha qizil poyandoz ---
            ic.Top(3.1f, 2.0f, 4.9f, N, 0.5f, new Color(150, 28, 40), 0.04f);
            ic.Top(3.1f, 2.0f, 3.22f, N, 0.6f, Pal.Gold);
            ic.Top(4.78f, 2.0f, 4.9f, N, 0.6f, Pal.Gold);
            for (float y = 2.6f; y < N; y += 1.0f)
                ic.C.FillPolygon(Pal.Gold, 0f, ic.P(4.0f, y - 0.2f, 0.7f), ic.P(4.25f, y, 0.7f), ic.P(4.0f, y + 0.2f, 0.7f), ic.P(3.75f, y, 0.7f));

            // --- Old devor kesimi, eshik (x 3..5) ochiq ---
            Color cut = Canvas.Shade(plaster, 0.9f);
            ic.Box(0, 7.86f, PalaceDoorX0, 8.0f, 0, 14, cut, 0, Pal.Gold);
            ic.Box(PalaceDoorX1 + 1, 7.86f, 8.0f, 8.0f, 0, 14, cut, 0, Pal.Gold);
            ic.Box(7.86f, 0, 8.0f, 7.86f, 0, 14, cut, 0, Pal.Gold);
            ic.Top(PalaceDoorX0 + 0.02f, 7.7f, PalaceDoorX1 + 0.98f, 8.0f, 0.8f, new Color(255, 232, 170, 110), 0f);
            return ic.C;
        }

        /// <summary>Oltin taxt: marmar supa, qizil baxmal o'rindiq, baland suyanchiq (2x2).</summary>
        public static Canvas Throne()
        {
            var ic = new IsoCanvas(2, 80);
            Color marble = new Color(236, 230, 216), gold = Pal.Gold, red = new Color(150, 28, 40);
            ic.Box(0, 0, 2, 2, 0, 5, marble);
            ic.Box(0.2f, 0.1f, 1.8f, 1.65f, 5, 10, Canvas.Shade(marble, 0.95f));
            ic.QuadSW(0.2f, 1.8f, 1.65f, 6, 8, gold);
            // suyanchiq
            ic.Box(0.45f, 0.2f, 1.55f, 0.45f, 10, 64, gold, 0, Canvas.Shade(gold, 1.1f));
            ic.QuadSW(0.6f, 1.4f, 0.451f, 26, 58, red);
            ic.Arch(true, 1.0f, 0.452f, 0.32f, 28, 56, Canvas.Shade(red, 1.2f));
            ic.Dome(0.5f, 0.32f, 64, 3.5f, 5, gold, 0, false);
            ic.Dome(1.5f, 0.32f, 64, 3.5f, 5, gold, 0, false);
            ic.Dome(1.0f, 0.32f, 64, 5, 8, gold, 0, true);
            // o'rindiq va suyanchiqlar
            ic.Box(0.45f, 0.45f, 1.55f, 1.3f, 10, 22, gold);
            ic.Box(0.5f, 0.5f, 1.5f, 1.25f, 22, 26, red, 0, Canvas.Shade(red, 1.15f));
            ic.Box(0.4f, 0.45f, 0.55f, 1.3f, 22, 34, gold);
            ic.Box(1.45f, 0.45f, 1.6f, 1.3f, 22, 34, gold);
            ic.C.FillEllipse(ic.P(1.0f, 1.3f, 16).X, ic.P(1.0f, 1.3f, 16).Y, 2.5f, 2.5f, new Color(200, 30, 40)); // gavhar
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        /// <summary>O'ymakor yog'och ustun, zarhal qoshli (1x1).</summary>
        public static Canvas Column()
        {
            var ic = new IsoCanvas(1, 112);
            Color wood = new Color(134, 86, 48);
            ic.Box(0.22f, 0.22f, 0.78f, 0.78f, 0, 7, new Color(236, 230, 216));
            ic.Cylinder(0.5f, 0.5f, 7, 86, 6.5f, wood, 6);
            for (int z = 20; z < 84; z += 21) ic.Band(0.5f, 0.5f, z, z + 3, 6.7f, Pal.Gold);
            ic.Box(0.22f, 0.22f, 0.78f, 0.78f, 86, 92, Pal.Gold);
            ic.Box(0.15f, 0.15f, 0.85f, 0.85f, 92, 97, Canvas.Shade(wood, 1.1f));
            ic.C.Outline(OutlineCol, 0.35f);
            return ic.C;
        }

        /// <summary>Zarhal mash'al-manqal (1x1). Olov yorug'i alohida (additive glow) chiziladi.</summary>
        public static Canvas Brazier()
        {
            var ic = new IsoCanvas(1, 64);
            Vector2 b = ic.P(0.5f, 0.5f, 0);
            ic.C.Line(b + new Vector2(-6, 0), b + new Vector2(0, -26), Canvas.Shade(Pal.Gold, 0.7f), 2);
            ic.C.Line(b + new Vector2(6, 0), b + new Vector2(0, -26), Canvas.Shade(Pal.Gold, 0.7f), 2);
            ic.C.Line(b + new Vector2(0, 3), b + new Vector2(0, -26), Canvas.Shade(Pal.Gold, 0.85f), 2);
            ic.C.FillEllipse(b.X, b.Y - 28, 9, 4, Pal.Gold);
            ic.C.FillEllipse(b.X, b.Y - 29, 7, 2.6f, new Color(80, 40, 20));
            ic.C.FillBlob(b.X, b.Y - 36, 5.5f, 8, new Color(255, 150, 40), 0.05f);
            ic.C.FillBlob(b.X, b.Y - 34, 3.2f, 5, new Color(255, 230, 120), 0.05f);
            ic.C.Outline(OutlineCol, 0.35f);
            return ic.C;
        }

        /// <summary>Ochiq xazina sandig'i: tilla tangalar va qimmatbaho toshlar (1x1).</summary>
        public static Canvas Treasure()
        {
            var ic = new IsoCanvas(1, 48);
            Color wood = new Color(110, 60, 36);
            ic.Box(0.15f, 0.25f, 0.85f, 0.82f, 0, 18, wood, 0, Canvas.Shade(wood, 1.1f));
            ic.QuadSW(0.15f, 0.85f, 0.82f, 7, 9, Pal.Gold);
            ic.QuadSE(0.85f, 0.25f, 0.82f, 7, 9, Canvas.Shade(Pal.Gold, 0.8f));
            ic.QuadSW(0.15f, 0.85f, 0.27f, 18, 34, Canvas.Shade(wood, 0.8f)); // ochiq qopqoq
            Vector2 c = ic.P(0.5f, 0.55f, 19);
            ic.C.FillBlob(c.X, c.Y - 2, 13, 6, Pal.Gold, 0.15f);
            for (int i = 0; i < 9; i++)
                ic.C.FillEllipse(c.X - 9 + (i * 7) % 18, c.Y - 4 + (i % 3) * 2, 1.8f, 1.2f, new Color(255, 236, 140));
            ic.C.FillEllipse(c.X - 4, c.Y - 5, 1.8f, 1.6f, new Color(210, 30, 40));
            ic.C.FillEllipse(c.X + 5, c.Y - 3, 1.8f, 1.6f, new Color(40, 170, 120));
            ic.C.FillEllipse(c.X + 1, c.Y - 7, 1.6f, 1.4f, new Color(60, 110, 220));
            ic.C.Outline(OutlineCol, 0.4f);
            return ic.C;
        }

        /// <summary>Yumshoq radial yorug'lik (o'choq olovi, chiroq).</summary>
        public static Canvas Glow(int size)
        {
            var c = new Canvas(size, size);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - r) / r, dy = (y + 0.5f - r) / r;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d >= 1f) continue;
                    float a = (1f - d) * (1f - d);
                    c.Pixels[y * size + x] = new Color(255, 255, 255, (int)(a * 255));
                }
            return c;
        }
    }
}
