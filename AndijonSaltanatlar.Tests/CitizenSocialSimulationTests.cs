using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityLife;
using AndijonSaltanatlar.Interiors;
using AndijonSaltanatlar.Population;
using Microsoft.Xna.Framework;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    public sealed class CitizenSocialSimulationTests
    {
        [Theory]
        [InlineData(29, false)]
        [InlineData(30, false)]
        [InlineData(31, true)]
        [InlineData(35, true)]
        [InlineData(50, true)]
        public void TaxRiotStartsAboveThirtyPercent(int tax, bool shouldRiot)
        {
            Assert.Equal(shouldRiot, TaxRiotSystem.TaxTooHigh(tax));
        }

        [Fact]
        public void BuildingVisitsPreferTheFrontEntranceOverCloserPerimeterTiles()
        {
            var building = new House(new Point(10, 10));
            var goals = new List<Point>
            {
                new Point(11, 9),
                building.FrontEntranceTile,
                new Point(10, 11)
            };

            CitizenSocialSimulation.OrderEntranceGoals(building, new Point(11, 9), goals);

            Assert.Equal(building.FrontEntranceTile, goals[0]);
        }

        [Fact]
        public void CompletedBarracksAndTowerCanBeEnteredButWallsCannot()
        {
            Assert.True(HouseInterior.IsEnterable(new Barracks(new Point(4, 5))));
            Assert.True(HouseInterior.IsEnterable(new Tower(new Point(8, 5)))); // minora — kuzatish maydonchasi
            Assert.False(HouseInterior.IsEnterable(new Wall(new Point(12, 5)))); // devor faqat tekshiriladi
        }

        [Fact]
        public void GladiatorArenaRemainsEnterable()
        {
            Assert.True(HouseInterior.IsEnterable(new GladiatorArenaBuilding(new Point(10, 5))));
        }

        [Fact]
        public void EmptyHouseholdSavingsEscalateFromComplaintToRevolt()
        {
            var household = new Household { SharedMoney = 0f };

            Assert.Equal(HouseholdHardshipEvent.Complaint, CitizenSocialSimulation.AdvanceHardship(household));
            Assert.Equal(HouseholdHardshipEvent.None, CitizenSocialSimulation.AdvanceHardship(household));
            Assert.Equal(HouseholdHardshipEvent.RevoltStarted, CitizenSocialSimulation.AdvanceHardship(household));
            Assert.True(household.IsRevolting);
            Assert.Equal(HouseholdHardshipEvent.None, CitizenSocialSimulation.AdvanceHardship(household));
        }

        [Fact]
        public void SavingsRecoveryEndsHardshipAndRevolt()
        {
            var household = new Household { SharedMoney = 0f };
            CitizenSocialSimulation.AdvanceHardship(household);
            CitizenSocialSimulation.AdvanceHardship(household);
            CitizenSocialSimulation.AdvanceHardship(household);

            household.SharedMoney = 1f;

            Assert.Equal(HouseholdHardshipEvent.Recovered, CitizenSocialSimulation.AdvanceHardship(household));
            Assert.Equal(0, household.HardshipDays);
            Assert.False(household.IsRevolting);
        }
    }
}
