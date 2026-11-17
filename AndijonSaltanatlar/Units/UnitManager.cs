using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Units
{
    /// <summary>Barcha jangchilarni saqlaydi, yangilaydi va sichqoncha ostidagisini topadi.</summary>
    public sealed class UnitManager
    {
        private readonly AssetManager _assets;
        private int _nextId = 1;

        public List<Unit> Units { get; } = new List<Unit>();

        public UnitManager(AssetManager assets)
        {
            _assets = assets;
        }

        public T Add<T>(T unit) where T : Unit
        {
            unit.Id = _nextId++;
            var frames = new Texture2D[SpriteKeys.UnitFrames];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = _assets.Get(unit.SpriteKey + "_" + i);
            unit.SetFrames(frames);
            var attack = new Texture2D[UnitArt.AttackKinds * UnitArt.AttackPhases];
            bool any = false;
            for (int i = 0; i < attack.Length; i++)
            {
                attack[i] = _assets.TryGet(AssetManager.AttackKey(unit.SpriteKey, i / UnitArt.AttackPhases, i % UnitArt.AttackPhases));
                any |= attack[i] != null;
            }
            unit.AttackFrames = any ? attack : null;
            Units.Add(unit);
            return unit;
        }

        /// <summary>Jangchini xos navkarga aylantiradi (ko'rinishi va zarba kadrlari bilan).</summary>
        public void MakeElite(Unit unit)
        {
            if (unit == null || unit.IsElite) return;
            var frames = new Texture2D[SpriteKeys.UnitFrames];
            for (int i = 0; i < frames.Length; i++) frames[i] = _assets.TryGet(SpriteKeys.UnitElite + "_" + i);
            var attack = new Texture2D[UnitArt.AttackKinds * UnitArt.AttackPhases];
            for (int i = 0; i < attack.Length; i++)
                attack[i] = _assets.TryGet(AssetManager.AttackKey(SpriteKeys.UnitElite, i / UnitArt.AttackPhases, i % UnitArt.AttackPhases));
            unit.MakeElite(frames, attack);
        }

        /// <summary>Unitni dunyodan olib tashlaydi (faqat vaqtinchalik unitlar — masalan bosqinchilar — uchun).</summary>
        public bool Remove(Unit unit) => Units.Remove(unit);

        public void Update(float dt)
        {
            foreach (var u in Units) u.Update(dt);
        }

        /// <summary>World nuqtasi ostidagi unit (eng oldinda turgani).</summary>
        public Unit GetAt(Vector2 world)
        {
            Unit best = null;
            foreach (var u in Units)
            {
                Vector2 feet = IsoMath.TileToWorld(u.Position);
                if (MathF.Abs(world.X - feet.X) > 11f) continue;
                if (world.Y > feet.Y + 5f || world.Y < feet.Y - 48f) continue;
                if (best == null || u.Depth > best.Depth) best = u;
            }
            return best;
        }

        /// <summary>Berilgan tile to'rtburchagi ichida unit bormi (qurilishni tekshirish uchun).</summary>
        public bool AnyInArea(Point pos, int size)
        {
            foreach (var u in Units)
            {
                if (u.Position.X >= pos.X && u.Position.X < pos.X + size &&
                    u.Position.Y >= pos.Y && u.Position.Y < pos.Y + size) return true;
            }
            return false;
        }
    }
}
