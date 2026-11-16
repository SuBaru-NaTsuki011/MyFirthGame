using System;
using System.Collections.Generic;

namespace AndijonSaltanatlar.Quests
{
    /// <summary>
    /// O'yin hodisasi — PROMPT 13, 7-bo'lim. Quest tizimi faqat shu hodisalardan progress yangilaydi
    /// (har bir frame'da hamma vazifani qayta tekshirmaydi — event-driven).
    /// </summary>
    public class GameEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");
        public GameEventType Type { get; set; }
        public int WorldId { get; set; }
        public int PlayerId { get; set; }
        public int KingdomId { get; set; }
        public int CityId { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Hodisaga oid qo'shimcha ma'lumot (masalan {"kind":"House"}, {"amount":"120"}).</summary>
        public Dictionary<string, string> Payload { get; set; } = new();

        public GameEvent() { }

        public GameEvent(GameEventType type)
        {
            Type = type;
        }

        public GameEvent With(string key, string value)
        {
            Payload[key] = value;
            return this;
        }

        public string Get(string key) => Payload.TryGetValue(key, out string v) ? v : "";

        public int GetInt(string key) => int.TryParse(Get(key), out int v) ? v : 0;
    }
}
