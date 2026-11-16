using System;
using AndijonSaltanatlar.Economy;

namespace AndijonSaltanatlar.Logistics
{
    /// <summary>
    /// Yetkazib berish vazifasi. Har bir vazifa bitta manba → bitta manzil, bitta mahsulot.
    /// Yuk faqat "Reserved → Loading → InTransit → Unloading → Completed" zanjiri bo'ylab
    /// BIR MARTA harakatlanadi: manbada kamayadi, manzilda qo'shiladi.
    /// </summary>
    public sealed class DeliveryJob
    {
        public int Id { get; set; }
        public int SourceBuildingId { get; set; }
        public int DestinationBuildingId { get; set; }
        public ResourceType ResourceType { get; set; } = ResourceType.Food;
        public float Amount { get; set; }
        public int AssignedTransportId { get; set; }
        public DeliveryJobStatus Status { get; set; } = DeliveryJobStatus.Pending;

        public int CreatedDay { get; set; }
        public int CompletedDay { get; set; }
        /// <summary>Taxminiy yetib borish vaqti (o'yin daqiqalari).</summary>
        public float EstimatedArrivalMinutes { get; set; }
        /// <summary>Holatda o'tkazilgan o'yin vaqti (sekund).</summary>
        public float StateTime { get; set; }
        /// <summary>Yuk manbadan haqiqatan olinganmi (ikki marta olinmasligi uchun).</summary>
        public bool Loaded { get; set; }
        /// <summary>Yuk manzilga haqiqatan qo'shilganmi (ikki marta qo'shilmasligi uchun).</summary>
        public bool Delivered { get; set; }
        public string NoteUz { get; set; } = "";

        public DeliveryJob() { }

        public DeliveryJob(int id, int src, int dst, ResourceType type, float amount, int day)
        {
            Id = id;
            SourceBuildingId = src;
            DestinationBuildingId = dst;
            ResourceType = type;
            Amount = amount;
            CreatedDay = day;
            Status = DeliveryJobStatus.Pending;
        }

        public bool IsTerminal => Status == DeliveryJobStatus.Completed
            || Status == DeliveryJobStatus.Cancelled
            || Status == DeliveryJobStatus.Failed;

        public bool IsAssigned => AssignedTransportId > 0 && !IsTerminal;

        /// <summary>Yuk yo'ldami (statistika: InTransit miqdor).</summary>
        public bool IsInTransit => Status == DeliveryJobStatus.InTransit
            || Status == DeliveryJobStatus.Loading
            || Status == DeliveryJobStatus.Unloading;

        public static string StatusNameUz(DeliveryJobStatus s)
        {
            switch (s)
            {
                case DeliveryJobStatus.Pending: return "Navbatda";
                case DeliveryJobStatus.Reserved: return "Ajratilgan";
                case DeliveryJobStatus.Loading: return "Yuklanmoqda";
                case DeliveryJobStatus.InTransit: return "Yo'lda";
                case DeliveryJobStatus.Unloading: return "Tushirilmoqda";
                case DeliveryJobStatus.Completed: return "Yetkazildi";
                case DeliveryJobStatus.Paused: return "To'xtatilgan";
                case DeliveryJobStatus.Cancelled: return "Bekor";
                default: return "Muvaffaqiyatsiz";
            }
        }
    }

    public enum DeliveryJobStatus
    {
        Pending = 0,
        Reserved = 1,
        Loading = 2,
        InTransit = 3,
        Unloading = 4,
        Completed = 5,
        Paused = 6,
        Cancelled = 7,
        Failed = 8
    }
}
