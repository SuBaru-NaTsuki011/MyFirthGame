using System;
using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    public enum FarmCropStage
    {
        Empty,
        Growing,
        Mature
    }

    public sealed class Farm : Building
    {
        public const int BuildCost = 180;
        public const int CropGrowthDays = 3;
        public const int CropYield = 12;

        public FarmCropStage CropStage { get; private set; }
        public int GrowthDays { get; private set; }
        public int StoredProduce { get; private set; }
        public bool IsPlayerControlled { get; set; }

        public Farm(Point position) : base(position)
        {
            Type = BuildingType.Farm;
            Name = "Ferma";
            Description = "Dehqonlar ekin ekib, oziq-ovqat yetishtiradigan xo'jalik.";
            Size = new Point(4, 4);
            Category = BuildingCategory.Nature;
            Cost = BuildCost;
            SetHitPoints(220);
            SpriteKey = SpriteKeys.Farm;
        }

        public bool PlantCrop()
        {
            if (CropStage != FarmCropStage.Empty) return false;
            CropStage = FarmCropStage.Growing;
            GrowthDays = 0;
            return true;
        }

        public int HarvestCrop()
        {
            if (CropStage != FarmCropStage.Mature) return 0;
            CropStage = FarmCropStage.Empty;
            GrowthDays = 0;
            StoredProduce += CropYield;
            return CropYield;
        }

        public int WorkAutonomously(bool hasFarmer)
        {
            if (!hasFarmer) return 0;
            if (IsPlayerControlled && CropStage != FarmCropStage.Growing) return 0;
            switch (CropStage)
            {
                case FarmCropStage.Empty:
                    PlantCrop();
                    return 0;
                case FarmCropStage.Growing:
                    GrowthDays++;
                    if (GrowthDays >= CropGrowthDays) CropStage = FarmCropStage.Mature;
                    return 0;
                case FarmCropStage.Mature:
                    return HarvestCrop();
                default:
                    return 0;
            }
        }

        public int SellStoredProduce()
        {
            int amount = StoredProduce;
            StoredProduce = 0;
            return amount;
        }

        public void RestoreCropState(int stage, int growthDays, int storedProduce)
        {
            CropStage = (FarmCropStage)Math.Clamp(stage, (int)FarmCropStage.Empty, (int)FarmCropStage.Mature);
            GrowthDays = CropStage == FarmCropStage.Growing
                ? Math.Clamp(growthDays, 0, CropGrowthDays - 1)
                : 0;
            StoredProduce = Math.Max(0, storedProduce);
        }
    }
}
