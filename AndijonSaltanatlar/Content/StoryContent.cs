using System.Collections.Generic;
using AndijonSaltanatlar.Quests;
using AndijonSaltanatlar.Narrative;
using AndijonSaltanatlar.Campaigns;
using AndijonSaltanatlar.Achievements;

namespace AndijonSaltanatlar.Content
{
    /// <summary>
    /// "Asaka Saltanatining Yuksalishi" hikoyasining kanonik kontenti — PROMPT 13, 4-bo'lim.
    /// Bu kontent engine mantig'idan ajratilgan (data-driven). Loader avval JSON fayllardan o'qiydi;
    /// fayllar bo'lmasa shu kod manba sifatida ishlatiladi, shunda o'yin hech qachon buzilmaydi.
    /// Mukofotlar faqat mavjud tizimlarga mos turlardan: Coin, Food (iqtisodiyot), Science, Culture (sivilizatsiya),
    /// Experience/Title/Decoration (story-qatlam). Wood/Stone ishlatilmaydi (o'yinda resurs tizimi yo'q).
    /// </summary>
    public static class StoryContent
    {
        public const string CampaignId = "asaka_rise";

        public static CampaignDefinition BuildCampaign()
        {
            return new CampaignDefinition
            {
                CampaignId = CampaignId,
                NameUz = "Bir shahardan buyuk saltanat sari",
                HistoricalPeriod = "Muqobil tarixiy badiiy ssenariy",
                StartDate = "",
                IsAlternateHistory = true,
                VerificationStatus = VerificationStatus.Fictional,
                StartingRegionIds = new List<string> { "asaka" },
                NarrativeChapters = new List<CampaignChapter>
                {
                    new() { ChapterId = "b1", TitleUz = "1-bob — Yangi hukmdor", IntroDialogueId = "d_b1_intro",
                            QuestIds = new() { "q_b1_house", "q_b1_market" } },
                    new() { ChapterId = "b2", TitleUz = "2-bob — Rivojlanayotgan shahar", IntroDialogueId = "d_b2_intro",
                            QuestIds = new() { "q_b2_pop", "q_b2_happy", "q_b2_houses" } },
                    new() { ChapterId = "b3", TitleUz = "3-bob — Yangi mahallalar", IntroDialogueId = "d_b3_intro",
                            QuestIds = new() { "q_b3_land", "q_b3_three" } },
                    new() { ChapterId = "b4", TitleUz = "4-bob — Ilm va madaniyat", IntroDialogueId = "d_b4_intro",
                            QuestIds = new() { "q_b4_madrasa", "q_b4_tech", "q_b4_upgrade" } },
                    new() { ChapterId = "b5", TitleUz = "5-bob — Buyuk savdo yo'llari", IntroDialogueId = "d_b5_intro",
                            QuestIds = new() { "q_b5_caravan", "q_b5_agreement", "q_b5_trade" } },
                    new() { ChapterId = "b6", TitleUz = "6-bob — Saltanat birligi", IntroDialogueId = "d_b6_intro",
                            QuestIds = new() { "q_b6_relation", "q_b6_alliance" } },
                    new() { ChapterId = "b7", TitleUz = "7-bob — Buyuk poytaxt", IntroDialogueId = "d_b7_intro",
                            QuestIds = new() { "q_b7_palace", "q_b7_pop", "q_b7_level" },
                            OutroDialogueId = "d_b7_outro" },
                },
                CompletionConditions = new Dictionary<string, int> { { "chaptersCompleted", 7 } }
            };
        }

