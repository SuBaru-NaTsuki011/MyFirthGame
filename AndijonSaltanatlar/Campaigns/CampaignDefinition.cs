using System.Collections.Generic;

namespace AndijonSaltanatlar.Campaigns
{
    /// <summary>Tarixiy kontentning tekshirilish holati — PROMPT 13, 10 & 17-bo'lim.</summary>
    public enum VerificationStatus
    {
        Fictional,            // Badiiy / muqobil tarixiy (tekshirish talab etilmaydi)
        Verified,             // Ishonchli manba bilan tasdiqlangan
        NeedsHistoricalReview // Tarixiy tekshiruv kutilmoqda — jonli kontent sifatida ko'rsatilmaydi
    }

    /// <summary>Kampaniya bobi — bir nechta vazifani va kirish dialogini bog'laydi.</summary>
    public class CampaignChapter
    {
        public string ChapterId { get; set; } = "";
        public string TitleUz { get; set; } = "";
        public string IntroDialogueId { get; set; } = "";
        /// <summary>Shu bobga tegishli vazifa id'lari (tartib bo'yicha).</summary>
        public List<string> QuestIds { get; set; } = new();
        /// <summary>Bob yakunida ko'rsatiladigan dialog (ixtiyoriy).</summary>
        public string OutroDialogueId { get; set; } = "";
    }

    /// <summary>
    /// Data-driven kampaniya ta'rifi — PROMPT 13, 9-bo'lim. Story Mode ham, tarixiy kampaniyalar ham
    /// shu tuzilmadan foydalanadi. Online dunyodan alohida (o'z world instance / save slot).
    /// </summary>
    public class CampaignDefinition
    {
        public string CampaignId { get; set; } = "";
        public string NameUz { get; set; } = "";
        public string HistoricalPeriod { get; set; } = "";
        public string StartDate { get; set; } = "";
        public List<string> StartingRegionIds { get; set; } = new();
        /// <summary>Boshlang'ich saltanat holati (kalit:qiymat) — resurs, aholi va h.k.</summary>
        public Dictionary<string, int> StartingKingdomState { get; set; } = new();
        public List<CampaignChapter> NarrativeChapters { get; set; } = new();
        public List<string> HistoricalReferenceIds { get; set; } = new();
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Fictional;
        /// <summary>Yakunlash shartlari (kalit:qiymat) — ixtiyoriy.</summary>
        public Dictionary<string, int> CompletionConditions { get; set; } = new();

        /// <summary>Rost => bu muqobil/badiiy hikoya (haqiqiy tarixiy voqea sifatida ko'rsatilmaydi).</summary>
        public bool IsAlternateHistory { get; set; } = true;
    }
}
