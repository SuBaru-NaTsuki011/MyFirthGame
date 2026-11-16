namespace AndijonSaltanatlar.Environment
{
    public enum Season { Bahor, Yoz, Kuz, Qish }

    /// <summary>
    /// Fasllarni o'yin oyidan aniqlaydi (har 3 oy — bir fasl) va ishlab chiqarishga ta'sirini beradi.
    /// Andijon iqlimidan ilhomlangan o'yin modeli — tarixiy ob-havo prognozi emas.
    /// </summary>
    public static class SeasonManager
    {
        public static Season Of(int month)
        {
            int idx = ((month - 1) / 3) % 4;
            if (idx < 0) idx += 4;
            return (Season)idx;
        }

        public static string Name(Season s)
        {
            switch (s)
            {
                case Season.Bahor: return "Bahor";
                case Season.Yoz: return "Yoz";
                case Season.Kuz: return "Kuz";
                default: return "Qish";
            }
        }

        /// <summary>Oziq-ovqat ishlab chiqarishga fasl ta'siri (koeffitsient).</summary>
        public static float FoodModifier(Season s)
        {
            switch (s)
            {
                case Season.Bahor: return 0.10f;
                case Season.Yoz: return 0.0f;
                case Season.Kuz: return 0.15f;   // hosil davri
                default: return -0.15f;          // qish
            }
        }
    }
}
