using System;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>
    /// Fuqarolar uchun placeholder sprite'lar: to'n kiygan, qurolsiz yurувchi figura.
    /// Har bir tur uchun 3 kadr (0 — turish, 1-2 — yurish). Jangchilar bilan bir uslubda.
    /// </summary>
    public static class CitizenArt
    {
        public const int Citizen = 0, Merchant = 1, Craftsman = 2, Farmer = 3, Buyer = 4, Shepherd = 5;
        public const int Kinds = 6;

        private static readonly Color[] Robes =
        {
            new Color(74, 112, 170),   // fuqaro — ko'k
            new Color(78, 150, 96),    // savdogar — yashil
            new Color(150, 110, 70),   // hunarmand — jigarrang
            new Color(182, 160, 102),  // dehqon — qumrang
            new Color(150, 72, 82),    // xaridor — bordo
            new Color(120, 110, 82)    // chopon — xira yashil
        };

        private static readonly Color[] Caps =
        {
            new Color(240, 240, 245),
            new Color(58, 120, 70),
            new Color(110, 82, 52),
            new Color(120, 100, 60),
            new Color(92, 52, 58),
            new Color(72, 88, 52)
        };

        public static Canvas Frame(int kind, int frame)
        {
            var c = new Canvas(32, 46);
            int cx = 16, fy = c.Height - 5;
            int k = ((kind % Kinds) + Kinds) % Kinds;
            Color robe = Robes[k];
            Color cap = Caps[k];
            Color skin = new Color(226, 184, 146);
            Color trouser = new Color(70, 56, 44), boot = new Color(48, 34, 26);

            c.FillEllipse(cx, fy, 8, 3.2f, new Color(0, 0, 0, 70));

            // oyoqlar (yurishda qadam)
            int step = frame == 1 ? 2 : frame == 2 ? -2 : 0;
            c.FillRect(cx - 3 - step, fy - 9, 3, 7, trouser);
            c.FillRect(cx - 3 - step, fy - 3, 4, 3, boot);
            c.FillRect(cx + 1 + step, fy - 9, 3, 7, trouser);
            c.FillRect(cx + 1 + step, fy - 3, 4, 3, boot);

            DrawChopon(c, cx, fy, k, frame);

            if (k == Farmer)
            {
                Color handle = new Color(90, 60, 35);
                Color metal = new Color(170, 176, 160);
                c.Line(new Vector2(cx + 5, fy - 13), new Vector2(cx + 10, fy - 35), handle, 2);
                c.Line(new Vector2(cx + 8, fy - 34), new Vector2(cx + 14, fy - 34), metal, 2);
            }

            // bosh
            c.FillEllipse(cx, fy - 26, 4, 4.5f, skin);
            if (k == Merchant || k == Shepherd)
            {
                // soqol (savdogarniki qora, cho'ponniki oqargan)
                Color beard = k == Merchant ? new Color(44, 32, 26) : new Color(196, 190, 180);
                c.FillBlob(cx + 1, fy - 22.5f, 3.4f, 2.4f, beard, 0.04f);
            }
            DrawHeadwear(c, cx, fy, k, cap);

            c.Outline(new Color(30, 24, 20), 0.5f);
            return c;
        }

        // ------------------------------------------------------------------
        // CHOPON: yo'l-yo'l beqasam matodan, qavima tikilgan, oldi ochiq (astari ko'rinadi),
        // jiyakli etak, uzun yenglar va belga bog'langan qiyiqcha (belbog').
        // ------------------------------------------------------------------

        private struct ChoponStyle
        {
            public Color Base, Stripe1, Stripe2, Lining, Trim, Sash;
            public bool Plain; // cho'pon chakmoni — yo'lsiz jun
        }

        private static ChoponStyle StyleFor(int k)
        {
            switch (k)
            {
                case Merchant: return new ChoponStyle { Base = new Color(104, 38, 92), Stripe1 = new Color(226, 178, 64), Stripe2 = new Color(176, 46, 58), Lining = new Color(56, 136, 82), Trim = new Color(226, 178, 64), Sash = new Color(236, 230, 214) };
                case Craftsman: return new ChoponStyle { Base = new Color(126, 78, 48), Stripe1 = new Color(214, 146, 66), Stripe2 = new Color(88, 58, 38), Lining = new Color(226, 196, 120), Trim = new Color(90, 58, 36), Sash = new Color(176, 58, 44) };
                case Farmer: return new ChoponStyle { Base = new Color(168, 148, 100), Stripe1 = new Color(138, 116, 78), Stripe2 = new Color(198, 182, 138), Lining = new Color(150, 60, 40), Trim = new Color(110, 90, 60), Sash = new Color(60, 110, 160) };
                case Buyer: return new ChoponStyle { Base = new Color(138, 38, 50), Stripe1 = new Color(58, 128, 88), Stripe2 = new Color(228, 196, 78), Lining = new Color(40, 90, 160), Trim = new Color(228, 196, 78), Sash = new Color(236, 230, 214) };
                case Shepherd: return new ChoponStyle { Base = new Color(104, 96, 80), Stripe1 = new Color(88, 82, 68), Stripe2 = new Color(118, 110, 94), Lining = new Color(160, 120, 70), Trim = new Color(70, 62, 50), Sash = new Color(150, 40, 36), Plain = true };
                default: return new ChoponStyle { Base = new Color(50, 88, 156), Stripe1 = new Color(222, 222, 232), Stripe2 = new Color(68, 148, 88), Lining = new Color(196, 40, 40), Trim = new Color(230, 188, 70), Sash = new Color(196, 40, 40) };
            }
        }

        private static void DrawChopon(Canvas c, int cx, int fy, int k, int frame)
        {
            ChoponStyle s = StyleFor(k);
            int top = fy - 21, bottom = fy - 3;

            // Etak pastga kengayadi; har bir qatorda yo'l-yo'l chiziqlar va qavima (har 4-qator to'qroq)
            for (int y = top; y <= bottom; y++)
            {
                float t = (y - top) / (float)(bottom - top);
                int half = (int)MathF.Round(4f + 3f * t);
                for (int x = -half; x <= half; x++)
                {
                    int col = ((x + 21) % 4 + 4) % 4;
                    Color pc = s.Plain ? (col == 0 ? s.Stripe1 : s.Base)
                             : col == 0 ? s.Stripe1 : col == 2 ? s.Stripe2 : s.Base;
                    if ((y - top) % 4 == 3) pc = Canvas.Shade(pc, 0.86f);          // qavima chocki
                    if (x >= half - 1) pc = Canvas.Shade(pc, 0.8f);                // o'ng tomon soyada
                    else if (x <= -half + 1) pc = Canvas.Shade(pc, 1.06f);
                    c.Blend(cx + x, y, pc, 0.03f);
                }
            }

            // Oldi ochiq: beldan pastda astar ko'rinadi
            for (int y = fy - 13; y <= bottom; y++)
            {
                c.Blend(cx, y, s.Lining);
                if (y > fy - 9) c.Blend(cx + 1, y, Canvas.Shade(s.Lining, 0.85f));
            }
            // Yoqa (V shaklida astar)
            c.Line(new Vector2(cx - 3, top), new Vector2(cx, top + 5), s.Lining);
            c.Line(new Vector2(cx + 3, top), new Vector2(cx, top + 5), Canvas.Shade(s.Lining, 0.85f));
            c.FillRect(cx - 1, top, 2, 2, new Color(236, 230, 214)); // ko'ylak yoqasi
            // Etak jiyagi
            int hem = (int)MathF.Round(4f + 3f);
            c.FillRect(cx - hem, bottom, hem * 2 + 1, 1, s.Trim);

            // Qiyiqcha (belbog') va yon tomonda osilib turgan tuguni
            c.FillRect(cx - 5, fy - 14, 11, 2, s.Sash);
            c.FillRect(cx - 5, fy - 13, 11, 1, Canvas.Shade(s.Sash, 0.8f));
            c.FillPolygon(Canvas.Shade(s.Sash, 0.9f), 0f, new Vector2(cx + 3, fy - 12), new Vector2(cx + 6, fy - 12), new Vector2(cx + 4.5f, fy - 8));

            // Uzun yenglar (yurishda tebranadi) — yo'l-yo'l, jiyakli
            int sw = frame == 1 ? 1 : frame == 2 ? -1 : 0;
            Color skin = new Color(226, 184, 146);
            for (int side = -1; side <= 1; side += 2)
            {
                int ax = side < 0 ? cx - 7 : cx + 5;
                int ay = fy - 20 + (side < 0 ? sw : -sw);
                c.FillRect(ax, ay, 3, 9, side < 0 ? s.Base : Canvas.Shade(s.Base, 0.82f));
                if (!s.Plain) c.FillRect(ax + 1, ay, 1, 9, side < 0 ? s.Stripe1 : Canvas.Shade(s.Stripe1, 0.82f));
                c.FillRect(ax, ay + 8, 3, 1, s.Trim);
                c.FillRect(ax + 1, ay + 9, 2, 2, skin);
            }
        }

        /// <summary>Bosh kiyim: chust do'ppi (qora, oq bodom naqshli), savdogarda oq salla, cho'ponda jun telpak.</summary>
        private static void DrawHeadwear(Canvas c, int cx, int fy, int k, Color cap)
        {
            if (k == Merchant)
            {
                c.FillBlob(cx, fy - 30, 5.6f, 3.6f, new Color(240, 238, 230), 0.03f);
                c.FillRect(cx - 5, fy - 29, 10, 1, new Color(206, 202, 192));
                c.FillRect(cx - 1, fy - 33, 3, 1, new Color(206, 202, 192));
                return;
            }
            if (k == Shepherd)
            {
                Color fur = new Color(92, 70, 48);
                c.FillBlob(cx, fy - 31, 5.4f, 4.6f, fur, 0.1f);
                c.FillRect(cx - 5, fy - 29, 10, 2, Canvas.Shade(fur, 0.8f));
                return;
            }
            // Chust do'ppi: to'rtburchakroq qora qalpoq, to'rt tomonida oq "qalampir" naqsh
            Color doppi = new Color(30, 30, 36);
            c.FillRect(cx - 4, fy - 31, 9, 4, doppi);
            c.FillRect(cx - 3, fy - 32, 7, 1, doppi);
            c.FillRect(cx - 3, fy - 30, 2, 2, new Color(236, 236, 240));
            c.FillRect(cx + 2, fy - 30, 2, 2, new Color(236, 236, 240));
            c.Blend(cx, fy - 31, new Color(236, 236, 240));
            c.FillRect(cx - 4, fy - 28, 9, 1, Canvas.Shade(doppi, 1.6f)); // zehi
        }
    }
}
