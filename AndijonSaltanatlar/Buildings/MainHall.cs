using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    /// <summary>Saroy (O'rda) — shaharning markaziy boshqaruv binosi. Faqat bitta bo'ladi.</summary>
    public sealed class MainHall : Building
    {
        public MainHall(Point position) : base(position)
        {
            Type = BuildingType.MainHall;
            Name = "Saroy";
            Description = "Hokim o‘rdasi — saltanatning yuragi. Feruza gumbazi butun shahardan ko‘rinadi.";
            Size = new Point(6, 6);
            Category = BuildingCategory.Government;
            Cost = 1500;
            PopulationCapacity = 20;
            SetHitPoints(3000);
            SpriteKey = SpriteKeys.MainHall;
        }
    }
}
