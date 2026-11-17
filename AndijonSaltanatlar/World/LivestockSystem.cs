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
    /// CHORVA: cho'ponlarning qo'y, echki va mollari.
    ///  - Har bir cho'ponning o'z podasi bor; poda cho'pon ortidan yuradi, o'tlaydi, atrofida sayr qiladi.
    ///    O'yinchi cho'pon ichiga kirsa, poda uning ortidan ergashadi.
    ///  - Har o'yin kuni poda KO'PAYADI: kamida ikki katta jonivor bo'lsa, qo'zichoq / uloq / buzoq tug'iladi;
    ///    bolalar 3 kunda katta bo'ladi. Poda yaylov sig'imigacha (cho'pon boshiga) o'sadi.
    ///  - Shahar podadan har kuni sut-go'sht oladi (oziq-ovqat).
    ///  - Dushman yaqinlashsa jonivorlar qochadi.
    /// </summary>
    public sealed class LivestockSystem
    {
        public enum Kind { Sheep, Goat, Cow }

        private const int HerdCapacity = 24;
        private const int GrowUpDays = 3;

        private sealed class Herd
        {
            public CitizenUnit Shepherd;
            public Vector2 Center;
            public readonly List<Animal> Animals = new List<Animal>();
        }

        private sealed class Animal : IWorldObject
        {
            public Kind Kind;
            public Herd Herd;
            public bool Baby;
            public int AgeDays;
            public Vector2 Pos;
            public Vector2 Target;
            public bool Moving;
            public bool Grazing;
            public bool FacingLeft;
            public float Timer;
            public float Anim;
            public float Speed;
            public Texture2D Tex;

            public float Depth => Pos.X + Pos.Y;
            public int SortLayer => 1;
            public Texture2D Texture => Tex;
            public bool Flipped => FacingLeft;
            public Vector2 DrawPosition
            {
                get
                {
                    Vector2 w = IsoMath.TileToWorld(Pos);
                    return new Vector2(MathF.Round(w.X - Tex.Width / 2f), MathF.Round(w.Y - Tex.Height + 2));
                }
            }
        }

        private readonly MapManager _map;
        private readonly UnitManager _units;
        private readonly Random _rng = new Random(2024);
        private readonly List<Herd> _herds = new List<Herd>();
        private readonly List<IWorldObject> _objects = new List<IWorldObject>();
        // [tur][bola?][kadr: 0 turish, 1-2 yurish, 3 o'tlash]
        private readonly Texture2D[,,] _tex = new Texture2D[3, 2, 4];

        public int Sheep { get; private set; }
        public int Goats { get; private set; }
        public int Cows { get; private set; }

        public LivestockSystem(AssetManager assets, MapManager map, UnitManager units)
        {
            _map = map;
            _units = units;
            GraphicsDevice gd = assets.GraphicsDevice;
            for (int k = 0; k < 3; k++)
                for (int b = 0; b < 2; b++)
                    for (int f = 0; f < 4; f++)
                        _tex[k, b, f] = LivestockArt.Frame((Kind)k, b == 1, f).ToTexture(gd);

            foreach (Unit u in units.Units)
                if (u is CitizenUnit c && c.Kind == CitizenArt.Shepherd)
                {
                    var herd = new Herd { Shepherd = c, Center = c.Position };
                    _herds.Add(herd);
                    // Boshlang'ich poda: 4 qo'y, 3 echki, 2 sigir
                    for (int i = 0; i < 4; i++) AddAnimal(herd, Kind.Sheep, false);
                    for (int i = 0; i < 3; i++) AddAnimal(herd, Kind.Goat, false);
                    for (int i = 0; i < 2; i++) AddAnimal(herd, Kind.Cow, false);
                }
            Recount();
        }

        private Animal AddAnimal(Herd herd, Kind kind, bool baby, Vector2? near = null)
        {
            Vector2 p = OpenNear(near ?? herd.Center, 2.2f) ?? herd.Center;
            var a = new Animal
            {
                Kind = kind, Herd = herd, Baby = baby, Pos = p, Target = p,
                Timer = (float)_rng.NextDouble() * 2f, FacingLeft = _rng.Next(2) == 0,
                Speed = kind == Kind.Cow ? 0.75f : kind == Kind.Goat ? 1.05f : 0.9f
            };
            a.Tex = _tex[(int)kind, baby ? 1 : 0, 0];
            herd.Animals.Add(a);
            return a;
        }

        private void Recount()
        {
            int s = 0, g = 0, c = 0;
            foreach (Herd h in _herds)
                foreach (Animal a in h.Animals)
                    if (a.Kind == Kind.Sheep) s++; else if (a.Kind == Kind.Goat) g++; else c++;
            Sheep = s; Goats = g; Cows = c;
        }

        private bool IsOpen(Vector2 p)
        {
            int x = (int)MathF.Floor(p.X), y = (int)MathF.Floor(p.Y);
            return _map.InBounds(x, y) && _map.GetTile(x, y) != TileType.Water && _map.GetOccupant(x, y) == null;
        }

        private Vector2? OpenNear(Vector2 c, float radius)
        {
            for (int i = 0; i < 24; i++)
            {
                float a = (float)(_rng.NextDouble() * MathF.PI * 2), r = (float)Math.Sqrt(_rng.NextDouble()) * radius;
                Vector2 p = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
                if (IsOpen(p)) return p;
            }
            return null;
        }

        // ==================================================================
        // Ko'payish (har o'yin kuni)
        // ==================================================================

        /// <returns>Shu kuni tug'ilganlar haqida qisqa xabar (yoki null).</returns>
        public string DayTick(GameStateManager state)
        {
            int born = 0;
            string last = null;
            foreach (Herd h in _herds)
            {
                foreach (Animal a in h.Animals)
                    if (a.Baby && ++a.AgeDays >= GrowUpDays) { a.Baby = false; a.AgeDays = 0; }

                foreach (Kind k in new[] { Kind.Sheep, Kind.Goat, Kind.Cow })
                {
                    if (h.Animals.Count >= HerdCapacity) break;
                    int adults = 0;
                    Animal mother = null;
                    foreach (Animal a in h.Animals)
                        if (a.Kind == k && !a.Baby) { adults++; mother = a; }
                    if (adults < 2) continue;
                    float chance = k == Kind.Cow ? 0.25f : k == Kind.Goat ? 0.4f : 0.45f;
                    if (_rng.NextDouble() < chance)
                    {
                        AddAnimal(h, k, true, mother.Pos);
                        born++;
                        last = k == Kind.Sheep ? "qo'zichoq" : k == Kind.Goat ? "uloq" : "buzoq";
                    }
                }
            }

            // Sut va go'sht: shahar oziq-ovqati
            int adultsAll = 0, cows = 0;
            foreach (Herd h in _herds)
                foreach (Animal a in h.Animals)
                    if (!a.Baby) { adultsAll++; if (a.Kind == Kind.Cow) cows++; }
            int food = adultsAll / 3 + cows;
            if (food > 0) state.Food += food;

            Recount();
            if (born == 0) return null;
            return born == 1
                ? "Chorvada yangi " + last + " tug'ildi! (Qo'y: " + Sheep + ", echki: " + Goats + ", sigir: " + Cows + ")  +" + food + " oziq"
                : "Chorvada " + born + " ta bola tug'ildi! (Qo'y: " + Sheep + ", echki: " + Goats + ", sigir: " + Cows + ")  +" + food + " oziq";
        }

        // ==================================================================
        // Harakat
        // ==================================================================

        public void Update(float dt)
        {
            if (dt <= 0f) return;
            foreach (Herd h in _herds)
            {
                if (h.Shepherd != null && !h.Shepherd.Hidden)
                    h.Center = Vector2.Lerp(h.Center, h.Shepherd.Position, 1f - MathF.Exp(-1.2f * dt));
                foreach (Animal a in h.Animals) UpdateAnimal(a, dt);
            }
        }

        private void UpdateAnimal(Animal a, float dt)
        {
            int k = (int)a.Kind, b = a.Baby ? 1 : 0;

            // Dushmandan qochish
            Unit danger = null;
            foreach (Unit u in _units.Units)
                if (u.Team == UnitTeam.Enemy && !u.IsDown && Vector2.DistanceSquared(u.Position, a.Pos) < 6.25f) { danger = u; break; }

            float speed = a.Speed;
            if (danger != null)
            {
                Vector2 away = a.Pos - danger.Position;
                if (away.LengthSquared() < 0.001f) away = new Vector2(1, 0);
                away.Normalize();
                a.Target = a.Pos + away * 2f;
                a.Moving = true; a.Grazing = false;
                speed *= 2.4f;
            }
            else
            {
                float far = Vector2.Distance(a.Pos, a.Herd.Center);
                if (far > 3.2f)
                {
                    // Poda ortidan qolib ketdi — cho'pon tomon yuradi
                    a.Target = a.Herd.Center + new Vector2((float)_rng.NextDouble() - 0.5f, (float)_rng.NextDouble() - 0.5f) * 2f;
                    a.Moving = true; a.Grazing = false;
                    speed *= far > 6f ? 2.2f : 1.4f;
                }
                else
                {
                    a.Timer -= dt;
                    if (a.Timer <= 0f && !a.Moving)
                    {
                        int act = _rng.Next(10);
                        if (act < 5) { a.Grazing = true; a.Timer = 1.5f + (float)_rng.NextDouble() * 3f; }
                        else if (act < 8)
                        {
                            Vector2? t = OpenNear(a.Herd.Center, 2.6f);
                            if (t.HasValue) { a.Target = t.Value; a.Moving = true; a.Grazing = false; }
                            a.Timer = 1f + (float)_rng.NextDouble() * 2f;
                        }
                        else { a.Grazing = false; a.Timer = 1f + (float)_rng.NextDouble() * 2f; }
                    }
                }
            }

            if (a.Moving)
            {
                Vector2 to = a.Target - a.Pos;
                float d = to.Length();
                if (d < 0.06f) { a.Moving = false; }
                else
                {
                    Vector2 step = to / d * MathF.Min(speed * dt, d);
                    Vector2 np = a.Pos + step;
                    if (IsOpen(np)) a.Pos = np;
                    else if (IsOpen(new Vector2(np.X, a.Pos.Y))) a.Pos = new Vector2(np.X, a.Pos.Y);
                    else if (IsOpen(new Vector2(a.Pos.X, np.Y))) a.Pos = new Vector2(a.Pos.X, np.Y);
                    else a.Moving = false;
                    float sx = IsoMath.TileToWorld(step).X;
                    if (MathF.Abs(sx) > 0.0001f) a.FacingLeft = sx < 0;
                }
                a.Anim += dt * speed * 7f;
                a.Tex = _tex[k, b, 1 + ((int)a.Anim % 2)];
            }
            else a.Tex = _tex[k, b, a.Grazing ? 3 : 0];
        }

        // ==================================================================
        // Chizish
        // ==================================================================

        /// <summary>Izometrik ko'rinish: jonivorlar binolar va odamlar bilan birga saralanadi.</summary>
        public IReadOnlyList<IWorldObject> GroundObjects
        {
            get
            {
                _objects.Clear();
                foreach (Herd h in _herds) foreach (Animal a in h.Animals) _objects.Add(a);
                return _objects;
            }
        }

        /// <summary>Tanlangan yoki boshqarilayotgan cho'ponning podasi haqida kichik panel.</summary>
        public void DrawHerdInfo(SpriteBatch sb, Texture2D px, PixelFont font, Unit focus, int vw)
        {
            if (!(focus is CitizenUnit c) || c.Kind != CitizenArt.Shepherd) return;
            Herd herd = null;
            foreach (Herd h in _herds) if (h.Shepherd == c) herd = h;
            if (herd == null) return;
            int s = 0, g = 0, cw = 0, babies = 0;
            foreach (Animal a in herd.Animals)
            {
                if (a.Baby) babies++;
                if (a.Kind == Kind.Sheep) s++; else if (a.Kind == Kind.Goat) g++; else cw++;
            }
            string t = "PODA:  " + s + " qo'y   " + g + " echki   " + cw + " sigir" + (babies > 0 ? "   (" + babies + " bola)" : "") + "   /" + HerdCapacity;
            Vector2 size = font.Measure(t, 2);
            var r = new Rectangle(vw / 2 - (int)size.X / 2 - 14, 82, (int)size.X + 28, 28);
            sb.Draw(px, new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), new Color(120, 160, 80, 230));
            sb.Draw(px, r, new Color(22, 30, 18, 235));
            font.DrawCentered(sb, t, r.Center.ToVector2(), new Color(214, 240, 170), 2);
        }
    }

    /// <summary>Chorva sprite'lari (o'ngga qarab). Bolalar — kichikroq (0.65 masshtab).</summary>
    public static class LivestockArt
    {
        private static readonly Color Ink = new Color(36, 28, 24);

        /// <param name="frame">0 — turish, 1-2 — yurish, 3 — o'tlash (bosh pastda).</param>
        public static Canvas Frame(LivestockSystem.Kind kind, bool baby, int frame)
        {
            float s = baby ? 0.65f : 1f;
            switch (kind)
            {
                case LivestockSystem.Kind.Goat: return Goat(s, frame);
                case LivestockSystem.Kind.Cow: return Cow(s, frame);
                default: return Sheep(s, frame);
            }
        }

        private static int Sz(float v, float s) => Math.Max(4, (int)MathF.Ceiling(v * s));

        private static void Legs(Canvas c, float s, int frame, float[] xs, float top, float bottom, Color col)
        {
            for (int i = 0; i < xs.Length; i++)
            {
                float off = frame == 1 ? (i % 2 == 0 ? 1.2f : -1.2f) : frame == 2 ? (i % 2 == 0 ? -1.2f : 1.2f) : 0f;
                int x = (int)MathF.Round((xs[i] + off) * s);
                c.FillRect(x, (int)(top * s), Math.Max(1, (int)(2 * s)), Math.Max(2, (int)((bottom - top) * s)), col);
            }
        }

        private static Canvas Sheep(float s, int frame)
        {
            var c = new Canvas(Sz(24, s), Sz(17, s));
            Color wool = new Color(238, 234, 222), woolDark = new Color(212, 206, 192), face = new Color(52, 44, 40);
            c.FillEllipse(12 * s, 16 * s, 8 * s, 1.4f * s, new Color(0, 0, 0, 60));
            Legs(c, s, frame, new[] { 6f, 9f, 14f, 17f }, 11, 16, face);
            // jingalak jun (bir nechta dumaloq)
            c.FillEllipse(11.5f * s, 8.5f * s, 7.5f * s, 4.6f * s, wool, 0.05f);
            for (int i = 0; i < 5; i++) c.FillEllipse((6 + i * 2.6f) * s, (5.8f + (i % 2)) * s, 2.4f * s, 2.2f * s, i % 2 == 0 ? wool : woolDark);
            c.FillEllipse(4.5f * s, 8 * s, 1.8f * s, 1.6f * s, woolDark); // dum
            bool graze = frame == 3;
            float hx = graze ? 20f : 19.5f, hy = graze ? 11.5f : 6.5f;
            c.FillEllipse(hx * s, hy * s, 2.6f * s, 2.2f * s, face);
            c.FillEllipse((hx - 1.5f) * s, (hy - 2f) * s, 1.4f * s, 0.9f * s, face); // quloq
            c.FillRect((int)((hx + 0.5f) * s), (int)((hy - 0.8f) * s), 1, 1, new Color(240, 220, 120));
            c.Outline(Ink, 0.4f);
            return c;
        }

        private static Canvas Goat(float s, int frame)
        {
            var c = new Canvas(Sz(22, s), Sz(19, s));
            Color fur = new Color(124, 86, 56), dark = new Color(70, 48, 34), light = new Color(214, 196, 170);
            c.FillEllipse(11 * s, 18 * s, 7 * s, 1.3f * s, new Color(0, 0, 0, 60));
            Legs(c, s, frame, new[] { 6f, 8.5f, 13f, 15.5f }, 12, 18, dark);
            c.FillEllipse(10.5f * s, 10 * s, 6.5f * s, 3.6f * s, fur, 0.06f);
            c.FillEllipse(10.5f * s, 11.5f * s, 4.5f * s, 1.6f * s, light);     // qorin
            c.FillPolygon(dark, 0f, new Vector2(4 * s, 8 * s), new Vector2(3 * s, 4.5f * s), new Vector2(5.5f * s, 7.5f * s)); // dikka dum
            bool graze = frame == 3;
            float hx = graze ? 18.5f : 17.5f, hy = graze ? 13.5f : 6f;
            c.FillEllipse(hx * s, hy * s, 2.4f * s, 2.1f * s, fur);
            c.Line(new Vector2((hx - 0.5f) * s, (hy - 1.8f) * s), new Vector2((hx - 3f) * s, (hy - 4.5f) * s), new Color(200, 190, 170)); // shoxlar
            c.Line(new Vector2((hx + 0.5f) * s, (hy - 1.8f) * s), new Vector2((hx - 1.5f) * s, (hy - 4.8f) * s), new Color(200, 190, 170));
            c.FillRect((int)((hx + 0.6f) * s), (int)((hy + 1.6f) * s), Math.Max(1, (int)(1.2f * s)), Math.Max(1, (int)(2.4f * s)), light); // soqol
            c.FillRect((int)((hx + 0.5f) * s), (int)((hy - 0.6f) * s), 1, 1, Ink);
            c.Outline(Ink, 0.4f);
            return c;
        }

        private static Canvas Cow(float s, int frame)
        {
            var c = new Canvas(Sz(32, s), Sz(23, s));
            Color body = new Color(236, 230, 220), patch = new Color(120, 72, 44), dark = new Color(60, 40, 30);
            c.FillEllipse(16 * s, 22 * s, 11 * s, 1.6f * s, new Color(0, 0, 0, 60));
            Legs(c, s, frame, new[] { 7f, 10f, 20f, 23f }, 14, 22, dark);
            c.FillEllipse(15 * s, 11 * s, 10 * s, 5.5f * s, body, 0.04f);
            c.FillBlob(11 * s, 9.5f * s, 3.6f * s, 2.8f * s, patch, 0.06f);   // ola dog'lar
            c.FillBlob(18.5f * s, 12 * s, 3f * s, 2.4f * s, patch, 0.06f);
            c.FillEllipse(17 * s, 15.5f * s, 2.2f * s, 1.2f * s, new Color(232, 160, 160)); // yelin
            c.Line(new Vector2(5 * s, 9 * s), new Vector2(4 * s, 15 * s), dark);            // dum
            bool graze = frame == 3;
            float hx = graze ? 27f : 26f, hy = graze ? 16.5f : 8f;
            c.FillEllipse(hx * s, hy * s, 3.4f * s, 2.8f * s, patch);
            c.FillEllipse((hx + 1.8f) * s, (hy + 1f) * s, 1.8f * s, 1.5f * s, new Color(220, 170, 150)); // tumshuq
            c.Line(new Vector2((hx - 1f) * s, (hy - 2.4f) * s), new Vector2((hx - 3f) * s, (hy - 4.2f) * s), new Color(236, 226, 200)); // shoxlar
            c.Line(new Vector2((hx + 1f) * s, (hy - 2.4f) * s), new Vector2((hx + 2.5f) * s, (hy - 4.4f) * s), new Color(236, 226, 200));
            c.FillRect((int)((hx + 0.2f) * s), (int)((hy - 0.8f) * s), 1, 1, Ink);
            c.Outline(Ink, 0.4f);
            return c;
        }
    }
}
