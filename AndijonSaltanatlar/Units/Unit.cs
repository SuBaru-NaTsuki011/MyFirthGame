using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Units
{
    public enum UnitState
    {
        Idle,     // bo'sh turibdi
        Walking,  // patrul nuqtasiga yuryapti
        Waiting,  // patrul nuqtasida to'xtab turibdi
        Guarding  // postda qo'riqlayapti
    }

    /// <summary>
    /// Tile o'qlari bo'yicha yo'nalish.
    /// Ekranda: North = o'ng-yuqori, East = o'ng-past, South = chap-past, West = chap-yuqori.
    /// </summary>
    public enum Direction { North, East, South, West }

    /// <summary>
    /// Barcha jangchilar uchun asos klass. Position — uzluksiz tile koordinatasi (tile markazi = +0.5).
    /// Prompt 16 (Transaction 11, 13, 15): frame-based animatsiya, to'g'ri yo'nalish,
    /// spring-based cheklov bilan yo'nalish yangilanishi va chekloviy "robotga o'xshab yurish"
    /// muammosini bartaraf etish.
    /// </summary>
    public abstract class Unit : IWorldObject
    {
        protected static readonly Random Rng = new Random(GameConfig.WorldSeed);

        // --- Animatsiya holati ---
        private readonly float[] _walkPhaseTicks = new float[4]; // 4 ta yo'nalish uchun phase offset (placeholder)
        private int _walkPhaseIndex;
        private float _animTimer;
        private float _moveNoise;
        private bool _facingDirVested;

        // yurish animatsiyasi cheklovlari
        private const float WalkCyclePerTile = 0.55f;   // 1 ta tile uchun yurish sikli (sekund)
        private const float MaxFrameDeltaFactor = 4.5f; // cheklov: animatsiya tezligi
        private const float DirectionSpringK = 18f;     // yo'nalish spring kuchi
        private const float DirectionDamping = 5f;       // yo'nalish dumpingsi
        private float _facingAngle;                     // tekislikdagi yo'nalish burchagi
        private float _facingVelocity;                  // burchak tezligi

        public int Id { get; set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; } = "";
        public Vector2 Position { get; set; }
        public Direction Direction { get; protected set; } = Direction.South;
        /// <summary>Tezlik: soniyasiga necha tile.</summary>
        public float Speed { get; protected set; }
        /// <summary>Tezlik koeffitsienti (kun-tun ritmi: tunda fuqarolar sekinlashadi).</summary>
        public float SpeedScale { get; set; } = 1f;
        public UnitState State { get; protected set; } = UnitState.Idle;
        /// <summary>Hozir ko'rsatilayotgan kadr.</summary>
        public Texture2D Texture { get; private set; }
        /// <summary>Kadrlar: 0 — turish, 1..2 — yurish.</summary>
        public Texture2D[] Frames { get; private set; }
        public List<Vector2> PatrolPoints { get; } = new List<Vector2>();
        public string SpriteKey { get; protected set; }
        public int HitPoints { get; set; }
        public int MaxHitPoints { get; protected set; }
        public int Attack { get; protected set; }

        /// <summary> Placeholder yoki real per-entity animatsiya renderlovchi konteksti.
        /// Hozircha cheklovli cheklovlarni boshqarish uchun fase g'oya qo'llaniladi. </summary>
        public float PerEntityPhaseOffset { get; } = (float)(Rng.NextDouble() * MathF.PI * 2);
        public float MoveNoise { get => _moveNoise; set => _moveNoise = Math.Clamp(value, -1f, 1f); }

        // --- IWorldObject ---
        public float Depth => Position.X + Position.Y;
        public int SortLayer => 1;
        public bool Flipped => Direction == Direction.South || Direction == Direction.West;
        public Vector2 DrawPosition
        {
            get
            {
                Vector2 w = IsoMath.TileToWorld(Position);
                return new Vector2(MathF.Round(w.X - Texture.Width / 2f), MathF.Round(w.Y - Texture.Height + UnitArt.FootOffset));
            }
        }

        protected Unit(Vector2 position)
        {
            Position = position;
        }

        protected void SetHitPoints(int hp)
        {
            MaxHitPoints = hp;
            HitPoints = hp;
        }

        public void SetFrames(Texture2D[] frames)
        {
            Frames = frames;
            Texture = frames[0];
            _facingDirVested = false;
        }

        /// <summary>
        /// PROMPT 17: suhbat paytida unit joyida turadi (Idle kadri). Suhbat tugagach false qilinadi va
        /// unit kundalik yo'nalishini davom ettiradi.
        /// </summary>
        public bool Hold { get; set; }

        public void Update(float dt)
        {
            // 1) Xatti-harakatni yangilash (suhbatda — joyida turadi)
            if (Hold) State = UnitState.Idle;
            else UpdateBehavior(dt);

            // 2) Yo'nalishni spring-based usulda yangilash (keskin burilishning oldini oladi)
            UpdateFacing(dt);

            // 3) Animatsiya kadrini yangilash (turish/yurish)
            UpdateAnimation(dt);
        }

        /// <summary>Har bir unit turi o'z xatti-harakatini shu yerda belgilaydi.</summary>
        protected abstract void UpdateBehavior(float dt);

        /// <summary>Yo'nalishni spring-based usulda yangilash — keskin burilishning oldini oladi.</summary>
        private void UpdateFacing(float dt)
        {
            // Hozirgi yo'nalishni burchakka o'girish
            float targetAngle = DirectionToAngle(Direction);
            float diff = WrapAngle(targetAngle - _facingAngle);
            _facingVelocity += diff * DirectionSpringK * dt;
            _facingVelocity *= (1f - DirectionDamping * dt);
            _facingAngle += _facingVelocity * dt;

            // Cheklov: chekloviy tebranishlarni oldini olish
            _facingAngle = WrapAngle(_facingAngle);
            _facingVelocity = Math.Clamp(_facingVelocity, -3f, 3f);
        }

        /// <summary>Burchakni [-PI..PI] chegarasiga olish.</summary>
        private static float WrapAngle(float a)
        {
            while (a > MathF.PI) a -= 2f * MathF.PI;
            while (a < -MathF.PI) a += 2f * MathF.PI;
            return a;
        }

        /// <summary>Yo'nalishni burchakka o'girish.</summary>
        private static float DirectionToAngle(Direction d)
        {
            return d switch
            {
                Direction.North => -MathF.PI / 2,   // o'ng-yuqori
                Direction.East => 0,                // o'ng-past
                Direction.South => MathF.PI / 2,     // chap-past
                Direction.West => MathF.PI,          // chap-yuqori
                _ => 0
            };
        }

        /// <summary>Sensor cheklovlariga ko'ra: har bir kasb xususiyatiga mos animatsiyani tanlash.</summary>
        protected void UpdateAnimation(float dt)
        {
            // If frames is empty, fallback to placeholder logic
            if (Frames == null || Frames.Length == 0)
            {
                Texture = null;
                return;
            }

            Texture = Frames[0]; // default

            // Placeholder animatsiya — faqat yurishda animatsiya yuritish
            if (State == UnitState.Walking)
            {
                // Harakat tezligiga mos keladigan animatsiya (spring-based eprimas)
                float rawSpeed = Speed * SpeedScale;
                float walkCycle = WalkCyclePerTile * rawSpeed; // sekundlik sikl
                float cycleFreq = Math.Max(0.1f, walkCycle);
                float phaseDelta = dt / cycleFreq;             // fase o'zgarishi

                // Per-entity phase offset + transform noise
                _walkPhaseIndex = (_walkPhaseIndex + 1) % 4;
                float phaseNoise = _moveNoise * 0.05f;        // so'nggi cheklovlar
                _animTimer += phaseDelta * (1f + phaseNoise);

                // Kadrni tanlash: cheklovlarda frames[1..2] — yurish frames, frames[0] — idle
                int walkFrames = Frames.Length - 1;
                float t = _animTimer * MaxFrameDeltaFactor;
                int idx = Frames.Length > 1 ? 1 + (int)(t % walkFrames) : 0;
                Texture = Frames[idx];
            }
            else
            {
                _animTimer = 0f;
                Texture = Frames[0];

                // Idle animatsiyasi fasl bilan bog'liq: placeholder cheklovlariga ko'ra
                if (State == UnitState.Idle && Frames.Length > 1 && dt > 0.016f)
                {
                    // Hozircha faqat 1-frame idle, lekin placeholder animatsiya vorislik qilishi mumkin
                    Texture = Frames[0];
                }
            }
        }

        /// <summary>Yo'nalishni tabiiy ravishda yangilash (FaceTowards — spring-based).</summary>
        public void FaceTowards(Vector2 delta, float dt)
        {
            if (delta.LengthSquared() < 0.0001f)
            {
                _facingDirVested = false;
                return;
            }

            // Hozirgi yo'nalishni spring-based usulda yangilash (keskin burilishning oldini oladi)
            float targetAngle = MathF.Atan2(delta.Y, delta.X);
            // Izometrik tuzilishga moslashtirish
            float angleDelta = WrapAngle(targetAngle - _facingAngle);
            _facingVelocity += angleDelta * DirectionSpringK * dt;
            _facingVelocity *= (1f - DirectionDamping * dt);

            _facingAngle += _facingVelocity * dt;
            float wrapped = WrapAngle(_facingAngle);
            _facingVelocity = Math.Clamp(_facingVelocity, -3f, 3f);

            // Yangi yo'nalishni aniqlash
            Direction = AngleToDirection(wrapped);
        }

        /// <summary>Burchakdan yo'nalishga o'girish.</summary>
        private static Direction AngleToDirection(float angle)
        {
            // Izometrik tuzilish: North = o'ng-yuqori, East = o'ng-past, South = chap-past, West = chap-yuqori.
            // Burchaklar: -PI/2 (N), 0 (E), PI/2 (S), PI (W)
            float a = WrapAngle(angle);
            if (a < -MathF.PI / 4) return Direction.North;
            if (a < MathF.PI / 4) return Direction.East;
            if (a < 3 * MathF.PI / 4) return Direction.South;
            return Direction.West;
        }

        public static string StateName(UnitState s)
        {
            switch (s)
            {
                case UnitState.Walking: return "Patrulda yuribdi";
                case UnitState.Waiting: return "Atrofni kuzatmoqda";
                case UnitState.Guarding: return "Postda qo‘riqlamoqda";
                default: return "Dam olmoqda";
            }
        }
    }
}
