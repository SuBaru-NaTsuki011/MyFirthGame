using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    /// <summary>Devor bo'lagi — 1x1 kungurador qal'a devori. Yonma-yon qo'yilsa uzluksiz devor hosil bo'ladi.</summary>
    public sealed class Wall : Building
    {
        public const int BuildCost = 15;

        public Wall(Point position) : base(position)
        {
            Type = BuildingType.Wall;
            Name = "Devor";
            Description = "Pishiq g‘ishtdan qurilgan qal’a devori bo‘lagi.";
            Size = new Point(1, 1);
            Category = BuildingCategory.Defense;
            Cost = BuildCost;
            SetHitPoints(500);
            SpriteKey = SpriteKeys.Wall;
        }
    }

    /// <summary>
    /// Darvoza ravog'i — devor ichidagi o'tish joyi (1x1). Unitlar ostidan o'ta oladi.
    /// alongY = true bo'lsa, devor Y o'qi bo'ylab (sharq/g'arb devori) — sprite aylantiriladi.
    /// </summary>
    public sealed class Gate : Building
    {
        public Gate(Point position, bool alongY = false) : base(position)
        {
            Type = BuildingType.Gate;
            Name = "Darvoza";
            Description = "Shahar darvozasi. Karvonlar va soqchilar shu yerdan o‘tadi.";
            Size = new Point(1, 1);
            Category = BuildingCategory.Defense;
            IsWalkable = true;
            Cost = 80;
            SetHitPoints(600);
            SpriteKey = SpriteKeys.Gate;
            Flipped = alongY;
        }
    }
}
