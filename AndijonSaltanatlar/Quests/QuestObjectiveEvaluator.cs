namespace AndijonSaltanatlar.Quests
{
    /// <summary>
    /// Vazifa talablarini hodisalarga moslaydi — PROMPT 13, 6-bo'lim. Barcha qiymatlar haqiqiy
    /// o'yin hodisalaridan olinadi. Ikki xil tur bor:
    ///  - Chegaraviy (threshold): joriy qiymat >= maqsad bo'lsa bajariladi (aholi, baxt, xazina...).
    ///  - Yig'ma (count): mos hodisalar soni/miqdori maqsadga yetganda bajariladi (bino, savdo...).
    /// </summary>
    public static class QuestObjectiveEvaluator
    {
        public static bool IsThreshold(ObjectiveType t) => t switch
        {
            ObjectiveType.ReachPopulation => true,
            ObjectiveType.ReachHappiness => true,
            ObjectiveType.ReachTreasury => true,
            ObjectiveType.ReachCityLevel => true,
            _ => false
        };

        /// <summary>Qaysi hodisa turi ushbu vazifa turini qondira oladi.</summary>
        public static GameEventType EventFor(ObjectiveType t) => t switch
        {
            ObjectiveType.BuildBuilding => GameEventType.BuildingConstructed,
            ObjectiveType.UpgradeBuilding => GameEventType.BuildingUpgraded,
            ObjectiveType.ReachPopulation => GameEventType.PopulationChanged,
            ObjectiveType.ReachHappiness => GameEventType.PopulationChanged,
            ObjectiveType.ReachTreasury => GameEventType.ResourceProduced,
            ObjectiveType.ProduceResource => GameEventType.ResourceProduced,
            ObjectiveType.UnlockLand => GameEventType.LandUnlocked,
            ObjectiveType.UnlockTechnology => GameEventType.TechnologyUnlocked,
            ObjectiveType.CreateTradeAgreement => GameEventType.TradeAgreementCreated,
            ObjectiveType.CompleteTrade => GameEventType.TradeCompleted,
            ObjectiveType.ImproveDiplomaticRelation => GameEventType.DiplomaticRelationChanged,
            ObjectiveType.UnlockRegion => GameEventType.LandUnlocked,
            ObjectiveType.ReachCityLevel => GameEventType.CityLevelChanged,
            ObjectiveType.CompleteTutorialStep => GameEventType.TutorialStepCompleted,
            ObjectiveType.ResolveCitizenConcern => GameEventType.CitizenConcernResolved,
            _ => GameEventType.BuildingConstructed
        };

        /// <summary>Hodisa shu vazifa talabiga tegishlimi? (tur + aniqlovchi kalit).</summary>
        public static bool Matches(QuestObjectiveDefinition obj, GameEvent evt)
        {
            if (obj == null || evt == null) return false;
            if (EventFor(obj.Type) != evt.Type) return false;

            // Kalit bo'yicha filtrlash (masalan faqat "House" binolari, faqat "Coin" resursi).
            if (!string.IsNullOrEmpty(obj.TargetKey))
            {
                string key = evt.Get("kind");
                if (string.IsNullOrEmpty(key)) key = evt.Get("key");
                if (!string.Equals(key, obj.TargetKey, System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Mos hodisadan yangi progressni hisoblaydi. Threshold'da hodisadagi joriy qiymat olinadi;
        /// yig'mada esa oldingi progressga hodisa miqdori (yoki 1) qo'shiladi.
        /// </summary>
        public static int Apply(QuestObjectiveDefinition obj, int currentProgress, GameEvent evt, int baseline)
        {
            if (IsThreshold(obj.Type))
            {
                int value = ThresholdValue(obj.Type, evt);
                return value; // eng so'nggi joriy qiymat
            }

            int amount = evt.Payload.ContainsKey("amount") ? evt.GetInt("amount") : 1;
            if (amount <= 0) amount = 1;
            // Eslatma: CountExisting=false bo'lgan vazifalarda mavjud obyektlar hisobga olinmaydi, chunki
            // hodisalar (BuildingConstructed va h.k.) FAQAT vazifa davomida yangi yakunlangan ishlardan chiqadi.
            return currentProgress + amount;
        }

        private static int ThresholdValue(ObjectiveType t, GameEvent evt) => t switch
        {
            ObjectiveType.ReachPopulation => evt.GetInt("population"),
            ObjectiveType.ReachHappiness => evt.GetInt("happiness"),
            ObjectiveType.ReachTreasury => evt.GetInt("coinTotal"),
            ObjectiveType.ReachCityLevel => evt.GetInt("level"),
            _ => 0
        };

        /// <summary>Ushbu objective bajarilganmi?</summary>
        public static bool IsSatisfied(QuestObjectiveDefinition obj, int progress) =>
            progress >= obj.TargetAmount;
    }
}
