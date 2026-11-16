using System;
using System.Collections.Generic;
using System.Linq;

namespace AndijonSaltanatlar.Quests
{
    /// <summary>
    /// Missiya dvigateli — PROMPT 13, 5-8-bo'lim. Katalog (ta'riflar) + holat (progress)ni boshqaradi,
    /// event bus'dan progress yangilaydi, holatlarni (Locked→Available→Active→Completed→RewardClaimed) o'tkazadi.
    /// Mukofot qo'llash tashqi callback orqali (RewardApplier) — iqtisodiyotdan ajratilgan.
    /// Server-authoritative: client o'zicha "quest yakunlandi" deb mukofot yozmaydi; faqat tasdiqlangan
    /// hodisalardan progress oshadi (PROMPT 13, 7-bo'lim).
    /// </summary>
    public class QuestManager
    {
        private readonly Dictionary<string, QuestDefinition> _catalog = new();
        private readonly List<string> _order = new();
        private Campaigns.CampaignState _state = new();
        private GameEventBus _bus;

        /// <summary>Mukofotni haqiqiy o'yin holatiga qo'llaydi (tanga qo'shish va h.k.). Integratsiya qatlami beradi.</summary>
        public Action<QuestReward> RewardApplier { get; set; }
        /// <summary>Vazifa bajarilganda (Vazir tabrigi/UI uchun).</summary>
        public Action<QuestDefinition> OnQuestCompleted { get; set; }
        /// <summary>Mukofot olinganda.</summary>
        public Action<QuestDefinition> OnRewardClaimed { get; set; }
        /// <summary>Yangi vazifa ochilganda (Available → Vazir tushuntirishi uchun).</summary>
        public Action<QuestDefinition> OnQuestAvailable { get; set; }

        public IReadOnlyList<QuestDefinition> Catalog => _order.Select(id => _catalog[id]).ToList();
        public Campaigns.CampaignState State => _state;

        // ---- Sozlash ----

        public void LoadCatalog(IEnumerable<QuestDefinition> definitions)
        {
            _catalog.Clear();
            _order.Clear();
            foreach (var d in definitions)
            {
                if (d == null || string.IsNullOrEmpty(d.QuestId) || _catalog.ContainsKey(d.QuestId)) continue;
                _catalog[d.QuestId] = d;
                _order.Add(d.QuestId);
            }
        }

        /// <summary>Holatni o'rnatadi (yangi o'yin yoki save'dan) va bus'ga obuna bo'ladi.</summary>
        public void Initialize(Campaigns.CampaignState state, GameEventBus bus)
        {
            _state = state ?? new Campaigns.CampaignState();
            _bus = bus;

            // Har bir katalog vazifasi uchun progress yozuvi borligini ta'minlaymiz.
            foreach (var id in _order)
            {
                if (_state.QuestProgress.All(q => q.QuestId != id))
                    _state.QuestProgress.Add(new QuestProgress { QuestId = id, Status = QuestStatus.Locked });
            }

            if (bus != null)
                foreach (GameEventType t in Enum.GetValues(typeof(GameEventType)))
                    bus.Subscribe(t, Handle);

            RefreshAvailability();
        }

