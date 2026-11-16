namespace AndijonSaltanatlar.Quests
{
    /// <summary>Missiya holati — PROMPT 13, 5-bo'lim.</summary>
    public enum QuestStatus
    {
        Locked,        // Shartlar bajarilmagan
        Available,     // Boshlash mumkin
        Active,        // Davom etayotgan
        Completed,     // Vazifalar bajarilgan, mukofot hali olinmagan
        RewardClaimed, // Mukofot olingan
        Failed,        // Faqat missiya qoidalari ruxsat berganda
        Cancelled      // Faqat missiya qoidalari ruxsat berganda
    }

    /// <summary>Missiya toifasi — Story Journal bo'limlari uchun.</summary>
    public enum QuestCategory
    {
        MainStory,   // Asosiy hikoya
        SideQuest,   // Qo'shimcha vazifa
        Daily,       // Kunlik
        Weekly,      // Haftalik
        Campaign     // Tarixiy kampaniya
    }

    /// <summary>
    /// Vazifa turlari — PROMPT 13, 6-bo'lim. Har bir qiymat haqiqiy o'yin tizimidan olinadi.
    /// </summary>
    public enum ObjectiveType
    {
        BuildBuilding,
        UpgradeBuilding,
        ReachPopulation,
        ReachHappiness,
        ReachTreasury,
        ProduceResource,
        UnlockLand,
        UnlockTechnology,
        CreateTradeAgreement,
        CompleteTrade,
        ImproveDiplomaticRelation,
        UnlockRegion,
        ReachCityLevel,
        CompleteTutorialStep,
        /// <summary>PROMPT 17: fuqaro aytgan muammo haqiqatan hal bo'lishi (TargetKey = "muammo:mahallaId").</summary>
        ResolveCitizenConcern
    }

    /// <summary>Mukofot turlari — PROMPT 13, 13-bo'lim.</summary>
    public enum RewardKind
    {
        Coin,
        Wood,
        Stone,
        Food,
        Science,
        Culture,
        Experience,
        Decoration,
        Title
    }

    /// <summary>O'yin hodisalari turi — PROMPT 13, 7-bo'lim (event bus).</summary>
    public enum GameEventType
    {
        BuildingConstructed,
        BuildingUpgraded,
        LandUnlocked,
        PopulationChanged,
        ResourceProduced,
        TechnologyUnlocked,
        TradeAgreementCreated,
        TradeCompleted,
        DiplomaticRelationChanged,
        CityLevelChanged,
        TutorialStepCompleted,
        /// <summary>PROMPT 17: fuqaro muammosi haqiqiy o'yin holatida hal bo'ldi (payload key = "muammo:mahallaId").</summary>
        CitizenConcernResolved
    }
}
