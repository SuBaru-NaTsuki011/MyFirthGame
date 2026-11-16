using System.Collections.Generic;
using AndijonSaltanatlar.Economy;

namespace AndijonSaltanatlar.Logistics
{
    /// <summary>
    /// Yuk band qilish xizmati. Vazifa yaratilganda yuk "band" qilinadi, lekin ombordan
    /// HAQIQATAN faqat yuklash paytida olinadi. Shu tufayli bir xil yuk ikki vazifaga berilmaydi.
    /// </summary>
    public sealed class CargoReservationService
    {
        private sealed class Reservation
        {
            public int JobId;
            public int BuildingId;
            public ResourceType Type;
            public float Amount;
            public bool Committed;
        }

        private readonly Dictionary<int, Reservation> _byJob = new Dictionary<int, Reservation>();

        public int Count => _byJob.Count;

        public bool TryReserve(int jobId, int buildingId, ResourceType type, float amount)
        {
            if (jobId <= 0 || amount <= 0f) return false;
            if (_byJob.ContainsKey(jobId)) return false;
            _byJob[jobId] = new Reservation
            {
                JobId = jobId, BuildingId = buildingId, Type = type, Amount = amount
            };
            return true;
        }

        public bool IsReserved(int jobId) => _byJob.ContainsKey(jobId);

        public float ReservedAmount(int jobId)
            => _byJob.TryGetValue(jobId, out Reservation r) ? r.Amount : 0f;

        /// <summary>Yukni manbadan oladi (bir marta). Muvaffaqiyatli bo'lsa true.</summary>
        public bool TryCommit(int jobId, WarehouseManager warehouses, out float loaded)
        {
            loaded = 0f;
            if (!_byJob.TryGetValue(jobId, out Reservation r) || r.Committed) return false;
            if (warehouses == null) return false;

            float got = warehouses.Withdraw(r.BuildingId, r.Type, r.Amount);
            if (got <= 0f) return false;
            r.Committed = true;
            loaded = got;
            return true;
        }

        /// <summary>Band qilishni bekor qiladi (yuk olinmagan).</summary>
        public void Release(int jobId, WarehouseManager warehouses)
        {
            if (!_byJob.TryGetValue(jobId, out Reservation r)) return;
            if (!r.Committed && warehouses != null)
                warehouses.ReleaseReservation(r.BuildingId, r.Type, r.Amount);
            _byJob.Remove(jobId);
        }

        public void Clear() => _byJob.Clear();
    }
}
