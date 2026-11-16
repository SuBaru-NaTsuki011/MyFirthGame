using System;
using System.Collections.Generic;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Narrative;
using AndijonSaltanatlar.NPC;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.Dialogue
{
    /// <summary>Suhbat paytidagi NPC holati (animatsiya va UI uchun).</summary>
    public enum NpcTalkState { Talking, Listening, Reacting }

    /// <summary>Ko'rsatishga tayyor javob varianti (shartlari tekshirilgan, matni to'ldirilgan).</summary>
    public sealed class DialogueChoiceView
    {
        public string TextUz = "";
        public StoryChoice Source;
    }

    /// <summary>
    /// Bitta faol suhbat: fuqaro, uning profili, tanlangan dialog va tayyor matn. Kontekst suhbat
    /// ochilganda BIR MARTA yig'iladi — har kadrda qayta generatsiya qilinmaydi.
    /// </summary>
    public sealed class NpcDialogueSession
    {
        public Citizen Citizen;
        public CitizenInteractionProfile Profile;
        public NPCMemory Memory;
        public NPCPersonality Personality;
        public DialogueContext Context;
        public DialogueDefinition Definition;
        public string Text = "";
        public List<DialogueChoiceView> Choices = new List<DialogueChoiceView>();
        public NpcTalkState TalkState = NpcTalkState.Talking;
        public int Day;
        /// <summary>Suhbat mavzusi bo'lgan muammo (ko'rsatkich va "Vazirga yuborish" uchun).</summary>
        public string ConcernKey = "";
        /// <summary>Oxirgi harakat natijasi haqida qisqa xabar (toast).</summary>
        public string LastMessage = "";
        public bool Closed;
        /// <summary>Erkin savolga berilgan javob ko'rsatilyaptimi (katalog dialogi emas).</summary>
        public bool IsFreeChat;

        public bool IsHistorical => Definition != null && Definition.IsHistoricalFact;
        public string SourceNote => Definition?.SourceNote ?? "";
        public DialogueKind Kind => Definition?.Kind ?? DialogueKind.Casual;
    }

    /// <summary>
    /// Fuqaro dialoglari dvigateli: katalogdan shartlarga mos, cooldown'da bo'lmagan, eng yuqori ustuvor
    /// dialogni tanlaydi, matnni haqiqiy holatdan to'ldiradi va tanlov natijasini bajaradi.
    /// Hikoya dialoglari (Narrative.DialogueManager) bilan bir xil ma'lumot modelidan foydalanadi.
    /// </summary>
    public sealed class CitizenDialogueManager
    {
        private readonly Dictionary<string, DialogueDefinition> _byId = new Dictionary<string, DialogueDefinition>();
        private readonly List<DialogueDefinition> _all = new List<DialogueDefinition>();
        private readonly DialogueActionExecutor _executor = new DialogueActionExecutor();

        public IReadOnlyList<DialogueDefinition> Catalog => _all;
        public NpcDialogueSession Current { get; private set; }
        public bool IsOpen => Current != null && !Current.Closed;

        public void LoadCatalog(IEnumerable<DialogueDefinition> defs)
        {
            _byId.Clear();
            _all.Clear();
            foreach (DialogueDefinition d in defs)
            {
                if (d == null || string.IsNullOrEmpty(d.DialogueId) || _byId.ContainsKey(d.DialogueId)) continue;
                _byId[d.DialogueId] = d;
                _all.Add(d);
            }
        }

        public DialogueDefinition Get(string id) => id != null && _byId.TryGetValue(id, out DialogueDefinition d) ? d : null;

        /// <summary>
        /// Kontekstga eng mos dialogni tanlaydi. Shartlar faqat shu yerda (suhbat ochilganda) tekshiriladi.
        /// </summary>
        public DialogueDefinition Pick(DialogueContext ctx, NPCMemory mem, string occupationId, int day, int citizenId)
        {
            DialogueDefinition best = null;
            int bestTie = int.MinValue;
            foreach (DialogueDefinition d in _all)
            {
                if (d.FollowUpOnly || d.Kind == DialogueKind.Story) continue;
                if (!string.IsNullOrEmpty(d.SpeakerType) && d.SpeakerType != "any" && d.SpeakerType != occupationId) continue;
                if (mem != null && mem.InCooldown(d.DialogueId, d.CooldownDays, day)) continue;
                if (!DialogueConditionEvaluator.CheckAll(d.When, ctx)) continue;

                // Teng ustuvorlikda — fuqaro va kunga bog'liq deterministik tanlov (har safar bir xil emas).
                int tie = StableHash(d.DialogueId, citizenId, day);
                if (best == null || d.Priority > best.Priority || (d.Priority == best.Priority && tie > bestTie))
                {
                    best = d;
                    bestTie = tie;
                }
            }
            return best;
        }

        private static int StableHash(string s, int a, int b)
        {
            unchecked
            {
                int h = 17;
                foreach (char ch in s) h = h * 31 + ch;
                h = h * 31 + a;
                h = h * 31 + b;
                return h & 0x7FFFFFFF;
            }
        }

        // ------------------------------------------------------------------
        // Suhbat hayot sikli
        // ------------------------------------------------------------------

        /// <summary>Fuqaro bilan suhbat ochadi. Mos dialog topilmasa null (bunday bo'lmasligi kerak — fallback bor).</summary>
        public NpcDialogueSession Open(Citizen c, CityLifeManager life, GameStateManager state, BuildingManager buildings)
        {
            if (c == null) return null;
            CitizenInteractionProfile profile = life.Npc.BuildProfile(c, life, state, buildings);
            NPCMemory mem = life.Npc.MemoryOf(c.Id);
            DialogueContext ctx = DialogueContextBuilder.Build(c, profile, mem, life, state, buildings);

            var session = new NpcDialogueSession
            {
                Citizen = c, Profile = profile, Memory = mem, Personality = NPCPersonality.For(c),
                Context = ctx, Day = state.Day, ConcernKey = profile.TopConcernId
            };
            DialogueDefinition def = Pick(ctx, mem, profile.OccupationId, state.Day, c.Id);
            if (def == null) return null;

            Current = session;
            Show(session, def, life, state);
            return session;
        }

        /// <summary>Dialogni sessiyaga o'rnatadi: natijalarni qo'llaydi, matn va tanlovlarni tayyorlaydi.</summary>
        private void Show(NpcDialogueSession s, DialogueDefinition def, CityLifeManager life, GameStateManager state)
        {
            s.Definition = def;
            s.IsFreeChat = false;
            s.Profile.CurrentDialogueId = def.DialogueId;
            s.Memory.StampDialogue(def.DialogueId, s.Day);
            ApplyShowConsequences(s, def);

            string text = s.Context.Resolve(def.TextUz);
            // Shaxsiyat faqat ohangga ta'sir qiladi; tarixiy faktlar va zanjir javoblari o'zgartirilmaydi.
            if (!def.IsHistoricalFact && !def.FollowUpOnly)
                text = s.Personality.Stylize(text, def.Kind == DialogueKind.Problem, s.Citizen.Id + s.Day);
            s.Text = text;

            s.Choices.Clear();
            if (def.Choices != null)
                foreach (StoryChoice ch in def.Choices)
                    if (DialogueConditionEvaluator.CheckAll(ch.Conditions, s.Context))
                        s.Choices.Add(new DialogueChoiceView { TextUz = s.Context.Resolve(ch.TextUz), Source = ch });
            if (s.Choices.Count == 0)
                s.Choices.Add(new DialogueChoiceView { TextUz = "Davom etish", Source = new StoryChoice { ActionId = "close", NextDialogueId = def.NextDialogueId } });

            s.TalkState = def.Kind == DialogueKind.Reaction || def.Kind == DialogueKind.Gratitude ? NpcTalkState.Reacting : NpcTalkState.Talking;
        }

        private static void ApplyShowConsequences(NpcDialogueSession s, DialogueDefinition def)
        {
            if (def.Consequences == null) return;
            foreach (var kv in def.Consequences)
            {
                string key = kv.Key;
                if (key == "trust") s.Memory.AdjustTrust(kv.Value);
                else if (key.StartsWith("report.", StringComparison.Ordinal))
                {
                    string concern = key.Substring(7);
                    s.Memory.Remember("reported." + concern, s.Day);
                    s.ConcernKey = concern;
                }
                else if (key.StartsWith("thank.", StringComparison.Ordinal))
                {
                    string concern = key.Substring(6);
                    s.Memory.Remember("thanked." + concern, s.Day);
                    // Muammo yopildi — eski "kutyapmiz" belgilarini tozalaymiz, toki keyin yana paydo bo'lsa qayta aytsin.
                    s.Memory.Forget("reported." + concern);
                    s.Memory.Forget("forwarded." + concern);
                    s.Memory.Forget("promised." + concern);
                    s.Memory.Forget("dismissed." + concern);
                    s.Memory.Forget("eased." + concern);
                }
                else if (key.StartsWith("ease.", StringComparison.Ordinal))
                {
                    s.Memory.Remember("eased." + key.Substring(5), s.Day);
                }
            }
        }

        /// <summary>
        /// Javob variantini tanlaydi: harakat bajariladi (DialogueActionExecutor), so'ng zanjirdagi keyingi
        /// dialog ko'rsatiladi yoki suhbat yopiladi.
        /// </summary>
        public void Choose(int index, CityLifeManager life, GameStateManager state, BuildingManager buildings)
        {
            NpcDialogueSession s = Current;
            if (s == null || s.Closed || index < 0 || index >= s.Choices.Count) return;
            StoryChoice choice = s.Choices[index].Source;

            DialogueActionResult result = _executor.Execute(choice.ActionId, s, life, state, buildings);
            if (!string.IsNullOrEmpty(result.MessageUz)) s.LastMessage = result.MessageUz;

            string next = !string.IsNullOrEmpty(result.NextDialogueId) ? result.NextDialogueId : choice.NextDialogueId;
            DialogueDefinition nextDef = result.Close ? null : Get(next);
            if (nextDef != null) Show(s, nextDef, life, state);
            else Close(state);
        }

        /// <summary>Pastki tugma: joriy muammoni Vazirga yuborish (dialog tanlovisiz ham).</summary>
        public bool ForwardCurrentConcern(CityLifeManager life, GameStateManager state, BuildingManager buildings)
        {
            NpcDialogueSession s = Current;
            if (s == null || s.Closed || string.IsNullOrEmpty(s.ConcernKey)) return false;
            DialogueActionResult r = _executor.Execute("forward:" + s.ConcernKey, s, life, state, buildings);
            if (!string.IsNullOrEmpty(r.MessageUz)) s.LastMessage = r.MessageUz;
            DialogueDefinition react = Get("r_forward");
            if (react != null) Show(s, react, life, state);
            return true;
        }

        /// <summary>Erkin savolga qoidaga asoslangan javob (LLM emas) — NpcFreeChat.</summary>
        public void AskFree(string question, CityLifeManager life, GameStateManager state)
        {
            NpcDialogueSession s = Current;
            if (s == null || s.Closed) return;
            s.Text = NpcFreeChat.Answer(question, s);
            s.IsFreeChat = true;
            s.TalkState = NpcTalkState.Talking;
        }

        public void Close(GameStateManager state)
        {
            NpcDialogueSession s = Current;
            if (s == null || s.Closed) return;
            s.Closed = true;
            s.Memory.TalkCount++;
            s.Memory.LastTalkDay = state.Day;
            s.Profile.CurrentDialogueId = null;
        }

        /// <summary>Yopilgan sessiyani tozalaydi (UI yopilish animatsiyasidan keyin chaqiradi).</summary>
        public void Clear() => Current = null;
    }
}
