using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Blueprints;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityPlanning;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Forecasting;
using AndijonSaltanatlar.SmartConstruction;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.AIGovernor
{
    /// <summary>Save/Load uchun navbat qadami — Point o'rniga aniq X/Y (System.Text.Json uchun).</summary>
    public class GovernorStepSave
    {
        public int Kind { get; set; }
        public int Building { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Size { get; set; } = 1;
        public int Cost { get; set; }
        public int Status { get; set; }
        public string LabelUz { get; set; } = "";
    }

    /// <summary>Save/Load uchun tasdiqlangan reja navbati.</summary>
    public class GovernorQueueSave
    {
        public string PlanName { get; set; } = "";
        public int State { get; set; }
        public int ApprovedBudget { get; set; }
        public int SpentSoFar { get; set; }
        public bool NeedsReapproval { get; set; }
        public bool PausedForResources { get; set; }
        public int HousingCapacityAtApproval { get; set; }
        public int PopulationAtApproval { get; set; }
        public List<GovernorStepSave> Steps { get; set; } = new();
    }

    /// <summary>Saqlanadigan blueprint binosi (Point o'rniga X/Y).</summary>
    public class GovernorBlueprintItemSave
    {
        public int Id { get; set; }
        public int Kind { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Size { get; set; } = 1;
        public int Cost { get; set; }
        public int Status { get; set; }
        public int BuildTimeDays { get; set; } = 3;
        public int PopulationCapacity { get; set; }
        public string BenefitUz { get; set; } = "";
        public string RequirementUz { get; set; } = "";
        public string RequiredTech { get; set; } = "";
        public string StatusReasonUz { get; set; } = "";
    }

    /// <summary>Saqlanadigan blueprint yo'l katagi.</summary>
    public class GovernorBlueprintRoadSave
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Cost { get; set; }
        public int Status { get; set; }
        public string StatusReasonUz { get; set; } = "";
    }

    /// <summary>Tasdiqlanmagan preview rejasi — Save/Load (PROMPT 15, 20-bo'lim).</summary>
    public class GovernorBlueprintSave
    {
        public int Id { get; set; }
        public int CreatedDay { get; set; }
        public int Type { get; set; }
        public string NameUz { get; set; } = "";
        public string RationaleUz { get; set; } = "";
        public int AnchorX { get; set; } = -1;
        public int AnchorY { get; set; } = -1;
        public int BuildingsCost { get; set; }
        public int RoadsCost { get; set; }
        public int TotalBuildDays { get; set; }
        public int State { get; set; }
        public bool IsValid { get; set; }
        public int SelectedItemId { get; set; }
        public List<GovernorBlueprintItemSave> Buildings { get; set; } = new();
        public List<GovernorBlueprintRoadSave> Roads { get; set; } = new();
    }

    /// <summary>Yakunlangan/bekor qilingan rejalar tarixi.</summary>
    public class GovernorHistoryEntry
    {
        public string PlanName { get; set; } = "";
        public PlanExecState State { get; set; }
        public int Day { get; set; }
        public int DoneSteps { get; set; }
        public int FailedSteps { get; set; }
        public int Spent { get; set; }
        public int ApprovedBudget { get; set; }

        /// <summary>Reja tasdiqlangandagi va qurilishdan keyingi haqiqiy natijalar.</summary>
        public int HousingBefore { get; set; }
        public int HousingAfter { get; set; }
        public int PopulationBefore { get; set; }
        public int PopulationAfter { get; set; }
        public int BuiltBuildings { get; set; }
        public int BuiltRoads { get; set; }

        public bool HasImpact => State == PlanExecState.Completed && (HousingBefore > 0 || PopulationBefore > 0);

        public string SummaryUz
        {
            get
            {
                string head = $"{PlanName}: {(State == PlanExecState.Completed ? "yakunlandi" : "bekor qilindi")}, " +
                              $"{DoneSteps} qadam, {Spent}/{ApprovedBudget} tanga" +
                              (FailedSteps > 0 ? $", {FailedSteps} muvaffaqiyatsiz" : "") +
                              $". Kun: {Day}.";
                if (!HasImpact) return head;
                return head + $" Natija: sig'im {HousingBefore}->{HousingAfter} " +
                       $"({HousingAfter - HousingBefore:+0;-0;0}), aholi {PopulationBefore}->{PopulationAfter} " +
                       $"({PopulationAfter - PopulationBefore:+0;-0;0}); {BuiltBuildings} bino, {BuiltRoads} yo'l.";
            }
        }
    }

    /// <summary>Save/Load uchun reja tarixi.</summary>
    public class GovernorHistorySave
    {
        public string PlanName { get; set; } = "";
        public int State { get; set; }
        public int Day { get; set; }
        public int DoneSteps { get; set; }
        public int FailedSteps { get; set; }
        public int Spent { get; set; }
        public int ApprovedBudget { get; set; }
        public int HousingBefore { get; set; }
        public int HousingAfter { get; set; }
        public int PopulationBefore { get; set; }
        public int PopulationAfter { get; set; }
        public int BuiltBuildings { get; set; }
        public int BuiltRoads { get; set; }
    }

    /// <summary>Save/Load uchun AI Governor holati — PROMPT 15, 20-bo'lim.</summary>
    public class AIGovernorSaveState
    {
        public int Level { get; set; } = (int)AutonomyLevel.ApprovedExecution;
        public int ApprovedBudget { get; set; }
        public GovernorQueueSave Queue { get; set; }
        public GovernorBlueprintSave Blueprint { get; set; }
        public List<GovernorHistorySave> History { get; set; } = new();
    }

    /// <summary>
    /// AI Governor (Donishmand Vazir) fasadi — PROMPT 15. Shahar tahlili, tavsiyalar, reja (blueprint),
    /// iqtisodiy prognoz va tasdiqlangan rejani ijro etishni bir joyda boshqaradi. Mavjud ConstructionSystem,
    /// LandExpansion va AdvisorManager tizimlari ustiga qo'shiladi (ularni qayta yaratmaydi).
    /// AI hukmdorning o'rniga mustaqil xarajat qilmaydi — har bir qurilish foydalanuvchi TASDIG'I bilan.
    /// </summary>
    public class AIGovernorManager
    {
        private GameStateManager _state;
        private BuildingManager _buildings;
        private MapManager _map;

        private CityAnalysisEngine _analysis;
        private RecommendationEngine _recEngine;
        private CityPlanningManager _planner;
        private EconomicForecastService _forecast;
        private ICityPlanningGrid _grid;

        public AdvisorPermissions Permissions { get; private set; } = new();
        public ConstructionQueueManager Queue { get; private set; }
        public BlueprintManager Blueprints { get; private set; }

        public CityAnalysisReport Report { get; private set; }
        public List<Recommendation> Recommendations { get; private set; } = new();
        public CityPlan Blueprint => Blueprints?.Plan;
        public PlanCostEstimate CurrentEstimate { get; private set; }
        public List<ForecastScenario> CurrentForecast { get; private set; } = new();
        public List<GovernorHistoryEntry> History { get; } = new();
        public string LastApprovedName { get; private set; } = "";
        private readonly HashSet<string> _historySignatures = new();

        /// <summary>Vazir xabarini ko'rsatish (toast) uchun.</summary>
        public Action<string> OnMessage { get; set; }

        private int _dayCounter;
        private string _lastQueueMessage = "";

        public void Initialize(GameStateManager state, BuildingManager buildings, MapManager map,
            ConstructionSystem construction, AIGovernorSaveState saved)
        {
            _state = state; _buildings = buildings; _map = map;
            _grid = new MapPlanningGrid(state, map);
            _analysis = new CityAnalysisEngine(state, buildings, map);
            _recEngine = new RecommendationEngine();
            _planner = new CityPlanningManager(_grid, () => state.Coins, state.IsBuildingUnlocked, () => state.Day);
            _forecast = new EconomicForecastService();
            Blueprints = new BlueprintManager(_grid, () => state.Coins, state.IsBuildingUnlocked);
            Queue = new ConstructionQueueManager(state, buildings, map, construction);

            if (saved != null)
            {
                Permissions.Level = (AutonomyLevel)saved.Level;
                Permissions.ApprovedBudget = saved.ApprovedBudget;
                if (saved.Queue != null) Queue.SetPlan(FromSave(saved.Queue));
                if (saved.Blueprint != null)
                    Blueprints.Restore(FromSave(saved.Blueprint), saved.Blueprint.SelectedItemId);
                foreach (var h in saved.History ?? new List<GovernorHistorySave>())
                {
                    var entry = new GovernorHistoryEntry
                    {
                        PlanName = h.PlanName, State = (PlanExecState)h.State, Day = h.Day,
                        DoneSteps = h.DoneSteps, FailedSteps = h.FailedSteps,
                        Spent = h.Spent, ApprovedBudget = h.ApprovedBudget,
                        HousingBefore = h.HousingBefore, HousingAfter = h.HousingAfter,
                        PopulationBefore = h.PopulationBefore, PopulationAfter = h.PopulationAfter,
                        BuiltBuildings = h.BuiltBuildings, BuiltRoads = h.BuiltRoads
                    };
                    History.Add(entry);
                    _historySignatures.Add(HistorySignature(entry));
                }
            }
            Analyze();
            if (Blueprint != null) RefreshPreviewData();
        }

        // ---- Tahlil & tavsiya ----
        public void Analyze()
        {
            if (_analysis == null) return;
            Report = _analysis.Analyze();
            Recommendations = _recEngine.Recommend(Report);
        }

        public string TopAdviceUz()
        {
            if (Recommendations == null || Recommendations.Count == 0) return "Shahar barqaror, hazratim.";
            var r = Recommendations[0];
            return "Hazratim, " + r.ReasonUz + " Tavsiya: " + r.TitleUz + ".";
        }

        public string LandAdviceUz()
        {
            if (_state == null) return "";
            if (Report != null && Report.BuildableTiles >= 20)
                return "Hozircha ochiq yer yetarli — yangi yer sotib olish shart emas.";
            foreach (var p in _state.Land.Parcels)
                if (p.State == AndijonSaltanatlar.LandExpansion.LandParcelState.Unlockable ||
                    p.State == AndijonSaltanatlar.LandExpansion.LandParcelState.Locked)
                    return $"Yangi hudud kerak bo'lsa: {p.NameUz} ({p.UnlockCost} tanga, {p.RequiredPopulation} aholi talab). " +
                           "AI o'zi yer sotib olmaydi — xaritada uchastkani tanlab, siz tasdiqlaysiz.";
            return "Ochiladigan yangi uchastka topilmadi.";
        }

        // ---- Rejalashtirish (blueprint) ----
        public CityPlan GeneratePlan(DistrictType type)
        {
            if (!Permissions.CanPlan)
            {
                OnMessage?.Invoke("AI rejalashtirish o'chirilgan. Rejimni 'Shahar me'mori'ga o'zgartiring.");
                return null;
            }
            CityPlan plan = _planner.GeneratePlan(type, Report?.HousingShortage ?? 0);
            return AdoptBlueprint(plan);
        }

        /// <summary>Mavjud mahalla ichidagi bo'shliqlarni to'ldiruvchi kichik reja.</summary>
        public CityPlan GenerateImprovementPlan()
        {
            if (!Permissions.CanPlan)
            {
                OnMessage?.Invoke("AI rejalashtirish o'chirilgan. Rejimni 'Shahar me'mori'ga o'zgartiring.");
                return null;
            }
            CityPlan plan = _planner.GeneratePlan(DistrictType.Mixed, Report?.HousingShortage ?? 0, true);
            return AdoptBlueprint(plan);
        }

        private CityPlan AdoptBlueprint(CityPlan plan)
        {
            Blueprints.SetPlan(plan);
            RefreshPreviewData();
            OnMessage?.Invoke(plan?.RationaleUz ?? "Reja tuzib bo'lmadi.");
            return plan;
        }

        public bool AddBlueprintItem(BuildingType kind, Point origin)
        {
            if (Blueprint == null)
            {
                OnMessage?.Invoke("Avval yangi reja tuzing yoki mavjud blueprintni oching.");
                return false;
            }
            bool ok = Blueprints.AddBuilding(kind, origin);
            RefreshPreviewData();
            OnMessage?.Invoke(Blueprints.LastMessageUz);
            return ok;
        }

        public string DescribeSelectedBlueprintUz()
            => BlueprintManager.Describe(Blueprints?.SelectedItem);

        public void CancelBlueprint()
        {
            Blueprints.Clear();
            CurrentEstimate = null;
            CurrentForecast = new List<ForecastScenario>();
        }

        public bool SelectBlueprintItem(int id) => Blueprints.SelectItem(id);
        public bool SelectBlueprintAt(Point tile) => Blueprints.SelectAt(tile);

        public bool RemoveSelectedBlueprintItem()
        {
            bool ok = Blueprints.RemoveSelected();
            RefreshPreviewData();
            OnMessage?.Invoke(Blueprints.LastMessageUz);
            return ok;
        }

        public bool MoveSelectedBlueprintItem(Point newOrigin)
        {
            bool ok = Blueprints.MoveSelected(newOrigin);
            RefreshPreviewData();
            OnMessage?.Invoke(Blueprints.LastMessageUz);
            return ok;
        }

        public bool ToggleBlueprintRoad(Point tile)
        {
            bool ok = Blueprints.ToggleRoad(tile);
            RefreshPreviewData();
            OnMessage?.Invoke(Blueprints.LastMessageUz);
            return ok;
        }

        public void RevalidateBlueprint()
        {
            if (Blueprint == null) return;
            Blueprints.Recalculate();
            RefreshPreviewData();
        }

        private void RefreshPreviewData()
        {
            if (Blueprint == null)
            {
                CurrentEstimate = null;
                CurrentForecast = new List<ForecastScenario>();
                return;
            }
            CurrentEstimate = PlanCostEstimator.Estimate(Blueprint, _state.Coins, BlueprintManager.RoadTileCost);
            CurrentForecast = _forecast.Forecast(Report ?? _analysis.Analyze(), Blueprint);
        }

        public List<ForecastScenario> ForecastCurrent() => CurrentForecast ?? new List<ForecastScenario>();

        // ---- Tasdiqlash & ijro ----
        /// <summary>Foydalanuvchi rejani tasdiqlaydi — navbatga qo'yiladi. Bu YAGONA xarajat ruxsati.</summary>
        public bool ApprovePlan()
        {
            if (Blueprint == null) { OnMessage?.Invoke("Tasdiqlash uchun reja yo'q."); return false; }
            if (Queue.HasActivePlan && Queue.Plan.State != PlanExecState.Completed &&
                Queue.Plan.State != PlanExecState.Cancelled)
            {
                OnMessage?.Invoke("Avval joriy qurilish rejasini yakunlang yoki bekor qiling.");
                return false;
            }

            Blueprints.Recalculate();
            if (!Blueprint.IsValid || Blueprint.BuildableCount == 0)
            {
                OnMessage?.Invoke("Reja yaroqsiz — qizil elementlarni tuzating yoki qayta rejalashtiring.");
                return false;
            }

            int budget = Blueprint.TotalCost;
            if (budget <= 0) { OnMessage?.Invoke("Rejada xarajat hisoblanmadi."); return false; }

            Permissions.ApprovedBudget = budget;
            LastApprovedName = Blueprint.NameUz;
            Blueprint.State = PlanExecState.Approved;
            Queue.Approve(Blueprint, budget);
            OnMessage?.Invoke(Permissions.CanExecute
                ? $"Reja tasdiqlandi ({budget} tanga). Qurilish boshlanadi."
                : $"Reja navbatga qo'yildi ({budget} tanga), lekin ijro rejimi o'chirilgan. " +
                  "Vazir panelida 'Tasdiqlangan reja ijrochisi'ni tanlang.");
            CancelBlueprint();
            return true;
        }

        public void PauseQueue() { Queue.Pause(); OnMessage?.Invoke(Queue.LastMessageUz); }
        public void ResumeQueue() { Queue.Resume(); OnMessage?.Invoke(Queue.LastMessageUz); }
        public void CancelQueue() { Queue.Cancel(); TrackHistory(); OnMessage?.Invoke(Queue.LastMessageUz); }

        public void ReapproveQueue()
        {
            if (Queue.Plan == null) return;
            if (!Queue.Plan.NeedsReapproval)
            {
                OnMessage?.Invoke("Qayta tasdiq kerak emas.");
                return;
            }
            int required = Queue.RequiredBudget;
            Queue.Reapprove(required);
            Permissions.ApprovedBudget = required;
            OnMessage?.Invoke(Queue.LastMessageUz);
        }

        /// <summary>Navbatni bir qadam oldinga suradi (test/smoke va UI uchun).</summary>
        public bool StepQueueOnce()
        {
            bool wasCompleted = Queue.Plan != null && Queue.Plan.State == PlanExecState.Completed;
            bool worked = Queue.StepOnce();
            bool completedNow = !wasCompleted && Queue.Plan != null &&
                                Queue.Plan.State == PlanExecState.Completed;
            if (completedNow) Analyze(); // qurilish natijasi darhol hisobotga tushadi
            TrackHistory();
            if (Queue.Plan != null && Queue.LastMessageUz != _lastQueueMessage)
            {
                _lastQueueMessage = Queue.LastMessageUz;
                OnMessage?.Invoke(Queue.LastMessageUz);
            }
            return worked;
        }

        /// <summary>UI uchun: bitta kun ichida bajariladigan qadamlar (pauza/tasdiq hurmat qilinadi).</summary>
        public void TickConstruction()
        {
            if (!Permissions.CanExecute || Queue.Plan == null) return;
            if (Queue.Plan.State != PlanExecState.InProgress) return;
            StepQueueOnce();
        }

        // ---- Kunlik tick ----
        public void OnDayTick()
        {
            _dayCounter++;
            if (_dayCounter % 3 == 0 && Report != null && Recommendations != null)
            {
                Analyze(); // har frame emas (performance)
                if (Blueprint != null) RevalidateBlueprint();
            }

            if (Permissions.CanExecute && Queue.HasActivePlan)
                TickConstruction();
            TrackHistory();
        }

        public AIGovernorSaveState GetSaveState()
        {
            TrackHistory();
            return new AIGovernorSaveState
            {
                Level = (int)Permissions.Level,
                ApprovedBudget = Permissions.ApprovedBudget,
                Queue = ToSave(Queue?.Plan),
                Blueprint = ToSave(Blueprints?.Plan, Blueprints?.SelectedItemId ?? 0),
                History = History.ConvertAll(h => new GovernorHistorySave
                {
                    PlanName = h.PlanName, State = (int)h.State, Day = h.Day,
                    DoneSteps = h.DoneSteps, FailedSteps = h.FailedSteps,
                    Spent = h.Spent, ApprovedBudget = h.ApprovedBudget,
                    HousingBefore = h.HousingBefore, HousingAfter = h.HousingAfter,
                    PopulationBefore = h.PopulationBefore, PopulationAfter = h.PopulationAfter,
                    BuiltBuildings = h.BuiltBuildings, BuiltRoads = h.BuiltRoads
                })
            };
        }

        private void TrackHistory()
        {
            ApprovedConstructionPlan plan = Queue?.Plan;
            if (plan == null) return;
            if (plan.State != PlanExecState.Completed && plan.State != PlanExecState.Cancelled) return;

            int failed = 0;
            foreach (var s in plan.Steps) if (s.Status == StepStatus.Failed) failed++;
            DevelopmentImpact impact = DevelopmentImpactEvaluator.Evaluate(plan,
                _buildings?.TotalPopulation ?? 0, _state?.Population ?? 0);
            var entry = new GovernorHistoryEntry
            {
                PlanName = plan.PlanName,
                State = plan.State,
                Day = _state?.Day ?? 1,
                DoneSteps = plan.DoneCount,
                FailedSteps = failed,
                Spent = plan.SpentSoFar,
                ApprovedBudget = plan.ApprovedBudget,
                HousingBefore = impact.HousingBefore,
                HousingAfter = impact.HousingAfter,
                PopulationBefore = impact.PopulationBefore,
                PopulationAfter = impact.PopulationAfter,
                BuiltBuildings = impact.BuiltBuildings,
                BuiltRoads = impact.BuiltRoads
            };
            string signature = HistorySignature(entry);
            if (_historySignatures.Add(signature)) History.Add(entry);
        }

        private static string HistorySignature(GovernorHistoryEntry h)
            => h.PlanName + "|" + (int)h.State + "|" + h.DoneSteps + "|" + h.Spent + "|" + h.Day;

        private static GovernorQueueSave ToSave(ApprovedConstructionPlan plan)
        {
            if (plan == null) return null;
            var save = new GovernorQueueSave
            {
                PlanName = plan.PlanName,
                State = (int)plan.State,
                ApprovedBudget = plan.ApprovedBudget,
                SpentSoFar = plan.SpentSoFar,
                NeedsReapproval = plan.NeedsReapproval,
                PausedForResources = plan.PausedForResources,
                HousingCapacityAtApproval = plan.HousingCapacityAtApproval,
                PopulationAtApproval = plan.PopulationAtApproval
            };
            foreach (var s in plan.Steps)
                save.Steps.Add(new GovernorStepSave
                {
                    Kind = (int)s.Kind, Building = (int)s.Building, X = s.Tile.X, Y = s.Tile.Y,
                    Size = s.Size, Cost = s.Cost, Status = (int)s.Status, LabelUz = s.LabelUz
                });
            return save;
        }

        private static ApprovedConstructionPlan FromSave(GovernorQueueSave save)
        {
            if (save == null) return null;
            var plan = new ApprovedConstructionPlan
            {
                PlanName = save.PlanName,
                State = (PlanExecState)save.State,
                ApprovedBudget = save.ApprovedBudget,
                SpentSoFar = save.SpentSoFar,
                NeedsReapproval = save.NeedsReapproval,
                PausedForResources = save.PausedForResources,
                HousingCapacityAtApproval = save.HousingCapacityAtApproval,
                PopulationAtApproval = save.PopulationAtApproval
            };
            foreach (var s in save.Steps ?? new List<GovernorStepSave>())
                plan.Steps.Add(new PlanStep
                {
                    Kind = (StepKind)s.Kind, Building = (BuildingType)s.Building,
                    Tile = new Point(s.X, s.Y), Size = s.Size, Cost = s.Cost,
                    Status = (StepStatus)s.Status, LabelUz = s.LabelUz
                });
            return plan;
        }

        /// <summary>Test va boshqa saqlash yo'llari uchun blueprint DTO'ga aylantirish.</summary>
        public static GovernorBlueprintSave ExportBlueprint(CityPlan plan, int selectedItemId)
            => ToSave(plan, selectedItemId);

        /// <summary>Test va serverdan tiklangan blueprintni qaytarish.</summary>
        public static CityPlan ImportBlueprint(GovernorBlueprintSave save) => FromSave(save);

        private static GovernorBlueprintSave ToSave(CityPlan plan, int selectedItemId)
        {
            if (plan == null) return null;
            var save = new GovernorBlueprintSave
            {
                Id = plan.Id, CreatedDay = plan.CreatedDay, Type = (int)plan.Type,
                NameUz = plan.NameUz, RationaleUz = plan.RationaleUz,
                AnchorX = plan.Anchor.X, AnchorY = plan.Anchor.Y,
                BuildingsCost = plan.BuildingsCost, RoadsCost = plan.RoadsCost,
                TotalBuildDays = plan.TotalBuildDays, State = (int)plan.State,
                IsValid = plan.IsValid, SelectedItemId = selectedItemId
            };
            foreach (var b in plan.Buildings)
                save.Buildings.Add(new GovernorBlueprintItemSave
                {
                    Id = b.Id, Kind = (int)b.Kind, X = b.Tile.X, Y = b.Tile.Y, Size = b.Size,
                    Cost = b.Cost, Status = (int)b.Status, BuildTimeDays = b.BuildTimeDays,
                    PopulationCapacity = b.PopulationCapacity, BenefitUz = b.BenefitUz,
                    RequirementUz = b.RequirementUz, RequiredTech = b.RequiredTech,
                    StatusReasonUz = b.StatusReasonUz
                });
            foreach (var r in plan.Roads)
                save.Roads.Add(new GovernorBlueprintRoadSave
                {
                    X = r.Tile.X, Y = r.Tile.Y, Cost = r.Cost,
                    Status = (int)r.Status, StatusReasonUz = r.StatusReasonUz
                });
            return save;
        }

        private static CityPlan FromSave(GovernorBlueprintSave save)
        {
            if (save == null) return null;
            var plan = new CityPlan
            {
                Id = save.Id, CreatedDay = save.CreatedDay, Type = (DistrictType)save.Type,
                NameUz = save.NameUz, RationaleUz = save.RationaleUz,
                Anchor = new Point(save.AnchorX, save.AnchorY),
                BuildingsCost = save.BuildingsCost, RoadsCost = save.RoadsCost,
                TotalBuildDays = save.TotalBuildDays, State = (PlanExecState)save.State,
                IsValid = save.IsValid
            };
            foreach (var b in save.Buildings ?? new List<GovernorBlueprintItemSave>())
                plan.Buildings.Add(new BlueprintItem
                {
                    Id = b.Id, Kind = (BuildingType)b.Kind, Tile = new Point(b.X, b.Y), Size = b.Size,
                    Cost = b.Cost, Status = (BlueprintStatus)b.Status, BuildTimeDays = b.BuildTimeDays,
                    PopulationCapacity = b.PopulationCapacity, BenefitUz = b.BenefitUz ?? "",
                    RequirementUz = b.RequirementUz ?? "", RequiredTech = b.RequiredTech ?? "",
                    StatusReasonUz = b.StatusReasonUz ?? ""
                });
            foreach (var r in save.Roads ?? new List<GovernorBlueprintRoadSave>())
                plan.Roads.Add(new BlueprintRoad
                {
                    Tile = new Point(r.X, r.Y), Cost = r.Cost,
                    Status = (BlueprintStatus)r.Status, StatusReasonUz = r.StatusReasonUz ?? ""
                });
            return plan;
        }

        // ---- Oddiy o'zbekcha intent parser (PROMPT 15, 18-bo'lim — ixtiyoriy poydevor) ----
        public string HandleCommand(string text)
        {
            string t = (text ?? "").ToLowerInvariant().Trim();
            if (t.Length == 0) return "Buyruq bo'sh, hazratim.";

            if (ContainsAny(t, "yordam", "help", "nima qila olasan"))
                return "Men tahlil qilaman, mahalla rejasini tuzaman, xarajat va prognozni ko'rsataman. " +
                       "Qurilish faqat siz tasdiqlagandan keyin boshlanadi. Buyruqlar: tahlil, reja, " +
                       "turar joy, savdo, dehqonchilik, bog', madaniyat, yaxshilash, holat, pauza, davom, bekor, yer.";

            if (ContainsAny(t, "yetishma", "nima kerak", "tahlil", "muammo"))
                return TopAdviceUz();

            if (ContainsAny(t, "aholini ko'paytir", "aholi ko'pay", "aholim ko'pay"))
            {
                var growth = GeneratePlan(DistrictType.Residential);
                return growth != null
                    ? "Aholini ko'paytirish uchun turar joy rejasi tayyor. " + growth.RationaleUz
                    : "Turar joy rejasi tuzib bo'lmadi — ochiq yer yoki texnologiya yetmayapti.";
            }

            if (ContainsAny(t, "aholi"))
                return "Aholini ko'paytirish uchun uy va baxt (bog') kerak. Yangi turar joy mahallasini rejalashtirishim mumkin.";

            if (ContainsAny(t, "xazina", "pul", "tanga"))
            {
                if (Report == null) return "Hozir xazinada " + (_state?.Coins ?? 0) + " tanga bor.";
                return "Hozir xazinada " + _state.Coins + " tanga. Kunlik daromad +" + Report.DailyIncome +
                       ", xarajat -" + Report.DailyExpense + " (sof " + Report.DailyNet + "). " +
                       (Report.DailyNet < 0
                           ? "Xazina kamaymoqda: soliqlarni ko'rib chiqing yoki bozor/karvonsaroy quring."
                           : "Oqim ijobiy — zaxira o'sib bormoqda.");
            }

            if (ContainsAny(t, "mavjud", "yaxshila", "bo'shliq"))
            {
                var improve = GenerateImprovementPlan();
                return improve != null ? improve.RationaleUz : "Mavjud mahallada bo'shliq topilmadi.";
            }

            if (ContainsAny(t, "yer", "hudud"))
                return LandAdviceUz();

            if (ContainsAny(t, "bekor", "to'xtat"))
            {
                if (Blueprint != null) { CancelBlueprint(); return "Reja qaytarildi."; }
                if (Queue.HasActivePlan) { CancelQueue(); return Queue.LastMessageUz; }
                return "Bekor qiladigan reja yo'q.";
            }

            if (ContainsAny(t, "pauza", "to'xta"))
            {
                Queue.Pause();
                return Queue.LastMessageUz;
            }

            if (ContainsAny(t, "davom", "resume"))
            {
                Queue.Resume();
                return Queue.LastMessageUz;
            }

            if (ContainsAny(t, "holat", "navbat", "qurilish"))
            {
                if (Queue.Plan == null) return "Faol qurilish navbati yo'q.";
                return Queue.StateNameUz + ". " + Queue.ProgressUz + " " + Queue.LastMessageUz;
            }

            if (ContainsAny(t, "tasdiq", "boshlash"))
            {
                if (Blueprint == null) return "Tasdiqlash uchun reja yo'q.";
                return ApprovePlan() ? "Reja tasdiqlandi." : "Rejani tasdiqlab bo'lmadi.";
            }

            DistrictType? district = null;
            if (ContainsAny(t, "turar", "uy", "uy-joy")) district = DistrictType.Residential;
            else if (ContainsAny(t, "savdo", "bozor")) district = DistrictType.Commercial;
            else if (ContainsAny(t, "hunarmand", "ustaxona")) district = DistrictType.Craft;
            else if (ContainsAny(t, "madaniyat", "madrasa", "masjid")) district = DistrictType.Cultural;
            else if (ContainsAny(t, "dehqon", "oziq-ovqat", "hosil")) district = DistrictType.Agricultural;
            else if (ContainsAny(t, "bog'", "bog", "dam olish")) district = DistrictType.Garden;
            else if (ContainsAny(t, "mahalla", "reja", "yangi shahar")) district = DistrictType.Mixed;

            if (district.HasValue)
            {
                var p = GeneratePlan(district.Value);
                return p != null ? p.RationaleUz : "Reja tuzib bo'lmadi.";
            }

            return "Kechirasiz, hazratim, bu buyruqni hali tushunmadim. 'yordam' deb yozing.";
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            foreach (string n in needles)
                if (text.Contains(n, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
