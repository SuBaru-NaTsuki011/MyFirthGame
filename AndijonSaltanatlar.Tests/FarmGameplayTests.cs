using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Economy;
using AndijonSaltanatlar.Population;
using Microsoft.Xna.Framework;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    public sealed class FarmGameplayTests
    {
        [Fact]
        public void FarmBuildingCreatesDehqonJobsAndIncreasesFoodProduction()
        {
            Farm farm = Assert.IsType<Farm>(BuildingFactory.Create(BuildingType.Farm, new Point(4, 5)));
            int slots = PopulationManager.JobSlots(farm.Type, out CitizenProfession profession);

            Assert.Equal(6, slots);
            Assert.Equal(CitizenProfession.Dehqon, profession);
            Assert.True(EconomyManager.ComputeFoodProduction(0, 0, 1, 0f) >
                        EconomyManager.ComputeFoodProduction(0, 0, 0, 0f));
        }

        [Fact]
        public void FarmCropGrowsHarvestsAndStoresProduceForSale()
        {
            var farm = new Farm(Point.Zero);
            Assert.True(farm.PlantCrop());
            Assert.False(farm.PlantCrop());
            Assert.Equal(0, farm.WorkAutonomously(false));
            Assert.Equal(FarmCropStage.Growing, farm.CropStage);

            for (int day = 0; day < Farm.CropGrowthDays; day++)
                farm.WorkAutonomously(true);

            Assert.Equal(FarmCropStage.Mature, farm.CropStage);
            Assert.Equal(Farm.CropYield, farm.HarvestCrop());
            Assert.Equal(Farm.CropYield, farm.SellStoredProduce());
            Assert.Equal(0, farm.StoredProduce);
            Assert.Equal(FarmCropStage.Empty, farm.CropStage);
        }

        [Fact]
        public void FarmAutonomouslyCompletesCropCycleWhenADehqonIsAvailable()
        {
            var farm = new Farm(Point.Zero);
            Assert.Equal(0, farm.WorkAutonomously(false));
            Assert.Equal(FarmCropStage.Empty, farm.CropStage);

            farm.WorkAutonomously(true);
            for (int day = 0; day < Farm.CropGrowthDays; day++)
                farm.WorkAutonomously(true);

            Assert.Equal(FarmCropStage.Mature, farm.CropStage);
            Assert.Equal(Farm.CropYield, farm.WorkAutonomously(true));
            Assert.Equal(Farm.CropYield, farm.StoredProduce);
            Assert.Equal(FarmCropStage.Empty, farm.CropStage);
        }

        [Fact]
        public void FarmCropStateRestoresWithSafeProgressBounds()
        {
            var farm = new Farm(Point.Zero);
            farm.RestoreCropState((int)FarmCropStage.Growing, 99, -4);

            Assert.Equal(Farm.CropGrowthDays - 1, farm.GrowthDays);
            Assert.Equal(0, farm.StoredProduce);
            Assert.Equal(FarmCropStage.Growing, farm.CropStage);
        }
    }
}
