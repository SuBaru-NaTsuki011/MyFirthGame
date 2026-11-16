using AndijonSaltanatlar.Buildings;

namespace AndijonSaltanatlar.Defense
{
    /// <summary>
    /// Shahar mudofaa ko'rsatkichini hisoblaydi: minoralar, devorlar, darvozalar, qarorgoh va
    /// garnizon (qo'riqchi qo'shin) kuchidan. Strategik janglarda himoya bonusi sifatida ishlatiladi.
    /// </summary>
    public static class CityDefenseManager
    {
        public static int Compute(BuildingManager buildings, int garrisonStrength)
        {
            int towers = buildings.Count(BuildingType.Tower);
            int walls = buildings.Count(BuildingType.Wall);
            int gates = buildings.Count(BuildingType.Gate);
            int barracks = buildings.Count(BuildingType.Barracks);

            int defense = towers * 15 + walls * 2 + gates * 6 + barracks * 10 + garrisonStrength / 3;
            return defense;
        }
    }
}