        public static List<QuestDefinition> BuildQuests()
        {
            var q = new List<QuestDefinition>();

            // ---------- BOB 1 ----------
            q.Add(Quest("q_b1_house", "b1", "Birinchi turar joyni quring",
                "Xalqimiz uchun munosib boshpana bilan boshlaymiz. Bitta uy quring.",
                QuestCategory.MainStory, 100, introDialogue: "d_b1_memor",
                objs: new[] { Obj(ObjectiveType.BuildBuilding, "House", 1, "Uy quring: 0/1") },
                rewards: new[] { R(RewardKind.Coin, 150), R(RewardKind.Experience, 20) }));

            q.Add(Quest("q_b1_market", "b1", "Birinchi bozorni quring",
                "Savdo-sotiq shahar hayotining qoni. Bir bozor barpo eting.",
                QuestCategory.MainStory, 95, prereq: new[] { "q_b1_house" },
                objs: new[] { Obj(ObjectiveType.BuildBuilding, "Market", 1, "Bozor quring: 0/1") },
                rewards: new[] { R(RewardKind.Coin, 100), R(RewardKind.Culture, 10) }));

            // ---------- BOB 2 ----------
            q.Add(Quest("q_b2_pop", "b2", "Aholini ko'paytiring",
                "Shahar gullab-yashnashi uchun aholi kerak. Aholini 120 nafarga yetkazing.",
                QuestCategory.MainStory, 90, prereq: new[] { "q_b1_market" },
                objs: new[] { Obj(ObjectiveType.ReachPopulation, "", 120, "Aholi 120 ga yetsin") },
                rewards: new[] { R(RewardKind.Food, 100), R(RewardKind.Experience, 15) }));

            q.Add(Quest("q_b2_happy", "b2", "Fuqarolar baxtini oshiring",
                "Baxtli xalq — kuchli saltanat. Baxt ko'rsatkichini 60 ga yetkazing.",
                QuestCategory.MainStory, 85, prereq: new[] { "q_b2_pop" },
                objs: new[] { Obj(ObjectiveType.ReachHappiness, "", 60, "Baxt 60 ga yetsin") },
                rewards: new[] { R(RewardKind.Coin, 50) }));

            q.Add(Quest("q_b2_houses", "b2", "Yangi uylar quring",
                "Yangi fuqarolar uchun uch-to'rtta uy quring.",
                QuestCategory.MainStory, 80, prereq: new[] { "q_b2_happy" },
                objs: new[] { Obj(ObjectiveType.BuildBuilding, "House", 3, "Uylar quring: 0/3") },
                rewards: new[] { R(RewardKind.Coin, 80), R(RewardKind.Experience, 15) }));

            // ---------- BOB 3 ----------
            q.Add(Quest("q_b3_land", "b3", "Yangi yer oching",
                "Shahar tor bo'lib qoldi. Qulflangan yer uchastkasini oching.",
                QuestCategory.MainStory, 75, prereq: new[] { "q_b2_houses" }, introDialogue: "d_b3_hint",
                objs: new[] { Obj(ObjectiveType.UnlockLand, "", 1, "Yer oching: 0/1") },
                rewards: new[] { R(RewardKind.Coin, 120), R(RewardKind.Experience, 25) }));

            q.Add(Quest("q_b3_three", "b3", "Yangi mahallada qurilish",
                "Ochilgan yangi yerda hayot boshlansin. Uchta bino quring.",
                QuestCategory.MainStory, 70, prereq: new[] { "q_b3_land" },
                objs: new[] { Obj(ObjectiveType.BuildBuilding, "", 3, "Bino quring: 0/3") },
                rewards: new[] { R(RewardKind.Coin, 100), R(RewardKind.Culture, 15) }));

            // ---------- BOB 4 ----------
            q.Add(Quest("q_b4_madrasa", "b4", "Madrasa quring",
                "Ilm — taraqqiyot kaliti. Madrasa (maktab) barpo eting.",
                QuestCategory.MainStory, 65, prereq: new[] { "q_b3_three" },
                objs: new[] { Obj(ObjectiveType.BuildBuilding, "Madrasa", 1, "Madrasa quring: 0/1") },
                rewards: new[] { R(RewardKind.Science, 50) }, encyclopedia: "enc_ilm"));

            q.Add(Quest("q_b4_tech", "b4", "Birinchi texnologiyani oching",
                "Bir texnologiyani tadqiq qilib yakunlang.",
                QuestCategory.MainStory, 60, prereq: new[] { "q_b4_madrasa" },
                objs: new[] { Obj(ObjectiveType.UnlockTechnology, "", 1, "Texnologiya oching: 0/1") },
                rewards: new[] { R(RewardKind.Science, 30), R(RewardKind.Culture, 20) }));

            q.Add(Quest("q_b4_upgrade", "b4", "Binoni yangilang",
                "Mavjud binolardan birini keyingi darajaga ko'taring.",
                QuestCategory.MainStory, 55, prereq: new[] { "q_b4_tech" },
                objs: new[] { Obj(ObjectiveType.UpgradeBuilding, "", 1, "Bino yangilang: 0/1") },
                rewards: new[] { R(RewardKind.Coin, 60) }));

            // ---------- BOB 5 ----------
            q.Add(Quest("q_b5_caravan", "b5", "Karvonsaroy quring",
                "Savdo yo'llari uchun karvonsaroy zarur.",
                QuestCategory.MainStory, 50, prereq: new[] { "q_b4_upgrade" },
                objs: new[] { Obj(ObjectiveType.BuildBuilding, "Caravanserai", 1, "Karvonsaroy quring: 0/1") },
                rewards: new[] { R(RewardKind.Coin, 100) }));

            q.Add(Quest("q_b5_agreement", "b5", "Savdo shartnomasi tuzing",
                "Boshqa hudud bilan savdo shartnomasini taklif qiling.",
                QuestCategory.MainStory, 45, prereq: new[] { "q_b5_caravan" },
                objs: new[] { Obj(ObjectiveType.CreateTradeAgreement, "", 1, "Shartnoma: 0/1") },
                rewards: new[] { R(RewardKind.Coin, 80) }));

            q.Add(Quest("q_b5_trade", "b5", "Birinchi savdoni yakunlang",
                "Birinchi savdo operatsiyasini muvaffaqiyatli yakunlang.",
                QuestCategory.MainStory, 40, prereq: new[] { "q_b5_agreement" },
                objs: new[] { Obj(ObjectiveType.CompleteTrade, "", 1, "Savdo: 0/1") },
                rewards: new[] { R(RewardKind.Coin, 120), R(RewardKind.Experience, 20) }));

            // ---------- BOB 6 ----------
            q.Add(Quest("q_b6_relation", "b6", "Diplomatik aloqani yaxshilang",
                "Qo'shni davlat bilan munosabatlarni yaxshilang.",
                QuestCategory.MainStory, 35, prereq: new[] { "q_b5_trade" },
                objs: new[] { Obj(ObjectiveType.ImproveDiplomaticRelation, "", 1, "Munosabat: 0/1") },
                rewards: new[] { R(RewardKind.Coin, 100) }));

            q.Add(Quest("q_b6_alliance", "b6", "Ittifoq tuzing",
                "Diplomatik yechim bilan ittifoq yoki kelishuvga erishing.",
                QuestCategory.MainStory, 30, prereq: new[] { "q_b6_relation" },
                objs: new[] { Obj(ObjectiveType.ImproveDiplomaticRelation, "", 2, "Kelishuv: 0/2") },
                rewards: new[] { R(RewardKind.Coin, 150), R(RewardKind.Culture, 20) }));

            // ---------- BOB 7 ----------
            q.Add(Quest("q_b7_palace", "b7", "Saroyni rivojlantiring",
                "Buyuk poytaxt buyuk saroyga muhtoj. Saroyni yangilang.",
                QuestCategory.MainStory, 25, prereq: new[] { "q_b6_alliance" },
                objs: new[] { Obj(ObjectiveType.UpgradeBuilding, "MainHall", 1, "Saroyni yangilang: 0/1") },
                rewards: new[] { R(RewardKind.Coin, 200) }));

            q.Add(Quest("q_b7_pop", "b7", "Poytaxt aholisi",
                "Poytaxt gavjum bo'lsin. Aholini 200 ga yetkazing.",
                QuestCategory.MainStory, 20, prereq: new[] { "q_b7_palace" },
                objs: new[] { Obj(ObjectiveType.ReachPopulation, "", 200, "Aholi 200 ga yetsin") },
                rewards: new[] { R(RewardKind.Culture, 100) }));

            q.Add(Quest("q_b7_level", "b7", "Rivojlangan shahar",
                "Shaharni 3-darajaga olib chiqing.",
                QuestCategory.MainStory, 15, prereq: new[] { "q_b7_pop" },
                objs: new[] { Obj(ObjectiveType.ReachCityLevel, "", 3, "Shahar darajasi 3") },
                rewards: new[] { R(RewardKind.Title, 0, "Buyuk Hukmdor"), R(RewardKind.Experience, 50) }));

            // ---------- Qo'shimcha vazifa (side quest) namunasi ----------
            q.Add(Quest("s_garden", "", "Shaharni ko'kalamzorlashtiring",
                "Fuqarolar dam olishi uchun bog' quring.",
                QuestCategory.SideQuest, 10, repeatable: false,
                objs: new[] { Obj(ObjectiveType.BuildBuilding, "Garden", 1, "Bog' quring: 0/1") },
                rewards: new[] { R(RewardKind.Culture, 15) }));

            return q;
        }

