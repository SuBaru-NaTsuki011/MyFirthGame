using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>
    /// Fuqarolar uchun placeholder sprite'lar: to'n kiygan, qurolsiz yurувchi figura.
    /// Har bir tur uchun 3 kadr (0 — turish, 1-2 — yurish). Jangchilar bilan bir uslubda.
    /// </summary>
    public static class CitizenArt
    {
        public const int Citizen = 0, Merchant = 1, Craftsman = 2, Farmer = 3, Buyer = 4;
        public const int Kinds = 5;

        private static readonly Color[] Robes =
        {
            new Color(74, 112, 170),   // fuqaro — ko'k
            new Color(78, 150, 96),    // savdogar — yashil
            new Color(150, 110, 70),   // hunarmand — jigarrang
            new Color(182, 160, 102),  // dehqon — qumrang
            new Color(150, 72, 82)     // xaridor — bordo
        };

        private static readonly Color[] Caps =
        {
            new Color(240, 240, 245),
            new Color(58, 120, 70),
            new Color(110, 82, 52),
            new Color(120, 100, 60),
            new Color(92, 52, 58)
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

            // to'n — pastga kengayadigan
            c.FillPolygon(robe, 0.06f,
                new Vector2(cx - 6, fy - 2), new Vector2(cx + 6, fy - 2),
                new Vector2(cx + 4, fy - 20), new Vector2(cx - 4, fy - 20));
            // belbog'
            c.FillRect(cx - 5, fy - 14, 10, 2, Canvas.Shade(robe, 0.7f));
            // ko'krak/yoqa
            c.FillRect(cx - 3, fy - 22, 6, 4, Canvas.Shade(robe, 1.12f));

            // qo'llar (yurishda tebranadi)
            int sw = frame == 1 ? 1 : frame == 2 ? -1 : 0;
            c.FillRect(cx - 6, fy - 20 + sw, 2, 8, robe);
            c.FillRect(cx + 4, fy - 20 - sw, 2, 8, robe);

            // bosh
            c.FillEllipse(cx, fy - 26, 4, 4.5f, skin);
            // salla / do'ppi
            c.FillEllipse(cx, fy - 29, 5, 3.2f, cap);
            c.FillRect(cx - 5, fy - 28, 10, 2, cap);

            c.Outline(new Color(30, 24, 20), 0.5f);
            return c;
        }
    }
}
