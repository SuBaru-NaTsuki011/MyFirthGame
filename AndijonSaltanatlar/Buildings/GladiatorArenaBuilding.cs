using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    public sealed class GladiatorArenaBuilding : Building
    {
        public const int BuildCost = 650;

        public GladiatorArenaBuilding(Point position) : base(position)
        {
            Type = BuildingType.GladiatorArena;
            Name = "Jang maydoni";
            Description = "Gladiatorlar mashq qiladigan jang maydoni.";
            Size = new Point(5, 5);
            Category = BuildingCategory.Military;
            Cost = BuildCost;
            SetHitPoints(650);
            SpriteKey = SpriteKeys.GladiatorArena;
        }
    }
}
