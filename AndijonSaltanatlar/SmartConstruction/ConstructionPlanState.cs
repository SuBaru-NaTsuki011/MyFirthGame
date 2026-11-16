using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.SmartConstruction
{
    /// <summary>Tasdiqlangan reja holati — PROMPT 15, 12-bo'lim.</summary>
    public enum PlanExecState
    {
        Draft,
        ReadyForApproval,
        Approved,
        Queued,
        InProgress,
        Paused,
        Completed,
        Cancelled,
        Failed
    }

    public enum StepKind { Building, Road }
    public enum StepStatus { Pending, Done, Failed, Skipped }

    /// <summary>Navbatdagi bitta qurilish qadami (bino yoki yo'l katagi).</summary>
    public class PlanStep
    {
        public StepKind Kind { get; set; }
        public BuildingType Building { get; set; }
        public Point Tile { get; set; }
        public int Size { get; set; } = 1;
        public int Cost { get; set; }
        public StepStatus Status { get; set; } = StepStatus.Pending;
        public string LabelUz { get; set; } = "";
    }

    /// <summary>
    /// Tasdiqlangan va navbatga qo'yilgan reja. Tasdiqlangan budjet saqlanadi; bundan oshsa qayta tasdiq.
    /// Save/Load uchun oddiy POCO (System.Text.Json).
    /// </summary>
    public class ApprovedConstructionPlan
    {
        public string PlanName { get; set; } = "";
        public PlanExecState State { get; set; } = PlanExecState.Queued;
        public int ApprovedBudget { get; set; }
        public int SpentSoFar { get; set; }
        public List<PlanStep> Steps { get; set; } = new();
        /// <summary>Qurilish narxi tasdiqlangan budjetdan oshgani uchun qayta tasdiq kutilmoqda.</summary>
        public bool NeedsReapproval { get; set; }
        /// <summary>Resurs yetmagani uchun pauzada.</summary>
        public bool PausedForResources { get; set; }

        /// <summary>Tasdiqlash paytidagi uy-joy sig'imi — rivojlanish natijasini solishtirish uchun.</summary>
        public int HousingCapacityAtApproval { get; set; }
        /// <summary>Tasdiqlash paytidagi aholi soni — rivojlanish natijasini solishtirish uchun.</summary>
        public int PopulationAtApproval { get; set; }

        public int PendingCount
        {
            get { int n = 0; foreach (var s in Steps) if (s.Status == StepStatus.Pending) n++; return n; }
        }
        public int DoneCount
        {
            get { int n = 0; foreach (var s in Steps) if (s.Status == StepStatus.Done) n++; return n; }
        }
        public bool IsActive => State == PlanExecState.InProgress || State == PlanExecState.Queued || State == PlanExecState.Paused;
    }
}
