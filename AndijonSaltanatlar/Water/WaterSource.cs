using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Water
{
    public enum WaterSourceType
    {
        Well = 0,
        Canal = 1,
        Pond = 2,
        Fountain = 3
    }

    /// <summary>
    /// Bitta suv manbasi. Bu o'yin uchun soddalashtirilgan model: haqiqiy gidravlika yo'q,
    /// lekin manba sig'imi, xizmat radiusi va mavsumiy o'zgarishi HAQIQIY hisoblanadi.
    /// </summary>
    public sealed class WaterSource
    {
        public int Id { get; set; }
        public WaterSourceType Type { get; set; }
        public string NameUz { get; set; } = "";
        public Point Position { get; set; }
        /// <summary>Shu manba bir kunda nechta iste'molchiga yetadi (birlik).</summary>
        public float Capacity { get; set; }
        /// <summary>Xizmat radiusi (katak).</summary>
        public int ServiceRadius { get; set; }
        /// <summary>Suv sifati 0..1 (quduq toza, ochiq ariq pastroq).</summary>
        public float Quality { get; set; } = 1f;
        /// <summary>Mavsumiy ko'paytiruvchi (yozda ariq sayoz, bahorda to'la).</summary>
        public float SeasonalModifier { get; set; } = 1f;
        /// <summary>Bezak Id'si (quduq/hovuz uchun) — o'chirilganda manba ham yo'qoladi.</summary>
        public int DecorationId { get; set; }
        /// <summary>Suv hududi (ariq/daryo) uchun nechta katakni qamraydi.</summary>
        public int TilesCovered { get; set; } = 1;

        public float EffectiveCapacity => Capacity * SeasonalModifier * Quality;

        public static string TypeNameUz(WaterSourceType t)
        {
            switch (t)
            {
                case WaterSourceType.Well: return "Quduq";
                case WaterSourceType.Canal: return "Ariq";
                case WaterSourceType.Pond: return "Suv havzasi";
                default: return "Favvora";
            }
        }
    }
}
