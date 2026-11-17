using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Core
{
    /// <summary>
    /// O'yin vaqti: real soniyalarni "kun"larga aylantiradi. Kun hisobi UI va iqtisodiyot
    /// uchun ishlatiladi. Keyin fasllar, tun/kun aylanishi shu yerga qo'shiladi.
    /// </summary>
    public sealed class GameClock
    {
        private float _elapsed;

        /// <summary>Bir o'yin kuni necha real soniya davom etadi.</summary>
        public float SecondsPerDay { get; set; } = 30f;

        public const int DaysPerMonth = 24;
        public const int MonthsPerYear = 12;

        public int Day { get; private set; } = 1;
        public int Month => (Day - 1) / DaysPerMonth % MonthsPerYear + 1;
        public int Year => 1 + (Day - 1) / (DaysPerMonth * MonthsPerYear);

        /// <summary>Joriy kun ichidagi ulush (0..1).</summary>
        public float DayFraction => SecondsPerDay <= 0f ? 0f : _elapsed / SecondsPerDay;

        public void Update(float dt)
        {
            _elapsed += dt;
            while (_elapsed >= SecondsPerDay)
            {
                _elapsed -= SecondsPerDay;
                Day++;
            }
        }

        /// <summary>Kun uzunligini o'zgartiradi — kun ichidagi payt (ulush) saqlanadi.</summary>
        public void SetDayLength(float seconds)
        {
            seconds = MathHelper.Clamp(seconds, 5f, 4f * 3600f);
            float frac = DayFraction;
            SecondsPerDay = seconds;
            _elapsed = MathHelper.Clamp(frac, 0f, 0.999f) * seconds;
        }

        /// <summary>Joriy soat (0..23).</summary>
        public int Hour => (int)(DayFraction * 24f) % 24;

        /// <summary>Saqlangan o'yindan tiklash uchun.</summary>
        public void Set(int day, float fraction)
        {
            Day = day < 1 ? 1 : day;
            _elapsed = MathHelper.Clamp(fraction, 0f, 0.999f) * SecondsPerDay;
        }
    }
}
