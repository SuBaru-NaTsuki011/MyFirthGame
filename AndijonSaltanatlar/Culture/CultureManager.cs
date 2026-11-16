using AndijonSaltanatlar.Buildings;

namespace AndijonSaltanatlar.Culture
{
    /// <summary>
    /// Madaniyat ballari ishlab chiqarishni hisoblaydi: masjid, madrasa, bog'lardan + texnologik bonus.
    /// Madaniyat harbiy/iqtisodiy ko'rsatkichlardan mustaqil — tinch, ilmiy saltanat ham gullaydi.
    /// </summary>
    public static class CultureManager
    {
        public static int DailyCulture(BuildingManager b, float cultureBonus)
        {
            int baseC = b.Count(BuildingType.Mosque) * 4 + b.Count(BuildingType.Madrasa) * 3
                      + b.Count(BuildingType.Garden) * 1 + 1;
            return (int)(baseC * (1f + cultureBonus));
        }
    }
}
