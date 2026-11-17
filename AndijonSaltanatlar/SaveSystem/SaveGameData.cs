using System.Collections.Generic;
using AndijonSaltanatlar.AIGovernor;
using AndijonSaltanatlar.Civilization;
using AndijonSaltanatlar.Military;
using AndijonSaltanatlar.Population;
using AndijonSaltanatlar.World;

namespace AndijonSaltanatlar.SaveSystem
{
    /// <summary>O'yinchi qurgan bitta bino haqida saqlanadigan ma'lumot.</summary>
    public sealed class SavedBuilding
    {
        public int Type { get; set; }       // BuildingType (int)
        public int X { get; set; }
        public int Y { get; set; }
        public int Variant { get; set; }
        public int HitPoints { get; set; }
        public int Level { get; set; } = 1;
    }

    /// <summary>O'yinchi qurgan yo'l katagi (Save/Load uchun).</summary>
    public sealed class SavedRoad
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>
    /// JSON'ga saqlanadigan to'liq o'yin holati. Versiya raqami bilan — kelajakdagi
    /// formatlar bilan moslik uchun. Bazaviy shahar (seed asosida) qayta generatsiya
    /// qilinadi, keyin o'yinchi qurgan binolar ustiga qo'yiladi.
    /// </summary>
    public sealed class SaveGameData
    {
        public int Version { get; set; } = 1;
        public int Seed { get; set; }
        /// <summary>
        /// Shahar bloki xaritada qancha siljigan (GameConfig.CityOffset). Eski (v1) saqlashlarda 0 —
        /// yuklashda barcha tile koordinatalari yangi kattaroq xaritaga ko'chiriladi.
        /// </summary>
        public int MapOffset { get; set; }

        public int Coins { get; set; }
        public int Food { get; set; }

        public int Day { get; set; } = 1;
        public float DayFraction { get; set; }

        public float CameraX { get; set; }
        public float CameraY { get; set; }
        public float Zoom { get; set; } = 1f;

        public int Speed { get; set; } = 1;
        public int PopTax { get; set; } = 10;
        public int TradeTax { get; set; } = 10;
        public int ProdTax { get; set; } = 10;

        public List<SavedBuilding> PlayerBuildings { get; set; } = new List<SavedBuilding>();
        public List<SavedRoad> Roads { get; set; } = new List<SavedRoad>();
        public List<CitizenSnapshot> Citizens { get; set; } = new List<CitizenSnapshot>();
        public WorldSnapshot World { get; set; }
        public MilitarySnapshot Military { get; set; }
        public CivSnapshot Civ { get; set; }
        public List<int> OpenParcels { get; set; } = new List<int>();
        public int Weather { get; set; }

        /// <summary>Story Mode / missiya holati (PROMPT 13). Null bo'lsa — hikoya rejimi ishlatilmagan.</summary>
        public AndijonSaltanatlar.Campaigns.CampaignState Story { get; set; }

        /// <summary>Donishmand Vazir ruxsat rejimi va tasdiqlangan qurilish navbati (PROMPT 15).</summary>
        public AIGovernorSaveState Governor { get; set; }
    }
}
