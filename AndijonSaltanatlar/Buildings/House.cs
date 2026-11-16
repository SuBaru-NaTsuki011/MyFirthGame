using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    /// <summary>Uy — hovlili loy-g'isht turar joy. 4 xil ko'rinishga ega.</summary>
    public sealed class House : Building
    {
        public const int BuildCost = 50;

        public House(Point position, int variant = 0) : base(position)
        {
            Type = BuildingType.House;
            Name = "Uy";
            Description = "Hovlili paxsa uy. Shaharga 5 nafar aholi qo‘shadi.";
            Size = new Point(2, 2);
            Category = BuildingCategory.Residential;
            Cost = BuildCost;
            PopulationCapacity = 5;
            SetHitPoints(200);
            int v = ((variant % SpriteKeys.HouseVariants) + SpriteKeys.HouseVariants) % SpriteKeys.HouseVariants;
            SpriteKey = SpriteKeys.House + "_" + v;
        }
    }
}
