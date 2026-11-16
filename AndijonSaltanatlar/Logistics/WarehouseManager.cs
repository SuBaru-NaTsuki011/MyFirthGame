using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Economy;
using AndijonSaltanatlar.RoadNetwork;

namespace AndijonSaltanatlar.Logistics
{
    /// <summary>
    /// Bitta ombor (yoki bozor ombori) holati. Zaxira va band qilingan zaxira alohida:
    /// band qilingan yuk boshqa vazifaga berilmaydi, lekin hali "yetkazilgan" hisoblanmaydi.
    /// </summary>
    public sealed class WarehouseState
    {
        public int BuildingId { get; set; }
        public string NameUz { get; set; } = "";
        public int Capacity { get; set; } = 40;
        public Dictionary<ResourceType, float> Stock { get; } = new Dictionary<ResourceType, float>();
        public Dictionary<ResourceType, float> Reserved { get; } = new Dictionary<ResourceType, float>();
        /// <summary>Kelayotgan yuk (yo'lda) — faqat ko'rsatish uchun, zaxiraga qo'shilmagan.</summary>
        public Dictionary<ResourceType, float> Incoming { get; } = new Dictionary<ResourceType, float>();
        public bool RoadConnected { get; set; }
        public int Workers { get; set; }
        public float Efficiency { get; set; } = 1f;
        public bool IsMarket { get; set; }

        public float TotalStock
        {
            get
            {
                float sum = 0f;
                foreach (var kv in Stock) sum += kv.Value;
                return sum;
            }
        }

        public float TotalReserved
        {
            get
            {
                float sum = 0f;
                foreach (var kv in Reserved) sum += kv.Value;
                return sum;
            }
        }

        public float FreeSpace => Math.Max(0f, Capacity - TotalStock - TotalReserved);
        public bool IsFull => FreeSpace <= 0.01f;

        public float Get(ResourceType t) => Stock.TryGetValue(t, out float v) ? v : 0f;
        public float GetReserved(ResourceType t) => Reserved.TryGetValue(t, out float v) ? v : 0f;
        public float GetIncoming(ResourceType t) => Incoming.TryGetValue(t, out float v) ? v : 0f;
        public float Available(ResourceType t) => Math.Max(0f, Get(t) - GetReserved(t));

        internal void AddTo(Dictionary<ResourceType, float> map, ResourceType t, float amount)
        {
            if (amount <= 0f) return;
            map[t] = (map.TryGetValue(t, out float v) ? v : 0f) + amount;
        }

        internal void RemoveFrom(Dictionary<ResourceType, float> map, ResourceType t, float amount)
        {
            if (amount <= 0f) return;
            float v = map.TryGetValue(t, out float cur) ? cur : 0f;
            map[t] = Math.Max(0f, v - amount);
        }
    }

    /// <summary>
    /// Omborlar va bozor zaxiralari. Ombor binosi — karvonsaroy (Caravanserai); bozor ham
    /// kichik zaxiraga ega. Sig'im bino darajasiga qarab o'sadi.
    /// </summary>
    public sealed class WarehouseManager
    {
        public const int WarehouseCapacityPerLevel = 40;
        public const int MarketCapacityPerLevel = 16;

        private readonly Dictionary<int, WarehouseState> _states = new Dictionary<int, WarehouseState>();

        public IReadOnlyDictionary<int, WarehouseState> All => _states;

        public int Count => _states.Count;

        public float TotalCapacity
        {
            get { float s = 0f; foreach (WarehouseState w in _states.Values) s += w.Capacity; return s; }
        }

        public float TotalStock
        {
            get { float s = 0f; foreach (WarehouseState w in _states.Values) s += w.TotalStock; return s; }
        }

        public float WarehouseStock
        {
            get
            {
                float s = 0f;
                foreach (WarehouseState w in _states.Values) if (!w.IsMarket) s += w.TotalStock;
                return s;
            }
        }

