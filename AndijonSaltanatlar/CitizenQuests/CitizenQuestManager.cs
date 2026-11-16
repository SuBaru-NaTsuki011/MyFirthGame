using System;
using System.Collections.Generic;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Quests;

namespace AndijonSaltanatlar.CitizenQuests
{
    /// <summary>Fuqaro bergan bitta vazifaning saqlanadigan yozuvi.</summary>
    public sealed class CitizenQuestRecord
    {
        public string QuestId { get; set; } = "";
        public string ConcernKey { get; set; } = "";
        public int DistrictId { get; set; }
        public string DistrictName { get; set; } = "";
        public int CitizenId { get; set; }
        public string CitizenName { get; set; } = "";
        public int StartDay { get; set; }
        public bool Completed { get; set; }
        public int CompletedDay { get; set; }
    }

    /// <summary>
    /// Fuqaro muammosidan missiya ta'rifi yaratadi (mavjud QuestDefinition formatida — alohida missiya
    /// tizimi yo'q). Har bir vazifa bitta talabga ega: muammo HAQIQATAN hal bo'lishi.
    /// </summary>
    public static class CitizenQuestGenerator
    {
        public static bool Supports(string concern)
        {
            switch (concern)
            {
                case ConcernKeys.NoRoad:
                case ConcernKeys.Unemployed:
                case ConcernKeys.NoMarket:
                case ConcernKeys.NoSchool:
                case ConcernKeys.Housing:
                    return true;
                default:
                    return false;
            }
        }

        public static string QuestId(string concern, int districtId, int serial) => "cq_" + concern + "_" + districtId + "_" + serial;
        public static string TargetKey(string concern, int districtId) => concern + ":" + districtId;

