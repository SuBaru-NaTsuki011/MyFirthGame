using System.Collections.Generic;
using System.Linq;
using Xunit;
using AndijonSaltanatlar.Quests;
using AndijonSaltanatlar.Campaigns;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>Missiya dvigateli testlari — PROMPT 13, 23-bo'lim (5-9, 19, 20).</summary>
    public class QuestEngineTests
    {
        private static QuestDefinition BuildQuest(string id, ObjectiveType type, string key, int amount, string[] prereq = null)
            => new QuestDefinition
            {
                QuestId = id,
                Objectives = new List<QuestObjectiveDefinition> { new() { Type = type, TargetKey = key, TargetAmount = amount } },
                Rewards = new List<QuestReward> { new() { Kind = RewardKind.Coin, Amount = 100 } },
                PrerequisiteQuestIds = new List<string>(prereq ?? new string[0])
            };

        private static (QuestManager qm, GameEventBus bus, CampaignState st) Setup(params QuestDefinition[] defs)
        {
            var bus = new GameEventBus();
            var qm = new QuestManager();
            qm.LoadCatalog(defs);
            var st = new CampaignState();
            qm.Initialize(st, bus);
            qm.AutoActivateAvailable();
            return (qm, bus, st);
        }

        [Fact] // test 5: bino qurilganda progress yangilanadi va vazifa yakunlanadi
        public void BuildBuilding_Completes_OnMatchingEvent()
        {
            var (qm, bus, _) = Setup(BuildQuest("q", ObjectiveType.BuildBuilding, "House", 1));
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "House"));
            Assert.Equal(QuestStatus.Completed, qm.ProgressOf("q").Status);
        }

        [Fact] // test 6: bitta event ikki marta hisoblanmaydi
        public void SameEvent_NotCountedTwice()
        {
            var (qm, bus, _) = Setup(BuildQuest("q", ObjectiveType.BuildBuilding, "House", 3));
            var e = new GameEvent(GameEventType.BuildingConstructed).With("kind", "House");
            bus.Publish(e);
            bus.Publish(e); // aynan shu event — qayta hisoblanmasligi kerak
            Assert.Equal(1, qm.ProgressOf("q").GetProgress(0));
            Assert.Equal(QuestStatus.Active, qm.ProgressOf("q").Status);
        }

        [Fact] // test 7: talab bajarilmaguncha vazifa yakunlanmaydi
        public void Quest_NotComplete_UntilTargetReached()
        {
            var (qm, bus, _) = Setup(BuildQuest("q", ObjectiveType.BuildBuilding, "House", 3));
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "House"));
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "House"));
            Assert.Equal(QuestStatus.Active, qm.ProgressOf("q").Status);
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "House"));
            Assert.Equal(QuestStatus.Completed, qm.ProgressOf("q").Status);
        }

        [Fact] // test 8: vazifa yakunlanganda keyingi (prerequisite) vazifa ochiladi
        public void CompletingQuest_UnlocksNext()
        {
            var (qm, bus, _) = Setup(
                BuildQuest("q1", ObjectiveType.BuildBuilding, "House", 1),
                BuildQuest("q2", ObjectiveType.BuildBuilding, "Market", 1, new[] { "q1" }));
            Assert.Equal(QuestStatus.Locked, qm.ProgressOf("q2").Status);
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "House"));
            Assert.Equal(QuestStatus.Completed, qm.ProgressOf("q1").Status);
            Assert.Equal(QuestStatus.Available, qm.ProgressOf("q2").Status);
        }

        [Fact] // test 9: mukofot faqat bir marta beriladi
        public void Reward_ClaimedOnlyOnce()
        {
            int applied = 0;
            var bus = new GameEventBus();
            var qm = new QuestManager();
            qm.LoadCatalog(new[] { BuildQuest("q", ObjectiveType.BuildBuilding, "House", 1) });
            qm.RewardApplier = _ => applied++;
            qm.Initialize(new CampaignState(), bus);
            qm.AutoActivateAvailable();
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "House"));

            Assert.True(qm.ClaimReward("q"));
            Assert.False(qm.ClaimReward("q")); // ikkinchi marta — rad etiladi
            Assert.Equal(1, applied);
            Assert.Equal(QuestStatus.RewardClaimed, qm.ProgressOf("q").Status);
        }

        [Fact] // chegaraviy vazifa (aholi) joriy qiymatdan hisoblanadi
        public void Threshold_Population_CompletesWhenReached()
        {
            var (qm, bus, _) = Setup(BuildQuest("q", ObjectiveType.ReachPopulation, "", 120));
            bus.Publish(new GameEvent(GameEventType.PopulationChanged).With("population", "100"));
            Assert.Equal(QuestStatus.Active, qm.ProgressOf("q").Status);
            bus.Publish(new GameEvent(GameEventType.PopulationChanged).With("population", "130"));
            Assert.Equal(QuestStatus.Completed, qm.ProgressOf("q").Status);
        }

        [Fact] // kalit bo'yicha filtr: boshqa bino turi hisoblanmaydi
        public void KindFilter_IgnoresOtherBuildings()
        {
            var (qm, bus, _) = Setup(BuildQuest("q", ObjectiveType.BuildBuilding, "House", 1));
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "Market"));
            Assert.Equal(0, qm.ProgressOf("q").GetProgress(0));
            Assert.Equal(QuestStatus.Active, qm.ProgressOf("q").Status);
        }

        [Fact] // faollashmagan vazifa hodisadan progress olmaydi (mavjud/oldingi ishlar hisoblanmaydi)
        public void InactiveQuest_DoesNotProgress()
        {
            var bus = new GameEventBus();
            var qm = new QuestManager();
            qm.LoadCatalog(new[] { BuildQuest("q", ObjectiveType.BuildBuilding, "House", 1) });
            qm.Initialize(new CampaignState(), bus); // AutoActivate CHAQIRILMAYDI — Available holatida qoladi
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "House"));
            Assert.Equal(0, qm.ProgressOf("q").GetProgress(0));
            Assert.Equal(QuestStatus.Available, qm.ProgressOf("q").Status);
        }

        [Fact] // test 10: yer ochish vazifasi
        public void UnlockLand_Completes()
        {
            var (qm, bus, _) = Setup(BuildQuest("q", ObjectiveType.UnlockLand, "", 1));
            bus.Publish(new GameEvent(GameEventType.LandUnlocked));
            Assert.Equal(QuestStatus.Completed, qm.ProgressOf("q").Status);
        }

        [Fact] // test 19: noto'g'ri prerequisite aniqlanadi
        public void Validator_DetectsMissingPrerequisite()
        {
            var quests = new List<QuestDefinition> { BuildQuest("q1", ObjectiveType.BuildBuilding, "House", 1, new[] { "yoq" }) };
            var errors = QuestCatalogValidator.FindMissingPrerequisites(quests);
            Assert.NotEmpty(errors);
        }

        [Fact] // test 20: quest bog'lanishlarida cycle tekshiriladi
        public void Validator_DetectsCycle()
        {
            var quests = new List<QuestDefinition>
            {
                BuildQuest("a", ObjectiveType.BuildBuilding, "House", 1, new[] { "b" }),
                BuildQuest("b", ObjectiveType.BuildBuilding, "House", 1, new[] { "a" })
            };
            Assert.True(QuestCatalogValidator.HasCycle(quests));
        }
    }
}
