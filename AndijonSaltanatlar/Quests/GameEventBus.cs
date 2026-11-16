using System;
using System.Collections.Generic;

namespace AndijonSaltanatlar.Quests
{
    /// <summary>
    /// Markaziy event bus — PROMPT 13, 7 & 21-bo'lim. Indekslangan obuna: har bir hodisa turi uchun
    /// faqat tegishli tinglovchilar chaqiriladi, shuning uchun har frame'da hamma vazifa qayta tekshirilmaydi.
    /// Mavjud o'yin tizimlari (bino, aholi, savdo...) shu bus orqali hodisa e'lon qiladi.
    /// </summary>
    public class GameEventBus
    {
        private readonly Dictionary<GameEventType, List<Action<GameEvent>>> _handlers = new();
        private readonly object _lock = new();

        /// <summary>Hodisa turiga obuna bo'lish. Faqat shu turdagi hodisalar uchun chaqiriladi.</summary>
        public void Subscribe(GameEventType type, Action<GameEvent> handler)
        {
            if (handler == null) return;
            lock (_lock)
            {
                if (!_handlers.TryGetValue(type, out var list))
                {
                    list = new List<Action<GameEvent>>();
                    _handlers[type] = list;
                }
                if (!list.Contains(handler)) list.Add(handler);
            }
        }

        public void Unsubscribe(GameEventType type, Action<GameEvent> handler)
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(type, out var list)) list.Remove(handler);
            }
        }

        /// <summary>Hodisa e'lon qilish — faqat shu turga obuna bo'lgan tinglovchilar xabardor qilinadi.</summary>
        public void Publish(GameEvent evt)
        {
            if (evt == null) return;
            List<Action<GameEvent>> snapshot;
            lock (_lock)
            {
                if (!_handlers.TryGetValue(evt.Type, out var list) || list.Count == 0) return;
                snapshot = new List<Action<GameEvent>>(list); // tinglovchi ichida obuna o'zgarishidan himoya
            }
            foreach (var h in snapshot) h(evt);
        }
    }
}
