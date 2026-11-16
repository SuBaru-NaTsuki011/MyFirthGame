using System;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityPlanning;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.World;

namespace AndijonSaltanatlar.SmartConstruction
{
    /// <summary>
    /// Tasdiqlangan rejani ijro etadi — PROMPT 15, 12-bo'lim. Qadamlar ketma-ket, mavjud
    /// ConstructionSystem orqali quriladi (narx BIR marta yechiladi). Resurs yetmasa yoki budjet
    /// oshsa pauzaga tushadi va yetishmayotgan narsani ko'rsatadi. AI hukmdor o'rniga budjetdan
    /// oshiq xarajat qilmaydi; har bir qadam oldindan qayta tekshiriladi — band joy uchun pul yechilmaydi.
    /// </summary>
    public class ConstructionQueueManager
    {
        private readonly GameStateManager _state;
        private readonly BuildingManager _buildings;
        private readonly MapManager _map;
        private readonly ConstructionSystem _construction;

        public ApprovedConstructionPlan Plan { get; private set; }
        public string LastMessageUz { get; private set; } = "";

        public ConstructionQueueManager(GameStateManager state, BuildingManager buildings, MapManager map,
            ConstructionSystem construction)
        {
            _state = state; _buildings = buildings; _map = map; _construction = construction;
        }

        public bool HasActivePlan => Plan != null && Plan.IsActive;

        public int RemainingPlannedCost
        {
            get
            {
                if (Plan == null) return 0;
                int sum = 0;
                foreach (var s in Plan.Steps)
                    if (s.Status == StepStatus.Pending) sum += s.Cost;
                return sum;
            }
        }

        /// <summary>Qayta tasdiqlash uchun aniq kerakli umumiy budjet (sarflangan + qolgan reja).</summary>
        public int RequiredBudget => (Plan?.SpentSoFar ?? 0) + RemainingPlannedCost;

        public string ProgressUz =>
            Plan == null ? "Faol reja yo'q." :
            $"{Plan.DoneCount}/{Plan.Steps.Count} qadam bajarildi · {Plan.SpentSoFar}/{Plan.ApprovedBudget} tanga.";

        public string StateNameUz => Plan == null ? "—" : Plan.State switch
        {
            PlanExecState.InProgress => "Qurilmoqda",
            PlanExecState.Paused => Plan.NeedsReapproval ? "Qayta tasdiq kutilmoqda" : "Pauzada",
            PlanExecState.Completed => "Yakunlandi",
            PlanExecState.Cancelled => "Bekor qilindi",
            PlanExecState.Failed => "Muvaffaqiyatsiz",
            _ => Plan.State.ToString()
        };

        /// <summary>CityPlan'ni tasdiqlangan navbatga aylantiradi (faqat yashil/sariq elementlar).</summary>
        public void Approve(CityPlan plan, int approvedBudget)
        {
            if (plan == null) return;
            var ap = new ApprovedConstructionPlan
            {
                PlanName = string.IsNullOrEmpty(plan.NameUz) ? "Yangi mahalla" : plan.NameUz,
                ApprovedBudget = Math.Max(0, approvedBudget),
                State = PlanExecState.InProgress,
                HousingCapacityAtApproval = _buildings.TotalPopulation,
                PopulationAtApproval = _state.Population
            };

            // Yo'llar birinchi — mahalla avval mavjud ko'chaga ulanadi.
            foreach (var r in plan.Roads)
            {
                if (r.Status == BlueprintStatus.Blocked) continue;
                ap.Steps.Add(new PlanStep
                {
                    Kind = StepKind.Road, Tile = r.Tile, Size = 1, Cost = Math.Max(0, r.Cost),
                    LabelUz = "Yo'l (" + r.Tile.X + "," + r.Tile.Y + ")"
                });
            }
            foreach (var b in plan.Buildings)
            {
                if (b.Status == BlueprintStatus.Blocked) continue;
                ap.Steps.Add(new PlanStep
                {
                    Kind = StepKind.Building, Building = b.Kind, Tile = b.Tile, Size = b.Size,
                    Cost = Math.Max(0, b.Cost), LabelUz = b.NameUz
                });
            }

            Plan = ap;
            LastMessageUz = ap.Steps.Count == 0
                ? "Rejada bajariladigan qadam yo'q."
                : $"Reja tasdiqlandi: {ap.Steps.Count} qadam, budjet {ap.ApprovedBudget} tanga.";
        }

