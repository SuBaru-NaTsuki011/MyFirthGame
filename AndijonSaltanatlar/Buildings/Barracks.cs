using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    /// <summary>Qo'shinxona — jangchilar yashaydigan va mashq qiladigan joy. Keyin shu yerda unit yollanadi.</summary>
    public sealed class Barracks : Building
    {
        public Barracks(Point position) : base(position)
        {
            Type = BuildingType.Barracks;
            Name = "Qo‘shinxona";
            Description = "Navkarlar turadigan va mashq qiladigan qo‘rg‘on.";
            Size = new Point(3, 3);
            Category = BuildingCategory.Military;
            Cost = 300;
            SetHitPoints(900);
            SpriteKey = SpriteKeys.Barracks;
        }
    }
}
