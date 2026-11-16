using System;
using System.Collections.Generic;

namespace AndijonSaltanatlar.NPC
{
    /// <summary>Xotiradagi bitta voqea (kalit + o'yin kuni).</summary>
    public sealed class NPCMemoryEvent
    {
        public string Key { get; set; } = "";
        public int Day { get; set; }
    }

    /// <summary>
    /// Fuqaroning strukturali o'yin xotirasi (til modeli xotirasi EMAS): qisqa voqealar ro'yxati,
    /// hukmdorga ishonch va dialog cooldown'lari. Cheksiz o'smaydi — eng eski yozuvlar o'chiriladi.
    /// Faqat o'yinchi suhbatlashgan fuqarolar uchun yaratiladi.
    ///
    /// Kalitlar: "reported.&lt;muammo&gt;" (muammosini aytdi), "forwarded.&lt;muammo&gt;" (Vazirga yuborildi),
    /// "promised.&lt;muammo&gt;" (reja va'da qilindi), "dismissed.&lt;muammo&gt;" (rad etildi),
    /// "thanked.&lt;muammo&gt;" (hal bo'lgach minnatdorchilik bildirdi), "quest.&lt;muammo&gt;" (vazifa berdi).
    /// </summary>
    public sealed class NPCMemory
    {
        public const int MaxEvents = 12;
        public const int MaxDialogueStamps = 16;

        public int CitizenId { get; set; }
        /// <summary>Hukmdorga ishonch / munosabat (0..100).</summary>
        public float Trust { get; set; } = 50f;
        public int TalkCount { get; set; }
        /// <summary>Oxirgi suhbat kuni (0 = hali gaplashilmagan).</summary>
        public int LastTalkDay { get; set; }
        public List<NPCMemoryEvent> Events { get; set; } = new List<NPCMemoryEvent>();
        /// <summary>Dialog id -> oxirgi ko'rsatilgan kun (cooldown uchun).</summary>
        public Dictionary<string, int> DialogueLastDay { get; set; } = new Dictionary<string, int>();

        public bool Has(string key)
        {
            foreach (NPCMemoryEvent e in Events) if (e.Key == key) return true;
            return false;
        }

        public int DayOf(string key)
        {
            foreach (NPCMemoryEvent e in Events) if (e.Key == key) return e.Day;
            return -1;
        }

        /// <summary>Biror prefiks bilan boshlanadigan voqea bormi (masalan "forwarded.").</summary>
        public bool HasPrefix(string prefix)
        {
            foreach (NPCMemoryEvent e in Events) if (e.Key.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Voqeani eslab qoladi (mavjud bo'lsa kunini yangilaydi). Ro'yxat MaxEvents dan oshmaydi.</summary>
        public void Remember(string key, int day)
        {
            foreach (NPCMemoryEvent e in Events)
                if (e.Key == key) { e.Day = day; return; }
            Events.Add(new NPCMemoryEvent { Key = key, Day = day });
            while (Events.Count > MaxEvents) Events.RemoveAt(0);
        }

        public void Forget(string key) => Events.RemoveAll(e => e.Key == key);

        public void AdjustTrust(float delta) => Trust = Math.Clamp(Trust + delta, 0f, 100f);

        public void StampDialogue(string dialogueId, int day)
        {
            DialogueLastDay[dialogueId] = day;
            if (DialogueLastDay.Count <= MaxDialogueStamps) return;
            string oldest = null; int oldestDay = int.MaxValue;
            foreach (var kv in DialogueLastDay) if (kv.Value < oldestDay) { oldestDay = kv.Value; oldest = kv.Key; }
            if (oldest != null) DialogueLastDay.Remove(oldest);
        }

        public bool InCooldown(string dialogueId, int cooldownDays, int day)
        {
            if (cooldownDays <= 0) return false;
            return DialogueLastDay.TryGetValue(dialogueId, out int last) && day - last < cooldownDays;
        }

        public string TrustLabelUz
        {
            get
            {
                if (Trust >= 75f) return "Sodiq";
                if (Trust >= 55f) return "Ishonadi";
                if (Trust >= 40f) return "Betaraf";
                if (Trust >= 25f) return "Norozi";
                return "Ishonmaydi";
            }
        }
    }

    /// <summary>
    /// Shahar miqyosidagi muhim o'zgarishlar (bozor qurildi, soliq o'zgardi...). Fuqarolar oxirgi
    /// suhbatdan keyin sodir bo'lgan voqealarga munosabat bildiradi. Faqat haqiqiy holat o'zgarishidan yoziladi.
    /// </summary>
    public sealed class CityMemory
    {
        public const int MaxEvents = 30;
        public List<NPCMemoryEvent> Events { get; set; } = new List<NPCMemoryEvent>();

        public void Record(string key, int day)
        {
            Events.Add(new NPCMemoryEvent { Key = key, Day = day });
            while (Events.Count > MaxEvents) Events.RemoveAt(0);
        }

        /// <summary>Berilgan kundan KEYIN shu voqea bo'lganmi.</summary>
        public bool HappenedAfter(string key, int day)
        {
            foreach (NPCMemoryEvent e in Events) if (e.Key == key && e.Day > day) return true;
            return false;
        }
    }
}
