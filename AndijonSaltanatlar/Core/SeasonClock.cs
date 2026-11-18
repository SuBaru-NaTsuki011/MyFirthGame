using AndijonSaltanatlar.Environment;

namespace AndijonSaltanatlar.Core
{
    /// <summary>
    /// Fasllarga bog'liq tong (quyosh chiqishi) va shom (quyosh botishi) soatlari.
    /// Kun-tun aylanishi (DayNightCycle) va VAQT paneli shu qiymatlardan foydalanadi.
    ///   Yoz: tong 05:00, shom 20:00 (kunduz uzun)
    ///   Bahor/Kuz: tong 06:00, shom 18:30
    ///   Qish: tong 07:30, shom 17:00 (kunduz qisqa)
    /// </summary>
    public static class SeasonClock
    {
        /// <summary>Quyosh chiqishi soati (0..24).</summary>
        public static float SunriseHour(Season s) => s switch
        {
            Season.Yoz => 5.0f,
            Season.Qish => 7.5f,
            _ => 6.0f, // Bahor, Kuz
        };

        /// <summary>Quyosh botishi soati (0..24).</summary>
        public static float SunsetHour(Season s) => s switch
        {
            Season.Yoz => 20.0f,
            Season.Qish => 17.0f,
            _ => 18.5f, // Bahor, Kuz
        };

        /// <summary>Tong — kun ulushi (0..1).</summary>
        public static float Sunrise(Season s) => SunriseHour(s) / 24f;

        /// <summary>Shom — kun ulushi (0..1).</summary>
        public static float Sunset(Season s) => SunsetHour(s) / 24f;

        /// <summary>Soatni "HH:MM" ko'rinishida.</summary>
        public static string HourText(float hour)
        {
            int total = (int)(hour * 60f + 0.5f);
            return (total / 60 % 24).ToString("00") + ":" + (total % 60).ToString("00");
        }

        public static string SunriseText(Season s) => HourText(SunriseHour(s));
        public static string SunsetText(Season s) => HourText(SunsetHour(s));
    }
}