        public static List<DialogueDefinition> BuildDialogues()
        {
            return new List<DialogueDefinition>
            {
                new() { DialogueId = "d_b1_intro", Speaker = Speaker.Vazir,
                    TextUz = "Xush kelibsiz, hukmdorim! Shahrimizning kelajagi sizning qarorlaringizga bog'liq. " +
                             "Avvalo xalqimiz uchun munosib yashash sharoitini yaratamiz.",
                    NextDialogueId = "d_b1_memor" },
                new() { DialogueId = "d_b1_memor", Speaker = Speaker.Memor,
                    TextUz = "Hazratim, shahrimizda yangi turar joy qurish uchun qulay joy topdik. Nima qilaylik?",
                    Choices = new List<StoryChoice>
                    {
                        new() { TextUz = "Qurilishni boshlashga tayyorgarlik ko'ring.", ConsequenceKey = "choice_build_now" },
                        new() { TextUz = "Kerakli resurslarni hisoblang.", ConsequenceKey = "choice_plan" },
                        new() { TextUz = "Bu taklifni keyinga qoldiramiz.", ConsequenceKey = "choice_delay" }
                    } },
                new() { DialogueId = "d_b2_intro", Speaker = Speaker.Vazir,
                    TextUz = "Hazratim, shahrimiz kengaymoqda. Endi aholimizning kundalik ehtiyojlari va savdo " +
                             "imkoniyatlarini yaxshilashimiz zarur." },
                new() { DialogueId = "d_b3_intro", Speaker = Speaker.Vazir,
                    TextUz = "Shahrimiz tor bo'lib qoldi, hukmdorim. Yangi mahallalar ochish vaqti keldi." },
                new() { DialogueId = "d_b3_hint", Speaker = Speaker.Vazir,
                    TextUz = "Yangi yer ochish uchun xazina va aholi talablari bor. 'XARITADA KO'RSAT' tugmasi " +
                             "sizni tegishli uchastkaga olib boradi." },
                new() { DialogueId = "d_b4_intro", Speaker = Speaker.Muallim,
                    TextUz = "Hazratim, ilm va madaniyatni rivojlantirsak, saltanatimiz asrlar osha yashaydi." },
                new() { DialogueId = "d_b5_intro", Speaker = Speaker.Savdogar,
                    TextUz = "Hazratim, rivojlangan shahar boshqa hududlar bilan savdo va hamkorlik orqali yanada yuksaladi." },
                new() { DialogueId = "d_b6_intro", Speaker = Speaker.Elchi,
                    TextUz = "Hazratim, qo'shni davlatlar bilan birlik va ittifoq saltanatimizni mustahkamlaydi." },
                new() { DialogueId = "d_b7_intro", Speaker = Speaker.Vazir,
                    TextUz = "Hukmdorim, biz uzoq yo'l bosib o'tdik. Endi Asakani buyuk poytaxtga aylantirish vaqti!" },
                new() { DialogueId = "d_b7_outro", Speaker = Speaker.Vazir,
                    TextUz = "Tabriklayman, hukmdorim! Asaka endi rivojlangan saltanat markazi. Xalqimiz sizdan minnatdor." },
            };
        }

