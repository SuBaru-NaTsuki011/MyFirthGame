using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Economy;

namespace AndijonSaltanatlar.Logistics
{
    /// <summary>
    /// Yetkazib berish ishlari navbati. Holat o'tishlarini LogisticsManager boshqaradi;
    /// scheduler faqat ro'yxat, Id va qidiruvni saqlaydi (bitta javobgarlik).
    /// </summary>
    public sealed class DeliveryScheduler
    {
        private readonly List<DeliveryJob> _jobs = new List<DeliveryJob>();
        private int _nextJobId = 1;

        public event Action<DeliveryJob> JobCreated;
        public event Action<DeliveryJob> JobCompleted;

        public IReadOnlyList<DeliveryJob> Jobs => _jobs;
        public int JobCount => _jobs.Count;
        public int NextId => _nextJobId;

        public int CreateDeliveryJob(int sourceBuildingId, int destinationBuildingId, ResourceType resourceType,
                                     float amount, int day = 0)
        {
            var job = new DeliveryJob(_nextJobId, sourceBuildingId, destinationBuildingId, resourceType, amount, day);
            _jobs.Add(job);
            JobCreated?.Invoke(job);
            return _nextJobId++;
        }

        public bool TryGetJob(int jobId, out DeliveryJob job)
        {
            for (int i = 0; i < _jobs.Count; i++)
                if (_jobs[i].Id == jobId) { job = _jobs[i]; return true; }
            job = null;
            return false;
        }

        public DeliveryJob ResolveJob(int jobId) => TryGetJob(jobId, out DeliveryJob j) ? j : null;

        public void NotifyCompleted(DeliveryJob job) => JobCompleted?.Invoke(job);

        /// <summary>Yakunlangan vazifalar ro'yxatdan chiqariladi (tarix alohida saqlanmaydi).</summary>
        public void RemoveCompletedJobs()
            => _jobs.RemoveAll(j => j.Status == DeliveryJobStatus.Completed
                                  || j.Status == DeliveryJobStatus.Cancelled
                                  || j.Status == DeliveryJobStatus.Failed);

        public List<DeliveryJob> ActiveJobs()
        {
            var list = new List<DeliveryJob>();
            foreach (DeliveryJob j in _jobs) if (!j.IsTerminal) list.Add(j);
            return list;
        }

        public void Clear()
        {
            _jobs.Clear();
            _nextJobId = 1;
        }

        /// <summary>Save/Load: Id hisoblagichi vazifalar bilan birga tiklanadi (Id to'qnashmasin).</summary>
        public void Restore(IEnumerable<DeliveryJob> saved, int nextId)
        {
            _jobs.Clear();
            if (saved != null) _jobs.AddRange(saved);
            _nextJobId = Math.Max(1, nextId);
        }
    }
}
