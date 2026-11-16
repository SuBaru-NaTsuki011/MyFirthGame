using System;
using AndijonSaltanatlar.Core;

namespace AndijonSaltanatlar.Environment
{
    public enum Weather { Clear, Cloudy, Rainy, Windy, Snowy }

    /// <summary>
    /// Ob-havoni faslga qarab har o'yin kuni yangilaydi. Ishlab chiqarishga yengil ta'sir qiladi.
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
            if (s == Season.Qish) return r < 0.4 ? Weather.Snowy : r < 0.7 ? Weather.Cloudy : Weather.Clear;
            if (s == Season.Yoz) return r < 0.7 ? Weather.Clear : r < 0.85 ? Weather.Windy : Weather.Cloudy;
            // Bahor / Kuz
            return r < 0.45 ? Weather.Clear : r < 0.65 ? Weather.Cloudy : r < 0.85 ? Weather.Rainy : Weather.Windy;
        }

        public float ProductionModifier()
        {
            switch (Current)
            {
                case Weather.Clear: return 0.05f;
                case Weather.Rainy: return -0.05f;
                case Weather.Snowy: return -0.10f;
                default: return 0f;
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
                default: return "Ochiq";
            }
        }

        public void SetCurrent(Weather w) => Current = w;
    }
}
