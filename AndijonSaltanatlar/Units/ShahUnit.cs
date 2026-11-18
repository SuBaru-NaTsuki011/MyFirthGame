using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Units
{
    /// <summary>
    /// Shoh — odatda saroy ichida taxtda o'tiradi (HouseInterior). O'yinchi saroyda shoh yonida F bossa,
    /// shoh shaharga chiqadi: shu unit dunyoga qo'shiladi, o'yinchi uni boshqaradi, soqchilar hamrohlik qiladi.
    /// Boshqaruv qo'yib yuborilsa, shoh saroy eshigiga qaytadi va dunyodan olinadi (yana taxtga o'tiradi).
    /// Shahar qo'shini hisobiga kirmaydi.
    /// </summary>
    public sealed class ShahUnit : Unit
    {
        public ShahUnit(Vector2 position) : base(position)
        {
            Name = "Shoh " + ShahName;
            Description = "Saltanat hukmdori Bobur. Zarbof to'n kiygan, boshida oq salla, qo'lida kitob.";
            SpriteKey = SpriteKeys.UnitShah;
            Team = UnitTeam.Player;
            ExternallyControlled = true;
            Attack = 12;
            Speed = 1.2f;
            GaitScale = 0.8f; // shoh — viqor bilan, shoshmay yuradi
            SetHitPoints(220);
        }

        public const string ShahName = "Zahiriddin";

        protected override void UpdateBehavior(float dt)
        {
            State = UnitState.Idle;
        }
    }
}
