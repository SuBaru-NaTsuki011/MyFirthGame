using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Military
{
    /// <summary>
    /// Harbiy unvonlar: Oddiy askar → Onboshi → Ellikboshi → Yuzboshi → Mingboshi → Sarkarda.
    /// Askar jangda, g'alayonni tinchlantirishda va kazarmada mashqda XP oladi; XP chegaradan oshsa
    /// unvon ko'tariladi va oz-ozdan bonus (zarba, mustahkamlik, tezlik) qo'shiladi. Eng yuqori unvon
    /// kazarma darajasi bilan cheklanadi (Promt 2).
    /// </summary>
    public static class MilitaryRank
    {
        public const int Max = 5; // 0..5 (6 unvon)

        private static readonly string[] Names =
            { "Oddiy askar", "Onboshi", "Ellikboshi", "Yuzboshi", "Mingboshi", "Sarkarda" };

        /// <summary>Unvonga YETISH uchun kerakli umumiy XP.</summary>
        private static readonly int[] Thresholds = { 0, 100, 300, 700, 1500, 3000 };

        // Har unvonга ko'tarilganda qo'shiladigan bonus (oz-oz).
        private static readonly int[] AtkBonus = { 0, 1, 2, 3, 5, 7 };   // kumulyativ
        private static readonly int[] HpBonus = { 0, 8, 18, 30, 48, 70 }; // kumulyativ
        private static readonly float[] SpdBonus = { 0f, 0.03f, 0.06f, 0.10f, 0.15f, 0.22f }; // kumulyativ

        public static string Name(int rank) => Names[Clamp(rank)];
        public static int Threshold(int rank) => Thresholds[Clamp(rank)];

        /// <summary>Keyingi unvongacha XP (eng yuqorida 0).</summary>
        public static int NextThreshold(int rank) => rank >= Max ? Thresholds[Max] : Thresholds[rank + 1];

        /// <summary>XP va kazarma chegarasi (maxRank) bo'yicha unvon.</summary>
        public static int RankForXp(int xp, int maxRank)
        {
            int cap = maxRank < 0 ? 0 : (maxRank > Max ? Max : maxRank);
            int r = 0;
            for (int i = 1; i <= cap; i++)
                if (xp >= Thresholds[i]) r = i; else break;
            return r;
        }

        /// <summary><paramref name="from"/> dan <paramref name="to"/> unvonga o'tishda qo'shiladigan bonus farqi.</summary>
        public static (int atk, int hp, float spd) BonusDelta(int from, int to)
        {
            from = Clamp(from); to = Clamp(to);
            return (AtkBonus[to] - AtkBonus[from], HpBonus[to] - HpBonus[from], SpdBonus[to] - SpdBonus[from]);
        }

        /// <summary>Pagondagi belgilar soni (unvon = belgi soni; oddiy askarda 0).</summary>
        public static int Pips(int rank) => Clamp(rank);

        /// <summary>Pagon rangi: quyi unvonlar kumush, yuqorilari oltin.</summary>
        public static Color PipColor(int rank)
        {
            if (rank >= 5) return new Color(255, 215, 90);  // porloq oltin — Sarkarda
            if (rank >= 3) return new Color(222, 178, 92);  // oltin
            return new Color(214, 218, 226);                // kumush
        }

        /// <summary>XP progress (0..1) joriy unvondan keyingisiga.</summary>
        public static float Progress(int xp, int rank)
        {
            if (rank >= Max) return 1f;
            int lo = Thresholds[rank], hi = Thresholds[rank + 1];
            if (hi <= lo) return 1f;
            float p = (xp - lo) / (float)(hi - lo);
            return p < 0f ? 0f : (p > 1f ? 1f : p);
        }

        private static int Clamp(int r) => r < 0 ? 0 : (r > Max ? Max : r);
    }
}