        public static List<AchievementDefinition> BuildAchievements()
        {
            return new List<AchievementDefinition>
            {
                Ach("a_first_step", "Birinchi qadam", "Birinchi binongizni qurdingiz.", AchievementCategory.Construction,
                    "BuildingConstructed", "", 1),
                Ach("a_new_quarter", "Yangi mahalla", "Birinchi yer uchastkasini ochdingiz.", AchievementCategory.Construction,
                    "LandUnlocked", "", 1),
                Ach("a_builder", "Bunyodkor", "20 ta bino qurdingiz.", AchievementCategory.Construction,
                    "BuildingConstructed", "", 20),
                Ach("a_wise_ruler", "Dono hukmdor", "Birinchi texnologiyani ochdingiz.", AchievementCategory.Technology,
                    "TechnologyUnlocked", "", 1),
                Ach("a_science_patron", "Ilm homiysi", "Madrasa qurdingiz.", AchievementCategory.Culture,
                    "BuildingConstructed", "Madrasa", 1),
                Ach("a_trade_master", "Savdo ustasi", "10 ta savdo operatsiyasini yakunladingiz.", AchievementCategory.Economy,
                    "TradeCompleted", "", 10),
                Ach("a_people_lover", "Xalqparvar", "Aholi baxtini 80 ga yetkazdingiz.", AchievementCategory.Population,
                    "PopulationChanged", "", 80, threshold: true),
                Ach("a_developed_city", "Rivojlangan shahar", "Shaharni 3-darajaga olib chiqdingiz.", AchievementCategory.Construction,
                    "CityLevelChanged", "", 3, threshold: true),
                Ach("a_great_architect", "Buyuk me'mor", "Saroyni maksimal darajaga yetkazdingiz.", AchievementCategory.Construction,
                    "BuildingUpgraded", "MainHall", 1),
                Ach("a_diplomacy_master", "Diplomatiya ustasi", "3 ta diplomatik kelishuv tuzdingiz.", AchievementCategory.Diplomacy,
                    "DiplomaticRelationChanged", "", 3),
                Ach("a_founder", "Saltanat asoschisi", "Birinchi saltanatingizni yaratdingiz.", AchievementCategory.Special,
                    "TutorialStepCompleted", "found_kingdom", 1),
                Ach("a_historian", "Tarix bilimdoni", "10 ta tasdiqlangan tarixiy yozuvni o'rgandingiz.", AchievementCategory.History,
                    "", "", 10), // hodisasiz — AchievementManager.Increment orqali
            };
        }

