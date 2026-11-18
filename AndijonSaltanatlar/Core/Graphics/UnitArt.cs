namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>
    /// Jangchi turlari va kadr o'lchovlari. Barcha jangchilar rasmi — Bobur bobo 3D modellaridan
    /// (Content/Sprites/unit_*.png). Eski piksel (kod bilan chizilgan) personajlar olib tashlangan;
    /// Frame/AttackFrame faqat shaffof bo'sh kanvas qaytaradi — PNG topilmagan favqulodda holat uchun.
    /// </summary>
    public static class UnitArt
    {
        public const int Swordsman = 0, Spearman = 1, Shield = 2, Patrol = 3, Raider = 4, RaiderChief = 5, Shah = 6;
        /// <summary>Xos navkar (VIP).</summary>
        public const int Elite = 7;

        /// <summary>Oyoq tagi sprite pastidan necha (o'yin) piksel yuqorida.</summary>
        public const int FootOffset = 5;

        /// <summary>Zarba turlari soni (kesish, sanchish, qalqon) va har biridagi bosqichlar (ko'tarish, urish, davomi).</summary>
        public const int AttackKinds = 3, AttackPhases = 3;

        /// <summary>Bo'sh (shaffof) oddiy kadr — haqiqiy rasm PNG'dan yuklanadi.</summary>
        public static Canvas Frame(int kind, int frame) => new Canvas(48, 60);

        /// <summary>Bo'sh (shaffof) zarba kadri — haqiqiy rasm PNG'dan yuklanadi.</summary>
        public static Canvas AttackFrame(int kind, int attack, int phase) => new Canvas(76, 64);
    }
}
