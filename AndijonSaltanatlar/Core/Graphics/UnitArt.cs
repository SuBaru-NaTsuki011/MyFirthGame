using System;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>
    /// Jangchilar uchun placeholder sprite'lar. Har bir tur uchun 3 kadr: 0 — turish, 1-2 — yurish.
    /// Sprite o'ngga qarab chiziladi; chapga yurganda gorizontal aylantiriladi.
    /// </summary>
    public static class UnitArt
    {
        public const int Swordsman = 0, Spearman = 1, Shield = 2, Patrol = 3, Raider = 4, RaiderChief = 5, Shah = 6;

        private static bool IsRaider(int kind) => kind == Raider || kind == RaiderChief;
        /// <summary>Oyoq tagi sprite pastidan necha piksel yuqorida.</summary>
        public const int FootOffset = 5;

        /// <summary>
        /// Shoh: semiz qomatli, zarbof (oltin naqshli) binafsha to'nli, qora soqolli,
        /// salla ustidan qimmatbaho toshli oltin toj kiygan. Kengroq kanvas (48x60).
        /// </summary>
        public static Canvas ShahFrame(int frame)
        {
            var c = new Canvas(48, 60);
            int cx = 24, fy = c.Height - FootOffset;
            Color robe = new Color(92, 36, 108), robeDark = new Color(66, 24, 80);
            Color gold = Pal.Gold, skin = new Color(226, 182, 144);

            c.FillEllipse(cx, fy, 13, 4.5f, new Color(0, 0, 0, 80));

            // --- kalta oyoqlar, qizil-oltin etiklar ---
            int step = frame == 1 ? 2 : frame == 2 ? -2 : 0;
            int liftA = frame == 1 ? 1 : 0, liftB = frame == 2 ? 1 : 0;
            c.FillRect(cx - 6 - step, fy - 7 - liftA, 5, 7, new Color(150, 36, 30));
            c.FillRect(cx - 7 - step, fy - 2 - liftA, 7, 2, gold);
            c.FillRect(cx + 1 + step, fy - 7 - liftB, 5, 7, new Color(150, 36, 30));
            c.FillRect(cx + 1 + step, fy - 2 - liftB, 7, 2, gold);

            // --- yelkadan tushgan uzun yopinchiq (orqada) ---
            c.FillPolygon(new Color(150, 30, 36), 0.04f, new Vector2(cx - 9, fy - 34), new Vector2(cx + 9, fy - 34), new Vector2(cx + 14, fy - 6), new Vector2(cx - 14, fy - 6));

            // --- semiz tana: zarbof to'n ---
            c.FillEllipse(cx, fy - 19, 13, 13, robe, 0.04f);            // qorin
            c.FillPolygon(robe, 0.04f, new Vector2(cx - 9, fy - 33), new Vector2(cx + 9, fy - 33), new Vector2(cx + 13, fy - 7), new Vector2(cx - 13, fy - 7));
            c.FillEllipse(cx + 3, fy - 21, 6, 8, new Color(112, 48, 128)); // qorindagi yorug'
            // oltin hoshiya va naqshlar
            c.FillRect(cx - 1, fy - 33, 2, 26, gold);
            for (int y = fy - 30; y < fy - 9; y += 5)
            {
                c.FillRect(cx - 9, y, 2, 2, gold);
                c.FillRect(cx + 7, y + 2, 2, 2, gold);
            }
            c.FillRect(cx - 13, fy - 9, 26, 2, gold);
            // kamar (belbog') va gavhar
            c.FillRect(cx - 12, fy - 21, 24, 3, gold);
            c.FillEllipse(cx, fy - 20, 2.4f, 2.2f, new Color(200, 30, 40));

            // --- qo'llar (keng yenglar) ---
            c.FillEllipse(cx - 13, fy - 24, 4, 7, robeDark);
            c.FillEllipse(cx + 13, fy - 24, 4, 7, robeDark);
            c.FillRect(cx - 15, fy - 19, 4, 3, skin);
            c.FillRect(cx + 11, fy - 19, 4, 3, skin);
            c.FillRect(cx - 15, fy - 23, 4, 1, gold); // bilaguzuk
            c.FillRect(cx + 11, fy - 23, 4, 1, gold);
            // tantanali qilich (yonda)
            c.Line(new Vector2(cx + 13, fy - 17), new Vector2(cx + 17, fy - 30), Pal.Steel, 2);
            c.FillRect(cx + 11, fy - 18, 6, 2, gold);

            // --- bosh: dumaloq yuz, qizil yonoqlar, qora soqol ---
            c.FillEllipse(cx, fy - 38, 6, 6, skin);
            c.FillEllipse(cx - 3, fy - 37, 1.6f, 1.2f, new Color(230, 140, 120));
            c.FillEllipse(cx + 4, fy - 37, 1.6f, 1.2f, new Color(230, 140, 120));
            c.FillRect(cx + 1, fy - 40, 2, 1, Pal.Ink);
            c.FillRect(cx - 3, fy - 40, 2, 1, Pal.Ink);
            c.FillBlob(cx, fy - 33, 6, 4.5f, new Color(36, 26, 22), 0.04f);  // soqol
            c.FillRect(cx - 4, fy - 36, 9, 2, new Color(36, 26, 22));         // mo'ylov

            // --- salla va oltin toj ---
            c.FillBlob(cx, fy - 44, 7.5f, 3.6f, new Color(244, 240, 228), 0.03f);
            c.FillRect(cx - 6, fy - 47, 12, 4, gold);                           // toj gardishi
            for (int i = 0; i < 5; i++)
            {
                int px = cx - 6 + i * 3;
                c.FillPolygon(gold, 0f, new Vector2(px - 1, fy - 47), new Vector2(px + 2, fy - 47), new Vector2(px + 0.5f, fy - 53 + (i % 2) * 2));
            }
            c.FillEllipse(cx, fy - 45, 1.6f, 1.4f, new Color(210, 30, 40));     // yoqut
            c.FillEllipse(cx - 4, fy - 45, 1.1f, 1f, new Color(40, 110, 200));  // zumrad/feruza
            c.FillEllipse(cx + 4, fy - 45, 1.1f, 1f, new Color(40, 170, 120));
            c.FillEllipse(cx, fy - 55, 1.2f, 1.2f, new Color(255, 250, 230));   // tepasidagi dur
            c.Line(new Vector2(cx + 2, fy - 50), new Vector2(cx + 6, fy - 57), new Color(250, 250, 255));  // pat
            c.Outline(new Color(30, 20, 16), 0.7f);
            return c;
        }

        public static Canvas Frame(int kind, int frame)
        {
            if (kind == Shah) return ShahFrame(frame);
            var c = new Canvas(40, 54);
            int cx = 20, fy = c.Height - FootOffset;
            Color tunic = kind switch
            {
                Swordsman => new Color(156, 44, 38),
                Spearman => new Color(44, 86, 156),
                Shield => new Color(48, 116, 74),
                Raider => new Color(58, 50, 56),
                RaiderChief => new Color(40, 34, 40),
                _ => new Color(164, 124, 76)
            };
            Color trouser = new Color(74, 58, 46), boot = new Color(46, 32, 24);
            Color skin = new Color(224, 180, 142);

            c.FillEllipse(cx, fy, 9, 3.5f, new Color(0, 0, 0, 70));

            // --- oyoqlar ---
            int step = frame == 1 ? 2 : frame == 2 ? -2 : 0;
            int liftA = frame == 1 ? 1 : 0, liftB = frame == 2 ? 1 : 0;
            c.FillRect(cx - 4 - step, fy - 11 - liftA, 3, 9, trouser);
            c.FillRect(cx - 4 - step, fy - 3 - liftA, 5, 3, boot);
            c.FillRect(cx + 1 + step, fy - 11 - liftB, 3, 9, trouser);
            c.FillRect(cx + 1 + step, fy - 3 - liftB, 5, 3, boot);

            // --- orqadagi qurol (tana ortida) ---
            if (kind == Spearman)
            {
                c.FillRect(cx + 8, fy - 50, 2, 50, new Color(134, 96, 56));
                c.FillPolygon(Pal.Steel, 0f, new Vector2(cx + 6, fy - 46), new Vector2(cx + 12, fy - 46), new Vector2(cx + 9, fy - 54));
                c.FillPolygon(Pal.Red, 0f, new Vector2(cx + 10, fy - 45), new Vector2(cx + 17, fy - 43), new Vector2(cx + 10, fy - 40));
            }
            else if (kind == Patrol)
            {
                c.FillRect(cx + 8, fy - 40, 2, 40, new Color(134, 96, 56));
                c.FillPolygon(Pal.Steel, 0f, new Vector2(cx + 6, fy - 38), new Vector2(cx + 13, fy - 40), new Vector2(cx + 9, fy - 46));
            }

            // --- tana: chopon ---
            c.FillPolygon(tunic, 0.06f, new Vector2(cx - 5, fy - 27), new Vector2(cx + 5, fy - 27), new Vector2(cx + 7, fy - 9), new Vector2(cx - 7, fy - 9));
            c.FillRect(cx - 1, fy - 27, 2, 18, Canvas.Shade(tunic, 0.72f));
            Color belt = kind == Patrol ? new Color(90, 60, 36)
                       : kind == Raider ? new Color(70, 48, 30)
                       : kind == RaiderChief ? new Color(170, 36, 30)
                       : Pal.Gold;
            c.FillRect(cx - 6, fy - 17, 12, 2, belt);
            if (kind == RaiderChief)
            {
                // mo'ynali yelka yopinchig'i
                c.FillBlob(cx, fy - 26, 8.5f, 3.2f, new Color(112, 84, 56), 0.06f);
                c.FillRect(cx - 6, fy - 16, 3, 5, new Color(170, 36, 30)); // osilib turgan belbog' uchi
            }
            else if (kind == Raider)
            {
                // charm nimcha
                c.FillRect(cx - 4, fy - 26, 8, 6, new Color(96, 70, 46), 0.08f);
            }
            if (kind != Patrol && !IsRaider(kind))
            {
                // sovut (ko'krak zirhi)
                c.FillRect(cx - 4, fy - 26, 8, 7, Canvas.Shade(Pal.Steel, 0.9f), 0.08f);
                c.FillRect(cx - 4, fy - 23, 8, 1, Canvas.Shade(Pal.Steel, 0.65f));
            }
            // qo'llar
            c.FillRect(cx - 8, fy - 26, 3, 10, Canvas.Shade(tunic, 0.82f));
            c.FillRect(cx + 5, fy - 26, 3, 10, Canvas.Shade(tunic, 0.82f));
            c.FillRect(cx + 5, fy - 17, 3, 2, skin);
            c.FillRect(cx - 8, fy - 17, 3, 2, skin);

            // --- bosh ---
            c.FillEllipse(cx, fy - 31, 4.4f, 4.6f, skin);
            c.FillRect(cx + 1, fy - 32, 2, 1, Pal.Ink);            // ko'z
            c.FillRect(cx - 1, fy - 29, 5, 2, new Color(60, 40, 28)); // mo'ylov-soqol

            if (IsRaider(kind))
            {
                // qalin qora soqol + yuz chandig'i
                c.FillRect(cx - 2, fy - 29, 6, 3, new Color(30, 22, 18));
                c.FillRect(cx + 2, fy - 33, 1, 3, new Color(170, 110, 90));
                // mo'ynali bo'rk (sardorniki balandroq, ustida qizil popuk)
                Color fur = new Color(122, 92, 60);
                float capH = kind == RaiderChief ? 4.6f : 3.6f;
                c.FillBlob(cx, fy - 36, 6f, capH, fur, 0.08f);
                c.FillRect(cx - 6, fy - 34, 12, 2, Canvas.Shade(fur, 0.7f));
                if (kind == RaiderChief) c.FillEllipse(cx, fy - 41, 1.8f, 1.8f, new Color(190, 40, 34));
            }
            else if (kind == Patrol)
            {
                // salla
                c.FillBlob(cx, fy - 36, 5.6f, 3.6f, new Color(240, 236, 224), 0.03f);
                c.FillRect(cx - 5, fy - 35, 10, 1, new Color(200, 196, 184));
            }
            else
            {
                // uchli turkiy dubulg'a
                Color helm = kind == Shield ? Pal.Gold : Pal.Steel;
                c.FillPolygon(helm, 0.05f, new Vector2(cx - 5, fy - 33), new Vector2(cx + 5, fy - 33), new Vector2(cx + 4, fy - 38), new Vector2(cx, fy - 43), new Vector2(cx - 4, fy - 38));
                c.FillRect(cx, fy - 46, 1, 4, helm);
                c.FillRect(cx - 5, fy - 34, 10, 1, Canvas.Shade(helm, 0.7f));
                c.FillRect(cx - 5, fy - 33, 2, 4, Canvas.Shade(Pal.Steel, 0.75f)); // bo'yin zirhi
                if (kind == Swordsman) c.FillRect(cx + 1, fy - 49, 2, 4, Pal.Red); // jig'a
            }

            // --- old qurollar ---
            if (kind == Swordsman)
            {
                // egri qilich (shamshir)
                c.Line(new Vector2(cx + 7, fy - 17), new Vector2(cx + 11, fy - 27), Pal.Steel, 2);
                c.Line(new Vector2(cx + 11, fy - 27), new Vector2(cx + 16, fy - 34), Pal.Steel, 2);
                c.FillRect(cx + 5, fy - 17, 5, 2, Pal.Gold);
                // kichik qalqon
                c.FillEllipse(cx - 8, fy - 20, 4.5f, 5, new Color(110, 76, 44));
                c.FillEllipse(cx - 8, fy - 20, 1.6f, 1.8f, Pal.Gold);
            }
            else if (kind == Shield)
            {
                // katta dumaloq qalqon
                c.FillEllipse(cx - 6, fy - 19, 8, 9, Pal.Gold);
                c.FillBlob(cx - 6, fy - 19, 6.6f, 7.6f, new Color(150, 40, 36), 0.04f);
                c.FillEllipse(cx - 6, fy - 19, 2.4f, 2.6f, Pal.Gold);
                // gurzi
                c.Line(new Vector2(cx + 7, fy - 16), new Vector2(cx + 11, fy - 28), new Color(134, 96, 56), 2);
                c.FillEllipse(cx + 12, fy - 30, 3, 3, Pal.Steel);
            }
            else if (kind == Spearman)
            {
                c.FillEllipse(cx - 8, fy - 20, 4.5f, 5.5f, new Color(60, 90, 140));
                c.FillEllipse(cx - 8, fy - 20, 1.6f, 1.8f, Pal.Steel);
            }
            else if (kind == Raider)
            {
                // qisqa bolta
                c.Line(new Vector2(cx + 7, fy - 15), new Vector2(cx + 11, fy - 30), new Color(110, 78, 46), 2);
                c.FillPolygon(Pal.Steel, 0.08f, new Vector2(cx + 10, fy - 31), new Vector2(cx + 16, fy - 33), new Vector2(cx + 15, fy - 26), new Vector2(cx + 11, fy - 27));
                // yog'och qalqoncha
                c.FillEllipse(cx - 8, fy - 20, 4.2f, 5f, new Color(92, 64, 40));
                c.FillRect(cx - 9, fy - 25, 2, 10, new Color(60, 42, 28));
            }
            else if (kind == RaiderChief)
            {
                // ikki qo'llik katta oybolta
                c.Line(new Vector2(cx + 6, fy - 10), new Vector2(cx + 12, fy - 40), new Color(96, 66, 40), 2);
                c.FillPolygon(Pal.Steel, 0.1f, new Vector2(cx + 11, fy - 42), new Vector2(cx + 19, fy - 46), new Vector2(cx + 20, fy - 36), new Vector2(cx + 13, fy - 35));
                c.FillPolygon(Canvas.Shade(Pal.Steel, 0.8f), 0.1f, new Vector2(cx + 11, fy - 41), new Vector2(cx + 6, fy - 43), new Vector2(cx + 7, fy - 37), new Vector2(cx + 12, fy - 36));
            }

            c.Outline(new Color(30, 20, 16), 0.7f);
            return c;
        }
    }
}
