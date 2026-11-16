using System.Collections.Generic;

namespace AndijonSaltanatlar.Campaigns
{
    /// <summary>
    /// Story Mode / kampaniyaning saqlanadigan to'liq holati — PROMPT 13, 19-bo'lim.
    /// Versiyalanadi (eski save'larga migratsiya uchun). Online dunyo holatidan alohida saqlanadi.
    /// </summary>
    public class CampaignState
    {
        /// <summary>Katalog/holat sxemasi versiyasi — migratsiya uchun.</summary>
        public int Version { get; set; } = 1;

        public string CampaignId { get; set; } = "";
        public string ActiveChapterId { get; set; } = "";

        /// <summary>Har bir vazifaning progressi (QuestId -> QuestProgress).</summary>
        public List<Quests.QuestProgress> QuestProgress { get; set; } = new();

        public List<string> CompletedQuestIds { get; set; } = new();
        public List<string> ClaimedRewardIds { get; set; } = new();
        public List<string> UnlockedAchievementIds { get; set; } = new();
        public List<Achievements.AchievementProgress> AchievementProgress { get; set; } = new();

        /// <summary>Dialog tanlovlari (ConsequenceKey -> qiymat/soni).</summary>
        public Dictionary<string, int> StoryChoices { get; set; } = new();
        /// <summary>Ko'rilgan dialoglar id'lari.</summary>
        public List<string> DialogueProgress { get; set; } = new();
        public List<string> HistoricalEntryUnlocks { get; set; } = new();

        /// <summary>Joriy kunlik/haftalik davr identifikatorlari (server vaqti/davr).</summary>
        public string DailyChallengePeriod { get; set; } = "";
        public string WeeklyChallengePeriod { get; set; } = "";

        /// <summary>Story-qatlam hisoblagichlari: tajriba ballari, unvonlar, dekoratsiyalar (mukofotlar).</summary>
        public int Experience { get; set; } = 0;
        public List<string> Titles { get; set; } = new();
        public List<string> Decorations { get; set; } = new();

        public bool StoryModeActive { get; set; } = false;
    }
}