        public bool AnyFull
        {
            get
            {
                foreach (WarehouseState w in _states.Values) if (!w.IsMarket && w.IsFull) return true;
                return false;
            }
        }

        public WarehouseState Get(int buildingId)
            => _states.TryGetValue(buildingId, out WarehouseState w) ? w : null;

        /// <summary>Binolardan omborlar ro'yxatini yangilaydi (yangi bino, daraja, yo'l ulanishi).</summary>
        public void Sync(BuildingManager buildings, RoadNetworkManager network)
        {
            if (buildings == null) return;
            var seen = new HashSet<int>();
            foreach (Building b in buildings.Buildings)
            {
                bool isWarehouse = b.Type == BuildingType.Caravanserai;
                bool isMarket = b.Type == BuildingType.Market;
                if (!isWarehouse && !isMarket) continue;
                seen.Add(b.Id);

                if (!_states.TryGetValue(b.Id, out WarehouseState w))
                {
                    w = new WarehouseState { BuildingId = b.Id, IsMarket = isMarket };
                    _states[b.Id] = w;
                }
                w.NameUz = b.Name;
                w.IsMarket = isMarket;
                w.Capacity = (isWarehouse ? WarehouseCapacityPerLevel : MarketCapacityPerLevel) * Math.Max(1, b.Level);
                w.RoadConnected = network != null && network.HasRoadAccess(b, 3);
                w.Workers = Population_Workers(b);
                // Yo'li yo'q ombor samarali ishlamaydi — bu ataylab shunday.
                w.Efficiency = w.RoadConnected ? 1f : 0.35f;
            }

            var stale = new List<int>();
            foreach (int id in _states.Keys) if (!seen.Contains(id)) stale.Add(id);
            foreach (int id in stale) _states.Remove(id);
        }

        private static int Population_Workers(Building b)
        {
            // Ombor ishchilari bino darajasiga qarab (haqiqiy ish joyi PopulationManager'da hisoblanadi).
            return Math.Max(1, b.Level);
        }

        /// <summary>Zaxirani qo'shadi (sig'imdan oshmaydi). Haqiqatan qo'shilgan miqdorni qaytaradi.</summary>
        public float Deposit(int buildingId, ResourceType type, float amount)
        {
            WarehouseState w = Get(buildingId);
            if (w == null || amount <= 0f) return 0f;
            float add = Math.Min(amount, w.FreeSpace);
            if (add <= 0f) return 0f;
            w.AddTo(w.Stock, type, add);
            return add;
        }

        /// <summary>Zaxiradan oladi (band qilinmagan qismidan).</summary>
        public float Withdraw(int buildingId, ResourceType type, float amount)
        {
            WarehouseState w = Get(buildingId);
            if (w == null || amount <= 0f) return 0f;
            float take = Math.Min(amount, w.Available(type));
            if (take <= 0f) return 0f;
            w.RemoveFrom(w.Stock, type, take);
            return take;
        }

        public void Reserve(int buildingId, ResourceType type, float amount)
        {
            WarehouseState w = Get(buildingId);
            if (w == null || amount <= 0f) return;
            w.AddTo(w.Reserved, type, Math.Min(amount, w.Available(type)));
        }

        public void ReleaseReservation(int buildingId, ResourceType type, float amount)
        {
            WarehouseState w = Get(buildingId);
            if (w == null) return;
            w.RemoveFrom(w.Reserved, type, amount);
        }

        public void AddIncoming(int buildingId, ResourceType type, float amount)
        {
            WarehouseState w = Get(buildingId);
            if (w != null) w.AddTo(w.Incoming, type, amount);
        }

        public void RemoveIncoming(int buildingId, ResourceType type, float amount)
        {
            WarehouseState w = Get(buildingId);
            if (w != null) w.RemoveFrom(w.Incoming, type, amount);
        }

        public void Clear() => _states.Clear();
    }
}
