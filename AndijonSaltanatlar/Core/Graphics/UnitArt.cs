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
        public const int Swordsman = 0, Spearman = 1, Shield = 2, Patrol = 3;
        /// <summary>Oyoq tagi sprite pastidan necha piksel yuqorida.</summary>
        public const int FootOffset = 5;

        public static Canvas Frame(int kind, int frame)
        {
            var c = new Canvas(40, 54);
            int cx = 20, fy = c.Height - FootOffset;
            Color tunic = kind switch
            {
                Swordsman => new Color(156, 44, 38),
                Spearman => new Color(44, 86, 156),
                Shield => new Color(48, 116, 74),
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
            c.FillRect(cx - 6, fy - 17, 12, 2, kind == Patrol ? new Color(90, 60, 36) : Pal.Gold);
            if (kind != Patrol)
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

            if (kind == Patrol)
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

            c.Outline(new Color(30, 20, 16), 0.7f);
            return c;
        }
    }
}
