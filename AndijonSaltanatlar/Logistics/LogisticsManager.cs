using System;
using System.Collections.Generic;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Economy;
using AndijonSaltanatlar.RoadNetwork;
using AndijonSaltanatlar.Transportation;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Logistics
{
    /// <summary>
    /// Logistika zanjiri (PROMPT 18):
    ///
    ///   DEHQON XO'JALIGI → (yuklash) → YO'L BO'YLAB TASHISH → (tushirish) → OMBOR → BOZOR → SAVDO
    ///
    /// QAT'IY QOIDA — resurs ikki marta hisoblanmaydi:
    ///   • yuklashda manba zaxirasi KAMAYADI (davlat ombori yoki ombor binosi);
    ///   • tashish paytida yuk faqat job.Cargo holatida bo'ladi (hech qayerda hisoblanmaydi);
    ///   • tushirishda manzil zaxirasi BIR MARTA oshadi (job.Delivered bayrog'i bilan himoyalangan);
    ///   • yo'l topilmasa yoki manzil to'la bo'lsa — yuk manbaga qaytariladi, yo'qolmaydi.
    /// </summary>
    public sealed class LogisticsManager
    {
        public const int MaxJobsPerDay = 6;
        public const float LoadSeconds = 1.6f;
        public const float UnloadSeconds = 1.4f;
        public const float MaxDeliveryUnits = 6f;

        private readonly RoadNetworkManager _network;
        private readonly BuildingManager _buildings;
        private readonly GameStateManager _state;
        private readonly TransportManager _transports;

        public WarehouseManager Warehouses { get; } = new WarehouseManager();
        public CargoReservationService Reservations { get; } = new CargoReservationService();
        public DeliveryScheduler Scheduler { get; } = new DeliveryScheduler();

        /// <summary>Shahar voqealari uchun (infratuzilma qatlami yozadi).</summary>
        public Action<string, string> OnEvent { get; set; }

        public int CompletedToday { get; private set; }
        public int FailedToday { get; private set; }
        public float SoldToday { get; private set; }
        public int CoinsFromSalesToday { get; private set; }
        private int _lastSellDay = -1;

        public int PendingCount
        {
            get
            {
                int n = 0;
                foreach (DeliveryJob j in Scheduler.Jobs) if (j.Status == DeliveryJobStatus.Pending) n++;
                return n;
            }
        }

        public int InTransitCount
        {
            get
            {
                int n = 0;
                foreach (DeliveryJob j in Scheduler.Jobs) if (j.IsInTransit) n++;
                return n;
            }
        }

        public float InTransitUnits
        {
            get
            {
                float s = 0f;
                foreach (DeliveryJob j in Scheduler.Jobs) if (j.IsInTransit && !j.Delivered) s += j.Amount;
                return s;
            }
        }

        public LogisticsManager(RoadNetworkManager network, BuildingManager buildings, GameStateManager state,
                                TransportManager transports)
        {
            _network = network;
            _buildings = buildings;
            _state = state;
            _transports = transports;
            if (_transports != null)
                _transports.Arrived += OnTransportArrived;
        }

        // ==================================================================
        //  KUNLIK: vazifalar yaratish va bozor savdosi
        // ==================================================================

        public void DayTick()
        {
            Warehouses.Sync(_buildings, _network);
            CompletedToday = 0;
            FailedToday = 0;
            SoldToday = 0f;
            CoinsFromSalesToday = 0;
            Scheduler.RemoveCompletedJobs();
            CreateDeliveryJobs();
            SellMarketStock();
        }

        private void CreateDeliveryJobs()
        {
            if (_buildings == null || _network == null) return;
            int created = 0;
            int day = _state?.Day ?? 0;

            // 1) DEHQON XO'JALIGI → OMBOR (yoki bevosita bozor): yuk davlat omboridan olinadi.
            foreach (Building garden in _buildings.Buildings)
            {
                if (created >= MaxJobsPerDay) break;
                if (garden.Type != BuildingType.Garden || garden.BuildProgress < 1f) continue;

                WarehouseState dest = BestDestination(garden, preferWarehouse: true);
                if (dest == null) continue;
                int room = (int)Math.Min(MaxDeliveryUnits, dest.FreeSpace);
                if (room < 1) continue;
                if (_state != null && _state.Food < room) room = Math.Min(room, _state.Food);
                if (room < 1) continue;
                if (!RouteFinder.IsReachable(_network, Center(garden), CenterOf(dest.BuildingId), 8)) continue;

                Scheduler.CreateDeliveryJob(garden.Id, dest.BuildingId, ResourceType.Food, room, day);
                Warehouses.AddIncoming(dest.BuildingId, ResourceType.Food, room);
                created++;
            }

            // 2) OMBOR → BOZOR: omborda mavjud zaxira bozorga tashiladi.
            foreach (WarehouseState market in Warehouses.All.Values)
            {
                if (created >= MaxJobsPerDay) break;
                if (!market.IsMarket || market.FreeSpace < 1f) continue;

                foreach (WarehouseState wh in Warehouses.All.Values)
                {
                    if (wh.IsMarket) continue;
                    float have = wh.Available(ResourceType.Food);
                    if (have < 1f) continue;
                    int amount = (int)Math.Min(Math.Min(MaxDeliveryUnits, have), market.FreeSpace);
                    if (amount < 1) continue;
                    if (!RouteFinder.IsReachable(_network, CenterOf(wh.BuildingId), CenterOf(market.BuildingId), 8)) continue;

                    Scheduler.CreateDeliveryJob(wh.BuildingId, market.BuildingId, ResourceType.Food, amount, day);
                    Warehouses.AddIncoming(market.BuildingId, ResourceType.Food, amount);
                    created++;
                    break;
                }
            }

            // 3) Omborda hech narsa bo'lmasa — bog'dan bevosita bozorga (zanjir uzilmasin).
            foreach (WarehouseState market in Warehouses.All.Values)
            {
                if (created >= MaxJobsPerDay) break;
                if (!market.IsMarket || market.FreeSpace < 1f) continue;
                if (market.TotalStock + market.GetIncoming(ResourceType.Food) > 2f) continue;

                foreach (Building garden in _buildings.Buildings)
                {
                    if (created >= MaxJobsPerDay) break;
                    if (garden.Type != BuildingType.Garden || garden.BuildProgress < 1f) continue;
                    int room = (int)Math.Min(Math.Min(MaxDeliveryUnits, market.FreeSpace), _state?.Food ?? 0);
                    if (room < 1) continue;
                    if (!RouteFinder.IsReachable(_network, Center(garden), CenterOf(market.BuildingId), 8)) continue;

                    Scheduler.CreateDeliveryJob(garden.Id, market.BuildingId, ResourceType.Food, room, day);
                    Warehouses.AddIncoming(market.BuildingId, ResourceType.Food, room);
                    created++;
                    break;
                }
            }
        }

        private WarehouseState BestDestination(Building source, bool preferWarehouse)
        {
            WarehouseState best = null;
            float bestScore = float.MinValue;
            foreach (WarehouseState w in Warehouses.All.Values)
            {
                if (w.FreeSpace < 1f) continue;
                if (preferWarehouse && w.IsMarket) continue;
                int dist = Distance(Center(source), CenterOf(w.BuildingId));
                float score = -dist + (w.RoadConnected ? 6f : 0f);
                if (score > bestScore) { bestScore = score; best = w; }
            }
            return best;
        }

        /// <summary>
        /// Bozor zaxirasi kunlik qismini sotadi: mol kamayadi, xazinaga tanga tushadi.
        /// Bu — haqiqiy savdo operatsiyasi; yangi mol yaratilmaydi, faqat mavjud zaxira sotiladi.
        /// </summary>
        private void SellMarketStock()
        {
            if (_state == null) return;
            int day = _state.Day;
            if (_lastSellDay == day) return;
            _lastSellDay = day;

            float price = 3f;
            AdvancedEconomyManager econ = _state.Life?.Economy;
            if (econ != null)
            {
                float p = econ.Prices.Price(ProductType.Food);
                if (p > 0.2f) price = p;
            }

            foreach (WarehouseState market in Warehouses.All.Values)
            {
                if (!market.IsMarket) continue;
                float stock = market.Get(ResourceType.Food);
                if (stock < 1f) continue;
                float sell = MathF.Floor(stock * 0.35f);
                if (sell < 1f) continue;
                market.RemoveFrom(market.Stock, ResourceType.Food, sell);
                int coins = (int)MathF.Round(sell * price);
                _state.Coins += coins;
                SoldToday += sell;
                CoinsFromSalesToday += coins;
            }
        }

        // ==================================================================
        //  HAR KADR: vazifa hayot sikli
        // ==================================================================

        public void Update(float dt)
        {
            if (dt <= 0f) return;
            AssignPendingJobs();

            foreach (DeliveryJob job in Scheduler.Jobs)
            {
                if (job.IsTerminal || !job.IsAssigned) continue;
                job.StateTime += dt;
                TransportEntity t = _transports.GetEntity(job.AssignedTransportId);
                if (t == null)
                {
                    Fail(job, "Transport yo'qoldi");
                    continue;
                }

                switch (job.Status)
                {
                    case DeliveryJobStatus.Reserved:
                        // Yuklash tugadi: yuk HAQIQATAN manbadan olinadi (bir marta).
                        if (job.StateTime >= LoadSeconds)
                        {
                            float loaded = LoadFromSource(job);
                            if (loaded <= 0.01f)
                            {
                                Fail(job, "Manbada yuk qolmadi");
                                break;
                            }
                            job.Amount = loaded;
                            job.Loaded = true;
                            t.Cargo = loaded;
                            t.CargoType = job.ResourceType;
                            t.DeliveryJobId = job.Id;
                            string msg;
                            if (!_transports.AssignRoute(t.Id, t.CurrentTile, CenterOf(job.DestinationBuildingId), out msg))
                            {
                                // Yo'l yo'q — yuk manbaga qaytariladi, yo'qolmaydi.
                                ReturnToSource(job, loaded);
                                job.Loaded = false;
                                t.Cargo = 0f;
                                Fail(job, "Yo'l topilmadi");
                                break;
                            }
                            job.EstimatedArrivalMinutes = (t.RouteTiles.Count - 1) * 4f;
                            job.Status = DeliveryJobStatus.InTransit;
                            job.StateTime = 0f;
                        }
                        break;

                    case DeliveryJobStatus.Unloading:
                        if (job.StateTime >= UnloadSeconds)
                            Complete(job, t);
                        break;

                    case DeliveryJobStatus.Paused:
                        break;
                }
            }
        }

        private void AssignPendingJobs()
        {
            List<TransportEntity> idle = null;
            foreach (DeliveryJob job in Scheduler.Jobs)
            {
                if (job.Status != DeliveryJobStatus.Pending) continue;
                idle ??= _transports.IdleTransports();
                if (idle.Count == 0) return;

                TransportEntity t = idle[0];
                idle.RemoveAt(0);

                Point from = CenterOf(job.SourceBuildingId);
                Point to = CenterOf(job.DestinationBuildingId);
                if (!RouteFinder.IsReachable(_network, from, to, 8))
                {
                    Fail(job, "Manzil yo'lga ulanmagan");
                    continue;
                }

                // Transport manbada paydo bo'ladi va yuklaydi.
                t.Position = TransportRoute.TileCenter(from);
                t.CurrentTile = from;
                t.RouteFrom = from;
                t.RouteTo = to;
                t.ClearRoute();
                t.WaitTimer = LoadSeconds;
                t.AnimationState = TransportAnimationState.Loading;
                t.Cargo = 0f;

                job.AssignedTransportId = t.Id;
                job.Status = DeliveryJobStatus.Reserved;
                job.StateTime = 0f;
                Reservations.TryReserve(job.Id, job.SourceBuildingId, job.ResourceType, job.Amount);
            }
        }

        /// <summary>Manbadan yukni oladi: davlat ombori yoki ombor binosi.</summary>
        private float LoadFromSource(DeliveryJob job)
        {
            Building src = FindBuilding(job.SourceBuildingId);
            float amount = job.Amount;
            if (src != null && src.Type == BuildingType.Garden)
            {
                // Dehqon xo'jaligi hosili davlat omboriga tushgan — shu yerdan olinadi.
                if (_state == null) return 0f;
                float have = _state.Food;
                amount = Math.Min(amount, have);
                if (amount < 1f) return 0f;
                amount = MathF.Floor(amount);
                _state.Food -= (int)amount;
                return amount;
            }

            if (Reservations.TryCommit(job.Id, Warehouses, out float loaded))
                return loaded;
            return 0f;
        }

        /// <summary>Yetkazib berishni yakunlaydi: manzil zaxirasi BIR MARTA oshadi.</summary>
        private void Complete(DeliveryJob job, TransportEntity t)
        {
            if (job.Delivered) { Finish(job, t); return; }

            float deposit = Warehouses.Deposit(job.DestinationBuildingId, job.ResourceType, job.Amount);
            job.Delivered = true;
            float leftover = job.Amount - deposit;
            if (leftover > 0.01f)
            {
                // Manzil to'la — qolgan yuk manbaga qaytariladi (yo'qolmaydi, ikki marta hisoblanmaydi).
                ReturnToSource(job, leftover);
            }
            job.NoteUz = deposit > 0f ? "Qabul qilindi: " + (int)deposit : "Manzil to'la";
            CompletedToday++;
            OnEvent?.Invoke("delivery_completed", "Yetkazib berildi: " + (int)deposit + " birlik");
            Finish(job, t);
        }

        private void Finish(DeliveryJob job, TransportEntity t)
        {
            job.Status = DeliveryJobStatus.Completed;
            job.CompletedDay = _state?.Day ?? 0;
            job.StateTime = 0f;
            Warehouses.RemoveIncoming(job.DestinationBuildingId, job.ResourceType, job.Amount);
            Reservations.Release(job.Id, Warehouses);
            ReleaseTransport(job, t);
        }

        private void Fail(DeliveryJob job, string reason)
        {
            if (job.IsTerminal) return;
            job.Status = DeliveryJobStatus.Failed;
            job.NoteUz = reason;
            FailedToday++;
            Warehouses.RemoveIncoming(job.DestinationBuildingId, job.ResourceType, job.Amount);
            Reservations.Release(job.Id, Warehouses);
            ReleaseTransport(job, _transports.GetEntity(job.AssignedTransportId));
        }

        private void ReleaseTransport(DeliveryJob job, TransportEntity t)
        {
            if (t == null) return;
            t.Cargo = 0f;
            t.DeliveryJobId = 0;
            t.WaitTimer = 0f;
            t.ClearRoute();
            t.AnimationState = TransportAnimationState.Idle;
        }

        /// <summary>Yo'qolgan yukni manbaga qaytaradi.</summary>
        private void ReturnToSource(DeliveryJob job, float amount)
        {
            if (amount <= 0f) return;
            Building src = FindBuilding(job.SourceBuildingId);
            if (src != null && src.Type == BuildingType.Garden)
            {
                if (_state != null) _state.Food += (int)MathF.Floor(amount);
                return;
            }
            Warehouses.Deposit(job.SourceBuildingId, job.ResourceType, amount);
        }

        private void OnTransportArrived(TransportEntity t)
        {
            if (t == null || t.DeliveryJobId <= 0) return;
            if (!Scheduler.TryGetJob(t.DeliveryJobId, out DeliveryJob job)) return;
            if (job.Status != DeliveryJobStatus.InTransit) return;
            job.Status = DeliveryJobStatus.Unloading;
            job.StateTime = 0f;
            t.WaitTimer = UnloadSeconds;
            t.AnimationState = TransportAnimationState.Unloading;
        }

        // ==================================================================
        //  Yordamchilar
        // ==================================================================

        public Building FindBuilding(int id)
        {
            if (_buildings == null) return null;
            foreach (Building b in _buildings.Buildings) if (b.Id == id) return b;
            return null;
        }

        public Point CenterOf(int buildingId)
        {
            Building b = FindBuilding(buildingId);
            return b == null ? new Point(-1, -1) : Center(b);
        }

        public static Point Center(Building b)
            => new Point(b.Position.X + b.Size.X / 2, b.Position.Y + b.Size.Y / 2);

        private static int Distance(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        public void Clear()
        {
            Scheduler.Clear();
            Warehouses.Clear();
            Reservations.Clear();
            CompletedToday = 0;
            FailedToday = 0;
            SoldToday = 0f;
            CoinsFromSalesToday = 0;
            _lastSellDay = -1;
        }
    }
}
