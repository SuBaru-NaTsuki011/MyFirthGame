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
            LoadFrames(unit.SpriteKey, out var frames, out var back);
            unit.SetFrames(frames);
            unit.BackFrames = back;
            unit.WorkFrames = LoadSet(unit.SpriteKey + "_h", 4);
            unit.BackWorkFrames = LoadSet(unit.SpriteKey + "_bh", 4);
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
            LoadFrames(SpriteKeys.UnitElite, out var frames, out var eliteBack);
            var attack = new Texture2D[UnitArt.AttackKinds * UnitArt.AttackPhases];
            for (int i = 0; i < attack.Length; i++)
                attack[i] = _assets.TryGet(AssetManager.AttackKey(SpriteKeys.UnitElite, i / UnitArt.AttackPhases, i % UnitArt.AttackPhases));
            unit.MakeElite(frames, attack);
            unit.BackFrames = eliteBack;
        }

        public const int WalkFrames = 8;

        /// <summary>prefix0..prefix{n-1} — hammasi bo'lsa qaytadi, aks holda null.</summary>
        private Texture2D[] LoadSet(string prefix, int n)
        {
            var a = new Texture2D[n];
            for (int i = 0; i < n; i++) { a[i] = _assets.TryGet(prefix + i); if (a[i] == null) return null; }
            return a;
        }

        /// <summary>
        /// Kadrlar: [turish, yurish...]. 3D modeldan 8 kadrli yurish sikli (key_w0..w7, orqa: key_bw0..bw7) bo'lsa — shu,
        /// aks holda eski 3 kadr. Orqa kadrlar old kadrlar bilan bir xil sonda bo'lmasa null.
        /// </summary>
        public void LoadFrames(string key, out Texture2D[] frames, out Texture2D[] back)
        {
            if (_assets.TryGet(key + "_w0") != null && _assets.TryGet(key + "_w" + (WalkFrames - 1)) != null)
            {
                frames = new Texture2D[1 + WalkFrames];
                frames[0] = _assets.Get(key + "_0");
                for (int i = 0; i < WalkFrames; i++) frames[1 + i] = _assets.TryGet(key + "_w" + i) ?? frames[0];
                back = null;
                Texture2D b0 = _assets.TryGet(key + "_b0");
                if (b0 != null && _assets.TryGet(key + "_bw0") != null)
                {
                    back = new Texture2D[1 + WalkFrames];
                    back[0] = b0;
                    for (int i = 0; i < WalkFrames; i++) back[1 + i] = _assets.TryGet(key + "_bw" + i) ?? b0;
                }
                return;
            }
            frames = new Texture2D[SpriteKeys.UnitFrames];
            for (int i = 0; i < frames.Length; i++) frames[i] = _assets.Get(key + "_" + i);
            back = LoadBack(key);
        }

        /// <summary>Orqa tomon kadrlari (key_b0..b2) — hammasi bo'lsa qaytadi, aks holda null.</summary>
        private Texture2D[] LoadBack(string key)
        {
            var back = new Texture2D[SpriteKeys.UnitFrames];
            for (int i = 0; i < back.Length; i++)
            {
                back[i] = _assets.TryGet(key + "_b" + i);
                if (back[i] == null) return null;
            }
            return back;
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
                if (u.Hidden) continue;
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
