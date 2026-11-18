using AndijonSaltanatlar.Military;
using AndijonSaltanatlar.Units;
using Microsoft.Xna.Framework;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>Promt 4: harbiy unvonlar — XP chegaralari, bonuslar, kazarma chegarasi, Unit integratsiyasi.</summary>
    public class MilitaryRankTests
    {
        [Theory]
        [InlineData(0, 5, 0)]     // Oddiy askar
        [InlineData(99, 5, 0)]
        [InlineData(100, 5, 1)]   // Onboshi
        [InlineData(300, 5, 2)]   // Ellikboshi
        [InlineData(700, 5, 3)]   // Yuzboshi
        [InlineData(1500, 5, 4)]  // Mingboshi
        [InlineData(3000, 5, 5)]  // Sarkarda
        public void RankForXp_Chegaralar(int xp, int maxRank, int expected)
        {
            Assert.Equal(expected, MilitaryRank.RankForXp(xp, maxRank));
        }

        [Fact]
        public void KazarmaChegarasi_UnvonniCheklaydi()
        {
            // Juda ko'p XP, lekin kazarma faqat 2-darajagacha ruxsat beradi.
            Assert.Equal(2, MilitaryRank.RankForXp(99999, 2));
            Assert.Equal(0, MilitaryRank.RankForXp(99999, 0)); // kazarma yo'q — unvon yo'q
        }

        [Fact]
        public void Nomlar_Togri()
        {
            Assert.Equal("Oddiy askar", MilitaryRank.Name(0));
            Assert.Equal("Onboshi", MilitaryRank.Name(1));
            Assert.Equal("Sarkarda", MilitaryRank.Name(5));
        }

        [Fact]
        public void BonusDelta_Kumulyativ()
        {
            var (atk1, hp1, spd1) = MilitaryRank.BonusDelta(0, 1);
            Assert.Equal(1, atk1);
            Assert.Equal(8, hp1);
            Assert.True(spd1 > 0f);

            var (atk5, hp5, _) = MilitaryRank.BonusDelta(0, 5);
            Assert.Equal(7, atk5);
            Assert.Equal(70, hp5);
        }

        [Fact]
        public void Pips_Rang_UnvonBoyicha()
        {
            Assert.Equal(0, MilitaryRank.Pips(0));
            Assert.Equal(3, MilitaryRank.Pips(3));
            Assert.NotEqual(MilitaryRank.PipColor(1), MilitaryRank.PipColor(5)); // kumush ≠ oltin
        }

        // --- Unit integratsiyasi ---

        [Fact]
        public void Askar_XPYigib_UnvonOshadi_BonusOladi()
        {
            var s = new PatrolUnit(new[] { Vector2.Zero }, 1f);
            Assert.True(s.CanRank);
            int atk0 = s.Attack, hp0 = s.MaxHitPoints;

            s.AddExperience(100, 5); // Onboshi
            Assert.Equal(1, s.Rank);
            Assert.Equal(atk0 + 1, s.Attack);
            Assert.Equal(hp0 + 8, s.MaxHitPoints);
            Assert.True(s.RankUpPending);
        }

        [Fact]
        public void Askar_KazarmaChegarasida_ToxtaydiKeyinOshadi()
        {
            var s = new PatrolUnit(new[] { Vector2.Zero }, 1f);
            s.AddExperience(99999, 2); // kazarma 2-darajada
            Assert.Equal(2, s.Rank);
        }

        [Fact]
        public void Fuqaro_UnvonOlmaydi()
        {
            var c = new CitizenUnit(new[] { Vector2.Zero }, 0);
            Assert.False(c.CanRank);
            c.AddExperience(1000, 5);
            Assert.Equal(0, c.Rank);
            Assert.Equal(0, c.Experience);
        }

        [Fact]
        public void RestoreRank_SaqlashdanTiklaydi()
        {
            var s = new PatrolUnit(new[] { Vector2.Zero }, 1f);
            int atk0 = s.Attack;
            s.RestoreRank(800, 3); // Yuzboshi
            Assert.Equal(3, s.Rank);
            Assert.Equal(800, s.Experience);
            var (atk, _, _) = MilitaryRank.BonusDelta(0, 3);
            Assert.Equal(atk0 + atk, s.Attack);
        }
    }
}
