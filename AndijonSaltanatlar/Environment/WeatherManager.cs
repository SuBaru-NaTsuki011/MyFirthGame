using System;
using AndijonSaltanatlar.Core;

namespace AndijonSaltanatlar.Environment
{
    public enum Weather { Clear, Cloudy, Rainy, Windy, Snowy, Hot, Storm }

    /// <summary>
    /// Ob-havoni faslga qarab har o'yin kuni yangilaydi. Hosil, aholi kayfiyati va jangga ta'sir qiladi.
    /// Belgilangan seed — takrorlanuvchan. Vizual effekt EnvironmentRenderer'da (yengil zarralar).
    /// </summary>
    public sealed class WeatherManager
    {
        private readonly Random _rng = new Random(GameConfig.WorldSeed + 321);
        private int _lastDay = -1;

        public Weather Current { get; private set; } = Weather.Clear;

        public void Update(int day, Season season)
        {
            if (day == _lastDay) return;
            _lastDay = day;
            Current = Pick(season);
        }

        private Weather Pick(Season s)
        {
            double r = _rng.NextDouble();
            if (s == Season.Qish) return r < 0.55 ? Weather.Snowy : r < 0.75 ? Weather.Cloudy : r < 0.9 ? Weather.Clear : Weather.Storm;
            if (s == Season.Yoz) return r < 0.5 ? Weather.Clear : r < 0.78 ? Weather.Hot : r < 0.9 ? Weather.Windy : Weather.Cloudy;
            if (s == Season.Bahor) return r < 0.32 ? Weather.Clear : r < 0.48 ? Weather.Cloudy : r < 0.78 ? Weather.Rainy : r < 0.9 ? Weather.Windy : Weather.Storm;
            // Kuz: bulutli, shamolli, yomg'ir, ba'zan bo'ron
            return r < 0.3 ? Weather.Clear : r < 0.52 ? Weather.Cloudy : r < 0.72 ? Weather.Windy : r < 0.9 ? Weather.Rainy : Weather.Storm;
        }

        /// <summary>Hosil/ishlab chiqarishga ta'sir (ulush): jazirama va bo'ron hosilni kamaytiradi.</summary>
        public float ProductionModifier() => ProductionModifier(Current);
        public static float ProductionModifier(Weather w)
        {
            switch (w)
            {
                case Weather.Clear: return 0.05f;
                case Weather.Rainy: return -0.04f;   // ish sekinroq, lekin ekinga foydali — yengil manfiy
                case Weather.Snowy: return -0.10f;
                case Weather.Hot: return -0.08f;     // jazirama — qurg'oqchilik
                case Weather.Storm: return -0.14f;   // bo'ron — hosil shikastlanadi
                default: return 0f;
            }
        }

        /// <summary>Aholi kayfiyatiga ta'sir (ulush): noqulay ob-havo mamnunlikni kamaytiradi.</summary>
        public float MoodModifier() => MoodModifier(Current);
        public static float MoodModifier(Weather w)
        {
            switch (w)
            {
                case Weather.Clear: return 0.04f;
                case Weather.Cloudy: return 0f;
                case Weather.Rainy: return -0.02f;
                case Weather.Windy: return -0.02f;
                case Weather.Snowy: return -0.05f;
                case Weather.Hot: return -0.06f;
                case Weather.Storm: return -0.08f;
                default: return 0f;
            }
        }

        /// <summary>Jangga ta'sir: 1.0 — normal; bo'ron/qor jang kuchини pasaytiradi (ikkala tomonga ham).</summary>
        public float BattleModifier() => BattleModifier(Current);
        public static float BattleModifier(Weather w)
        {
            switch (w)
            {
                case Weather.Storm: return 0.85f;
                case Weather.Snowy: return 0.90f;
                case Weather.Rainy: return 0.94f;
                case Weather.Hot: return 0.95f;
                default: return 1.0f;
            }
        }

        public string Name() => NameOf(Current);

        public static string NameOf(Weather w)
        {
            switch (w)
            {
                case Weather.Cloudy: return "Bulutli";
                case Weather.Rainy: return "Yomg'irli";
                case Weather.Windy: return "Shamolli";
                case Weather.Snowy: return "Qorli";
                case Weather.Hot: return "Jazirama";
                case Weather.Storm: return "Bo'ron";
                default: return "Ochiq";
            }
        }

        public void SetCurrent(Weather w) => Current = w;
    }
}
