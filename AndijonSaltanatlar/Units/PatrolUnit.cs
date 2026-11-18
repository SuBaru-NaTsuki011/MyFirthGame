using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Units
{
    /// <summary>
    /// Patrul askari: PatrolPoints bo'yicha aylanib yuradi, har nuqtada biroz to'xtaydi.
    /// Ikki nuqta berilsa — oldinga-orqaga, ko'proq berilsa — aylana bo'ylab yuradi.
    /// </summary>
    public class PatrolUnit : Unit
    {
        private int _targetIndex;
        private float _waitTimer;

        /// <summary>Har bir nuqtada to'xtab turish vaqti (soniya).</summary>
        public float PauseTime { get; set; } = 1.5f;
        public Vector2 NextPatrolPoint => PatrolPoints.Count == 0 ? Position : PatrolPoints[_targetIndex % PatrolPoints.Count];

        public PatrolUnit(IEnumerable<Vector2> patrolPoints, float speed = 1.1f) : base(Vector2.Zero)
        {
            Name = "Patrul askari";
            Description = "Sallali navkar. Ko‘chalarni aylanib, tartibni saqlaydi.";
            SpriteKey = SpriteKeys.UnitPatrol;
            Speed = speed;
            Attack = 6;
            SetHitPoints(90);
            PatrolPoints.AddRange(patrolPoints);
            if (PatrolPoints.Count > 0) Position = PatrolPoints[0];
            _targetIndex = PatrolPoints.Count > 1 ? 1 : 0;
            State = PatrolPoints.Count > 1 ? UnitState.Walking : UnitState.Idle;
        }

        protected override void UpdateBehavior(float dt)
        {
            if (PatrolPoints.Count < 2)
            {
                State = UnitState.Idle;
                return;
            }

            if (State == UnitState.Waiting)
            {
                _waitTimer -= dt;
                if (_waitTimer <= 0f)
                {
                    _targetIndex = (_targetIndex + 1) % PatrolPoints.Count;
                    State = UnitState.Walking;
                }
                return;
            }

            State = UnitState.Walking;
            Vector2 target = PatrolPoints[_targetIndex];
            Vector2 delta = target - Position;
            float dist = delta.Length();
            float step = Speed * SpeedScale * dt;
            if (dist <= step)
            {
                Position = target;
                State = UnitState.Waiting;
                _waitTimer = PauseTime;
            }
            else
            {
                // Spring-based yo'nalish yangilanishi (Unit.UpdateFacing orqali bajariladi).
                // Yurish vektori animation facing'idan oldin Position ga qo'shiladi.
                FaceTowards(delta, dt);
                Position += delta / dist * step;
            }
        }
    }

    /// <summary>Qilichli soqchi — maydon va bozorni aylanib yuradi.</summary>
    public class SwordsmanUnit : PatrolUnit
    {
        public SwordsmanUnit(IEnumerable<Vector2> patrolPoints, float speed = 1.0f) : base(patrolPoints, speed)
        {
            Name = "Qilichli soqchi";
            Description = "Egri shamshir taqqan tajribali soqchi. Maydon va bozor tinchligini qo‘riqlaydi.";
            SpriteKey = SpriteKeys.UnitSwordsman;
            Attack = 14;
            SetHitPoints(120);
        }
    }

    /// <summary>Otliq soqchi — bosh ko'chalarda ot minib yuradi (Bobur bobo 3D modelidan).</summary>
    public class CavalryUnit : PatrolUnit
    {
        public CavalryUnit(IEnumerable<Vector2> patrolPoints, float speed = 1.6f) : base(patrolPoints, speed)
        {
            Name = "Otliq soqchi";
            Description = "Nayzali otliq navkar. Bosh ko'chalarni ot minib qo'riqlaydi.";
            SpriteKey = SpriteKeys.UnitCavalry;
            WalkCycleTiles = 1.7f;
            Attack = 18;
            SetHitPoints(220);
        }
    }
}
