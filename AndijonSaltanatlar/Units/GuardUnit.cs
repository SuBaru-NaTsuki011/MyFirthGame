using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Units
{
    /// <summary>
    /// Postda turadigan soqchi: joyidan qimirlamaydi, vaqti-vaqti bilan atrofga qaraydi.
    /// Standart ko'rinishi — qalqonli jangchi.
    /// </summary>
    public class GuardUnit : Unit
    {
        private readonly Direction _homeDirection;
        private float _lookTimer;

        public GuardUnit(Vector2 position, Direction facing) : base(position)
        {
            Name = "Qalqonli jangchi";
            Description = "Og‘ir qalqon va gurzi bilan qurollangan. Saroy va darvozalarni qo‘riqlaydi.";
            SpriteKey = SpriteKeys.UnitShield;
            Speed = 0f;
            Attack = 8;
            SetHitPoints(140);
            Direction = _homeDirection = facing;
            State = UnitState.Guarding;
            _lookTimer = 2f + (float)Rng.NextDouble() * 4f;
        }

        protected override void UpdateBehavior(float dt)
        {
            State = UnitState.Guarding;
            _lookTimer -= dt;
            if (_lookTimer > 0f) return;

            // Bir muddat yon tomonga qarab, keyin yana asosiy yo'nalishga qaytadi.
            if (Direction == _homeDirection)
            {
                int turn = Rng.Next(2) == 0 ? 1 : 3;
                Direction = (Direction)(((int)_homeDirection + turn) % 4);
                _lookTimer = 1.2f + (float)Rng.NextDouble() * 1.5f;
            }
            else
            {
                Direction = _homeDirection;
                _lookTimer = 3f + (float)Rng.NextDouble() * 5f;
            }
        }
    }

    /// <summary>Nayzali soqchi — darvoza va minoralar yonida turadi.</summary>
    public class SpearmanUnit : GuardUnit
    {
        public SpearmanUnit(Vector2 position, Direction facing) : base(position, facing)
        {
            Name = "Nayzali soqchi";
            Description = "Uzun nayzali darvozabon. Shaharga kirganlarni sinchiklab kuzatadi.";
            SpriteKey = SpriteKeys.UnitSpearman;
            Attack = 12;
            AttackRange = 1.35f; // uzun nayza
            SetHitPoints(110);
        }
    }
}
