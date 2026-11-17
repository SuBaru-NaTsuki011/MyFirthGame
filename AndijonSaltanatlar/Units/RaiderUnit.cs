using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Units
{
    /// <summary>
    /// Bosqinchi — shaharga tashqaridan hujum qiladigan dushman jangchisi.
    /// O'z xatti-harakati yo'q: uni har doim Combat.RaidSystem boshqaradi (ExternallyControlled = true).
    /// PatrolUnit/GuardUnit'dan meros OLMAYDI — shuning uchun shahar qo'shini hisobiga qo'shilmaydi.
    /// </summary>
    public class RaiderUnit : Unit
    {
        public bool IsChief { get; }
        /// <summary>Yurish tezligi (tile/soniya).</summary>
        public float MoveSpeed { get; }

        public RaiderUnit(Vector2 position, bool chief) : base(position)
        {
            IsChief = chief;
            Team = UnitTeam.Enemy;
            ExternallyControlled = true;
            if (chief)
            {
                Name = "Bosqinchi sardori";
                Description = "Mo'ynali bo'rkli, og'ir oybolta ko'targan bosqinchilar boshlig'i. Uni yengsangiz, qolganlarning ruhi tushadi.";
                SpriteKey = SpriteKeys.UnitRaiderChief;
                Attack = 15;
                SetHitPoints(180);
                MoveSpeed = 1.35f;
                AttackRange = 1.05f;
            }
            else
            {
                Name = "Bosqinchi";
                Description = "Tashqaridan kelgan talonchi. Shahar xazinasiga ko'z tikkan.";
                SpriteKey = SpriteKeys.UnitRaider;
                Attack = 8;
                SetHitPoints(70);
                MoveSpeed = 1.55f;
            }
            Speed = MoveSpeed;
        }

        protected override void UpdateBehavior(float dt)
        {
            // Boshqaruv to'liq RaidSystem'da.
            State = UnitState.Idle;
        }
    }
}
