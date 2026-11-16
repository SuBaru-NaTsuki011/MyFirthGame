using System;
using AndijonSaltanatlar.Advisor;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CitizenQuests;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.NPC;

namespace AndijonSaltanatlar.AdvancedEconomy
{
    /// <summary>
    /// Shahar "tirik hayoti" markazi (PROMPT 17 qatlami): mahallalar statistikasi, bozor narxlari,
    /// NPC xotirasi, fuqaro murojaatlari, fuqaro vazifalari va shahar voqealari jurnali.
    ///
    /// Bu klass YAGONA egasi: boshqa tizimlar shu obyekt orqali haqiqiy holatga murojaat qiladi.
    /// PROMPT 18 infratuzilma tizimlari ham shu yerdagi mahalla ma'lumotidan foydalanadi —
    /// ikkinchi nusxa yaratilmaydi.
    /// </summary>
    public sealed class CityLifeManager
    {
        /// <summary>Suhbat triggerlari uchun voqea kalitlari (faqat haqiqiy voqealar yoziladi).</summary>
        public static readonly string[] CityEventKeys =
        {
            "road_built", "road_removed", "bridge_built", "well_built",
            "market_opened", "warehouse_full", "water_shortage",
            "district_connected", "transport_congested", "delivery_completed"
        };

        public AdvancedEconomyManager Economy { get; }
        public DistrictEconomyManager Districts { get; } = new DistrictEconomyManager();
        public NPCInteractionManager Npc { get; } = new NPCInteractionManager();
        public CitizenReportsAnalyzer Reports { get; } = new CitizenReportsAnalyzer();
        public CitizenQuestManager Quests { get; } = new CitizenQuestManager();
        public CityEventLog CityEvents { get; } = new CityEventLog();
        public UiRequestQueue UiRequests { get; } = new UiRequestQueue();

        /// <summary>Yo'l bor-yo'qligini bilish uchun tashqi manba (RoadNetworkManager yoki MapManager).</summary>
        public Func<int, int, bool> IsRoad { get; set; }

        private int _lastDistrictDay = -1;

        public CityLifeManager() : this(null) { }

        public CityLifeManager(AdvancedEconomyManager economy)
        {
            Economy = economy ?? new AdvancedEconomyManager();
        }

        /// <summary>Mahallalar statistikasi eng oxirgi marta qaysi kunda hisoblangan.</summary>
        public int LastDistrictDay => _lastDistrictDay;

        /// <summary>Fuqaro xotirasi (suhbatlashilgan fuqarolar uchun yaratiladi).</summary>
        public NPCMemory MemoryOf(int citizenId) => Npc.MemoryOf(citizenId);

        /// <summary>Mahalla statistikasini darhol qayta hisoblaydi (masalan bino qurilgandan keyin).</summary>
        public void RecomputeDistricts(GameStateManager state, BuildingManager buildings, bool force = false)
        {
            if (state == null || buildings == null) return;
            if (!force && _lastDistrictDay == state.Day) return;
            _lastDistrictDay = state.Day;
            // Bozor holatlari (faqat obyektlar) — pul harakati bu yerda sodir bo'lmaydi.
            Economy.Merchants.Sync(buildings, state.Pop);
            Districts.Recompute(buildings, state.Pop, state.Land, Economy, IsRoad, state.Policy.PopulationTax);
            Reports.Refresh(Districts, state.Day);
            Quests.OnDayTick(Districts, state.Day);
        }

        /// <summary>Kunlik yangilanish: bir kunda faqat bir marta.</summary>
        public void DayTick(GameStateManager state, BuildingManager buildings)
        {
            if (state == null) return;
            if (_lastDistrictDay == state.Day) return;
            RecomputeDistricts(state, buildings);
        }

        /// <summary>Mahalla nomi (UI va Vazir matnlari uchun).</summary>
        public string DistrictName(int districtId) => Districts.NameOf(districtId);

        public void RecordEvent(string key, int day, string textUz = null) => CityEvents.Record(key, day, textUz);

        public void Clear()
        {
            CityEvents.Clear();
            Reports.Reports.Clear();
            Quests.Records.Clear();
            _lastDistrictDay = -1;
        }
    }
}
