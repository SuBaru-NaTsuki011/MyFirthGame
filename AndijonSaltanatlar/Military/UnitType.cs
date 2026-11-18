using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;

namespace AndijonSaltanatlar.Military
{
    /// <summary>
    /// Jangchi turlari. Saqlashda butun son sifatida yoziladi — yangi turlar faqat OXIRIGA qo'shiladi
    /// (eski saqlangan o'yinlar buzilmaydi).
    /// </summary>
    public enum UnitType { Piyoda, Qilichboz, Qalqonchi, Nayzador, Kamonchi, Otliq, SaroyQoriqchisi, ElitaOtliq, HarbiyXabarchi, TaminotBirligi }

    /// <summary>Jangdagi vazifasi (katalog kartasi va jang sahnasi uchun).</summary>
    public enum UnitRole { Piyoda, Otliq, Masofaviy, Yordamchi }

    /// <summary>Jangchi turining bazaviy statistikasi (o'yin balansi uchun shartli qiymatlar, tarixiy emas).</summary>
    public sealed class UnitDef
    {
        public UnitType Type;
        public string NameUz;
        public UnitRole Role;
        public int Health, Attack, Defense, Speed;
        public int TrainingCost, TrainingDays, Upkeep;
        /// <summary>Yollash uchun kerakli bino (null — talab yo'q) va uning eng kam darajasi.</summary>
        public BuildingType? RequiredBuilding;
        public int RequiredLevel = 1;
        /// <summary>Katalog kartasidagi qisqa tavsif.</summary>
        public string Hint = "";
        /// <summary>Katalog belgisi uchun sprayt kaliti (kadr qo'shimchasisiz).</summary>
        public string IconKey = "";
        /// <summary>Kuch (karta va taqqoslash uchun): hujum + mudofaa.</summary>
        public int Power => Attack + Defense;
    }

    /// <summary>Kengaytiriladigan jangchilar katalogi (10 tur).</summary>
    public static class UnitCatalog
    {
        public static readonly List<UnitDef> All = new List<UnitDef>
        {
            new UnitDef { Type = UnitType.Piyoda,    NameUz = "Piyoda",    Role = UnitRole.Piyoda, Health = 60,  Attack = 10, Defense = 6,  Speed = 5, TrainingCost = 50,  TrainingDays = 1, Upkeep = 1,
                          Hint = "Arzon, tez tayyorlanadi", IconKey = "unit_spearman" },
            new UnitDef { Type = UnitType.Qilichboz, NameUz = "Qilichboz", Role = UnitRole.Piyoda, Health = 80,  Attack = 16, Defense = 8,  Speed = 5, TrainingCost = 90,  TrainingDays = 2, Upkeep = 2,
                          Hint = "Yaqin jangda kuchli", IconKey = "unit_swordsman" },
            new UnitDef { Type = UnitType.Qalqonchi, NameUz = "Qalqonchi", Role = UnitRole.Piyoda, Health = 120, Attack = 8,  Defense = 18, Speed = 4, TrainingCost = 110, TrainingDays = 2, Upkeep = 2,
                          RequiredBuilding = BuildingType.Barracks, Hint = "Saf oldini ushlab turadi", IconKey = "unit_shield" },
            new UnitDef { Type = UnitType.Nayzador,  NameUz = "Nayzachi",  Role = UnitRole.Piyoda, Health = 90,  Attack = 14, Defense = 10, Speed = 5, TrainingCost = 120, TrainingDays = 3, Upkeep = 2,
                          Hint = "Otliqlarga qarshi", IconKey = "unit_spearman" },
            new UnitDef { Type = UnitType.Kamonchi,  NameUz = "Kamonchi",  Role = UnitRole.Masofaviy, Health = 70, Attack = 18, Defense = 6, Speed = 5, TrainingCost = 140, TrainingDays = 3, Upkeep = 3,
                          RequiredBuilding = BuildingType.Barracks, Hint = "Uzoqdan o'q uzadi", IconKey = "unit_patrol" },
            new UnitDef { Type = UnitType.Otliq,     NameUz = "Otliq",     Role = UnitRole.Otliq,  Health = 140, Attack = 22, Defense = 12, Speed = 9, TrainingCost = 250, TrainingDays = 5, Upkeep = 5,
                          RequiredBuilding = BuildingType.Barracks, Hint = "Tez zarba beradi", IconKey = "unit_cavalry" },
            new UnitDef { Type = UnitType.SaroyQoriqchisi, NameUz = "Saroy qo'riqchisi", Role = UnitRole.Piyoda, Health = 160, Attack = 20, Defense = 22, Speed = 5, TrainingCost = 320, TrainingDays = 4, Upkeep = 6,
                          RequiredBuilding = BuildingType.MainHall, RequiredLevel = 2, Hint = "Saroyni himoya qiladi", IconKey = "unit_elite" },
            new UnitDef { Type = UnitType.ElitaOtliq, NameUz = "Elita otliq", Role = UnitRole.Otliq, Health = 200, Attack = 32, Defense = 18, Speed = 10, TrainingCost = 480, TrainingDays = 6, Upkeep = 8,
                          RequiredBuilding = BuildingType.GladiatorArena, Hint = "Eng kuchli otliqlar", IconKey = "unit_cavalry" },
            new UnitDef { Type = UnitType.HarbiyXabarchi, NameUz = "Harbiy xabarchi", Role = UnitRole.Yordamchi, Health = 50, Attack = 6, Defense = 4, Speed = 12, TrainingCost = 70, TrainingDays = 1, Upkeep = 1,
                          RequiredBuilding = BuildingType.Tower, Hint = "Ruhni ko'taradi, xabar yetkazadi", IconKey = "unit_patrol" },
            new UnitDef { Type = UnitType.TaminotBirligi, NameUz = "Ta'minot birligi", Role = UnitRole.Yordamchi, Health = 90, Attack = 2, Defense = 8, Speed = 4, TrainingCost = 100, TrainingDays = 2, Upkeep = 1,
                          RequiredBuilding = BuildingType.Market, Hint = "Oziq va qurol tashiydi", IconKey = "unit_citizen2" },
        };

        public static UnitDef Get(UnitType t)
        {
            foreach (UnitDef d in All) if (d.Type == t) return d;
            return All[0];
        }

        public static string RoleName(UnitRole r)
        {
            switch (r)
            {
                case UnitRole.Otliq: return "Otliq";
                case UnitRole.Masofaviy: return "Masofaviy";
                case UnitRole.Yordamchi: return "Yordamchi";
                default: return "Piyoda";
            }
        }

        /// <summary>Talab qilinadigan bino nomi (kartada "QULF: ..." uchun).</summary>
        public static string RequirementText(UnitDef d)
        {
            if (d.RequiredBuilding == null) return "";
            string name;
            switch (d.RequiredBuilding.Value)
            {
                case BuildingType.Barracks: name = "Qo'shinxona"; break;
                case BuildingType.GladiatorArena: name = "Jang maydoni"; break;
                case BuildingType.MainHall: name = "Saroy"; break;
                case BuildingType.Tower: name = "Minora"; break;
                case BuildingType.Market: name = "Bozor"; break;
                default: name = d.RequiredBuilding.Value.ToString(); break;
            }
            return d.RequiredLevel > 1 ? name + " " + d.RequiredLevel + "-d." : name;
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
