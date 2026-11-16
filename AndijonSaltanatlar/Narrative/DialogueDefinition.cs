using System.Collections.Generic;

namespace AndijonSaltanatlar.Narrative
{
    /// <summary>Hikoya personajlari — PROMPT 13, 11-bo'lim. PROMPT 17: shahar fuqarolari qo'shildi.</summary>
    public enum Speaker
    {
        Vazir,     // Donishmand Vazir
        Savdogar,  // Savdogar
        Memor,     // Me'mor
        Muallim,   // Muallim
        Dehqon,    // Dehqon
        Elchi,     // Elchi
        Fuqaro     // Oddiy shahar fuqarosi (PROMPT 17)
    }

    /// <summary>Fuqaro dialogi turi — PROMPT 17, 5-bo'lim.</summary>
    public enum DialogueKind
    {
        Story,       // hikoya dialogi (PROMPT 13)
        Casual,      // oddiy suhbat
        Work,        // ish bilan bog'liq
        Problem,     // muammo / murojaat
        Suggestion,  // taklif
        Gratitude,   // minnatdorchilik (faqat haqiqatan hal bo'lgan muammoga)
        Reaction,    // shahardagi o'zgarishga munosabat
        History      // tarixiy ma'lumot
    }

    /// <summary>
    /// Dialog sharti: kontekstdagi qiymatni (DialogueContext) solishtiradi. Masalan
    /// { Key = "citizen.unemployed", Op = ">=", Value = 1 }. Kalit kontekstda bo'lmasa qiymat 0 hisoblanadi.
    /// </summary>
    public class DialogueCondition
    {
        public string Key { get; set; } = "";
        /// <summary>Solishtirish: "&gt;=", "&lt;=", "&gt;", "&lt;", "==", "!=".</summary>
        public string Op { get; set; } = ">=";
        public float Value { get; set; } = 1f;

        public DialogueCondition() { }
        public DialogueCondition(string key, string op = ">=", float value = 1f) { Key = key; Op = op; Value = value; }
    }

    /// <summary>Dialog tanlovi — haqiqiy o'yin holatiga ta'sir qilishi mumkin.</summary>
    public class StoryChoice
    {
        public string TextUz { get; set; } = "";
        /// <summary>Tanlov natijasi (consequence) kaliti — DialogueManager qayta ishlaydi (ixtiyoriy).</summary>
        public string ConsequenceKey { get; set; } = "";
        /// <summary>Keyingi dialog id (bo'sh => dialog yopiladi).</summary>
        public string NextDialogueId { get; set; } = "";

        // ---- PROMPT 17: fuqaro dialoglari ----
        /// <summary>Bajariladigan o'yin harakati (DialogueActionExecutor): "plan:Craft", "forward", "show_district"...</summary>
        public string ActionId { get; set; } = "";
        /// <summary>Tanlov ko'rinishi uchun shartlar (hammasi bajarilishi kerak).</summary>
        public List<DialogueCondition> Conditions { get; set; } = new();
    }

    /// <summary>
    /// Dialog bo'lagi — PROMPT 13, 11-bo'lim. SpeakerId, TextUz, Choices, Conditions, Consequences, NextDialogueId.
    /// PROMPT 17: fuqaro dialoglari uchun tur, ustuvorlik, cooldown va shart ifodalari qo'shildi
    /// (bitta ma'lumot modeli — hikoya va fuqaro dialoglari uchun alohida nusxa yo'q).
    /// </summary>
    public class DialogueDefinition
    {
        public string DialogueId { get; set; } = "";
        public Speaker Speaker { get; set; } = Speaker.Vazir;
        public string TextUz { get; set; } = "";

        /// <summary>Tanlovlar (bo'sh bo'lsa "Davom etish" ko'rsatiladi).</summary>
        public List<StoryChoice> Choices { get; set; } = new();

        /// <summary>Ko'rsatish shartlari (kalit:qiymat) — ixtiyoriy.</summary>
        public Dictionary<string, int> Conditions { get; set; } = new();
        /// <summary>Dialog ko'rsatilganda qo'llanadigan natijalar — ixtiyoriy.</summary>
        public Dictionary<string, int> Consequences { get; set; } = new();

        /// <summary>Tanlov bo'lmaganda keyingi dialog (bo'sh => yopiladi).</summary>
        public string NextDialogueId { get; set; } = "";

        // ---- PROMPT 17: fuqaro dialoglari ----
        public DialogueKind Kind { get; set; } = DialogueKind.Story;
        /// <summary>Kim aytadi: "any" yoki kasb kaliti ("savdogar", "bogbon", "tashuvchi", "soqchi", "amaldor", "muallim", "none").</summary>
        public string SpeakerType { get; set; } = "any";
        /// <summary>Shart ifodalari (hammasi bajarilsa dialog tanlanishi mumkin).</summary>
        public List<DialogueCondition> When { get; set; } = new();
        /// <summary>Ustuvorlik: bir nechta dialog mos kelsa eng yuqorisi tanlanadi.</summary>
        public int Priority { get; set; }
        /// <summary>Shu fuqaro bilan qayta ko'rsatilguncha o'tishi kerak bo'lgan o'yin kunlari.</summary>
        public int CooldownDays { get; set; }
        /// <summary>Faqat zanjir ichida (NextDialogueId orqali) ochiladi — mustaqil tanlanmaydi.</summary>
        public bool FollowUpOnly { get; set; }
        /// <summary>Rost bo'lsa matn tarixiy fakt sifatida belgilanadi (badiiy NPC so'zidan farqli).</summary>
        public bool IsHistoricalFact { get; set; }
        /// <summary>Tarixiy ma'lumot manbasi (IsHistoricalFact uchun majburiy).</summary>
        public string SourceNote { get; set; } = "";
    }
}