        public static QuestDefinition Create(string concern, int districtId, string districtName, string citizenName, int serial)
        {
            string title, desc, objective;
            int coin;
            switch (concern)
            {
                case ConcernKeys.NoRoad:
                    title = "Mahallamizga yo'l kerak";
                    desc = citizenName + " aytdi: " + districtName + "dagi ayrim uylar ko'chaga ulanmagan.";
                    objective = "Mahalladagi barcha uylarni yo'l tarmog'iga ulash";
                    coin = 80; break;
                case ConcernKeys.Unemployed:
                    title = "Ish o'rinlari yetishmayapti";
                    desc = citizenName + " aytdi: " + districtName + "da ishsizlar ko'p, bo'sh ish o'rni yo'q.";
                    objective = "Yangi ish joylari yaratish (bozor, bog', karvonsaroy...)";
                    coin = 120; break;
                case ConcernKeys.NoMarket:
                    title = "Yangi bozor kerak";
                    desc = citizenName + " aytdi: " + districtName + "da savdo imkoniyatlari yetarli emas.";
                    objective = "Mahalla ehtiyojini qoplaydigan bozor qurish";
                    coin = 110; break;
                case ConcernKeys.NoSchool:
                    title = "Maktab quraylik";
                    desc = citizenName + " aytdi: " + districtName + "da ta'lim olish imkoni yo'q.";
                    objective = "Madrasa qurish";
                    coin = 150; break;
                default:
                    title = "Uy-joy yetishmayapti";
                    desc = citizenName + " aytdi: " + districtName + "da uylar tor, bo'sh uy yo'q.";
                    objective = "Yangi uylar qurib, uy-joy tanqisligini bartaraf etish";
                    coin = 90; break;
            }

            return new QuestDefinition
            {
                QuestId = QuestId(concern, districtId, serial),
                CampaignId = "citizens",
                ChapterId = "citizens",
                TitleUz = title,
                DescriptionUz = desc,
                Category = QuestCategory.SideQuest,
                Priority = 40,
                Objectives = new List<QuestObjectiveDefinition>
                {
                    new QuestObjectiveDefinition
                    {
                        Type = ObjectiveType.ResolveCitizenConcern,
                        TargetKey = TargetKey(concern, districtId),
                        TargetAmount = 1,
                        DescriptionUz = objective
                    }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Kind = RewardKind.Coin, Amount = coin },
                    new QuestReward { Kind = RewardKind.Experience, Amount = 20 }
                }
            };
        }
    }

    /// <summary>Vazifa shartlari — faqat haqiqiy shahar holatidan tekshiriladi.</summary>
    public static class CitizenQuestConditions
    {
        /// <summary>Vazifa berilishi mumkinmi: muammo mahallada haqiqatan mavjud bo'lishi kerak.</summary>
        public static bool CanStart(string concern, DistrictStats district)
            => district != null && CitizenQuestGenerator.Supports(concern) && district.Needs.Contains(concern);

        /// <summary>Muammo hal bo'ldimi: mahalla ehtiyojlari ro'yxatida endi yo'q.</summary>
        public static bool IsResolved(string concern, DistrictStats district)
            => district == null || !district.Needs.Contains(concern);
    }

    /// <summary>
    /// Fuqarolar bergan vazifalarni yuritadi. Mavjud QuestManager bilan ishlaydi: vazifa uning katalogiga
    /// dinamik qo'shiladi, yakunlanishi esa CitizenConcernResolved hodisasi orqali — bu hodisa faqat
    /// muammo haqiqiy o'yin holatida yo'qolganda e'lon qilinadi. Mukofot bir marta beriladi.
    /// </summary>
    public sealed class CitizenQuestManager
    {
        private QuestManager _quests;
        private GameEventBus _bus;
        private int _serial;

        public List<CitizenQuestRecord> Records { get; } = new List<CitizenQuestRecord>();
        /// <summary>Vazifa yakunlanganda (fuqaro xotirasi va xabar uchun).</summary>
        public Action<CitizenQuestRecord> OnCompleted { get; set; }

        public void Attach(QuestManager quests, GameEventBus bus)
        {
            _quests = quests;
            _bus = bus;
        }

        public CitizenQuestRecord FindActive(string concern, int districtId)
        {
            foreach (CitizenQuestRecord r in Records)
                if (!r.Completed && r.ConcernKey == concern && r.DistrictId == districtId) return r;
            return null;
        }

        public int ActiveCount
        {
            get { int n = 0; foreach (CitizenQuestRecord r in Records) if (!r.Completed) n++; return n; }
        }

        /// <summary>
        /// Fuqaro muammosi bo'yicha vazifa boshlaydi. Bir mahalla + bir muammo uchun bir vaqtda bitta vazifa.
        /// Muammo mahallada haqiqatan bo'lmasa vazifa berilmaydi.
        /// </summary>
        public CitizenQuestRecord TryStart(string concern, DistrictStats district, int citizenId, string citizenName, int day)
        {
            if (_quests == null || !CitizenQuestConditions.CanStart(concern, district)) return null;
            CitizenQuestRecord existing = FindActive(concern, district.Id);
            if (existing != null) return existing;

            _serial++;
            QuestDefinition def = CitizenQuestGenerator.Create(concern, district.Id, district.NameUz, citizenName, _serial);
            if (!_quests.RegisterDynamic(def)) return null;
            var rec = new CitizenQuestRecord
            {
                QuestId = def.QuestId, ConcernKey = concern, DistrictId = district.Id, DistrictName = district.NameUz,
                CitizenId = citizenId, CitizenName = citizenName, StartDay = day
            };
            Records.Add(rec);
            return rec;
        }

        /// <summary>Kunlik tekshiruv: hal bo'lgan muammolar uchun hodisa e'lon qiladi, mukofotni beradi.</summary>
        public void OnDayTick(DistrictEconomyManager districts, int day)
        {
            if (_quests == null || _bus == null) return;
            foreach (CitizenQuestRecord rec in Records)
            {
                if (rec.Completed) continue;
                if (!CitizenQuestConditions.IsResolved(rec.ConcernKey, districts.Get(rec.DistrictId))) continue;

                _bus.Publish(new GameEvent(GameEventType.CitizenConcernResolved)
                    .With("key", CitizenQuestGenerator.TargetKey(rec.ConcernKey, rec.DistrictId)));
                QuestProgress prog = _quests.ProgressOf(rec.QuestId);
                if (prog == null || (prog.Status != QuestStatus.Completed && prog.Status != QuestStatus.RewardClaimed)) continue;

                _quests.ClaimReward(rec.QuestId); // idempotent: mukofot faqat bir marta
                rec.Completed = true;
                rec.CompletedDay = day;
                OnCompleted?.Invoke(rec);
            }
            // Eski yakunlangan yozuvlar cheksiz to'planmasin.
            Records.RemoveAll(r => r.Completed && day - r.CompletedDay > 30);
        }

        // ---- Save / Load ----
        public int Serial => _serial;

        public void Restore(List<CitizenQuestRecord> saved, int serial)
        {
            Records.Clear();
            _serial = serial;
            if (saved == null || _quests == null) return;
            foreach (CitizenQuestRecord rec in saved)
            {
                if (rec == null) continue;
                Records.Add(rec);
                if (rec.Completed) continue;
                // Ta'rifni qayta ro'yxatga olamiz — progress CampaignState'da saqlangan, qayta noldan boshlanmaydi.
                int s = ParseSerial(rec.QuestId);
                _quests.RegisterDynamic(CitizenQuestGenerator.Create(rec.ConcernKey, rec.DistrictId, rec.DistrictName, rec.CitizenName, s));
            }
        }

        private static int ParseSerial(string questId)
        {
            int i = questId.LastIndexOf('_');
            return i >= 0 && int.TryParse(questId.Substring(i + 1), out int v) ? v : 0;
        }
    }
}
