using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Units
{
    public sealed class GladiatorUnit : PatrolUnit
    {
        public int ArenaBuildingId { get; set; }

        public GladiatorUnit(IEnumerable<Vector2> route, string name, int arenaBuildingId) : base(route, 0.45f)
        {
            Name = name;
            Description = "Jang maydoni gladiatori.";
            SpriteKey = SpriteKeys.UnitSwordsman;
            Attack = 14;
            SetHitPoints(120);
            ArenaBuildingId = arenaBuildingId;
        }
    }
}
