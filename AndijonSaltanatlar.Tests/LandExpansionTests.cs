using System.Linq;
using AndijonSaltanatlar;
using AndijonSaltanatlar.LandExpansion;
using Microsoft.Xna.Framework;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    public sealed class LandExpansionTests
    {
        [Fact]
        public void InitialCityRingIsOpenAndSideParcelsAreExpanded()
        {
            var land = new CityLandExpansionManager();
            land.Initialize();
            var firstRing = land.Parcels.Where(parcel => parcel.Ring == 1).ToList();

            Assert.Equal(4, firstRing.Count);
            foreach (LandParcel parcel in firstRing)
            {
                Assert.Equal(LandParcelState.Open, parcel.State);
                Assert.True(land.IsBuildable(parcel.GridBounds.X, parcel.GridBounds.Y));
            }

            LandParcel west = firstRing.Single(parcel => parcel.Side == LandSide.West);
            LandParcel east = firstRing.Single(parcel => parcel.Side == LandSide.East);
            Assert.True(west.GridBounds.Width * west.GridBounds.Height > 10 * 36);
            Assert.True(east.GridBounds.Width * east.GridBounds.Height > 9 * 36);
            Assert.Equal(new Rectangle(GameConfig.CityOffset, GameConfig.CityOffset + 14, 14, 36), west.GridBounds);
            Assert.Equal(new Rectangle(GameConfig.CityOffset + 50, GameConfig.CityOffset + 14, 14, 36), east.GridBounds);
            Assert.All(land.Parcels.Where(parcel => parcel.Ring > 1),
                parcel => Assert.Equal(LandParcelState.Locked, parcel.State));
        }
    }
}
