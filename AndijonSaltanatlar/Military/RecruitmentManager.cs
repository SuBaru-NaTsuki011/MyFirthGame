using System.Collections.Generic;

namespace AndijonSaltanatlar.Military
{
    /// <summary>Tayyorlanayotgan bitta jangchi buyurtmasi.</summary>
    public sealed class RecruitOrder
    {
        public UnitType Type { get; set; }
        public int DaysLeft { get; set; }
        public int OwnerKingdomId { get; set; }
        public int RegionId { get; set; }
    }

    /// <summary>
    /// Jangchi tayyorlash navbati. Bir vaqtda MaxConcurrent birlik tayyorlanadi (bino darajasiga bog'liq).
    /// </summary>
    public sealed class RecruitmentManager
    {
        public List<RecruitOrder> Queue { get; } = new List<RecruitOrder>();
        public int MaxConcurrent { get; set; } = 2;

        public void Enqueue(UnitType type, int owner, int region)
        {
            UnitDef d = UnitCatalog.Get(type);
            Queue.Add(new RecruitOrder { Type = type, DaysLeft = d.TrainingDays, OwnerKingdomId = owner, RegionId = region });
        }

        /// <summary>Bir kunni qayta ishlaydi; tayyor bo'lgan buyurtmalarni qaytaradi.</summary>
        public List<RecruitOrder> Tick()
        {
            var done = new List<RecruitOrder>();
            int active = 0;
            foreach (RecruitOrder o in Queue)
            {
                if (active >= MaxConcurrent) break;
                active++;
                o.DaysLeft--;
            }
            for (int i = Queue.Count - 1; i >= 0; i--)
                if (Queue[i].DaysLeft <= 0) { done.Add(Queue[i]); Queue.RemoveAt(i); }
            return done;
        }
    }
}
