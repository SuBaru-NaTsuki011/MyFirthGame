using System.Collections.Generic;

namespace AndijonSaltanatlar.AdvancedEconomy
{
    /// <summary>Shaharda sodir bo'lgan bitta voqea (fuqaro suhbatida "oxirgi safardan keyin nima o'zgardi").</summary>
    public sealed class CityEvent
    {
        public string Key { get; set; } = "";
        public int Day { get; set; }
        public string TextUz { get; set; } = "";
    }

    /// <summary>
    /// Shahar voqealari jurnali. Faqat HAQIQIY tizimlar tomonidan yoziladi (yo'l qurildi, quduq
    /// ishga tushdi, tirbandlik paydo bo'ldi...). Suhbat matnlari shu jurnalga tayanadi — o'ylab
    /// topilgan voqea ko'rsatilmaydi.
    /// </summary>
    public sealed class CityEventLog
    {
        public const int Keep = 40;

        private readonly List<CityEvent> _events = new List<CityEvent>();
        private readonly Dictionary<string, int> _lastDay = new Dictionary<string, int>();

        public IReadOnlyList<CityEvent> Events => _events;

        public void Record(string key, int day, string textUz = null)
        {
            if (string.IsNullOrEmpty(key)) return;
            _lastDay[key] = day;
            _events.Add(new CityEvent { Key = key, Day = day, TextUz = textUz ?? "" });
            while (_events.Count > Keep) _events.RemoveAt(0);
        }

        /// <summary>Voqea berilgan kundan KEYIN sodir bo'lganmi.</summary>
        public bool HappenedAfter(string key, int day)
            => !string.IsNullOrEmpty(key) && _lastDay.TryGetValue(key, out int d) && d > day;

        public int LastDayOf(string key) => _lastDay.TryGetValue(key, out int d) ? d : -1;

        public void Clear()
        {
            _events.Clear();
            _lastDay.Clear();
        }
    }
}
