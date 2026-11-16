using System;
using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Quests;

namespace AndijonSaltanatlar.Achievements
{
    /// <summary>
    /// Yutuqlar dvigateli — PROMPT 13, 12 & 16-bo'lim. Event bus'dan progress yangilaydi, yutuqni
    /// BIR MARTA ochadi, bildirishnoma navbatini boshqaradi (bir vaqtda ko'payib ketmasligi uchun).
    /// </summary>
    public class AchievementManager
    {
        private readonly Dictionary<string, AchievementDefinition> _catalog = new();
        private readonly List<string> _order = new();
        private Campaigns.CampaignState _state;

        /// <summary>Ochilgan yutuqlarning bildirishnoma navbati (UI birma-bir ko'rsatadi).</summary>
        private readonly Queue<AchievementDefinition> _pending = new();

        public Action<Quests.QuestReward> RewardApplier { get; set; }
        public Action<AchievementDefinition> OnUnlocked { get; set; }

        public IReadOnlyList<AchievementDefinition> Catalog => _order.Select(id => _catalog[id]).ToList();

        public void LoadCatalog(IEnumerable<AchievementDefinition> defs)
        {
            _catalog.Clear(); _order.Clear();
            foreach (var d in defs)
            {
                if (d == null || string.IsNullOrEmpty(d.Id) || _catalog.ContainsKey(d.Id)) continue;
                _catalog[d.Id] = d;
                _order.Add(d.Id);
            }
        }

        public void Initialize(Campaigns.CampaignState state, GameEventBus bus)
        {
            _state = state ?? new Campaigns.CampaignState();
            foreach (var id in _order)
                if (_state.AchievementProgress.All(a => a.Id != id))
                    _state.AchievementProgress.Add(new AchievementProgress { Id = id });

            if (bus != null)
                foreach (GameEventType t in Enum.GetValues(typeof(GameEventType)))
                    bus.Subscribe(t, Handle);
        }

        public AchievementProgress ProgressOf(string id) =>
            _state.AchievementProgress.FirstOrDefault(a => a.Id == id);

        public bool IsUnlocked(string id)
        {
            var p = ProgressOf(id);
            return p != null && p.IsUnlocked;
        }

        public void Handle(GameEvent evt)
        {
            if (evt == null) return;
            foreach (var id in _order)
            {
                var def = _catalog[id];
                var prog = ProgressOf(id);
                if (prog == null || prog.IsUnlocked) continue;
                if (!Matches(def, evt)) continue;

                if (def.IsThreshold)
                {
                    int value = ThresholdValue(evt);
                    if (value >= def.RequirementAmount) Unlock(def, prog);
                    else prog.Progress = value;
                }
                else
                {
                    prog.Progress += 1;
                    if (prog.Progress >= def.RequirementAmount) Unlock(def, prog);
                }
            }
        }

        private void Unlock(AchievementDefinition def, AchievementProgress prog)
        {
            prog.IsUnlocked = true;
            prog.Progress = def.RequirementAmount;
            if (!_state.UnlockedAchievementIds.Contains(def.Id)) _state.UnlockedAchievementIds.Add(def.Id);
            foreach (var r in def.Rewards) RewardApplier?.Invoke(r);
            _pending.Enqueue(def);
            OnUnlocked?.Invoke(def);
        }

        /// <summary>Tarix yutug'i kabi to'g'ridan-to'g'ri hodisasiz yutuqlar uchun qo'lda oshirish.</summary>
        public void Increment(string id, int by = 1)
        {
            var def = _catalog.GetValueOrDefault(id);
            var prog = ProgressOf(id);
            if (def == null || prog == null || prog.IsUnlocked) return;
            prog.Progress += by;
            if (prog.Progress >= def.RequirementAmount) Unlock(def, prog);
        }

        private bool Matches(AchievementDefinition def, GameEvent evt)
        {
            if (string.IsNullOrEmpty(def.RequirementEvent)) return false;
            if (!string.Equals(def.RequirementEvent, evt.Type.ToString(), StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrEmpty(def.RequirementKey))
            {
                string key = evt.Get("kind");
                if (string.IsNullOrEmpty(key)) key = evt.Get("key");
                if (!string.Equals(key, def.RequirementKey, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        private static int ThresholdValue(GameEvent evt)
        {
            if (evt.Payload.ContainsKey("happiness")) return evt.GetInt("happiness");
            if (evt.Payload.ContainsKey("population")) return evt.GetInt("population");
            if (evt.Payload.ContainsKey("level")) return evt.GetInt("level");
            if (evt.Payload.ContainsKey("coinTotal")) return evt.GetInt("coinTotal");
            return evt.GetInt("value");
        }

        // ---- Bildirishnoma navbati ----
        public bool HasPendingNotification => _pending.Count > 0;
        public AchievementDefinition DequeueNotification() => _pending.Count > 0 ? _pending.Dequeue() : null;

        public int UnlockedCount => _state?.UnlockedAchievementIds.Count ?? 0;
    }
}
