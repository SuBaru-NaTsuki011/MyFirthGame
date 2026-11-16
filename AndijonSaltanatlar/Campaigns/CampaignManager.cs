using System;
using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Quests;

namespace AndijonSaltanatlar.Campaigns
{
    /// <summary>
    /// Kampaniya/Story Mode oqimini boshqaradi — PROMPT 13, 4 & 9-bo'lim. Boblarni ketma-ket ochadi:
    /// bob boshlanganda kirish dialogini ko'rsatadi va bob vazifalarini faollashtiradi; bob vazifalari
    /// tugagach keyingi bobga o'tadi; oxirgi bob tugaganda yakuniy ekranni belgilaydi.
    /// </summary>
    public class CampaignManager
    {
        private CampaignDefinition _campaign;
        private CampaignState _state;
        private QuestManager _quests;
        private Narrative.DialogueManager _dialogue;

        /// <summary>Bob boshlanishida kirish dialogi (ixtiyoriy) ko'rsatish uchun.</summary>
        public Action<string> OnShowDialogue { get; set; }
        /// <summary>Kampaniya to'liq yakunlanganda (yakuniy ekran).</summary>
        public Action<CampaignDefinition> OnCampaignCompleted { get; set; }
        /// <summary>Yangi bobga o'tilganda (UI/Vazir uchun).</summary>
        public Action<CampaignChapter> OnChapterStarted { get; set; }

        public CampaignDefinition Active => _campaign;
        public bool IsCompleted { get; private set; }

        public CampaignChapter CurrentChapter =>
            _campaign?.NarrativeChapters.FirstOrDefault(c => c.ChapterId == _state.ActiveChapterId);

        public void Begin(CampaignDefinition campaign, CampaignState state, QuestManager quests, Narrative.DialogueManager dialogue)
        {
            _campaign = campaign;
            _state = state;
            _quests = quests;
            _dialogue = dialogue;
            _state.CampaignId = campaign.CampaignId;
            _state.StoryModeActive = true;

            if (string.IsNullOrEmpty(_state.ActiveChapterId) && campaign.NarrativeChapters.Count > 0)
                StartChapter(campaign.NarrativeChapters[0]);
            else
                EnsureCurrentChapterActive();
        }

        private void StartChapter(CampaignChapter chapter)
        {
            _state.ActiveChapterId = chapter.ChapterId;
            OnChapterStarted?.Invoke(chapter);

            if (!string.IsNullOrEmpty(chapter.IntroDialogueId))
                OnShowDialogue?.Invoke(chapter.IntroDialogueId);

            // Bob vazifalarini ochish/faollashtirish.
            _quests.RefreshAvailability();
            _quests.AutoActivateAvailable();
        }

        private void EnsureCurrentChapterActive()
        {
            _quests.RefreshAvailability();
            _quests.AutoActivateAvailable();
        }

        /// <summary>Joriy bob vazifalari tugaganini tekshiradi; tugagan bo'lsa keyingi bobga o'tadi.</summary>
        public void Update()
        {
            if (_campaign == null || IsCompleted) return;
            var chapter = CurrentChapter;
            if (chapter == null) return;

            bool allDone = chapter.QuestIds.All(qid =>
            {
                var p = _quests.ProgressOf(qid);
                return p != null && (p.Status == QuestStatus.Completed || p.Status == QuestStatus.RewardClaimed);
            });
            if (!allDone) return;

            if (!string.IsNullOrEmpty(chapter.OutroDialogueId) && _dialogue != null && !_dialogue.IsOpen)
            {
                // Outro faqat bir marta: DialogueProgress orqali tekshiramiz.
                if (!_state.DialogueProgress.Contains(chapter.OutroDialogueId))
                {
                    OnShowDialogue?.Invoke(chapter.OutroDialogueId);
                    return; // o'yinchi outroni o'qiguncha kutamiz
                }
            }

            int idx = _campaign.NarrativeChapters.FindIndex(c => c.ChapterId == chapter.ChapterId);
            if (idx >= 0 && idx + 1 < _campaign.NarrativeChapters.Count)
            {
                StartChapter(_campaign.NarrativeChapters[idx + 1]);
            }
            else
            {
                IsCompleted = true;
                OnCampaignCompleted?.Invoke(_campaign);
            }
        }
    }
}
