using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    /// <summary>Bog' (chorbog') — mevali daraxtlar va hovuzcha. Oziq-ovqat beradi.</summary>
    public sealed class Garden : Building
    {
        public Garden(Point position) : base(position)
        {
            Type = BuildingType.Garden;
            Name = "Bog‘";
            Description = "Chorbog‘: o‘rik, anor va gulzorlar. Oziq-ovqat yetishtiradi.";
            Size = new Point(3, 3);
            Category = BuildingCategory.Nature;
            Cost = 90;
            SetHitPoints(150);
            SpriteKey = SpriteKeys.Garden;
        }
    }
}
