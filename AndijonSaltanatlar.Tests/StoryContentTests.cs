using System.Linq;
using System.Text.Json;
using Xunit;
using AndijonSaltanatlar.Quests;
using AndijonSaltanatlar.Narrative;
using AndijonSaltanatlar.Campaigns;
using AndijonSaltanatlar.Achievements;
using AndijonSaltanatlar.Content;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>Kontent va yutuq/dialog/save testlari — PROMPT 13, 23-bo'lim (1-4, 13-16, 18, 21).</summary>
    public class StoryContentTests
    {
        [Fact] // test 1-2: kampaniya yuklanadi, 7 bob mavjud
        public void Campaign_HasSevenChapters()
        {
            var camp = StoryContent.BuildCampaign();
            Assert.Equal(7, camp.NarrativeChapters.Count);
            Assert.NotEmpty(camp.NarrativeChapters[0].QuestIds);
        }

        [Fact] // kanonik katalog yaroqli (noto'g'ri prerequisite yo'q, cycle yo'q)
        public void DefaultCatalog_IsValid()
        {
            var quests = StoryContent.BuildQuests();
            bool ok = QuestCatalogValidator.IsValid(quests, out var errors);
            Assert.True(ok, string.Join("; ", errors));
        }

        [Fact] // 12 ta yutuq mavjud (PROMPT 13, 12-bo'lim)
        public void Achievements_TwelveDefined()
        {
            Assert.Equal(12, StoryContent.BuildAchievements().Count);
        }

        [Fact] // test 18: data-driven — JSON round-trip kontentni saqlaydi
        public void Content_JsonRoundTrip_PreservesQuests()
        {
            var bundle = StoryContentLoader.DefaultBundle();
            string json = StoryContentLoader.ToJson(bundle);
            var back = StoryContentLoader.FromJson(json);
            Assert.Equal(bundle.Quests.Count, back.Quests.Count);
            Assert.Equal(bundle.Dialogues.Count, back.Dialogues.Count);
            Assert.Equal(bundle.Campaign.NarrativeChapters.Count, back.Campaign.NarrativeChapters.Count);
            // Enumlar matn sifatida saqlanadi (o'qilishi oson)
            Assert.Contains("BuildBuilding", json);
        }

        [Fact] // test 14-15: yutuq bir marta ochiladi
        public void Achievement_UnlocksOnce()
        {
            int applied = 0;
            var bus = new GameEventBus();
            var am = new AchievementManager();
            am.LoadCatalog(StoryContent.BuildAchievements());
            am.RewardApplier = _ => applied++;
            var st = new CampaignState();
            am.Initialize(st, bus);

            // "Birinchi qadam" — har qanday bino qurilishi
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "House"));
            bus.Publish(new GameEvent(GameEventType.BuildingConstructed).With("kind", "Market"));
            Assert.True(am.IsUnlocked("a_first_step"));
            Assert.Single(st.UnlockedAchievementIds, x => x == "a_first_step");
        }

        [Fact] // chegaraviy yutuq (baxt >= 80)
        public void Achievement_Threshold_Happiness()
        {
            var bus = new GameEventBus();
            var am = new AchievementManager();
            am.LoadCatalog(StoryContent.BuildAchievements());
            am.Initialize(new CampaignState(), bus);
            bus.Publish(new GameEvent(GameEventType.PopulationChanged).With("happiness", "85"));
            Assert.True(am.IsUnlocked("a_people_lover"));
        }

        [Fact] // test 13: dialog tanlovlari saqlanadi
        public void Dialogue_Choice_RecordsConsequence()
        {
            var st = new CampaignState();
            var dm = new DialogueManager();
            dm.LoadCatalog(StoryContent.BuildDialogues());
            dm.Bind(st);
            dm.Open("d_b1_memor");
            Assert.True(dm.IsOpen);
            dm.Choose(0); // "Qurilishni boshlashga tayyorgarlik ko'ring"
            Assert.True(st.StoryChoices.ContainsKey("choice_build_now"));
            Assert.Contains("d_b1_memor", st.DialogueProgress);
        }

        [Fact] // test 16: save/load (CampaignState) progressni tiklaydi
        public void CampaignState_Serialization_RoundTrips()
        {
            var st = new CampaignState { CampaignId = "asaka_rise", ActiveChapterId = "b2", Experience = 55 };
            st.CompletedQuestIds.Add("q_b1_house");
            st.ClaimedRewardIds.Add("q_b1_house");
            st.QuestProgress.Add(new QuestProgress { QuestId = "q_b1_market", Status = QuestStatus.Active });
            st.StoryChoices["choice_plan"] = 1;

            string json = JsonSerializer.Serialize(st);
            var back = JsonSerializer.Deserialize<CampaignState>(json);

            Assert.Equal("b2", back.ActiveChapterId);
            Assert.Equal(55, back.Experience);
            Assert.Contains("q_b1_house", back.CompletedQuestIds);
            Assert.Equal(QuestStatus.Active, back.QuestProgress.Single().Status);
            Assert.Equal(1, back.StoryChoices["choice_plan"]);
        }

        [Fact] // tarixiy metadata validatsiyasi (PROMPT 13, 10 & 18-bo'lim)
        public void HistoricalValidator_RejectsUnsourced()
        {
            var unsourced = new AndijonSaltanatlar.History.HistoricalReference
            { Id = "x", Status = VerificationStatus.NeedsHistoricalReview };
            Assert.False(AndijonSaltanatlar.History.HistoricalContentValidator.CanPresentAsFact(unsourced));

            var verified = new AndijonSaltanatlar.History.HistoricalReference
            { Id = "y", Status = VerificationStatus.Verified, Citation = "Muzey materiali" };
            Assert.True(AndijonSaltanatlar.History.HistoricalContentValidator.CanPresentAsFact(verified));
        }
    }
}
