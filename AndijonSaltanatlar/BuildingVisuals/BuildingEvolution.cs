using AndijonSaltanatlar.Buildings;

namespace AndijonSaltanatlar.BuildingVisuals
{
    /// <summary>
    /// Bino darajasining vizual ifodasi. Hozir: daraja oshganda bino ustida oltin "daraja nishoni"
    /// chiziladi (WorldRenderer) va uy ko'rinish varianti almashadi. To'liq har-daraja sprite'lari
    /// haqiqiy grafik assetlar kiritilganda shu yerdan boshqariladi (kengaytiriladigan nuqta).
    /// </summary>
    public static class BuildingEvolution
    {
        /// <summary>Daraja nishoni sonining (pips) — 0 agar 1-daraja.</summary>
        public static int BadgePips(Building b) => b == null ? 0 : (b.Level > 1 ? b.Level - 1 : 0);

        /// <summary>Uy ko'rinish varianti darajaga qarab (mavjud 4 variantdan). Boshqa binolar o'zgarmaydi.</summary>
        public static int HouseVariantForLevel(int level, int variantCount)
        {
            if (variantCount <= 0) return 0;
            return ((level - 1) % variantCount + variantCount) % variantCount;
        }
    }
}
