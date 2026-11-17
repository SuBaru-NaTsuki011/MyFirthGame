using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Units;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// PARRANDALAR: shaharni jonlantiradigan qushlar.
    ///  - KAPTARLAR: maydonlarda galasi bilan don cho'qilaydi; vaqti-vaqti bilan butun gala ko'tarilib,
    ///    shahar ustida aylanadi va boshqa maydonga qo'nadi.
    ///  - CHUMCHUQLAR: bog'lar va yashil joylarda sakrab yuradi.
    ///  - LAYLAKLAR: osmondan sekin qanot qoqib uchib o'tadi.
    /// Odam (ayniqsa o'yinchi boshqarayotgan qahramon) yaqinlashsa, qushlar cho'chib uchib ketadi.
    /// Tunda uchmaydi — yerda mudraydi. Faqat bezak: iqtisod/save'ga ta'sir qilmaydi.
    /// </summary>
    public sealed class BirdSystem
    {
        private enum Species { Pigeon, Sparrow, Stork }
        private enum BirdState { Ground, Flying, Landing }

        private sealed class Flock
        {
            public Vector2 Center;
            public Vector2 Drift;
            public bool Circling;
            public float Timer;
            public Vector2 LandAt;
        }

        private sealed class Bird : IWorldObject
        {
            public Species Kind;
            public BirdState State;
            public Vector2 Pos;        // tile
            public float Alt;          // balandlik (world piksel)
            public Vector2 Vel;        // tile/s
            public Vector2 Target;
            public float Timer;        // keyingi qaror / uchish vaqti
            public float Anim;
            public bool Pecking;
            public bool FacingLeft;
            public Flock Flock;
            public float Orbit, OrbitR, OrbitAlt;
            public Texture2D Tex;

            // IWorldObject (faqat yerdagi qushlar renderer orqali binolar bilan saralanadi)
            public float Depth => Pos.X + Pos.Y;
            public int SortLayer => 1;
            public Texture2D Texture => Tex;
            public bool Flipped => FacingLeft;
            public Vector2 DrawPosition
            {
                get
                {
                    Vector2 w = IsoMath.TileToWorld(Pos);
                    return new Vector2(MathF.Round(w.X - Tex.Width / 2f), MathF.Round(w.Y - Tex.Height + 1 - Alt));
                }
            }
        }

        private readonly MapManager _map;
        private readonly UnitManager _units;
        private readonly Random _rng = new Random(77);
        private readonly List<Bird> _birds = new List<Bird>();
        private readonly List<Flock> _flocks = new List<Flock>();
        private readonly List<IWorldObject> _ground = new List<IWorldObject>();
        private float _storkTimer = 8f;

        // Sprite'lar: [tur][kadr]
        private readonly Texture2D[] _pigeonGround, _pigeonFly, _sparrowGround, _sparrowFly, _storkFly;
        private readonly Texture2D _shadow;

        // Kaptarlar to'planadigan maydonlar (CityGenerator: markaziy maydon, bozor maydoni, saroy oldi)
        private static readonly Vector2[] Plazas = { new Vector2(30.5f, 33.5f), new Vector2(42.5f, 37f), new Vector2(34.5f, 29f), new Vector2(27.5f, 35.5f) };

        public BirdSystem(AssetManager assets, MapManager map, UnitManager units)
        {
            _map = map;
            _units = units;
            GraphicsDevice gd = assets.GraphicsDevice;
            _pigeonGround = new[] { BirdArt.PigeonGround(false).ToTexture(gd), BirdArt.PigeonGround(true).ToTexture(gd) };
            _pigeonFly = new[] { BirdArt.PigeonFly(0).ToTexture(gd), BirdArt.PigeonFly(1).ToTexture(gd), BirdArt.PigeonFly(2).ToTexture(gd) };
            _sparrowGround = new[] { BirdArt.SparrowGround(false).ToTexture(gd), BirdArt.SparrowGround(true).ToTexture(gd) };
            _sparrowFly = new[] { BirdArt.SparrowFly(0).ToTexture(gd), BirdArt.SparrowFly(1).ToTexture(gd) };
            _storkFly = new[] { BirdArt.StorkFly(0).ToTexture(gd), BirdArt.StorkFly(1).ToTexture(gd), BirdArt.StorkFly(2).ToTexture(gd) };
            _shadow = BirdArt.Shadow().ToTexture(gd);
            Spawn();
        }

        /// <summary>Yerdagi qushlar — WorldRenderer ularni binolar va odamlar bilan birga saralab chizadi.</summary>
        public IReadOnlyList<IWorldObject> GroundObjects
        {
            get
            {
                _ground.Clear();
                foreach (Bird b in _birds) if (b.State == BirdState.Ground) _ground.Add(b);
                return _ground;
            }
        }

        // ==================================================================
        // Paydo bo'lish
        // ==================================================================

        private void Spawn()
        {
            // 3 ta kaptar galasi
            for (int f = 0; f < 3; f++)
            {
                Vector2 home = Plazas[f];
                var flock = new Flock { Center = home, LandAt = home, Timer = 15f + (float)_rng.NextDouble() * 20f };
                _flocks.Add(flock);
                int count = 6 + _rng.Next(4);
                for (int i = 0; i < count; i++)
                {
                    Vector2 p = OpenNear(home, 1.8f) ?? home;
                    _birds.Add(new Bird
                    {
                        Kind = Species.Pigeon, State = BirdState.Ground, Pos = p, Flock = flock,
                        Timer = (float)_rng.NextDouble() * 2f, FacingLeft = _rng.Next(2) == 0,
                        Orbit = (float)(_rng.NextDouble() * MathF.PI * 2), OrbitR = 1.6f + (float)_rng.NextDouble() * 2.2f,
                        OrbitAlt = 70f + (float)_rng.NextDouble() * 40f
                    });
                }
            }
            // Chumchuqlar — yashil joylarda kichik to'dachalar
            for (int i = 0; i < 12; i++)
            {
                Vector2? p = RandomGrass();
                if (p == null) continue;
                _birds.Add(new Bird { Kind = Species.Sparrow, State = BirdState.Ground, Pos = p.Value, Timer = (float)_rng.NextDouble() * 1.5f, FacingLeft = _rng.Next(2) == 0 });
            }
            foreach (Bird b in _birds) b.Tex = b.Kind == Species.Pigeon ? _pigeonGround[0] : _sparrowGround[0];
        }

        private bool IsOpen(int x, int y)
        {
            if (!_map.InBounds(x, y) || _map.GetOccupant(x, y) != null) return false;
            TileType t = _map.GetTile(x, y);
            return t != TileType.Water;
        }

        private Vector2? OpenNear(Vector2 c, float radius)
        {
            for (int tries = 0; tries < 20; tries++)
            {
                float a = (float)(_rng.NextDouble() * MathF.PI * 2), r = (float)Math.Sqrt(_rng.NextDouble()) * radius;
                Vector2 p = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
                if (IsOpen((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y))) return p;
            }
            return null;
        }

        private Vector2? RandomGrass()
        {
            for (int tries = 0; tries < 60; tries++)
            {
                int x = 12 + _rng.Next(40), y = 12 + _rng.Next(40);
                if (IsOpen(x, y) && _map.GetTile(x, y) == TileType.Grass) return new Vector2(x + 0.5f, y + 0.5f);
            }
            return null;
        }

        // ==================================================================
        // Update
        // ==================================================================

        /// <param name="dt">O'yin vaqti (pauzada 0).</param>
        /// <param name="night">0 — kunduz, 1 — chuqur tun.</param>
        /// <param name="hero">O'yinchi boshqarayotgan odam — qushlar undan kuchliroq cho'chiydi.</param>
        public void Update(float dt, float night, Unit hero)
        {
            if (dt <= 0f) return;
            bool sleepy = night > 0.6f;

            // Gala: vaqti kelsa butun gala ko'tarilib aylanadi, keyin boshqa maydonga qo'nadi
            foreach (Flock f in _flocks)
            {
                f.Timer -= dt;
                if (f.Circling)
                {
                    f.Center += f.Drift * dt;
                    if (f.Timer <= 0f || sleepy)
                    {
                        f.Circling = false;
                        f.LandAt = Plazas[_rng.Next(Plazas.Length)];
                        f.Timer = 25f + (float)_rng.NextDouble() * 30f;
                        foreach (Bird b in _birds)
                            if (b.Flock == f && b.State == BirdState.Flying) BeginLanding(b, OpenNear(f.LandAt, 2f) ?? f.LandAt);
                    }
                }
                else if (f.Timer <= 0f && !sleepy)
                {
                    f.Circling = true;
                    f.Timer = 10f + (float)_rng.NextDouble() * 8f;
                    f.Center = f.LandAt;
                    float a = (float)(_rng.NextDouble() * MathF.PI * 2);
                    f.Drift = new Vector2(MathF.Cos(a), MathF.Sin(a)) * 0.6f;
                    foreach (Bird b in _birds)
                        if (b.Flock == f && b.State == BirdState.Ground) TakeOff(b, Vector2.Zero);
                }
            }

            foreach (Bird b in _birds)
            {
                switch (b.State)
                {
                    case BirdState.Ground: UpdateGround(b, dt, sleepy, hero); break;
                    case BirdState.Flying: UpdateFlying(b, dt, sleepy); break;
                    case BirdState.Landing: UpdateLanding(b, dt); break;
                }
            }

            // Laylaklar osmondan uchib o'tadi (kunduzi)
            _storkTimer -= dt;
            if (_storkTimer <= 0f)
            {
                _storkTimer = 25f + (float)_rng.NextDouble() * 35f;
                if (!sleepy) SpawnStork();
            }
            _birds.RemoveAll(b => b.Kind == Species.Stork && (b.Pos.X < -6 || b.Pos.Y < -6 || b.Pos.X > _map.Width + 6 || b.Pos.Y > _map.Height + 6));
        }

        private void UpdateGround(Bird b, float dt, bool sleepy, Unit hero)
        {
            Texture2D[] frames = b.Kind == Species.Pigeon ? _pigeonGround : _sparrowGround;

            // Cho'chish: odam yaqinlashsa uchib ketadi
            foreach (Unit u in _units.Units)
            {
                if (u.IsDown) continue;
                float fear = u == hero ? 2.0f : (u.State == UnitState.Walking ? 1.0f : 0.6f);
                if (b.Kind == Species.Sparrow) fear *= 1.2f;
                Vector2 d = b.Pos - u.Position;
                if (d.LengthSquared() < fear * fear)
                {
                    TakeOff(b, d);
                    if (b.Flock != null && u == hero)
                        foreach (Bird o in _birds) // butun gala birga ko'tariladi
                            if (o.Flock == b.Flock && o.State == BirdState.Ground && Vector2.Distance(o.Pos, b.Pos) < 3f) TakeOff(o, o.Pos - u.Position);
                    return;
                }
            }

            if (sleepy) { b.Pecking = false; b.Tex = frames[0]; return; }

            b.Timer -= dt;
            if (b.Timer <= 0f)
            {
                int act = _rng.Next(10);
                if (act < 5) { b.Pecking = true; b.Timer = 0.3f + (float)_rng.NextDouble() * 0.6f; }
                else
                {
                    b.Pecking = false;
                    // kichik sakrash / qadam
                    float a = (float)(_rng.NextDouble() * MathF.PI * 2);
                    Vector2 step = new Vector2(MathF.Cos(a), MathF.Sin(a)) * (b.Kind == Species.Sparrow ? 0.35f : 0.22f);
                    Vector2 np = b.Pos + step;
                    if (IsOpen((int)MathF.Floor(np.X), (int)MathF.Floor(np.Y)) && (b.Flock == null || Vector2.Distance(np, b.Flock.LandAt) < 3f))
                    {
                        b.Pos = np;
                        b.FacingLeft = IsoMath.TileToWorld(step).X < 0;
                    }
                    b.Timer = 0.4f + (float)_rng.NextDouble() * 1.4f;
                }
            }
            // sakrashda biroz ko'tarilish (chumchuq)
            b.Alt = 0f;
            b.Tex = frames[b.Pecking && ((int)(b.Timer * 8f) % 2 == 0) ? 1 : 0];
        }

        private void TakeOff(Bird b, Vector2 away)
        {
            b.State = BirdState.Flying;
            if (away.LengthSquared() < 0.0001f)
            {
                float a = (float)(_rng.NextDouble() * MathF.PI * 2);
                away = new Vector2(MathF.Cos(a), MathF.Sin(a));
            }
            away.Normalize();
            float speed = b.Kind == Species.Sparrow ? 3.2f : 2.6f;
            b.Vel = away * speed;
            b.Timer = 2.5f + (float)_rng.NextDouble() * 3.5f;
            b.Pecking = false;
        }

        private void BeginLanding(Bird b, Vector2 at)
        {
            b.State = BirdState.Landing;
            b.Target = at;
        }

        private void UpdateFlying(Bird b, float dt, bool sleepy)
        {
            Texture2D[] frames = b.Kind switch { Species.Stork => _storkFly, Species.Sparrow => _sparrowFly, _ => _pigeonFly };
            float flap = b.Kind == Species.Stork ? 3f : b.Kind == Species.Sparrow ? 16f : 11f;

            if (b.Kind == Species.Stork)
            {
                // Laylak: to'g'ri chiziqda sekin uchadi, ba'zan qanot qoqmay sirpanadi
                b.Pos += b.Vel * dt;
                b.Anim += dt;
                bool glide = (int)(b.Anim / 2.2f) % 2 == 1;
                b.Tex = glide ? frames[1] : frames[(int)(b.Anim * flap) % frames.Length];
                return;
            }

            b.Anim += dt;
            b.Tex = frames[(int)(b.Anim * flap) % frames.Length];

            if (b.Flock != null && b.Flock.Circling)
            {
                // Gala bilan aylanish
                b.Orbit += dt * (0.9f + b.OrbitR * 0.05f);
                Vector2 goal = b.Flock.Center + new Vector2(MathF.Cos(b.Orbit), MathF.Sin(b.Orbit)) * b.OrbitR;
                Vector2 to = goal - b.Pos;
                b.Vel = Vector2.Lerp(b.Vel, to * 2.2f, 1f - MathF.Exp(-3f * dt));
                b.Alt = MathHelper.Lerp(b.Alt, b.OrbitAlt, 1f - MathF.Exp(-1.5f * dt));
            }
            else
            {
                // Cho'chib uchgan: ko'tarilib uzoqlashadi, keyin qo'nish joyini tanlaydi
                b.Alt = MathHelper.Lerp(b.Alt, b.Kind == Species.Sparrow ? 45f : 75f, 1f - MathF.Exp(-1.8f * dt));
                b.Timer -= dt;
                if (b.Timer <= 0f || sleepy)
                {
                    Vector2 at = b.Flock != null ? (OpenNear(b.Flock.LandAt, 2f) ?? b.Flock.LandAt)
                               : (OpenNear(b.Pos, 6f) ?? RandomGrass() ?? b.Pos);
                    BeginLanding(b, at);
                }
            }
            b.Pos += b.Vel * dt;
            b.Pos = new Vector2(MathHelper.Clamp(b.Pos.X, 1f, _map.Width - 1f), MathHelper.Clamp(b.Pos.Y, 1f, _map.Height - 1f));
            if (MathF.Abs(IsoMath.TileToWorld(b.Vel).X) > 0.05f) b.FacingLeft = IsoMath.TileToWorld(b.Vel).X < 0;
        }

        private void UpdateLanding(Bird b, float dt)
        {
            Texture2D[] frames = b.Kind == Species.Sparrow ? _sparrowFly : _pigeonFly;
            b.Anim += dt;
            b.Tex = frames[(int)(b.Anim * 12f) % frames.Length];
            Vector2 to = b.Target - b.Pos;
            float dist = to.Length();
            float speed = MathF.Min(2.6f, 0.6f + dist * 1.2f);
            if (dist > 0.05f)
            {
                b.Vel = to / dist * speed;
                b.Pos += b.Vel * MathF.Min(dt, dist / speed);
                if (MathF.Abs(IsoMath.TileToWorld(b.Vel).X) > 0.05f) b.FacingLeft = IsoMath.TileToWorld(b.Vel).X < 0;
            }
            // yaqinlashgan sari pasayadi
            float wantAlt = MathF.Min(b.Alt, dist * 26f);
            b.Alt = MathHelper.Lerp(b.Alt, wantAlt, 1f - MathF.Exp(-4f * dt));
            if (dist < 0.08f && b.Alt < 3f)
            {
                b.State = BirdState.Ground;
                b.Alt = 0f;
                b.Timer = 0.5f + (float)_rng.NextDouble();
                if (!IsOpen((int)MathF.Floor(b.Pos.X), (int)MathF.Floor(b.Pos.Y))) BeginLanding(b, OpenNear(b.Pos, 3f) ?? b.Target);
            }
        }

        private void SpawnStork()
        {
            // Xaritaning bir chetidan narigi chetiga
            bool alongX = _rng.Next(2) == 0;
            float lane = 18f + (float)_rng.NextDouble() * 28f;
            bool forward = _rng.Next(2) == 0;
            Vector2 start = alongX ? new Vector2(forward ? -4f : _map.Width + 4f, lane) : new Vector2(lane, forward ? -4f : _map.Height + 4f);
            Vector2 dir = alongX ? new Vector2(forward ? 1 : -1, 0) : new Vector2(0, forward ? 1 : -1);
            int n = 1 + _rng.Next(2);
            for (int i = 0; i < n; i++)
            {
                var s = new Bird
                {
                    Kind = Species.Stork, State = BirdState.Flying, Pos = start - dir * (i * 2.2f) + new Vector2(dir.Y, dir.X) * (i * 1.2f),
                    Vel = dir * 1.5f, Alt = 120f + i * 8f, Anim = (float)_rng.NextDouble() * 3f, Tex = _storkFly[0]
                };
                s.FacingLeft = IsoMath.TileToWorld(dir).X < 0;
                _birds.Add(s);
            }
        }

        // ==================================================================
        // Chizish
        // ==================================================================

        /// <summary>Uchayotgan qushlarning yerdagi soyalari (yer qatlamida, binolardan oldin).</summary>
        public void DrawShadows(SpriteBatch sb, Vector4 view)
        {
            foreach (Bird b in _birds)
            {
                if (b.State == BirdState.Ground || b.Alt < 2f) continue;
                Vector2 w = IsoMath.TileToWorld(b.Pos);
                if (w.X < view.X - 20 || w.X > view.Z + 20 || w.Y < view.Y - 20 || w.Y > view.W + 20) continue;
                float a = MathHelper.Clamp(1f - b.Alt / 160f, 0.15f, 0.6f);
                float s = b.Kind == Species.Stork ? 1.8f : b.Kind == Species.Sparrow ? 0.7f : 1f;
                sb.Draw(_shadow, w, null, Color.White * a, 0f, new Vector2(_shadow.Width / 2f, _shadow.Height / 2f), s, SpriteEffects.None, 0f);
            }
        }

        /// <summary>Osmondagi qushlar — hamma narsaning ustida (binolardan keyin).</summary>
        public void DrawSky(SpriteBatch sb, Vector4 view)
        {
            foreach (Bird b in _birds)
            {
                if (b.State == BirdState.Ground) continue;
                Vector2 p = b.DrawPosition;
                if (p.X + b.Tex.Width < view.X || p.X > view.Z || p.Y + b.Tex.Height < view.Y || p.Y > view.W) continue;
                sb.Draw(b.Tex, p, null, Color.White, 0f, Vector2.Zero, 1f, b.FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            }
        }
    }

    /// <summary>Qushlar uchun kodda chizilgan kichik sprite'lar (o'ngga qarab; chapga uchganda aylantiriladi).</summary>
    public static class BirdArt
    {
        private static readonly Color Ink = new Color(34, 28, 26);

        public static Canvas PigeonGround(bool peck)
        {
            var c = new Canvas(14, 11);
            Color body = new Color(150, 154, 168), dark = new Color(104, 106, 122);
            c.FillEllipse(7, 10, 4, 1.2f, new Color(0, 0, 0, 60));
            c.FillPolygon(dark, 0f, new Vector2(1, 5), new Vector2(4, 5), new Vector2(4, 7), new Vector2(1, 7)); // dum
            c.FillEllipse(6.5f, 6.5f, 4f, 2.6f, body);
            c.FillRect(4, 6, 4, 1, dark);                                  // qanot chizig'i
            float hx = peck ? 10.5f : 9.5f, hy = peck ? 7.5f : 3.8f;
            c.FillEllipse(hx - 1, hy + 1.5f, 1.8f, 1.6f, new Color(90, 140, 120)); // yaltiroq bo'yin
            c.FillEllipse(hx, hy, 1.9f, 1.8f, dark);
            c.FillRect((int)hx + 2, (int)hy, 1, 1, new Color(220, 200, 170)); // tumshuq
            c.FillRect((int)hx, (int)hy - 1, 1, 1, new Color(230, 120, 40));  // ko'z
            c.FillRect(6, 9, 1, 1, new Color(220, 110, 90));
            c.FillRect(8, 9, 1, 1, new Color(220, 110, 90));
            c.Outline(Ink, 0.4f);
            return c;
        }

        public static Canvas PigeonFly(int frame)
        {
            var c = new Canvas(16, 12);
            Color body = new Color(156, 160, 174), dark = new Color(96, 98, 114);
            c.FillEllipse(8, 7, 3.6f, 1.9f, body);
            c.FillEllipse(12, 6, 1.7f, 1.6f, dark);
            c.FillRect(14, 6, 1, 1, new Color(220, 200, 170));
            c.FillPolygon(dark, 0f, new Vector2(2, 6), new Vector2(5, 6.5f), new Vector2(5, 8), new Vector2(2, 8.5f));
            // qanotlar: 0 — tepada, 1 — o'rtada, 2 — pastda
            if (frame == 0) { c.FillPolygon(body, 0f, new Vector2(6, 6), new Vector2(10, 6), new Vector2(7, 0)); c.FillRect(6, 0, 2, 1, dark); }
            else if (frame == 1) { c.FillPolygon(body, 0f, new Vector2(5, 6), new Vector2(11, 6), new Vector2(8, 4)); c.FillPolygon(dark, 0f, new Vector2(3, 5), new Vector2(6, 6), new Vector2(5, 5)); }
            else { c.FillPolygon(body, 0f, new Vector2(6, 7), new Vector2(10, 7), new Vector2(7, 11.5f)); c.FillRect(6, 11, 2, 1, dark); }
            c.Outline(Ink, 0.4f);
            return c;
        }

        public static Canvas SparrowGround(bool peck)
        {
            var c = new Canvas(10, 8);
            Color body = new Color(150, 106, 62), belly = new Color(214, 196, 166);
            c.FillEllipse(5, 7.2f, 2.8f, 0.8f, new Color(0, 0, 0, 55));
            c.FillRect(1, 4, 2, 1, new Color(96, 66, 40));
            c.FillEllipse(4.5f, 4.6f, 2.8f, 1.9f, body);
            c.FillEllipse(4.8f, 5.3f, 1.8f, 1f, belly);
            float hx = peck ? 7.5f : 6.8f, hy = peck ? 5f : 2.8f;
            c.FillEllipse(hx, hy, 1.5f, 1.4f, new Color(120, 112, 104));
            c.FillRect((int)hx + 1, (int)hy, 1, 1, new Color(60, 50, 40));
            c.Outline(Ink, 0.4f);
            return c;
        }

        public static Canvas SparrowFly(int frame)
        {
            var c = new Canvas(11, 8);
            Color body = new Color(150, 106, 62);
            c.FillEllipse(5.5f, 4.5f, 2.6f, 1.4f, body);
            c.FillEllipse(8.4f, 4f, 1.3f, 1.2f, new Color(120, 112, 104));
            if (frame == 0) c.FillPolygon(Canvas.Shade(body, 0.85f), 0f, new Vector2(4, 4), new Vector2(7, 4), new Vector2(5, 0));
            else c.FillPolygon(Canvas.Shade(body, 0.85f), 0f, new Vector2(4, 5), new Vector2(7, 5), new Vector2(5, 7.5f));
            c.Outline(Ink, 0.35f);
            return c;
        }

        public static Canvas StorkFly(int frame)
        {
            var c = new Canvas(34, 18);
            Color white = new Color(244, 242, 236), black = new Color(30, 28, 30), red = new Color(214, 70, 46);
            c.Line(new Vector2(4, 10), new Vector2(0, 11), red);                 // orqaga cho'zilgan oyoqlar
            c.FillEllipse(14, 10, 7, 2.4f, white);                                // tana
            c.Line(new Vector2(19, 9), new Vector2(26, 8), white, 2);             // uzun bo'yin
            c.FillEllipse(27, 8, 1.8f, 1.6f, white);
            c.Line(new Vector2(28, 8), new Vector2(33, 9), red);                  // uzun tumshuq
            // keng qanotlar, uchlari qora
            if (frame == 0)
            {
                c.FillPolygon(white, 0f, new Vector2(9, 9), new Vector2(18, 9), new Vector2(12, 1));
                c.FillPolygon(black, 0f, new Vector2(10, 4), new Vector2(13, 4), new Vector2(12, 0));
            }
            else if (frame == 1)
            {
                c.FillPolygon(white, 0f, new Vector2(6, 9), new Vector2(20, 9), new Vector2(13, 7));
                c.FillPolygon(black, 0f, new Vector2(2, 8), new Vector2(8, 9), new Vector2(7, 8));
                c.FillPolygon(black, 0f, new Vector2(18, 9), new Vector2(24, 8), new Vector2(19, 8));
            }
            else
            {
                c.FillPolygon(white, 0f, new Vector2(9, 11), new Vector2(18, 11), new Vector2(12, 17.5f));
                c.FillPolygon(black, 0f, new Vector2(10, 15), new Vector2(13, 15), new Vector2(12, 17.5f));
            }
            c.Outline(Ink, 0.35f);
            return c;
        }

        public static Canvas Shadow()
        {
            var c = new Canvas(10, 4);
            c.FillEllipse(5, 2, 4.5f, 1.6f, new Color(0, 0, 0, 110));
            return c;
        }
    }
}