        /// <summary>Save'dan tiklash uchun.</summary>
        public void SetPlan(ApprovedConstructionPlan plan)
        {
            Plan = plan;
            if (Plan == null) { LastMessageUz = ""; return; }
            Plan.Steps ??= new System.Collections.Generic.List<PlanStep>();
            if (Plan.State == PlanExecState.Queued || Plan.State == PlanExecState.Approved)
                Plan.State = PlanExecState.InProgress;
            LastMessageUz = "Tasdiqlangan reja yuklandi: " + ProgressUz;
        }

        public void Pause()
        {
            if (Plan == null || Plan.State != PlanExecState.InProgress) return;
            Plan.State = PlanExecState.Paused;
            LastMessageUz = "Qurilish pauzaga qo'yildi.";
        }

        public void Resume()
        {
            if (Plan == null || Plan.State != PlanExecState.Paused) return;
            if (Plan.NeedsReapproval)
            {
                LastMessageUz = "Avval yangi budjetni tasdiqlang.";
                return;
            }
            Plan.State = PlanExecState.InProgress;
            Plan.PausedForResources = false;
            LastMessageUz = "Qurilish davom ettirildi.";
        }

        public void Cancel()
        {
            if (Plan == null) return;
            foreach (var s in Plan.Steps)
                if (s.Status == StepStatus.Pending) s.Status = StepStatus.Skipped;
            Plan.State = PlanExecState.Cancelled;
            LastMessageUz = "Reja bekor qilindi (boshlanmagan ishlar to'xtatildi).";
        }

        /// <summary>Foydalanuvchi yangi budjetni tasdiqlaydi — bu yana bitta aniq ruxsat.</summary>
        public void Reapprove(int newBudget)
        {
            if (Plan == null) return;
            Plan.ApprovedBudget = Math.Max(Plan.SpentSoFar, newBudget);
            Plan.NeedsReapproval = false;
            Plan.PausedForResources = false;
            Plan.State = PlanExecState.InProgress;
            LastMessageUz = "Budjet qayta tasdiqlandi: " + Plan.ApprovedBudget + " tanga.";
        }

        /// <summary>Reja uchun zarur bo'lgan aniq budjetni avtomatik tasdiqlaydi (UI tugmasi).</summary>
        public void ReapproveRequired()
        {
            if (Plan == null) return;
            Reapprove(RequiredBudget);
        }

        /// <summary>Bitta navbat qadamini bajaradi. Ish bajarilsa true. Kunlik tick yoki smoke chaqiradi.</summary>
        public bool StepOnce()
        {
            if (Plan == null) return false;
            if (Plan.State == PlanExecState.Queued) Plan.State = PlanExecState.InProgress;
            if (Plan.State != PlanExecState.InProgress) return false;

            PlanStep step = NextPending();
            if (step == null) { Complete(); return false; }

            // Budjet nazorati — tasdiqlangandan oshmaydi.
            if (Plan.SpentSoFar + step.Cost > Plan.ApprovedBudget)
            {
                Plan.NeedsReapproval = true;
                Plan.State = PlanExecState.Paused;
                LastMessageUz = $"Budjet chegarasi: {step.LabelUz} uchun {step.Cost} tanga kerak, " +
                                $"tasdiqlangan budjet {Plan.ApprovedBudget} tanga. Qayta tasdiqlang.";
                return false;
            }

            // Resurs nazorati — hech narsa yechilmaydi.
            if (_state.Coins < step.Cost)
            {
                Plan.PausedForResources = true;
                Plan.State = PlanExecState.Paused;
                LastMessageUz = $"Tanga yetarli emas: {step.LabelUz} uchun {step.Cost}, xazinada {_state.Coins}. Qurilish pauzada.";
                return false;
            }

            if (step.Kind == StepKind.Road) ExecuteRoad(step);
            else ExecuteBuilding(step);

            if (Plan.PendingCount == 0) Complete();
            return true;
        }

