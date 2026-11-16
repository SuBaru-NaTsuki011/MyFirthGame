using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    /// <summary>Minora — qo'riqlash minorasi. Keyingi bosqichda o'q otish radiusi shu yerga qo'shiladi.</summary>
    public sealed class Tower : Building
    {
        public const int BuildCost = 120;

        /// <summary>Kuzatuv radiusi (tile) — hozircha faqat ma'lumot uchun.</summary>
        public int WatchRadius { get; } = 8;

        public Tower(Point position) : base(position)
        {
            Type = BuildingType.Tower;
            Name = "Minora";
            Description = "Qo‘riqlash minorasi. Tepasida qorovullar shaharni kuzatadi.";
            Size = new Point(2, 2);
            Category = BuildingCategory.Defense;
            Cost = BuildCost;
            SetHitPoints(800);
            SpriteKey = SpriteKeys.Tower;
        }
    }
}
