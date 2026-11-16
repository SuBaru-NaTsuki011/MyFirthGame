using System.Collections.Generic;

namespace AndijonSaltanatlar.Quests
{
    /// <summary>
    /// Bitta vazifa talabi — PROMPT 13, 6-bo'lim. Target qiymat va param'lar haqiqiy tizimlardan tekshiriladi.
    /// Masalan BuildBuilding + TargetKey="House" + TargetAmount=3 => "3 ta uy qurish".
    /// </summary>
    public class QuestObjectiveDefinition
    {
        public ObjectiveType Type { get; set; }
        /// <summary>Bino turi / resurs turi / texnologiya id kabi aniqlovchi kalit (ixtiyoriy).</summary>
        public string TargetKey { get; set; } = "";
        /// <summary>Kerakli miqdor yoki chegaraviy qiymat (aholi, baxt, tanga, bino soni...).</summary>
        public int TargetAmount { get; set; } = 1;
        /// <summary>UI'da ko'rsatiladigan tavsif (o'zbekcha).</summary>
        public string DescriptionUz { get; set; } = "";
        /// <summary>
        /// Rost bo'lsa, vazifa boshlanishidan OLDIN mavjud bo'lgan obyektlar ham hisobga olinadi
        /// (masalan allaqachon qurilgan uylar). Standart: faqat vazifa faollashgach qilingan ishlar.
        /// </summary>
        public bool CountExisting { get; set; } = false;
    }

    /// <summary>Bitta mukofot elementi — PROMPT 13, 13-bo'lim.</summary>
    public class QuestReward
    {
        public RewardKind Kind { get; set; }
        public int Amount { get; set; }
        /// <summary>Dekoratsiya yoki unvon uchun identifikator (ixtiyoriy).</summary>
        public string RefKey { get; set; } = "";
    }

    /// <summary>
    /// Data-driven missiya ta'rifi — PROMPT 13, 5-bo'lim. JSON katalogdan yuklanadi, kodda hardcode qilinmaydi.
    /// </summary>
    public class QuestDefinition
    {
        public string QuestId { get; set; } = "";
        public string CampaignId { get; set; } = "";
        public string ChapterId { get; set; } = "";
        public string TitleUz { get; set; } = "";
        public string DescriptionUz { get; set; } = "";
        public QuestCategory Category { get; set; } = QuestCategory.MainStory;

        /// <summary>Bu vazifa ochilishidan oldin tugashi kerak bo'lgan vazifalar.</summary>
        public List<string> PrerequisiteQuestIds { get; set; } = new();
        public List<QuestObjectiveDefinition> Objectives { get; set; } = new();
        public List<QuestReward> Rewards { get; set; } = new();

        /// <summary>Qo'shimcha ochilish shartlari (masalan minimal aholi) — kalit:qiymat.</summary>
        public Dictionary<string, int> UnlockConditions { get; set; } = new();

        public bool IsRepeatable { get; set; } = false;
        /// <summary>Vaqt chegarasi (soniya). 0 => cheksiz.</summary>
        public int TimeLimit { get; set; } = 0;
        /// <summary>Muhimlik: katta son = yuqori ustuvorlik.</summary>
        public int Priority { get; set; } = 0;

        /// <summary>Vazifa tugaganda ochiladigan ensiklopediya yozuvi (ixtiyoriy) — PROMPT 13, 17-bo'lim.</summary>
        public string EncyclopediaEntryId { get; set; } = "";
        /// <summary>Vazifa boshlanishida ko'rsatiladigan dialog id (ixtiyoriy) — Vazir bilan bog'liq.</summary>
        public string IntroDialogueId { get; set; } = "";
    }
}
