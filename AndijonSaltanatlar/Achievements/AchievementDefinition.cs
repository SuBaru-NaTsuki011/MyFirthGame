using System.Collections.Generic;

namespace AndijonSaltanatlar.Achievements
{
    /// <summary>Yutuq toifasi — PROMPT 13, 12-bo'lim.</summary>
    public enum AchievementCategory
    {
        Construction,  // Qurilish
        Economy,       // Iqtisodiyot
        Population,    // Aholi
        Culture,       // Madaniyat
        Technology,    // Texnologiya
        Diplomacy,     // Diplomatiya
        History,       // Tarix
        Special        // Maxsus
    }

    /// <summary>
    /// Yutuq ta'rifi — PROMPT 13, 12-bo'lim. Shart o'yin hodisalaridan hisoblanadi, bir marta ochiladi.
    /// </summary>
    public class AchievementDefinition
    {
        public string Id { get; set; } = "";
        public string TitleUz { get; set; } = "";
        public string DescriptionUz { get; set; } = "";
        public string IconPath { get; set; } = "";
        public AchievementCategory Category { get; set; } = AchievementCategory.Special;

        /// <summary>Hisoblanadigan hodisa turi (Quests.GameEventType nomi bilan mos keladi).</summary>
        public string RequirementEvent { get; set; } = "";
        /// <summary>Hodisani aniqlashtiruvchi kalit (masalan bino turi "House"; bo'sh => har qanday).</summary>
        public string RequirementKey { get; set; } = "";
        /// <summary>Talab qilinadigan miqdor (masalan 20 ta bino). Chegaraviy hodisalar uchun maqsad qiymat.</summary>
        public int RequirementAmount { get; set; } = 1;
        /// <summary>Rost => hodisa qiymati (masalan baxt=80) talabga teng/katta bo'lsa ochiladi (hisob emas).</summary>
        public bool IsThreshold { get; set; } = false;

        public List<AndijonSaltanatlar.Quests.QuestReward> Rewards { get; set; } = new();

        /// <summary>Ochilmaguncha yashirin yutuq.</summary>
        public bool IsHidden { get; set; } = false;
    }

    /// <summary>Yutuqning saqlanadigan runtime holati.</summary>
    public class AchievementProgress
    {
        public string Id { get; set; } = "";
        public int Progress { get; set; } = 0;
        public bool IsUnlocked { get; set; } = false;
    }
}
