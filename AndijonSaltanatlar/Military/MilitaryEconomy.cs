using System.Collections.Generic;

namespace AndijonSaltanatlar.Military
{
    /// <summary>Qo'shin saqlash xarajati (kunlik upkeep). EconomyManager bilan integratsiya qilinadi — bir marta hisoblanadi.</summary>
    public static class MilitaryEconomy
    {
        public static int DailyUpkeep(IEnumerable<MilitaryUnit> units)
        {
            int sum = 0;
            foreach (MilitaryUnit u in units) sum += u.UpkeepCost;
            return sum;
        }
    }
}