        /// <summary>
        /// PROMPT 17: o'yin davomida paydo bo'ladigan vazifani (masalan fuqaro bergan topshiriq) katalogga
        /// qo'shadi va faollashtiradi. Save'dan qayta ro'yxatga olinsa mavjud progress saqlanib qoladi.
        /// Vazifa allaqachon yakunlangan bo'lsa qayta faollashmaydi (false qaytaradi).
        /// </summary>
        public bool RegisterDynamic(QuestDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.QuestId)) return false;
            if (!_catalog.ContainsKey(def.QuestId))
            {
                _catalog[def.QuestId] = def;
                _order.Add(def.QuestId);
            }
            QuestProgress prog = ProgressOf(def.QuestId);
            if (prog == null)
            {
                prog = new QuestProgress { QuestId = def.QuestId, Status = QuestStatus.Locked };
                _state.QuestProgress.Add(prog);
            }
            if (prog.Status == QuestStatus.Completed || prog.Status == QuestStatus.RewardClaimed) return false;
            if (prog.Status != QuestStatus.Active)
            {
                prog.Status = QuestStatus.Active;
                prog.ActivatedAtUtc = DateTime.UtcNow;
            }
            return true;
        }

        public QuestProgress ProgressOf(string questId) =>
            _state.QuestProgress.FirstOrDefault(q => q.QuestId == questId);

        public QuestDefinition DefinitionOf(string questId) =>
            _catalog.TryGetValue(questId, out var d) ? d : null;

        // ---- Holat o'tishlari ----

        /// <summary>Shartlar bajarilgan Locked vazifalarni Available ga o'tkazadi.</summary>
        public void RefreshAvailability()
        {
            foreach (var id in _order)
            {
                var prog = ProgressOf(id);
                if (prog == null || prog.Status != QuestStatus.Locked) continue;
                if (PrerequisitesMet(id))
                {
                    prog.Status = QuestStatus.Available;
                    OnQuestAvailable?.Invoke(_catalog[id]);
                }
            }
        }

        private bool PrerequisitesMet(string questId)
        {
            var def = _catalog[questId];
            foreach (var pre in def.PrerequisiteQuestIds)
            {
                var p = ProgressOf(pre);
                if (p == null) return false;
                if (p.Status != QuestStatus.Completed && p.Status != QuestStatus.RewardClaimed) return false;
            }
            return true;
        }

        /// <summary>Available vazifani faollashtiradi (threshold bazaviy qiymatlarni belgilash uchun snapshot kerak bo'lishi mumkin).</summary>
        public void Activate(string questId, Func<QuestObjectiveDefinition, int> currentValueProvider = null)
        {
            var prog = ProgressOf(questId);
            var def = DefinitionOf(questId);
            if (prog == null || def == null) return;
            if (prog.Status != QuestStatus.Available) return;

            prog.Status = QuestStatus.Active;
            prog.ActivatedAtUtc = DateTime.UtcNow;

            // CountExisting=false bo'lgan yig'ma vazifalar uchun bazaviy qiymatni yozamiz,
            // shunda vazifa boshlanishidan oldingi obyektlar hisobga olinmaydi.
            for (int i = 0; i < def.Objectives.Count; i++)
            {
                var obj = def.Objectives[i];
                if (!QuestObjectiveEvaluator.IsThreshold(obj.Type) && !obj.CountExisting && currentValueProvider != null)
                    prog.Baseline[i] = currentValueProvider(obj);
            }
        }

        /// <summary>Barcha Available vazifalarni avtomatik faollashtiradi (Story Mode qulayligi).</summary>
        public void AutoActivateAvailable(Func<QuestObjectiveDefinition, int> currentValueProvider = null)
        {
            foreach (var id in _order.ToList())
            {
                var prog = ProgressOf(id);
                if (prog != null && prog.Status == QuestStatus.Available)
                    Activate(id, currentValueProvider);
            }
        }

        // ---- Event ishlovi ----

        public void Handle(GameEvent evt)
        {
            if (evt == null) return;
            bool anyCompleted = false;

            foreach (var id in _order)
            {
                var prog = ProgressOf(id);
                if (prog == null || prog.Status != QuestStatus.Active) continue;

                // Idempotentlik: bir xil event ikki marta progressni oshirmaydi (PROMPT 13, 6-bo'lim).
                if (!string.IsNullOrEmpty(evt.EventId) && prog.ProcessedEventIds.Contains(evt.EventId)) continue;

                var def = _catalog[id];
                bool touched = false;
                for (int i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];
                    if (!QuestObjectiveEvaluator.Matches(obj, evt)) continue;
                    int cur = prog.GetProgress(i);
                    int baseline = prog.Baseline.TryGetValue(i, out int b) ? b : 0;
                    int next = QuestObjectiveEvaluator.Apply(obj, cur, evt, baseline);
                    prog.ObjectiveProgress[i] = next;
                    touched = true;
                }

                if (touched)
                {
                    prog.ProcessedEventIds.Add(evt.EventId);
                    if (IsQuestComplete(def, prog))
                    {
                        prog.Status = QuestStatus.Completed;
                        if (!_state.CompletedQuestIds.Contains(id)) _state.CompletedQuestIds.Add(id);
                        OnQuestCompleted?.Invoke(def);
                        anyCompleted = true;
                    }
                }
            }

            if (anyCompleted) RefreshAvailability();
        }

        public bool IsQuestComplete(QuestDefinition def, QuestProgress prog)
        {
            for (int i = 0; i < def.Objectives.Count; i++)
                if (!QuestObjectiveEvaluator.IsSatisfied(def.Objectives[i], prog.GetProgress(i)))
                    return false;
            return true;
        }

        // ---- Mukofot (idempotent) ----

        /// <summary>
        /// Tugallangan vazifa mukofotini beradi. RewardClaimId = QuestId => bitta mukofot ikki marta olinmaydi
        /// (PROMPT 13, 13-bo'lim). Rost qaytsa mukofot shu chaqiruvda berildi.
        /// </summary>
        public bool ClaimReward(string questId)
        {
            var prog = ProgressOf(questId);
            var def = DefinitionOf(questId);
            if (prog == null || def == null) return false;
            if (prog.Status != QuestStatus.Completed) return false;
            if (prog.RewardClaimed || _state.ClaimedRewardIds.Contains(questId)) return false;

            foreach (var r in def.Rewards) RewardApplier?.Invoke(r);

            prog.RewardClaimed = true;
            prog.Status = QuestStatus.RewardClaimed;
            _state.ClaimedRewardIds.Add(questId);
            OnRewardClaimed?.Invoke(def);
            RefreshAvailability();
            return true;
        }

        // ---- UI uchun so'rovlar ----

        public IEnumerable<QuestDefinition> ByCategory(QuestCategory cat) =>
            _order.Select(id => _catalog[id]).Where(d => d.Category == cat);

        public IEnumerable<QuestDefinition> ActiveQuests() =>
            _order.Select(id => _catalog[id])
                  .Where(d => { var p = ProgressOf(d.QuestId); return p != null && p.Status == QuestStatus.Active; });

        /// <summary>Tracker HUD uchun eng muhim faol vazifa.</summary>
        public QuestDefinition TopTrackedQuest() =>
            ActiveQuests().OrderByDescending(d => d.Priority).FirstOrDefault();
    }
}
