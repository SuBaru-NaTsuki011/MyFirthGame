using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    /// <summary>Bozor — gumbazli savdo toqi. Tanga daromadi beradi.</summary>
    public sealed class Market : Building
    {
        public const int BuildCost = 200;

        public Market(Point position) : base(position)
        {
            Type = BuildingType.Market;
            Name = "Bozor";
            Description = "Gumbazli savdo toqi. Har hosilda xazinaga tanga keltiradi.";
            Size = new Point(4, 4);
            Category = BuildingCategory.Economy;
            Cost = BuildCost;
            SetHitPoints(450);
            SpriteKey = SpriteKeys.Market;
        }
    }
}
