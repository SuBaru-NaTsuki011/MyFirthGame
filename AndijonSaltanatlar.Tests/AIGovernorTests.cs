using System;
using System.Collections.Generic;
using System.Text.Json;
using AndijonSaltanatlar.AIGovernor;
using AndijonSaltanatlar.Blueprints;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityPlanning;
using AndijonSaltanatlar.Forecasting;
using AndijonSaltanatlar.SmartConstruction;
using Microsoft.Xna.Framework;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>
    /// AI Governor (Donishmand Vazir) testlari — PROMPT 15: joylashuv, yo'l, validatsiya,
    /// muharrir, smeta, prognoz, ruxsatlar va save DTO.
    /// </summary>
    public class AIGovernorTests
    {
        // ------------------------------------------------------------------
        // Test uchun xotira grid'i — MapManager/GraphicsDevice talab qilmaydi.
        // ------------------------------------------------------------------
        private sealed class FakeGrid : ICityPlanningGrid
        {
            public int Width { get; }
            public int Height { get; }
            public HashSet<Point> Water { get; } = new();
            public HashSet<Point> Occupied { get; } = new();
            public HashSet<Point> Locked { get; } = new();
            public HashSet<Point> Protected { get; } = new();
            public HashSet<Point> Roads { get; } = new();
            public Dictionary<Point, BuildingType> Buildings { get; } = new();

            public FakeGrid(int w = 24, int h = 24) { Width = w; Height = h; }

            public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
            public bool IsBuildableTerrain(int x, int y)
                => InBounds(x, y) && !Water.Contains(new Point(x, y)) && !Roads.Contains(new Point(x, y));
            public bool IsOpenLand(int x, int y) => InBounds(x, y) && !Locked.Contains(new Point(x, y));
            public bool IsBlockedForBuilding(int x, int y) => Occupied.Contains(new Point(x, y));
            public bool IsBlockedForRoad(int x, int y)
                => Occupied.Contains(new Point(x, y)) || Water.Contains(new Point(x, y));
            public bool IsRoad(int x, int y) => Roads.Contains(new Point(x, y));
            public bool IsProtected(int x, int y) => Protected.Contains(new Point(x, y));
            public BuildingType? BuildingAt(int x, int y)
                => Buildings.TryGetValue(new Point(x, y), out BuildingType t) ? t : (BuildingType?)null;

            public void AddRoad(int x, int y) => Roads.Add(new Point(x, y));
            public void Occupy(int x, int y, BuildingType type = BuildingType.House)
            {
                Occupied.Add(new Point(x, y));
                Buildings[new Point(x, y)] = type;
            }
        }

        private static BlueprintItem Item(int id, BuildingType kind, int x, int y, int size = 2, int cost = 50)
            => new BlueprintItem
            {
                Id = id, Kind = kind, Tile = new Point(x, y), Size = size, Cost = cost,
                PopulationCapacity = kind == BuildingType.House ? 5 : 0
            };

        private static CityPlan PlanWithRoad(BlueprintItem building)
        {
            var plan = new CityPlan { Id = 1, NameUz = "Test mahalla", Type = DistrictType.Residential };
            plan.Buildings.Add(building);
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(7, 5), Cost = 10 });
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(8, 5), Cost = 10 });
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(9, 5), Cost = 10 });
            return plan;
        }

        private static bool Validate(CityPlan plan, FakeGrid grid, int treasury = 1000, Func<BuildingType, bool> unlocked = null)
            => PlanValidator.Validate(plan, grid, treasury, unlocked ?? (_ => true), 10);

        // ------------------------------------------------------------------
        // Validator
        // ------------------------------------------------------------------
        [Fact]
        public void Validator_Accepts_ConnectedBuildablePlan()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5); // mavjud ko'cha
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));

            bool valid = Validate(plan, grid);

            Assert.True(valid);
            Assert.True(plan.IsValid);
            Assert.Equal(BlueprintStatus.Buildable, plan.Buildings[0].Status);
            Assert.Equal(50 + 30, plan.TotalCost);
        }

        [Fact]
        public void Validator_Blocks_OccupiedTile()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            grid.Occupy(6, 6, BuildingType.Market);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));

            bool valid = Validate(plan, grid);

            Assert.False(valid);
            Assert.Equal(BlueprintStatus.Blocked, plan.Buildings[0].Status);
            Assert.Contains("band", plan.Buildings[0].StatusReasonUz);
        }

        [Fact]
        public void Validator_Blocks_LockedLand()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            grid.Locked.Add(new Point(5, 5));
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));

            Assert.False(Validate(plan, grid));
            Assert.Equal(BlueprintStatus.Blocked, plan.Buildings[0].Status);
        }

        [Fact]
        public void Validator_Blocks_ProtectedZone()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            grid.Protected.Add(new Point(6, 6));
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));

            Assert.False(Validate(plan, grid));
            Assert.Equal(BlueprintStatus.Blocked, plan.Buildings[0].Status);
        }

        [Fact]
        public void Validator_Blocks_LockedTechnology()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.Mosque, 5, 5, 4, 700));

            bool valid = Validate(plan, grid, 5000, t => t != BuildingType.Mosque);

            Assert.False(valid);
            Assert.Equal(BlueprintStatus.Blocked, plan.Buildings[0].Status);
            Assert.Contains("Texnologiya", plan.Buildings[0].StatusReasonUz);
        }

        [Fact]
        public void Validator_Blocks_OverlappingBuildings()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));
            plan.Buildings.Add(Item(2, BuildingType.House, 6, 6));

            Validate(plan, grid);

            Assert.Equal(BlueprintStatus.Buildable, plan.Buildings[0].Status);
            Assert.Equal(BlueprintStatus.Blocked, plan.Buildings[1].Status);
        }

        [Fact]
        public void Validator_Blocks_DisconnectedRoads()
        {
            var grid = new FakeGrid(); // mavjud yo'l yo'q
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));

            Assert.False(Validate(plan, grid));
            Assert.Equal(BlueprintStatus.Blocked, plan.Roads[0].Status);
            Assert.Contains("ulanmagan", plan.Roads[0].StatusReasonUz);
        }

        [Fact]
        public void Validator_Marks_NeedsResource_ButKeepsPlanValid()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));

            bool valid = Validate(plan, grid, treasury: 20);

            Assert.True(valid);
            Assert.Equal(BlueprintStatus.NeedsResource, plan.Buildings[0].Status);
            Assert.Contains("yetmaydi", plan.Buildings[0].StatusReasonUz);
        }

        // ------------------------------------------------------------------
        // Smeta va prognoz
        // ------------------------------------------------------------------
        [Fact]
        public void CostEstimator_UsesRealCatalogPrices()
        {
            var plan = new CityPlan();
            plan.Buildings.Add(Item(1, BuildingType.House, 0, 0, 2, 1)); // noto'g'ri narx — estimator tuzatadi
            plan.Buildings.Add(Item(2, BuildingType.Market, 4, 0, 4, 1));
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(0, 8), Cost = 1 });

            PlanCostEstimate est = PlanCostEstimator.Estimate(plan, 1000, 10);

            Assert.Equal(50 + 200, est.BuildingsCost);
            Assert.Equal(10, est.RoadsCost);
            Assert.Equal(260, est.TotalCost);
            Assert.True(est.CanAffordNow);

            PlanCostEstimator.ApplyCosts(plan, 10);
            Assert.Equal(50, plan.Buildings[0].Cost);
            Assert.Equal(4, plan.Buildings[1].Size);
            Assert.Equal(10, plan.Roads[0].Cost);
        }

        [Fact]
        public void Forecast_ProducesThreeScenarios_WithHousingAndCost()
        {
            var report = new CityAnalysisReport
            {
                Population = 60, HousingCapacity = 50, Treasury = 1000,
                DailyIncome = 50, DailyExpense = 40
            };
            var plan = new CityPlan();
            plan.Buildings.Add(Item(1, BuildingType.House, 0, 0, 2, 50));
            plan.Buildings.Add(Item(2, BuildingType.House, 3, 0, 2, 50));
            plan.Buildings.Add(Item(3, BuildingType.House, 6, 0, 2, 50));
            plan.Buildings.Add(Item(4, BuildingType.Market, 9, 0, 4, 200));
            plan.Buildings.Add(Item(5, BuildingType.Garden, 14, 0, 3, 90));
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(0, 8), Cost = 10 });

            var scenarios = new EconomicForecastService().Forecast(report, plan);

            Assert.Equal(3, scenarios.Count);
            ForecastScenario expected = scenarios[0];
            Assert.Equal("Odatiy", expected.NameUz);
            Assert.Equal(15, expected.AddedHousing);
            Assert.Equal(450, expected.TotalCost);
            Assert.Equal(550, expected.TreasuryAfter);
            Assert.Equal(65, expected.HousingCapacityAfter);
            Assert.Equal(-5, expected.HousingShortageAfter);
            Assert.Equal(10, expected.DailyNet);
            Assert.True(expected.PaybackDays > 0);
        }

        // ------------------------------------------------------------------
        // Blueprint muharriri
        // ------------------------------------------------------------------
        [Fact]
        public void Editor_MovesBuilding_WhenNewSpotStillConnected()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));
            Assert.True(Validate(plan, grid));

            bool moved = BlueprintEditor.MoveBuilding(plan, 1, new Point(5, 4), grid,
                _ => true, 1000, 10);

            Assert.True(moved);
            Assert.Equal(new Point(5, 4), plan.Buildings[0].Tile);
            Assert.True(plan.IsValid);
        }

        [Fact]
        public void Editor_RejectsMove_ThatBreaksRoadConnection_AndReverts()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));
            Assert.True(Validate(plan, grid));

            bool moved = BlueprintEditor.MoveBuilding(plan, 1, new Point(14, 14), grid,
                _ => true, 1000, 10);

            Assert.False(moved);
            Assert.Equal(new Point(5, 5), plan.Buildings[0].Tile);
            Assert.True(plan.IsValid);
        }

        [Fact]
        public void Editor_RejectsMoveOutsideMap()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));

            Assert.False(BlueprintEditor.MoveBuilding(plan, 1, new Point(-4, -4), grid, _ => true, 1000, 10));
            Assert.Equal(new Point(5, 5), plan.Buildings[0].Tile);
        }

        [Fact]
        public void Editor_RemovesBuilding_AndInvalidatesEmptyPlan()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));

            bool removed = BlueprintEditor.RemoveBuilding(plan, 1, grid, _ => true, 1000, 10);

            Assert.True(removed);
            Assert.Empty(plan.Buildings);
            Assert.False(plan.IsValid);
        }

        [Fact]
        public void Editor_RejectsRoadRemoval_ThatDisconnectsNetwork()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));
            Assert.True(Validate(plan, grid));

            // (9,5) olib tashlansa qolgan (7,5)-(8,5) mavjud ko'chaga tegmaydi.
            bool changed = BlueprintEditor.ToggleRoad(plan, new Point(9, 5), grid, _ => true, 1000, 10);

            Assert.False(changed);
            Assert.Contains(plan.Roads, r => r.Tile == new Point(9, 5));
            Assert.True(plan.IsValid);
        }

        [Fact]
        public void Editor_AddsRoad_WhenItKeepsNetworkConnected()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));
            Assert.True(Validate(plan, grid));

            bool changed = BlueprintEditor.ToggleRoad(plan, new Point(7, 4), grid, _ => true, 1000, 10);

            Assert.True(changed);
            Assert.Contains(plan.Roads, r => r.Tile == new Point(7, 4));
            Assert.True(plan.IsValid);
        }

        // ------------------------------------------------------------------
        // Yo'l rejalashtiruvchi
        // ------------------------------------------------------------------
        [Fact]
        public void RoadPlanner_ConnectsDistrictAndBuildings_ToExistingRoad()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            var house = Item(1, BuildingType.House, 4, 4);
            var reserved = new HashSet<Point>
            {
                new Point(4, 4), new Point(5, 4), new Point(4, 5), new Point(5, 5)
            };

            RoadPlanResult result = SmartRoadPlanner.PlanRoads(grid, new Point(8, 8),
                new[] { house }, reserved, 10);

            Assert.True(result.ConnectedToExistingNetwork);
            Assert.Equal(0, result.UnconnectedBuildings);
            Assert.NotEmpty(result.Tiles);
            Assert.True(result.Cost > 0);
            Assert.Contains(result.Tiles, p => Math.Abs(p.X - 6) + Math.Abs(p.Y - 5) <= 3);
        }

        [Fact]
        public void RoadPlanner_ConnectsBuilding_WhenAnchorAlreadyOnRoad()
        {
            var grid = new FakeGrid();
            grid.AddRoad(8, 8); // anchor allaqachon mavjud ko'chada
            var house = Item(1, BuildingType.House, 4, 4);
            var reserved = new HashSet<Point>
            {
                new Point(4, 4), new Point(5, 4), new Point(4, 5), new Point(5, 5)
            };

            RoadPlanResult result = SmartRoadPlanner.PlanRoads(grid, new Point(8, 8),
                new[] { house }, reserved, 10);

            Assert.True(result.ConnectedToExistingNetwork);
            Assert.Equal(0, result.UnconnectedBuildings);
            Assert.NotEmpty(result.Tiles);
            Assert.Contains(result.Tiles, p => Math.Abs(p.X - 8) + Math.Abs(p.Y - 8) == 1);
        }

        // ------------------------------------------------------------------
        // Tavsiya va ruxsatlar
        // ------------------------------------------------------------------
        [Fact]
        public void Recommendations_AreSorted_AndSuggestResidentialDistrict()
        {
            var report = new CityAnalysisReport
            {
                Population = 120, HousingCapacity = 80, HousingShortage = 40,
                Treasury = 800, Happiness = 45, Food = 50, BuildableTiles = 10,
                OpenParcels = 2, LockedParcels = 1
            };
            report.BuildingCounts[BuildingType.Market] = 0;

            var recs = new RecommendationEngine().Recommend(report);

            Assert.NotEmpty(recs);
            for (int i = 1; i < recs.Count; i++)
                Assert.True(recs[i - 1].PriorityScore >= recs[i].PriorityScore);
            Assert.Contains(recs, r => r.SuggestedDistrict == DistrictType.Residential);
            Assert.Contains(recs, r => r.Action == RecommendationAction.UnlockLand);
        }

        [Fact]
        public void Permissions_NeverAllowAutonomousLandPurchase_AndCycleModes()
        {
            var p = new AdvisorPermissions();
            Assert.False(p.CanBuyLandWithoutApproval);
            Assert.True(p.RequiresApprovalForSpending);
            Assert.True(p.CanExecute);

            p.CycleLevel();
            Assert.Equal(AutonomyLevel.Manual, p.Level);
            Assert.False(p.CanPlan);
            Assert.False(p.CanExecute);

            p.CycleLevel();
            Assert.Equal(AutonomyLevel.Planning, p.Level);
            Assert.True(p.CanPlan);
            Assert.False(p.CanExecute);
        }

        // ------------------------------------------------------------------
        // Save DTO — Point emas, aniq X/Y
        // ------------------------------------------------------------------
        [Fact]
        public void GovernorSaveState_RoundTrips_WithExplicitXy()
        {
            var state = new AIGovernorSaveState
            {
                Level = (int)AutonomyLevel.Planning,
                ApprovedBudget = 570,
                Queue = new GovernorQueueSave
                {
                    PlanName = "Turar joy mahallasi",
                    State = 4,
                    ApprovedBudget = 570,
                    Steps =
                    {
                        new GovernorStepSave { Kind = 1, Building = 3, X = 12, Y = 18, Size = 2, Cost = 50 },
                        new GovernorStepSave { Kind = 0, X = 7, Y = 9, Size = 1, Cost = 10 }
                    }
                }
            };

            string json = JsonSerializer.Serialize(state);
            AIGovernorSaveState back = JsonSerializer.Deserialize<AIGovernorSaveState>(json);

            Assert.Contains("\"X\":12", json);
            Assert.NotNull(back?.Queue);
            Assert.Equal(2, back.Queue.Steps.Count);
            Assert.Equal(12, back.Queue.Steps[0].X);
            Assert.Equal(18, back.Queue.Steps[0].Y);
            Assert.Equal(7, back.Queue.Steps[1].X);
        }

        [Fact]
        public void GovernorSaveState_RoundTrips_BlueprintAndPlannedPositions()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));
            Assert.True(Validate(plan, grid));
            plan.Buildings[0].BenefitUz = "Aholi sig'imi";
            plan.Buildings[0].RequirementUz = "Yo'l ulanishi";

            GovernorBlueprintSave save = AIGovernorManager.ExportBlueprint(plan, 1);
            var state = new AIGovernorSaveState { Blueprint = save };
            string json = JsonSerializer.Serialize(state);
            AIGovernorSaveState back = JsonSerializer.Deserialize<AIGovernorSaveState>(json);
            CityPlan restored = AIGovernorManager.ImportBlueprint(back.Blueprint);

            Assert.NotNull(back.Blueprint);
            Assert.Equal(1, back.Blueprint.SelectedItemId);
            Assert.Equal(3, back.Blueprint.Roads.Count);
            Assert.Equal(new Point(5, 5), restored.Buildings[0].Tile);
            Assert.Equal(2, restored.Buildings[0].Size);
            Assert.Equal("Aholi sig'imi", restored.Buildings[0].BenefitUz);
            Assert.Equal(3, restored.Roads.Count);
            Assert.Equal(BlueprintStatus.Buildable, restored.Roads[0].Status);
            Assert.Equal(AndijonSaltanatlar.SmartConstruction.PlanExecState.ReadyForApproval, restored.State);
        }

        // ------------------------------------------------------------------
        // Yangi bino qo'shish va rejalashtirilgan yo'l bilan to'qnashuv
        // ------------------------------------------------------------------
        [Fact]
        public void Editor_AddsBuilding_WhenSpotIsFreeAndConnected()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));
            Assert.True(Validate(plan, grid));

            bool added = BlueprintEditor.AddBuilding(plan, BuildingType.Garden, new Point(10, 2),
                grid, _ => true, 1000, 10);

            Assert.True(added);
            Assert.Equal(2, plan.Buildings.Count);
            Assert.Equal(BuildingType.Garden, plan.Buildings[1].Kind);
            Assert.Equal(BlueprintStatus.Buildable, plan.Buildings[1].Status);
            Assert.True(plan.IsValid);
        }

        [Fact]
        public void Editor_RejectsAddBuilding_WhenOverlappingOrDisconnected()
        {
            var grid = new FakeGrid();
            grid.AddRoad(10, 5);
            CityPlan plan = PlanWithRoad(Item(1, BuildingType.House, 5, 5));
            Assert.True(Validate(plan, grid));

            Assert.False(BlueprintEditor.AddBuilding(plan, BuildingType.Garden, new Point(5, 5),
                grid, _ => true, 1000, 10));
            Assert.False(BlueprintEditor.AddBuilding(plan, BuildingType.Garden, new Point(16, 16),
                grid, _ => true, 1000, 10));
            Assert.Single(plan.Buildings);
            Assert.True(plan.IsValid);
        }

        [Fact]
        public void Validator_BlocksBuilding_ThatOverlapsPlannedRoad()
        {
            var grid = new FakeGrid();
            grid.AddRoad(12, 5);
            var plan = new CityPlan { Id = 2, NameUz = "Yo'l to'qnashuvi", Type = DistrictType.Mixed };
            plan.Buildings.Add(Item(1, BuildingType.House, 7, 5));
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(7, 5), Cost = 10 });
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(8, 5), Cost = 10 });
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(9, 5), Cost = 10 });
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(10, 5), Cost = 10 });
            plan.Roads.Add(new BlueprintRoad { Tile = new Point(11, 5), Cost = 10 });

            Assert.False(Validate(plan, grid));
            Assert.Equal(BlueprintStatus.Blocked, plan.Buildings[0].Status);
            Assert.Contains("yo'l", plan.Buildings[0].StatusReasonUz);
        }

        // ------------------------------------------------------------------
        // Rivojlanish natijasi (prognoz emas — haqiqiy o'lchov)
        // ------------------------------------------------------------------
        [Fact]
        public void DevelopmentImpact_ComputesRealDeltasAndStepCounts()
        {
            var plan = new ApprovedConstructionPlan
            {
                State = PlanExecState.Completed,
                SpentSoFar = 570,
                HousingCapacityAtApproval = 178,
                PopulationAtApproval = 120
            };
            plan.Steps.Add(new PlanStep { Kind = StepKind.Road, Status = StepStatus.Done });
            plan.Steps.Add(new PlanStep { Kind = StepKind.Road, Status = StepStatus.Done });
            plan.Steps.Add(new PlanStep { Kind = StepKind.Building, Building = BuildingType.House, Status = StepStatus.Done });
            plan.Steps.Add(new PlanStep { Kind = StepKind.Building, Building = BuildingType.Market, Status = StepStatus.Done });
            plan.Steps.Add(new PlanStep { Kind = StepKind.Building, Building = BuildingType.Garden, Status = StepStatus.Failed });

            DevelopmentImpact impact = DevelopmentImpactEvaluator.Evaluate(plan, 193, 135);

            Assert.Equal(178, impact.HousingBefore);
            Assert.Equal(193, impact.HousingAfter);
            Assert.Equal(15, impact.HousingDelta);
            Assert.Equal(120, impact.PopulationBefore);
            Assert.Equal(135, impact.PopulationAfter);
            Assert.Equal(15, impact.PopulationDelta);
            Assert.Equal(2, impact.BuiltBuildings);
            Assert.Equal(2, impact.BuiltRoads);
            Assert.Equal(1, impact.FailedSteps);
            Assert.Equal(570, impact.Spent);
            Assert.Contains("178->193", impact.SummaryUz);
            Assert.Contains("120->135", impact.SummaryUz);
        }

        [Fact]
        public void DevelopmentImpact_WithoutBaseline_ReportsCountsWithoutArrows()
        {
            var plan = new ApprovedConstructionPlan { State = PlanExecState.Completed, SpentSoFar = 80 };
            plan.Steps.Add(new PlanStep { Kind = StepKind.Building, Status = StepStatus.Done });
            plan.Steps.Add(new PlanStep { Kind = StepKind.Road, Status = StepStatus.Skipped });

            DevelopmentImpact impact = DevelopmentImpactEvaluator.Evaluate(plan, 0, 0);

            Assert.False(impact.HasBaseline);
            Assert.Equal(1, impact.BuiltBuildings);
            Assert.Equal(0, impact.BuiltRoads);
            Assert.DoesNotContain("->", impact.SummaryUz);
            Assert.Contains("1 bino", impact.SummaryUz);
        }

        [Fact]
        public void GovernorSaveState_RoundTrips_ImpactBaselineAndHistory()
        {
            var state = new AIGovernorSaveState
            {
                Queue = new GovernorQueueSave
                {
                    PlanName = "Turar joy mahallasi",
                    State = (int)PlanExecState.Completed,
                    ApprovedBudget = 570,
                    SpentSoFar = 570,
                    HousingCapacityAtApproval = 178,
                    PopulationAtApproval = 120
                },
                History =
                {
                    new GovernorHistorySave
                    {
                        PlanName = "Turar joy mahallasi",
                        State = (int)PlanExecState.Completed,
                        Day = 3,
                        DoneSteps = 18,
                        Spent = 570,
                        ApprovedBudget = 570,
                        HousingBefore = 178,
                        HousingAfter = 193,
                        PopulationBefore = 120,
                        PopulationAfter = 135,
                        BuiltBuildings = 5,
                        BuiltRoads = 13
                    }
                }
            };

            string json = JsonSerializer.Serialize(state);
            AIGovernorSaveState back = JsonSerializer.Deserialize<AIGovernorSaveState>(json);

            Assert.NotNull(back);
            Assert.Equal(178, back.Queue.HousingCapacityAtApproval);
            Assert.Equal(120, back.Queue.PopulationAtApproval);
            GovernorHistorySave h = Assert.Single(back.History);
            Assert.Equal(193, h.HousingAfter);
            Assert.Equal(135, h.PopulationAfter);
            Assert.Equal(5, h.BuiltBuildings);
            Assert.Equal(13, h.BuiltRoads);
        }

        // ------------------------------------------------------------------
        // Dehqonchilik va mavjud mahallani yaxshilash
        // ------------------------------------------------------------------
        [Fact]
        public void DistrictObjectives_IncludeAgricultural_AndImprovement()
        {
            PlanningObjective agri = PlanningObjective.ForDistrict(DistrictType.Agricultural, 0);
            PlanningObjective improve = PlanningObjective.ForImprovement(12);

            Assert.Equal("Dehqonchilik hududi", agri.LabelUz);
            Assert.True(agri.Gardens >= 3);
            Assert.Equal("Mavjud mahallani yaxshilash", improve.LabelUz);
            Assert.Equal(2, improve.Houses);
            Assert.True(improve.Gardens >= 1);
        }

        [Fact]
        public void InfillAnchors_PreferExistingNeighbourhood()
        {
            var grid = new FakeGrid();
            grid.AddRoad(7, 5);
            grid.Occupy(4, 4, BuildingType.House);

            var anchors = SmartBuildingPlacement.FindInfillAnchors(grid, 5);

            Assert.NotEmpty(anchors);
            Assert.Contains(anchors, p => Math.Abs(p.X - 5) + Math.Abs(p.Y - 5) <= 4);
        }
    }
}
