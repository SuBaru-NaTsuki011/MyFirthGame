using System;
using System.Collections.Generic;
using System.Linq;

namespace AndijonSaltanatlar.Narrative
{
    /// <summary>
    /// Dialog dvigateli — PROMPT 13, 11-bo'lim. Personaj dialoglarini ketma-ketlikda ko'rsatadi,
    /// tanlov natijalarini (consequence) CampaignState'ga yozadi. UI faqat Current'ni chizadi.
    /// </summary>
    public class DialogueManager
    {
        private readonly Dictionary<string, DialogueDefinition> _catalog = new();
        private Campaigns.CampaignState _state;

        public DialogueDefinition Current { get; private set; }
        public bool IsOpen => Current != null;

        /// <summary>Tanlov qilinganda natija kalitini qabul qiladi (o'yin holatiga ta'sir uchun).</summary>
        public Action<string> OnConsequence { get; set; }
        /// <summary>Dialog yopilganda (masalan vazifani avto-faollashtirish uchun).</summary>
        public Action OnClosed { get; set; }

        public void LoadCatalog(IEnumerable<DialogueDefinition> defs)
        {
            _catalog.Clear();
            foreach (var d in defs)
                if (d != null && !string.IsNullOrEmpty(d.DialogueId))
                    _catalog[d.DialogueId] = d;
        }

        public void Bind(Campaigns.CampaignState state) => _state = state ?? new Campaigns.CampaignState();

        public bool Has(string dialogueId) => _catalog.ContainsKey(dialogueId);

        /// <summary>Dialogni ochadi (shartlar bajarilsa). Ko'rilgan dialoglar ro'yxatiga yoziladi.</summary>
        public void Open(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId) || !_catalog.TryGetValue(dialogueId, out var d)) { Current = null; return; }
            Current = d;
            if (_state != null && !_state.DialogueProgress.Contains(dialogueId))
                _state.DialogueProgress.Add(dialogueId);

            // Ko'rsatish natijalarini qo'llash.
            if (_state != null)
                foreach (var kv in d.Consequences)
                {
                    _state.StoryChoices[kv.Key] = _state.StoryChoices.GetValueOrDefault(kv.Key) + kv.Value;
                    OnConsequence?.Invoke(kv.Key);
                }
        }

        /// <summary>Tanlov indeksi bo'yicha davom etadi. Choices bo'sh bo'lsa index e'tiborsiz (NextDialogueId).</summary>
        public void Choose(int index)
        {
            if (Current == null) return;
            string next;
            if (Current.Choices != null && Current.Choices.Count > 0)
            {
                if (index < 0 || index >= Current.Choices.Count) return;
                var choice = Current.Choices[index];
                if (!string.IsNullOrEmpty(choice.ConsequenceKey) && _state != null)
                {
                    _state.StoryChoices[choice.ConsequenceKey] = _state.StoryChoices.GetValueOrDefault(choice.ConsequenceKey) + 1;
                    OnConsequence?.Invoke(choice.ConsequenceKey);
                }
                next = choice.NextDialogueId;
            }
            else
            {
                next = Current.NextDialogueId;
            }

            if (string.IsNullOrEmpty(next)) Close();
            else Open(next);
        }

        public void Close()
        {
            Current = null;
            OnClosed?.Invoke();
        }

        /// <summary>Vazir kabi personajning ad-hoc xabarini ko'rsatish (katalogsiz bitta matn).</summary>
        public void ShowAdHoc(Speaker speaker, string textUz)
        {
            Current = new DialogueDefinition { DialogueId = "_adhoc", Speaker = speaker, TextUz = textUz };
        }
    }
}
