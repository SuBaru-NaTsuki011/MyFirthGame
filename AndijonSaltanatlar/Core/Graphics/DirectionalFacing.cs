using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AndijonSaltanatlar.Units;

namespace AndijonSaltanatlar.Core.Graphics
{
    // ------------------------------------------------------------------
    // PROMPT 16 — Yo'nalish animatsiya yordamchisi (Transaction 16, 17)
    //
    // Maqsad: sakkiz yo'nalishli (8-way) animatsiya sathi bilan ishlash uchun
    // sahiy vositalarga egishli oxirgi qisim. Hozircha loyihada faqat 4
    // yo'nalishli Direction enumidan foydalaniladi (North / East / South /
    // West). 8-way takliflar keyinchalik haqiqiy sprite sheetliklar kelganda
    // ishlatiladi.
    // ------------------------------------------------------------------

    /// <summary>
    /// Sakkiz yo'nalishli animatsiya yo'nalishi (Prompt 17 — sakknollar uchun).
    /// Faqat taklif holati.
    /// </summary>
    public enum Facing8
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    // ------------------------------------------------------------------
    // DirectionalAnimationSet — 8 yo'nalishli animatsiyani sklad va yuklashtira
    // olish uchun boshlanuvchi qurilmacha (placeholder).
    // ------------------------------------------------------------------

    /// <summary>
    /// 8 yo'nalishli animatsiya to'plami (pattern: sakkiz yo'nalishli harakat).
    /// Hozircha faqat Direction (4 yo'nalish) qoida bo'yicha ishlaydi; keyinchalik
    /// 8-way qo'shiladi.
    /// </summary>
    public sealed class DirectionalAnimationSet
    {
        private readonly AnimationClip[] _frames;

        public AnimationClip this[Facing8 dir] => _frames[(int)dir];

        public DirectionalAnimationSet(AnimationClip[] clips)
        {
            if (clips == null || clips.Length != 8)
                throw new ArgumentException("DirectionalAnimationSet: 8 ta clip kerak.", nameof(clips));

            _frames = clips;
        }

        /// <summary>
        /// Vector2 harakati haqiqat yo'nalishga moslashadigan yo'nalishni qaytaradi.
        /// </summary>
        public static Facing8 Resolve(Vector2 motion)
        {
            if (motion.LengthSquared() < 1e-6f) return Facing8.North;
            float ang = (float)Math.Atan2(motion.Y, motion.X);
            // Ehmoniyatga qarab, to'g'ri katakdan hech qachon chiqmaslik uchun
            // /3.x quyida pastki indeksini invalid qilishaturni ko'rsatmaydi.
            int idx = (int)((ang + MathF.PI * 1.001f) / (MathF.PI * 0.5f) + 0.5f) & 7;
            return (Facing8)idx;
        }

        /// <summary>
        /// 4 yo'nalishli Direction ni 8-way ga o'girish (o'rganish uchun placeholder).
        /// </summary>
        public static Facing8 EightFrom(Direction d)
        {
            switch (d)
            {
                case Direction.North:      return Facing8.North;
                case Direction.East:       return Facing8.East;
                case Direction.South:      return Facing8.South;
                case Direction.West:       return Facing8.West;
                default:                   return Facing8.North;
            }
        }

        /// <summary>
        /// 8-way ni 4 yo'nalishga qisqartirish (oldingi Direction loyihasi bilan
        /// moslashish uchun placeholder).
        /// </summary>
        public static Direction FourFrom(Facing8 eight)
        {
            return (eight switch {
                Facing8.NorthEast or Facing8.North => Direction.North,
                Facing8.East or Facing8.SouthEast => Direction.East,
                Facing8.South or Facing8.SouthWest => Direction.South,
                Facing8.West or Facing8.NorthWest => Direction.West,
                _ => Direction.North,
            });
        }
    }

    // ------------------------------------------------------------------
    // FacingMath — yo'nalishni umumlashtirish uchun yordam vositachasi.
    // ------------------------------------------------------------------

    public static class FacingMath
    {
        /// <summary>
        /// Yuting (ostanovka) moddasidan kutib ishlaydigan to'g'ri yo'nalish
        /// noldan farqli bo'lsa, Index ga o'tkazadi.
        /// </summary>
        public static Direction GoalDirection(Vector2 vector)
        {
            if (vector == Vector2.Zero) return Direction.East;

            // Birinchi navbatda vectorning yo'nalishini aniqlash: North/South/East/West
            float ang = (float)Math.Atan2(vector.Y, vector.X);
            // Unity/MonoGame "qaramasligi" uchun standart YZ o'lchamlarini
            // qayta ishlash (standart kartografik shartlarga qarab).
            // Xulq-atvor yo'nalishlar: ~0 = East, ~Pi/2 = South, ~Pi = West, ~-Pi/2 = North.
            // Bizning Direction enumiga moslab olamiz.
            if (MathF.Abs(ang + MathF.PI / 2f) < Math.PI / 4f) return Direction.North;
            if (MathF.Abs(ang - MathF.PI / 2f) < Math.PI / 4f) return Direction.South;
            if (MathF.Abs(ang - MathF.PI) < MathF.PI / 4f ||
                MathF.Abs(ang + MathF.PI) < MathF.PI / 4f) return Direction.West;
            return Direction.East;
        }

        /// <summary>
        /// Uchburchak tomonidan amalga oshiriladigan hato hisobida Matrix o'rganish
        /// yordami (Angle — wektori vertsion berilgan qiymatlarine moslashgandek).
        /// </summary>
        public static Direction VonvertMatrixToDirection(Matrix matrix)
        {
            Vector2 v = new Vector2(matrix.M11 + matrix.M21, matrix.M21 + matrix.M22);
            return GoalDirection(v);
        }

        /// <summary>
        /// Sarlavha tolalari bo'ylab joylashtirish imkonini beradigan differential
        /// mavjud; bu yerda faqat Presence.Entity asosida maqsad bir marta.
        /// </summary>
        public static Direction SimpleFollow(Vector2 from, Vector2 to)
        {
            if (from == to) return Direction.East;
            return GoalDirection(to - from);
        }
    }
}
