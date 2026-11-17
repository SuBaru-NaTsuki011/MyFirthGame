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
        /// <summary>Xos navkar (VIP): oltin sovut, qirmizi to'n va yopinchiq, oq patli oltin dubulg'a, oltin qalqon.</summary>
        public const int Elite = 7;

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

        /// <summary>Oddiy kadr: 0 — turish, 1-2 — yurish.</summary>
        public static Canvas Frame(int kind, int frame) => Build(kind, frame, null);

        /// <summary>Zarba turlari soni (kesish, chopish, sanchish) va har biridagi bosqichlar (ko'tarish, urish, davomi).</summary>
        public const int AttackKinds = 3, AttackPhases = 3;

        /// <summary>
        /// Zarba kadri: qurolli qo'l haqiqatan harakatlanadi — ko'tarish (kuch yig'ish), urish (qurol oldinga), davomi
        /// (qurol pastga/yonga o'tib ketadi). Kanvas kengroq (76x64), oyoq osti markazda — oddiy kadrlar bilan bir joyda turadi.
        /// </summary>
        public static Canvas AttackFrame(int kind, int attack, int phase) => Build(kind, 0, ArmPose.For(kind, attack, phase));

        /// <summary>Zarba paytidagi qo'l va qurol holati (graduslarda; 0 — oldinga, -90 — tepaga, 90 — pastga).</summary>
        public sealed class ArmPose
        {
            public float Arm, Weapon;
            public int Lean, Stance;
            public bool Behind => MathF.Cos(MathHelper.ToRadians(Arm)) < -0.35f;

            private static readonly float[,] Table =
            {
                // kesish: orqaga ko'tarish -> gorizontal urish -> pastga o'tib ketish
                { -140, -120, -1, 0 }, { -15, -5, 2, 3 }, { 35, 62, 2, 3 },
                // chopish: tepaga ko'tarish -> oldinda tushirish -> yerga urish
                { -105, -98, -2, 1 }, { -45, -22, 2, 3 }, { 42, 78, 3, 3 },
                // sanchish: orqaga tortish -> to'liq cho'zilish -> qaytish
                { 165, 0, -2, 1 }, { 0, 0, 3, 4 }, { 20, 12, 1, 3 },
            };

            public static ArmPose For(int kind, int attack, int phase)
            {
                int row = Math.Clamp(attack, 0, AttackKinds - 1) * AttackPhases + Math.Clamp(phase, 0, AttackPhases - 1);
                return new ArmPose { Arm = Table[row, 0], Weapon = Table[row, 1], Lean = (int)Table[row, 2], Stance = (int)Table[row, 3] };
            }
        }

        /// <summary>Qurolli qo'l: yelkadan qo'lgacha, keyin qurol (har bir jangchi turiga o'z quroli).</summary>
        private static void DrawArmWeapon(Canvas c, int kind, int cx, int fy, ArmPose pose, Color tunic, Color skin)
        {
            Vector2 S = new Vector2(cx + 6, fy - 25);
            float a = MathHelper.ToRadians(pose.Arm), w = MathHelper.ToRadians(pose.Weapon);
            Vector2 ad = new Vector2(MathF.Cos(a), MathF.Sin(a));
            Vector2 H = S + ad * 8f;
            Vector2 d = new Vector2(MathF.Cos(w), MathF.Sin(w));
            Vector2 n = new Vector2(-d.Y, d.X);
            Color wood = new Color(134, 96, 56), steelHi = new Color(232, 238, 246);
            switch (kind)
            {
                case Spearman:
                    c.Line(H - d * 14, H + d * 16, wood, 2);
                    c.FillPolygon(Pal.Steel, 0f, H + d * 16 + n * 2.6f, H + d * 16 - n * 2.6f, H + d * 24);
                    c.FillPolygon(Pal.Red, 0f, H + d * 12, H + d * 15, H + d * 13.5f - n * 6f);
                    break;
                case Patrol:
                    c.Line(H - d * 10, H + d * 12, wood, 2);
                    c.FillPolygon(Pal.Steel, 0f, H + d * 12 + n * 2.4f, H + d * 12 - n * 2.4f, H + d * 19);
                    break;
                case Shield:
                    // gurzi
                    c.Line(H - d * 2, H + d * 11, wood, 2);
                    c.FillEllipse((H + d * 13).X, (H + d * 13).Y, 3.4f, 3.4f, Pal.Steel);
                    c.FillRect((int)(H + d * 13 - n * 4).X, (int)(H + d * 13 - n * 4).Y, 2, 2, Pal.Steel);
                    c.FillRect((int)(H + d * 13 + n * 4).X, (int)(H + d * 13 + n * 4).Y, 2, 2, Pal.Steel);
                    c.FillRect((int)(H + d * 17).X, (int)(H + d * 17).Y, 2, 2, Pal.Steel);
                    break;
                case Raider:
                    // qisqa bolta
                    c.Line(H - d * 2, H + d * 13, new Color(110, 78, 46), 2);
                    {
                        Vector2 e = H + d * 12;
                        c.FillPolygon(Pal.Steel, 0.06f, e - d * 1 - n * 1, e + d * 3 - n * 1, e + d * 4 - n * 7, e - d * 2 - n * 6);
                    }
                    break;
                case RaiderChief:
                    // ikki qo'llik katta oybolta
                    c.Line(H - d * 6, H + d * 21, new Color(96, 66, 40), 2);
                    {
                        Vector2 e = H + d * 18;
                        c.FillPolygon(Pal.Steel, 0.08f, e - d * 2 - n * 1, e + d * 3 - n * 1, e + d * 5 - n * 9, e - d * 4 - n * 8);
                        c.FillPolygon(Canvas.Shade(Pal.Steel, 0.8f), 0.08f, e - d * 1 + n * 1, e + d * 2 + n * 1, e + d * 3 + n * 5, e - d * 2 + n * 5);
                    }
                    break;
                default:
                    // egri qilich (shamshir): sopi, oltin ushlagich va bukilgan tig'
                    c.Line(H - d * 2, H, new Color(90, 60, 36), 2);
                    c.Line(H + d * 1 + n * 3, H + d * 1 - n * 3, Pal.Gold, 2);
                    c.Line(H + d * 2, H + d * 10, Pal.Steel, 2);
                    c.Line(H + d * 10, H + d * 17 - n * 2.5f, Pal.Steel, 2);
                    c.Line(H + d * 3 - n * 0.6f, H + d * 9 - n * 0.6f, steelHi, 1);
                    break;
            }
            c.Line(S, H, Canvas.Shade(tunic, 0.82f), 3);
            c.FillRect((int)H.X - 1, (int)H.Y - 1, 3, 3, skin);
        }

        private static Canvas Build(int kind, int frame, ArmPose pose)
        {
            if (kind == Shah) return ShahFrame(frame);
            var c = pose == null ? new Canvas(40, 54) : new Canvas(76, 64);
            int cx = c.Width / 2, fy = c.Height - FootOffset;
            Color tunic = kind switch
            {
                Swordsman => new Color(156, 44, 38),
                Spearman => new Color(44, 86, 156),
                Shield => new Color(48, 116, 74),
                Raider => new Color(58, 50, 56),
                RaiderChief => new Color(40, 34, 40),
                Elite => new Color(150, 26, 34),
                _ => new Color(164, 124, 76)
            };
            Color trouser = new Color(74, 58, 46), boot = new Color(46, 32, 24);
            Color skin = new Color(224, 180, 142);

            c.FillEllipse(cx, fy, 9, 3.5f, new Color(0, 0, 0, 70));

            // --- oyoqlar ---
            int step = pose != null ? pose.Stance : frame == 1 ? 2 : frame == 2 ? -2 : 0;
            int liftA = pose == null && frame == 1 ? 1 : 0, liftB = pose == null && frame == 2 ? 1 : 0;
            c.FillRect(cx - 4 - step, fy - 11 - liftA, 3, 9, trouser);
            c.FillRect(cx - 4 - step, fy - 3 - liftA, 5, 3, boot);
            c.FillRect(cx + 1 + step, fy - 11 - liftB, 3, 9, trouser);
            c.FillRect(cx + 1 + step, fy - 3 - liftB, 5, 3, boot);
            // Zarbada tana oldinga/orqaga engashadi (oyoqlar joyida qoladi)
            if (pose != null) cx += pose.Lean;

            // --- orqadagi qurol (tana ortida) ---
            if (pose == null && kind == Spearman)
            {
                c.FillRect(cx + 8, fy - 50, 2, 50, new Color(134, 96, 56));
                c.FillPolygon(Pal.Steel, 0f, new Vector2(cx + 6, fy - 46), new Vector2(cx + 12, fy - 46), new Vector2(cx + 9, fy - 54));
                c.FillPolygon(Pal.Red, 0f, new Vector2(cx + 10, fy - 45), new Vector2(cx + 17, fy - 43), new Vector2(cx + 10, fy - 40));
            }
            else if (pose == null && kind == Patrol)
            {
                c.FillRect(cx + 8, fy - 40, 2, 40, new Color(134, 96, 56));
                c.FillPolygon(Pal.Steel, 0f, new Vector2(cx + 6, fy - 38), new Vector2(cx + 13, fy - 40), new Vector2(cx + 9, fy - 46));
            }

            // Qurol orqada (ko'tarish bosqichi) — tana ortida chiziladi
            if (pose != null && pose.Behind) DrawArmWeapon(c, kind, cx, fy, pose, tunic, skin);

            if (kind == Elite)
            {
                // qirmizi yopinchiq (orqada)
                c.FillPolygon(new Color(112, 18, 26), 0.04f, new Vector2(cx - 6, fy - 27), new Vector2(cx + 5, fy - 27), new Vector2(cx + 8, fy - 6), new Vector2(cx - 10, fy - 6));
            }

            // --- tana: chopon ---
            c.FillPolygon(tunic, 0.06f, new Vector2(cx - 5, fy - 27), new Vector2(cx + 5, fy - 27), new Vector2(cx + 7, fy - 9), new Vector2(cx - 7, fy - 9));
            c.FillRect(cx - 1, fy - 27, 2, 18, Canvas.Shade(tunic, 0.72f));
            Color belt = kind == Elite ? Pal.Gold : kind == Patrol ? new Color(90, 60, 36)
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
                // sovut (ko'krak zirhi); xos navkarniki — oltin
                Color armor = kind == Elite ? Pal.Gold : Canvas.Shade(Pal.Steel, 0.9f);
                c.FillRect(cx - 4, fy - 26, 8, 7, armor, 0.08f);
                c.FillRect(cx - 4, fy - 23, 8, 1, Canvas.Shade(kind == Elite ? Pal.Gold : Pal.Steel, 0.65f));
                if (kind == Elite) { c.FillRect(cx - 5, fy - 27, 2, 3, Pal.Gold); c.FillRect(cx + 3, fy - 27, 2, 3, Pal.Gold); } // yelkaliklar
            }
            // qo'llar
            c.FillRect(cx - 8, fy - 26, 3, 10, Canvas.Shade(tunic, 0.82f));
            c.FillRect(cx - 8, fy - 17, 3, 2, skin);
            if (pose == null)
            {
                c.FillRect(cx + 5, fy - 26, 3, 10, Canvas.Shade(tunic, 0.82f));
                c.FillRect(cx + 5, fy - 17, 3, 2, skin);
            }

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
                Color helm = kind == Shield || kind == Elite ? Pal.Gold : Pal.Steel;
                c.FillPolygon(helm, 0.05f, new Vector2(cx - 5, fy - 33), new Vector2(cx + 5, fy - 33), new Vector2(cx + 4, fy - 38), new Vector2(cx, fy - 43), new Vector2(cx - 4, fy - 38));
                c.FillRect(cx, fy - 46, 1, 4, helm);
                c.FillRect(cx - 5, fy - 34, 10, 1, Canvas.Shade(helm, 0.7f));
                c.FillRect(cx - 5, fy - 33, 2, 4, Canvas.Shade(Pal.Steel, 0.75f)); // bo'yin zirhi
                if (kind == Swordsman) c.FillRect(cx + 1, fy - 49, 2, 4, Pal.Red); // jig'a
                if (kind == Elite)
                {
                    // oq pat va qizil toshli jig'a
                    c.FillRect(cx, fy - 53, 2, 8, new Color(246, 244, 240));
                    c.FillRect(cx + 2, fy - 52, 2, 5, new Color(232, 230, 226));
                    c.FillEllipse(cx + 0.5f, fy - 40, 1.3f, 1.3f, Pal.Red);
                }
            }

            if (pose == null)
            {
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
                else if (kind == Elite)
                {
                    // oltin sopli shamshir va oltin qalqon (qizil markaz)
                    c.Line(new Vector2(cx + 7, fy - 17), new Vector2(cx + 11, fy - 27), Pal.Steel, 2);
                    c.Line(new Vector2(cx + 11, fy - 27), new Vector2(cx + 16, fy - 34), Pal.Steel, 2);
                    c.FillRect(cx + 5, fy - 17, 6, 2, Pal.Gold);
                    c.FillEllipse(cx - 8, fy - 20, 5.2f, 6, Pal.Gold);
                    c.FillEllipse(cx - 8, fy - 20, 3.4f, 4, new Color(160, 30, 36));
                    c.FillEllipse(cx - 8, fy - 20, 1.4f, 1.6f, Pal.Gold);
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
            }
            else
            {
                // Zarba kadri: chap qo'ldagi qalqonlar joyida, o'ng qo'l qurol bilan harakatda
                if (kind == Swordsman)
                {
                    c.FillEllipse(cx - 8, fy - 20, 4.5f, 5, new Color(110, 76, 44));
                    c.FillEllipse(cx - 8, fy - 20, 1.6f, 1.8f, Pal.Gold);
                }
                else if (kind == Elite)
                {
                    c.FillEllipse(cx - 8, fy - 20, 5.2f, 6, Pal.Gold);
                    c.FillEllipse(cx - 8, fy - 20, 3.4f, 4, new Color(160, 30, 36));
                    c.FillEllipse(cx - 8, fy - 20, 1.4f, 1.6f, Pal.Gold);
                }
                else if (kind == Shield)
                {
                    c.FillEllipse(cx - 6, fy - 19, 8, 9, Pal.Gold);
                    c.FillBlob(cx - 6, fy - 19, 6.6f, 7.6f, new Color(150, 40, 36), 0.04f);
                    c.FillEllipse(cx - 6, fy - 19, 2.4f, 2.6f, Pal.Gold);
                }
                else if (kind == Spearman)
                {
                    c.FillEllipse(cx - 8, fy - 20, 4.5f, 5.5f, new Color(60, 90, 140));
                    c.FillEllipse(cx - 8, fy - 20, 1.6f, 1.8f, Pal.Steel);
                }
                else if (kind == Raider)
                {
                    c.FillEllipse(cx - 8, fy - 20, 4.2f, 5f, new Color(92, 64, 40));
                    c.FillRect(cx - 9, fy - 25, 2, 10, new Color(60, 42, 28));
                }
                if (!pose.Behind) DrawArmWeapon(c, kind, cx, fy, pose, tunic, skin);
            }

            c.Outline(new Color(30, 20, 16), 0.7f);
            return c;
        }
    }
}