        private void ExecuteRoad(PlanStep step)
        {
            int x = step.Tile.X, y = step.Tile.Y;
            if (!_map.InBounds(x, y))
            {
                step.Status = StepStatus.Failed;
                LastMessageUz = "Yo'l katagi xarita tashqarisida — o'tkazib yuborildi.";
                return;
            }
            if (_map.GetTile(x, y) == TileType.Road)
            {
                // Allaqachon qurilgan yo'l uchun pul yechilmaydi.
                step.Status = StepStatus.Skipped;
                LastMessageUz = "Yo'l allaqachon mavjud — takroriy xarajat olinmadi.";
                return;
            }
            if (_map.GetOccupant(x, y) != null || !_map.IsBuildableTerrain(x, y))
            {
                step.Status = StepStatus.Failed;
                LastMessageUz = "Yo'l joyi band yoki suv — o'tkazib yuborildi (pul yechilmadi).";
                return;
            }
            if (_state.Coins < step.Cost)
            {
                Plan.PausedForResources = true;
                Plan.State = PlanExecState.Paused;
                LastMessageUz = "Tanga yetarli emas — yo'l qurilishi pauzada.";
                return;
            }

            _map.SetTile(x, y, TileType.Road);
            _state.Coins -= step.Cost;
            Plan.SpentSoFar += step.Cost;
            step.Status = StepStatus.Done;
            LastMessageUz = $"Yo'l qurildi ({step.Tile.X},{step.Tile.Y}) — {step.Cost} tanga.";
        }

        private void ExecuteBuilding(PlanStep step)
        {
            int size = Math.Max(1, step.Size);
            if (!_map.InBounds(step.Tile.X, step.Tile.Y) ||
                !_state.IsLandBuildable(step.Tile, size) || !_buildings.CanPlace(step.Tile, size))
            {
                step.Status = StepStatus.Failed;
                LastMessageUz = $"{step.LabelUz} joyi endi band yoki yopiq — o'tkazib yuborildi (pul yechilmadi).";
                return;
            }

            if (_state.Coins < step.Cost)
            {
                Plan.PausedForResources = true;
                Plan.State = PlanExecState.Paused;
                LastMessageUz = "Tanga yetarli emas — bino qurilishi pauzada.";
                return;
            }

            _state.EnterBuildMode(step.Building);
            bool ok = _construction.PlaceAtOrigin(step.Tile, false); // narx shu yerda BIR marta yechiladi
            _state.CancelBuild();
            if (ok)
            {
                step.Status = StepStatus.Done;
                Plan.SpentSoFar += step.Cost;
                LastMessageUz = $"{step.LabelUz} qurildi — {step.Cost} tanga.";
            }
            else
            {
                step.Status = StepStatus.Failed;
                LastMessageUz = $"{step.LabelUz} qurilmadi (joy/texnologiya) — o'tkazib yuborildi.";
            }
        }

        private void Complete()
        {
            int done = 0, failed = 0;
            foreach (var s in Plan.Steps)
            {
                if (s.Status == StepStatus.Done) done++;
                else if (s.Status == StepStatus.Failed) failed++;
            }
            Plan.State = PlanExecState.Completed;
            LastMessageUz = $"Reja yakunlandi: {done} qadam bajarildi" +
                            (failed > 0 ? $", {failed} qadam o'tkazib yuborildi." : ".") +
                            $" Sarflandi: {Plan.SpentSoFar} tanga.";
        }

        private PlanStep NextPending()
        {
            foreach (var s in Plan.Steps) if (s.Status == StepStatus.Pending) return s;
            return null;
        }
    }
}
