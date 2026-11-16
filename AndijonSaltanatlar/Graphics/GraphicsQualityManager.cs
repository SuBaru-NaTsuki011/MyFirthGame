namespace AndijonSaltanatlar.Graphics
{
    public enum GraphicsQuality { Past, Orta, Yuqori }

    /// <summary>
    /// Grafika sifati — ob-havo zarralari va dekoratsiya zichligini boshqaradi. Kuchsiz GPU uchun
    /// "Past" rejimi yengilroq. O'yin barqaror ishlashi birinchi o'rinda.
    /// </summary>
    public static class GraphicsQualityManager
    {
        public static GraphicsQuality Current = GraphicsQuality.Orta;

        public static void Cycle()
            => Current = Current == GraphicsQuality.Yuqori ? GraphicsQuality.Past : Current + 1;

        /// <summary>Ob-havo zarralari soni (sifatga qarab).</summary>
        public static int ParticleCount
        {
            get
            {
                switch (Current)
                {
                    case GraphicsQuality.Past: return 40;
                    case GraphicsQuality.Yuqori: return 200;
                    default: return 110;
                }
            }
        }

        public static string Name()
        {
            switch (Current)
            {
                case GraphicsQuality.Past: return "Past";
                case GraphicsQuality.Yuqori: return "Yuqori";
                default: return "O'rta";
            }
        }
    }
}