        // ---- yordamchi quruvchilar ----
        private static QuestDefinition Quest(string id, string chapter, string title, string desc,
            QuestCategory cat, int priority, QuestObjectiveDefinition[] objs, QuestReward[] rewards,
            string[] prereq = null, bool repeatable = false, string introDialogue = "", string encyclopedia = "")
            => new()
            {
                QuestId = id, CampaignId = CampaignId, ChapterId = chapter, TitleUz = title, DescriptionUz = desc,
                Category = cat, Priority = priority, IsRepeatable = repeatable,
                PrerequisiteQuestIds = new List<string>(prereq ?? new string[0]),
                Objectives = new List<QuestObjectiveDefinition>(objs),
                Rewards = new List<QuestReward>(rewards),
                IntroDialogueId = introDialogue, EncyclopediaEntryId = encyclopedia
            };

        private static QuestObjectiveDefinition Obj(ObjectiveType t, string key, int amount, string desc)
            => new() { Type = t, TargetKey = key, TargetAmount = amount, DescriptionUz = desc, CountExisting = false };

        private static QuestReward R(RewardKind k, int amount, string refKey = "")
            => new() { Kind = k, Amount = amount, RefKey = refKey };

        private static AchievementDefinition Ach(string id, string title, string desc, AchievementCategory cat,
            string reqEvent, string reqKey, int amount, bool threshold = false)
            => new()
            {
                Id = id, TitleUz = title, DescriptionUz = desc, Category = cat,
                RequirementEvent = reqEvent, RequirementKey = reqKey, RequirementAmount = amount, IsThreshold = threshold
            };
    }
}
