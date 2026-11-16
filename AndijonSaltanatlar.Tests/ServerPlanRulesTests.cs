using System.Collections.Generic;
using AndijonSaltanatlar.Shared.Planning;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>
    /// Server-authoritative AI reja qoidalari — PROMPT 15, 19-bo'lim. Bu qoidalar ASP.NET Core
    /// endpointida ham, lokaldagi tayyorgarlikda ham bir xil ishlaydi.
    /// </summary>
    public class ServerPlanRulesTests
    {
        private static AiPlanValidationContext Context(int coin = 1000) => new()
        {
            Coin = coin,
            MapWidth = 64,
            MapHeight = 64
        };

        private static AiPlanValidationRequest Request(params AiPlanStepDto[] steps) => new()
        {
            KingdomId = 1,
            ApprovedBudget = 1000,
            Steps = new List<AiPlanStepDto>(steps)
        };

        [Fact]
        public void Server_Accepts_ValidPlan_AndComputesServerPrices()
        {
            var req = Request(
                new AiPlanStepDto { Kind = "Road", X = 10, Y = 10, Size = 1 },
                new AiPlanStepDto { Kind = "Building", Building = "House", X = 8, Y = 8, Size = 1 },
                new AiPlanStepDto { Kind = "Building", Building = "Garden", X = 12, Y = 8, Size = 1 });

            AiPlanValidationResult r = AiPlanRules.Validate(req, Context());

            Assert.True(r.Accepted);
            Assert.True(r.Affordable);
            Assert.Equal(10 + 50 + 90, r.ComputedCost); // client Size e'tiborga olinmaydi
            Assert.Equal(2, r.ValidBuildingSteps);
            Assert.Equal(1, r.ValidRoadSteps);
        }

        [Fact]
        public void Server_Rejects_Plan_WhenOwnershipFails()
        {
            var ctx = Context();
            ctx.KingdomOwnedByPlayer = false;

            AiPlanValidationResult r = AiPlanRules.Validate(
                Request(new AiPlanStepDto { Kind = "Building", Building = "House", X = 4, Y = 4 }), ctx);

            Assert.False(r.Accepted);
            Assert.Contains(r.ErrorsUz, e => e.Contains("Saltanat"));
        }

        [Fact]
        public void Server_Rejects_Plan_WhenTechnologyIsLocked()
        {
            var req = Request(new AiPlanStepDto { Kind = "Building", Building = "Mosque", X = 20, Y = 20 });

            AiPlanValidationResult r = AiPlanRules.Validate(req, Context());

            Assert.False(r.Accepted);
            Assert.Contains(r.ErrorsUz, e => e.Contains("texnologiyasi"));
        }

        [Fact]
        public void Server_Rejects_Overlap_WithExistingAndPlannedBuildings()
        {
            var ctx = Context();
            ctx.ExistingBuildings.Add(new AiExistingBuildingDto { Kind = "Market", X = 30, Y = 30, Size = 4 });

            var req = Request(
                new AiPlanStepDto { Kind = "Building", Building = "House", X = 31, Y = 31 },
                new AiPlanStepDto { Kind = "Building", Building = "House", X = 40, Y = 40 },
                new AiPlanStepDto { Kind = "Building", Building = "House", X = 40, Y = 40 });

            AiPlanValidationResult r = AiPlanRules.Validate(req, ctx);

            Assert.False(r.Accepted);
            Assert.Contains(r.ErrorsUz, e => e.Contains("ustma-ust"));
            Assert.Equal(1, r.ValidBuildingSteps);
        }

        [Fact]
        public void Server_Rejects_BudgetBelowComputedCost()
        {
            var req = Request(new AiPlanStepDto { Kind = "Building", Building = "Market", X = 5, Y = 5 });
            req.ApprovedBudget = 100;

            AiPlanValidationResult r = AiPlanRules.Validate(req, Context());

            Assert.False(r.Accepted);
            Assert.Contains(r.ErrorsUz, e => e.Contains("budjet"));
            Assert.Equal(200, r.ComputedCost);
        }

        [Fact]
        public void Server_Warns_WhenTreasuryIsLow_ButPlanShapeIsValid()
        {
            var req = Request(new AiPlanStepDto { Kind = "Building", Building = "House", X = 5, Y = 5 });
            req.ApprovedBudget = 50;

            AiPlanValidationResult r = AiPlanRules.Validate(req, Context(coin: 10));

            Assert.True(r.Accepted);
            Assert.False(r.Affordable);
            Assert.NotEmpty(r.WarningsUz);
        }

        [Fact]
        public void Server_Rejects_UnknownBuilding_AndDuplicateRoad()
        {
            var req = Request(
                new AiPlanStepDto { Kind = "Building", Building = "DragonTower", X = 5, Y = 5 },
                new AiPlanStepDto { Kind = "Road", X = 10, Y = 10 },
                new AiPlanStepDto { Kind = "Road", X = 10, Y = 10 });

            AiPlanValidationResult r = AiPlanRules.Validate(req, Context());

            Assert.False(r.Accepted);
            Assert.Contains(r.ErrorsUz, e => e.Contains("noma'lum"));
            Assert.Contains(r.ErrorsUz, e => e.Contains("takror"));
        }
    }
}
