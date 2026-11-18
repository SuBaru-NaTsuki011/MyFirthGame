using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Progression;
using Microsoft.Xna.Framework;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>Promt 2: har bino turi o'ziga xos kattalashtirish effekti, nomlari va narxini oladi.</summary>
    public class BuildingUpgradeTests
    {
        // Sinov uchun oddiy bino (kontentsiz).
        private sealed class TestBuilding : Building
        {
            public TestBuilding(BuildingType type, int cost, int maxHp, int cap) : base(new Point(0, 0))
            {
                Type = type;
                Cost = cost;
                MaxHitPoints = maxHp;
                HitPoints = maxHp;
                PopulationCapacity = cap;
            }
        }

        // --- 1. Daraja nomlari tur bo'yicha ---

        [Theory]
        [InlineData(BuildingType.Farm, 1, "Tomorqa")]
        [InlineData(BuildingType.Farm, 5, "Shoh xo'jaligi")]
        [InlineData(BuildingType.Market, 1, "Rasta")]
        [InlineData(BuildingType.Market, 5, "Saltanat bozori")]
        [InlineData(BuildingType.House, 1, "Kulba")]
        [InlineData(BuildingType.Barracks, 1, "Qarorgoh")]
        public void LevelName_TurBoyicha(BuildingType t, int level, string expected)
        {
            Assert.Equal(expected, BuildingUpgradeEffects.LevelName(t, level));
        }

        [Fact]
        public void LevelName_ChegaradanTashqari_Cheklanadi()
        {
            Assert.Equal("Tomorqa", BuildingUpgradeEffects.LevelName(BuildingType.Farm, 0));
            Assert.Equal("Shoh xo'jaligi", BuildingUpgradeEffects.LevelName(BuildingType.Farm, 99));
        }

        [Fact]
        public void HarTur_OzNomlariOladi_UmumiyEmas()
        {
            // Ferma va bozor 2-darajada boshqa-boshqa nomlanadi.
            Assert.NotEqual(BuildingUpgradeEffects.LevelName(BuildingType.Farm, 2),
                            BuildingUpgradeEffects.LevelName(BuildingType.Market, 2));
        }

        // --- 2. Narx koeffitsienti ---

        [Fact]
        public void Narx_DevorArzon_SaroyQimmat()
        {
            Assert.True(BuildingUpgradeEffects.CostFactor(BuildingType.Wall) < BuildingUpgradeEffects.CostFactor(BuildingType.Market));
            Assert.True(BuildingUpgradeEffects.CostFactor(BuildingType.Market) < BuildingUpgradeEffects.CostFactor(BuildingType.MainHall));
        }

        [Fact]
        public void UpgradeCost_DevorBozordanArzon()
        {
            var wall = new TestBuilding(BuildingType.Wall, 200, 100, 0);
            var market = new TestBuilding(BuildingType.Market, 200, 100, 0);
            Assert.True(BuildingUpgradeSystem.UpgradeCost(wall) < BuildingUpgradeSystem.UpgradeCost(market));
        }

        // --- 3. Iqtisodiy hissa daraja bilan oshadi ---

        [Theory]
        [InlineData(BuildingType.Market, 1, 1.0)]
        [InlineData(BuildingType.Market, 5, 3.0)]   // 1 + 4*0.5
        [InlineData(BuildingType.Farm, 3, 2.0)]     // 1 + 2*0.5
        [InlineData(BuildingType.Garden, 5, 2.6)]   // 1 + 4*0.4
        [InlineData(BuildingType.Tower, 5, 1.0)]    // iqtisodiy bino emas
        public void EconomyFactor_DarajaBilanOshadi(BuildingType t, int level, double expected)
        {
            Assert.Equal((float)expected, BuildingUpgradeEffects.EconomyFactor(t, level), 3);
        }

        // --- 4. Mustahkamlik/sig'im o'sishi tur bo'yicha ---

        [Fact]
        public void HpOsishi_DevorUydan_Kop()
        {
            Assert.True(BuildingUpgradeEffects.HpGrowth(BuildingType.Wall) > BuildingUpgradeEffects.HpGrowth(BuildingType.House));
        }

        [Fact]
        public void Upgrade_DevorKopHP_UyKopSigim()
        {
            var wall = new TestBuilding(BuildingType.Wall, 200, 100, 0);
            wall.Upgrade();
            Assert.Equal(2, wall.Level);
            Assert.Equal(150, wall.MaxHitPoints);   // +50%
            Assert.Equal(wall.MaxHitPoints, wall.HitPoints);

            var house = new TestBuilding(BuildingType.House, 100, 100, 5);
            house.Upgrade();
            Assert.Equal(125, house.MaxHitPoints);  // +25%
            Assert.Equal(8, house.PopulationCapacity); // +3

            var farm = new TestBuilding(BuildingType.Farm, 100, 100, 0);
            farm.Upgrade();
            Assert.Equal(0, farm.PopulationCapacity); // sig'imi yo'q — qo'shilmaydi
        }

        // --- Panel foydasi har tur uchun bor ---

        [Fact]
        public void Benefit_HarTurUchunBor()
        {
            foreach (BuildingType t in System.Enum.GetValues(typeof(BuildingType)))
                Assert.False(string.IsNullOrWhiteSpace(BuildingUpgradeEffects.Benefit(t)));
        }
    }
}
