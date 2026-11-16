using System.Collections.Generic;

namespace AndijonSaltanatlar.Military
{
    public enum UnitType { Piyoda, Qilichboz, Qalqonchi, Nayzador, Kamonchi, Otliq }

    /// <summary>Jangchi turining bazaviy statistikasi (o'yin balansi uchun shartli qiymatlar, tarixiy emas).</summary>
    public sealed class UnitDef
    {
        public UnitType Type;
        public string NameUz;
        public int Health, Attack, Defense, Speed;
        public int TrainingCost, TrainingDays, Upkeep;
    }

    /// <summary>Kengaytiriladigan jangchilar katalogi.</summary>
    public static class UnitCatalog
    {
        public static readonly List<UnitDef> All = new List<UnitDef>
        {
            new UnitDef { Type = UnitType.Piyoda,    NameUz = "Piyoda",    Health = 60,  Attack = 10, Defense = 6,  Speed = 5, TrainingCost = 50,  TrainingDays = 1, Upkeep = 1 },
            new UnitDef { Type = UnitType.Qilichboz, NameUz = "Qilichboz", Health = 80,  Attack = 16, Defense = 8,  Speed = 5, TrainingCost = 90,  TrainingDays = 2, Upkeep = 2 },
            new UnitDef { Type = UnitType.Qalqonchi, NameUz = "Qalqonchi", Health = 120, Attack = 8,  Defense = 18, Speed = 4, TrainingCost = 110, TrainingDays = 2, Upkeep = 2 },
            new UnitDef { Type = UnitType.Nayzador,  NameUz = "Nayzador",  Health = 90,  Attack = 14, Defense = 10, Speed = 5, TrainingCost = 120, TrainingDays = 3, Upkeep = 2 },
            new UnitDef { Type = UnitType.Kamonchi,  NameUz = "Kamonchi",  Health = 70,  Attack = 18, Defense = 6,  Speed = 5, TrainingCost = 140, TrainingDays = 3, Upkeep = 3 },
            new UnitDef { Type = UnitType.Otliq,     NameUz = "Otliq",     Health = 140, Attack = 22, Defense = 12, Speed = 9, TrainingCost = 250, TrainingDays = 5, Upkeep = 5 },
        };

        public static UnitDef Get(UnitType t)
        {
            foreach (UnitDef d in All) if (d.Type == t) return d;
            return All[0];
        }

        public static string LevelName(int level)
        {
            switch (level)
            {
                case 1: return "Yangi";
                case 2: return "Tajribali";
                case 3: return "Mohir";
                case 4: return "Elita";
                default: return "Saltanat faxri";
            }
        }
    }
}
