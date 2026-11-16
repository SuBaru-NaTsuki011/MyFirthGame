using System.Collections.Generic;

namespace AndijonSaltanatlar.Core
{
    /// <summary>
    /// Tizimlar UI'dan bajarishni so'raydigan amallar. Hech bir tizim panelni to'g'ridan-to'g'ri
    /// ochmaydi: so'rov navbatga qo'yiladi, UI uni o'z kadrida bajaradi (qatlamlar ajratilgan).
    /// </summary>
    public enum UiRequestKind
    {
        None = 0,
        ShowReports = 1,
        GeneratePlan = 2,
        EnterBuild = 3,
        ShowDistrict = 4,
        ShowHousehold = 5,
        ShowMarket = 6,
        FocusMarket = 7,
        ShowDashboard = 8,
        /// <summary>INFRATUZILMA panelini ochish (PROMPT 18).</summary>
        ShowInfrastructure = 9
    }

    /// <summary>Bitta UI so'rovi: tur + ixtiyoriy butun parametr (bino turi, mahalla Id, reja turi...).</summary>
    public sealed class UiRequest
    {
        public UiRequestKind Kind { get; }
        public int Value { get; }

        public UiRequest(UiRequestKind kind, int value = 0)
        {
            Kind = kind;
            Value = value;
        }
    }

    /// <summary>Cheklangan navbat — UI o'chirilgan bo'lsa ham xotira cheksiz o'smaydi.</summary>
    public sealed class UiRequestQueue
    {
        public const int MaxPending = 16;
        private readonly Queue<UiRequest> _items = new Queue<UiRequest>();

        public int Count => _items.Count;

        public void Enqueue(UiRequest r)
        {
            if (r == null) return;
            if (_items.Count >= MaxPending) _items.Dequeue();
            _items.Enqueue(r);
        }

        public bool TryDequeue(out UiRequest r)
        {
            if (_items.Count == 0) { r = null; return false; }
            r = _items.Dequeue();
            return true;
        }

        public void Clear() => _items.Clear();
    }
}
