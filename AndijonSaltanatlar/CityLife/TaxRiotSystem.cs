using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Units;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.CityLife
{
    /// <summary>
    /// SOLIQ G'ALAYONI: aholi solig'i 35% yoki undan yuqori bo'lsa, butun xalq ko'chaga chiqadi.
    ///  - Har bir uydan odamlar ketma-ket chiqadi, ko'chadagi fuqarolar ham qo'shiladi
    ///    (cho'ponlar yaylovda qoladi — podani tashlab ketmaydi).
    ///  - Hammasi A* yo'l bilan SAROY OLDIGA to'planadi, shior aytadi va saroyga TOSH OTADI.
    ///  - G'alayon davom etgan har kuni barqarorlik va xalq baxti pasayadi.
    ///  - Soliq 35% dan pastga tushirilsa — xalq tarqaladi: uylardan chiqqanlar uyiga qaytadi,
    ///    ko'cha aholisi kundalik yo'liga qaytadi.
    ///  - Bosqin boshlansa g'alayon darhol to'xtaydi (xalq uylariga yashirinadi).
    /// </summary>
    public sealed class TaxRiotSystem
    {
        public const int Threshold = 30;
        private const int MaxFromHouses = 44;
        private const int PerHouse = 2;

        private enum Mode { Waiting, Marching, Protesting, Returning }

        private sealed class Rioter
        {
            public CitizenUnit Unit;
            public bool Spawned;          // uydan chiqqan vaqtinchalik figura (tarqalganda olib tashlanadi)
            public Vector2 Home;          // qaytish nuqtasi (uy eshigi yoki patrul yo'li)
            public Vector2 Spot;          // saroy oldidagi joyi
            public List<Point> Path;
            public int PathIndex;
            public Mode Mode;
            public float Delay;
            public float ThrowTimer;
            public float ShoutTimer;
            /// <summary>Askar yetib kelib to'xtatgan — endi tosh otmaydi.</summary>
            public bool Restrained;
        }

        // ---- Askarlar g'alayonchilarni to'xtatadi: har askar bitta odamga boradi; askar yetmagan odamlar otishda davom etadi ----
        private sealed class GuardJob
        {
            public Unit U;
            public Rioter R;
            public List<Point> Path;
            public int Idx;
            public bool Arrived;
            public bool Returning;
            public Vector2 Home;
            public float Talk;
        }
        private readonly List<GuardJob> _guards = new List<GuardJob>();
        private float _assignTimer;
        /// <summary>Askarlar g'alayonchilarning qancha qismini to'xtata oladi (qolganlari tosh otaveradi).</summary>
        private const float GuardShare = 0.6f;
        private static readonly string[] GuardLines = { "To'xta! Tosh otma!", "Tarqalinglar!", "Shohning amri — tinch turing!", "Qo'lingni tushir!" };
        private static readonly string[] CalmLines = { "Xo'p, xo'p... to'xtadim", "Mayli, otmayman", "Bizni eshitishsin, xolos" };

        /// <summary>Hozir g'alayonchini to'xtatib turgan askarlar soni.</summary>
        public int GuardsHolding { get { int n = 0; foreach (GuardJob g in _guards) if (g.Arrived && !g.Returning) n++; return n; } }

        private sealed class Stone
        {
            public Vector2 From, To;      // tile koordinatalari
            public float H0, H1;          // boshlang'ich / nishondagi balandlik (world piksel)
            public float T, Duration, Arc;
        }

        private sealed class Dust
        {
            public Vector2 World;
            public float T;
            public float Seed;
        }

        private static readonly string[] Shouts =
        {
            "Soliqni kamaytiring!", "Zulmga yo'q!", "Bolalarimiz och!", "Adolat bering, hazratim!",
            "Biz qul emasmiz!", "Soliq — bo'yinga sirtmoq!", "Xazina to'ldi, qozon bo'sh!", "Haqqimizni bering!"
        };

        private readonly Random _rng = new Random(GameConfig.WorldSeed + 351);
        private readonly List<Rioter> _rioters = new List<Rioter>();
        private readonly List<Stone> _stones = new List<Stone>();
        private readonly List<Dust> _dust = new List<Dust>();
        private Building _palace;
        private List<Vector2> _spots = new List<Vector2>();
        private int _nextSpot;
        private int _lastDay = -1;

        /// <summary>G'alayon davom etmoqda (xalq saroy tomon yo'lda yoki saroy oldida).</summary>
        public bool Active { get; private set; }

        /// <summary>Hozir saroy oldida turgan (tosh otayotgan) odamlar soni.</summary>
        public int Protesters
        {
            get
            {
                int n = 0;
                foreach (Rioter r in _rioters) if (r.Mode == Mode.Protesting && !r.Restrained) n++;
                return n;
            }
        }

        public string DebugSummary
        {
            get
            {
                int[] m = new int[4];
                foreach (Rioter r in _rioters) m[(int)r.Mode]++;
                int noPath = 0;
                foreach (Rioter r in _rioters) if (r.Mode == Mode.Marching && r.Path == null) noPath++;
                return "W" + m[0] + " M" + m[1] + " P" + m[2] + " R" + m[3] + " nopath" + noPath + " stones" + _stones.Count + " spots" + _spots.Count + " guards" + _guards.Count + " hold" + GuardsHolding;
            }
        }

        /// <summary>Shu soliqda g'alayon boshlanadimi (UI ogohlantirishi uchun).</summary>
        public static bool TaxTooHigh(int populationTax) => populationTax >= Threshold;

        // ==================================================================
        // Update
        // ==================================================================

        /// <param name="dt">O'yin vaqti (pauzada 0).</param>
        /// <param name="suspended">Bosqin yoki boshqa holat — g'alayon to'xtatiladi.</param>
        public void Update(float dt, GameStateManager state, MapManager map, BuildingManager buildings,
                           UnitManager units, Unit possessed, bool suspended)
        {
            if (state == null || map == null || buildings == null || units == null) return;

            if (suspended)
            {
                if (_rioters.Count > 0 || Active) Abort(units);
                UpdateProjectiles(dt);
                return;
            }

            Building palace = FindPalace(buildings);
            bool want = TaxTooHigh(state.Policy.PopulationTax) && palace != null;
            if (want && !Active) Begin(state, map, buildings, units, palace, possessed);
            else if (!want && Active) End(state, map);

            if (dt <= 0f) return;

            // Boshqarib olingan yoki dunyodan yo'qolgan figuralarni ro'yxatdan chiqarish
            for (int i = _rioters.Count - 1; i >= 0; i--)
            {
                Rioter r = _rioters[i];
                if (!units.Units.Contains(r.Unit)) { _rioters.RemoveAt(i); continue; }
                if (r.Unit == possessed)
                {
                    r.Unit.SetActivity(CitizenActivityIcon.None);
                    _rioters.RemoveAt(i); // CombatSystem o'zi boshqaradi
                }
            }

            for (int i = _rioters.Count - 1; i >= 0; i--)
                if (UpdateRioter(_rioters[i], dt, map, units)) _rioters.RemoveAt(i);

            UpdateGuards(dt, map, units, possessed);

            UpdateProjectiles(dt);

            // Har o'yin kuni: barqarorlik va baxt pasayadi
            if (Active && state.Day != _lastDay)
            {
                _lastDay = state.Day;
                int n = Protesters;
                if (n > 0)
                {
                    var kingdom = state.World.PlayerKingdom;
                    kingdom.Stability = Math.Max(0, kingdom.Stability - 6);
                    state.Pop.AdjustHappiness(-3f);
                    string text = "Xalq saroy oldida tosh otmoqda! (" + n + " kishi)  Barqarorlik: " + kingdom.Stability
                                + ". Aholi solig'ini " + Threshold + "% dan pastga tushiring.";
                    state.Life.RecordEvent("tax_riot", state.Day, text);
                    state.ShowMessage(text, isError: true);
                }
            }
        }

        private static Building FindPalace(BuildingManager buildings)
        {
            foreach (Building b in buildings.Buildings)
                if (b.Type == BuildingType.MainHall && b.BuildProgress >= 1f) return b;
            return null;
        }

        // ==================================================================
        // Boshlanish / tugash
        // ==================================================================

        private void Begin(GameStateManager state, MapManager map, BuildingManager buildings, UnitManager units,
                           Building palace, Unit possessed)
        {
            Active = true;
            _lastDay = state.Day; // birinchi jarima ertasiga — xalq avval yig'ilib olsin
            _palace = palace;
            _spots = GatherSpots(map, palace);
            _nextSpot = 0;

            // Qaytayotganlar yana saroy tomon buriladi
            foreach (Rioter r in _rioters)
            {
                r.Mode = Mode.Waiting;
                r.Delay = (float)_rng.NextDouble() * 1.5f;
                r.Spot = NextSpot();
            }

            // 1) Uylardan chiqadiganlar
            int spawned = 0;
            foreach (Building b in buildings.Buildings)
            {
                if (b.Type != BuildingType.House || b.BuildProgress < 1f) continue;
                Point? door = DoorTile(map, b);
                if (!door.HasValue) continue;
                int count = Math.Max(1, Math.Min(PerHouse, b.PopulationCapacity / 3));
                for (int i = 0; i < count && spawned < MaxFromHouses; i++, spawned++)
                {
                    Vector2 doorPos = new Vector2(door.Value.X + 0.5f, door.Value.Y + 0.5f);
                    int kind = PickKind();
                    var unit = units.Add(new CitizenUnit(new[] { doorPos }, kind, 0.85f + (float)_rng.NextDouble() * 0.25f));
                    unit.ExternallyControlled = true;
                    unit.Hidden = true; // hali uy ichida
                    unit.DriveIdle();
                    _rioters.Add(new Rioter
                    {
                        Unit = unit, Spawned = true, Home = doorPos, Spot = NextSpot(), Mode = Mode.Waiting,
                        Delay = 0.3f + (float)_rng.NextDouble() * 7f
                    });
                }
            }

            // 2) Ko'chadagi aholi ham qo'shiladi
            foreach (Unit u in units.Units)
            {
                if (!(u is CitizenUnit c) || c == possessed || c.IsDown || c.Hidden || c.ExternallyControlled) continue;
                if (c.Kind == CitizenArt.Shepherd) continue;
                if (IsRioter(c)) continue;
                c.ExternallyControlled = true;
                c.DriveIdle();
                _rioters.Add(new Rioter
                {
                    Unit = c, Spawned = false, Home = c.NextPatrolPoint, Spot = NextSpot(), Mode = Mode.Waiting,
                    Delay = (float)_rng.NextDouble() * 2.5f
                });
            }

            string text = "Aholi solig'i " + state.Policy.PopulationTax + "%! Xalq uylaridan chiqib saroy oldiga to'planmoqda!";
            state.Life.RecordEvent("tax_riot_started", state.Day, text);
            state.ShowMessage(text, isError: true);
        }

        private void End(GameStateManager state, MapManager map)
        {
            Active = false;
            SendGuardsHome(map);
            foreach (Rioter r in _rioters) BeginReturn(r, map);
            var kingdom = state.World.PlayerKingdom;
            kingdom.Stability = Math.Min(100, kingdom.Stability + 3);
            string text = "Soliq kamaytirildi — xalq tinchlanib, uy-uyiga tarqalmoqda.";
            state.Life.RecordEvent("tax_riot_ended", state.Day, text);
            state.ShowMessage(text);
        }

        /// <summary>Darhol to'xtatish (bosqin): uydan chiqqanlar yo'qoladi, ko'cha aholisi bo'shatiladi.</summary>
        private void Abort(UnitManager units)
        {
            Active = false;
            foreach (GuardJob g in _guards) ReleaseGuard(g);
            _guards.Clear();
            foreach (Rioter r in _rioters)
            {
                if (r.Spawned) units.Remove(r.Unit);
                else Release(r.Unit);
            }
            _rioters.Clear();
        }

        // ==================================================================
        // Askarlar
        // ==================================================================

        private static bool IsSoldier(Unit u) =>
            (u is GuardUnit) || (u is PatrolUnit && !(u is CitizenUnit) && !(u is GladiatorUnit));

        private void UpdateGuards(float dt, MapManager map, UnitManager units, Unit possessed)
        {
            // Yaroqsiz topshiriqlar
            for (int i = _guards.Count - 1; i >= 0; i--)
            {
                GuardJob g = _guards[i];
                bool lost = !units.Units.Contains(g.U) || g.U.IsDown || g.U == possessed;
                bool targetGone = !g.Returning && (!_rioters.Contains(g.R) || g.R.Mode == Mode.Returning);
                if (lost) { ReleaseGuard(g); _guards.RemoveAt(i); continue; }
                if (targetGone) StartGuardReturn(g, map);
            }

            // Yangi askarlarni yuborish (g'alayon davom etayotgan bo'lsa)
            _assignTimer -= dt;
            if (Active && _assignTimer <= 0f)
            {
                _assignTimer = 1.5f;
                int active = 0;
                foreach (Rioter r in _rioters) if (r.Mode == Mode.Protesting || r.Mode == Mode.Marching) active++;
                int busy = 0;
                foreach (GuardJob g in _guards) if (!g.Returning) busy++;
                int want = (int)Math.Ceiling(active * GuardShare);
                Vector2 pc = PalaceCenter();
                var free = new List<Unit>();
                foreach (Unit u in units.Units)
                {
                    if (!IsSoldier(u) || u == possessed || u.IsDown || u.ExternallyControlled) continue;
                    if (Vector2.Distance(u.Position, pc) > 40f) continue;
                    bool taken = false;
                    foreach (GuardJob g in _guards) if (g.U == u) { taken = true; break; }
                    if (!taken) free.Add(u);
                }
                free.Sort((a, b) => Vector2.DistanceSquared(a.Position, pc).CompareTo(Vector2.DistanceSquared(b.Position, pc)));
                foreach (Unit u in free)
                {
                    if (busy >= want) break;
                    Rioter target = null; float best = float.MaxValue;
                    foreach (Rioter r in _rioters)
                    {
                        if (r.Restrained || (r.Mode != Mode.Protesting && r.Mode != Mode.Marching)) continue;
                        bool covered = false;
                        foreach (GuardJob g in _guards) if (g.R == r && !g.Returning) { covered = true; break; }
                        if (covered) continue;
                        float d = Vector2.DistanceSquared(r.Unit.Position, u.Position);
                        if (d < best) { best = d; target = r; }
                    }
                    if (target == null) break;
                    var job = new GuardJob { U = u, R = target, Home = u.Position, Talk = (float)_rng.NextDouble() * 2f };
                    job.Path = PathTo(map, u.Position, target.Unit.Position);
                    job.Idx = job.Path != null && job.Path.Count > 1 ? 1 : 0;
                    u.ExternallyControlled = true;
                    _guards.Add(job);
                    busy++;
                }
            }

            // Harakat
            for (int i = _guards.Count - 1; i >= 0; i--)
            {
                GuardJob g = _guards[i];
                Unit u = g.U;
                if (g.Returning)
                {
                    if (FollowPath(u, g, dt, 1.2f) && StepToUnit(u, g.Home, dt, 1.2f)) { ReleaseGuard(g); _guards.RemoveAt(i); }
                    continue;
                }
                Vector2 rp = g.R.Unit.Position;
                if (!g.Arrived)
                {
                    float d = Vector2.Distance(u.Position, rp);
                    if (d < 0.75f)
                    {
                        g.Arrived = true;
                        g.R.Restrained = true;
                        g.R.ShoutTimer = 1.5f;
                        u.DriveIdle();
                        u.SetFacing(rp - u.Position);
                       
                        g.R.Unit.SetFacing(u.Position - rp);
                        continue;
                    }
                    // Yo'l bo'ylab, oxirida to'g'ridan-to'g'ri g'alayonchi yoniga
                    if (FollowPath(u, g, dt, 1.7f))
                    {
                        Vector2 dir = rp - u.Position;
                        Vector2 stop = rp - (dir.LengthSquared() > 0.0001f ? Vector2.Normalize(dir) * 0.6f : Vector2.Zero);
                        StepToUnit(u, stop, dt, 1.6f);
                        if (Vector2.Distance(u.Position, stop) < 0.08f) { g.Arrived = false; u.Position = stop; g.R.Restrained = true; g.Arrived = true; u.DriveIdle(); u.SetFacing(rp - u.Position); }
                    }
                }
                else
                {
                    u.DriveIdle();
                    u.SetFacing(rp - u.Position);
                    g.Talk -= dt;
                    if (g.Talk <= 0f) { g.Talk = 12f + (float)_rng.NextDouble() * 10f; }
                }
            }
        }

        private void StartGuardReturn(GuardJob g, MapManager map)
        {
            if (g.R != null) g.R.Restrained = false;
            g.Returning = true;
            g.Arrived = false;
            g.Path = PathTo(map, g.U.Position, g.Home);
            g.Idx = g.Path != null && g.Path.Count > 1 ? 1 : 0;
        }

        private void SendGuardsHome(MapManager map)
        {
            foreach (GuardJob g in _guards) if (!g.Returning) StartGuardReturn(g, map);
        }

        private static void ReleaseGuard(GuardJob g)
        {
            g.U.ExternallyControlled = false;
            g.U.DriveIdle();
        }

        /// <returns>true — yo'l tugadi.</returns>
        private static bool FollowPath(Unit u, GuardJob g, float dt, float speed)
        {
            if (g.Path == null) return true;
            float step = speed * dt;
            while (g.Idx < g.Path.Count)
            {
                Point tile = g.Path[g.Idx];
                Vector2 target = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
                Vector2 delta = target - u.Position;
                float distance = delta.Length();
                if (distance <= MathF.Max(0.03f, step)) { u.Position = target; step -= distance; g.Idx++; continue; }
                u.DriveMove(delta, speed);
                u.Position += delta / distance * step;
                return false;
            }
            return true;
        }

        private static bool StepToUnit(Unit u, Vector2 to, float dt, float speed)
        {
            Vector2 d = to - u.Position;
            float dist = d.Length();
            if (dist < 0.05f) { u.DriveIdle(); return true; }
            u.DriveMove(d, speed);
            u.Position += d / dist * MathF.Min(dist, speed * dt);
            return false;
        }

        private bool IsRioter(CitizenUnit c)
        {
            foreach (Rioter r in _rioters) if (r.Unit == c) return true;
            return false;
        }

        private int PickKind()
        {
            int[] kinds = { CitizenArt.Citizen, CitizenArt.Citizen, CitizenArt.Craftsman, CitizenArt.Farmer, CitizenArt.Buyer, CitizenArt.Merchant };
            return kinds[_rng.Next(kinds.Length)];
        }

        private static Point? DoorTile(MapManager map, Building b)
        {
            Point front = b.FrontEntranceTile;
            if (map.IsWalkable(front.X, front.Y)) return front;
            for (int y = b.Position.Y - 1; y <= b.Position.Y + b.Size.Y; y++)
                for (int x = b.Position.X - 1; x <= b.Position.X + b.Size.X; x++)
                {
                    bool edge = x == b.Position.X - 1 || x == b.Position.X + b.Size.X || y == b.Position.Y - 1 || y == b.Position.Y + b.Size.Y;
                    if (edge && map.IsWalkable(x, y)) return new Point(x, y);
                }
            return null;
        }

        /// <summary>
        /// Saroy oldidagi to'planish joylari: avval old (darvoza) tomondagi maydon, kerak bo'lsa boshqa tomonlar.
        /// Eshikka yaqin joylar birinchi.
        /// </summary>
        private List<Vector2> GatherSpots(MapManager map, Building p)
        {
            var front = new List<Point>();
            var around = new List<Point>();
            Point door = p.FrontEntranceTile;
            int x0 = p.Position.X, y0 = p.Position.Y, x1 = p.Position.X + p.Size.X - 1, y1 = p.Position.Y + p.Size.Y - 1;
            for (int y = y0 - 5; y <= y1 + 6; y++)
                for (int x = x0 - 5; x <= x1 + 5; x++)
                {
                    if (x >= x0 && x <= x1 && y >= y0 && y <= y1) continue;
                    if (!map.IsWalkable(x, y) || map.GetOccupant(x, y) != null) continue;
                    int dx = x < x0 ? x0 - x : x > x1 ? x - x1 : 0;
                    int dy = y < y0 ? y0 - y : y > y1 ? y - y1 : 0;
                    int ring = Math.Max(dx, dy);
                    if (ring < 1) continue;
                    if (y > y1 && x >= x0 - 2 && x <= x1 + 2 && ring <= 6) front.Add(new Point(x, y));
                    else if (ring <= 4) around.Add(new Point(x, y));
                }
            int D(Point a) => Math.Abs(a.X - door.X) + Math.Abs(a.Y - door.Y);
            front.Sort((a, b) => D(a).CompareTo(D(b)));
            around.Sort((a, b) => D(a).CompareTo(D(b)));
            var spots = new List<Vector2>();
            foreach (Point t in front) spots.Add(new Vector2(t.X + 0.5f, t.Y + 0.5f));
            foreach (Point t in around) spots.Add(new Vector2(t.X + 0.5f, t.Y + 0.5f));
            if (spots.Count == 0) spots.Add(new Vector2(door.X + 0.5f, door.Y + 1.5f));
            return spots;
        }

        private Vector2 NextSpot()
        {
            int i = _nextSpot++;
            Vector2 s = _spots[i % _spots.Count];
            // Joy to'lsa, keyingi aylanada biroz siljigan holda (olomon zichlashadi)
            float j = i >= _spots.Count ? 0.32f : 0.18f;
            return s + new Vector2(((float)_rng.NextDouble() - 0.5f) * 2f * j, ((float)_rng.NextDouble() - 0.5f) * 2f * j);
        }

        // ==================================================================
        // Har bir g'alayonchi
        // ==================================================================

        /// <returns>true — ro'yxatdan chiqarish kerak.</returns>
        private bool UpdateRioter(Rioter r, float dt, MapManager map, UnitManager units)
        {
            CitizenUnit u = r.Unit;
            switch (r.Mode)
            {
                case Mode.Waiting:
                    r.Delay -= dt;
                    u.DriveIdle();
                    if (r.Delay > 0f) return false;
                    u.Hidden = false; // eshikdan chiqdi
                    u.SetActivity(CitizenActivityIcon.Revolting);
                    r.Path = PathTo(map, u.Position, r.Spot);
                    r.PathIndex = r.Path != null && r.Path.Count > 1 ? 1 : 0;
                    r.Mode = Mode.Marching;
                    if (_rng.Next(3) == 0) u.Say(Shouts[_rng.Next(Shouts.Length)], 2.5f);
                    return false;

                case Mode.Marching:
                    if (MoveAlong(r, dt, 1.25f))
                    {
                        // Yo'l oxiri — joyiga to'g'ri qadam qo'yib, saroyga yuzlanadi
                        if (StepTo(u, r.Spot, dt, 1.0f))
                        {
                            r.Mode = Mode.Protesting;
                            r.ThrowTimer = 0.5f + (float)_rng.NextDouble() * 2.5f;
                            r.ShoutTimer = 1f + (float)_rng.NextDouble() * 8f;
                        }
                    }
                    return false;

                case Mode.Protesting:
                    u.DriveIdle();
                    if (r.Restrained)
                    {
                        // Askar to'xtatgan: tosh otmaydi, faqat ba'zan noliydi
                        r.ShoutTimer -= dt;
                        if (r.ShoutTimer <= 0f) { r.ShoutTimer = 10f + (float)_rng.NextDouble() * 10f; u.Say(CalmLines[_rng.Next(CalmLines.Length)], 2.5f); }
                        return false;
                    }
                    Vector2 center = PalaceCenter();
                    u.SetFacing(center - u.Position);
                    r.ThrowTimer -= dt;
                    if (r.ThrowTimer <= 0f)
                    {
                        r.ThrowTimer = 1.6f + (float)_rng.NextDouble() * 2.4f;
                        ThrowStone(u);
                    }
                    r.ShoutTimer -= dt;
                    if (r.ShoutTimer <= 0f)
                    {
                        r.ShoutTimer = 6f + (float)_rng.NextDouble() * 10f;
                        u.Say(Shouts[_rng.Next(Shouts.Length)], 2.8f);
                    }
                    return false;

                case Mode.Returning:
                    if (MoveAlong(r, dt, 0.9f))
                    {
                        if (r.Spawned)
                        {
                            units.Remove(u); // uyiga kirdi
                            return true;
                        }
                        Release(u);
                        return true;
                    }
                    return false;
            }
            return false;
        }

        private Vector2 PalaceCenter() => _palace == null ? Vector2.Zero
            : new Vector2(_palace.Position.X + _palace.Size.X / 2f, _palace.Position.Y + _palace.Size.Y / 2f);

        private static List<Point> PathTo(MapManager map, Vector2 from, Vector2 to)
        {
            Point a = new Point((int)MathF.Floor(from.X), (int)MathF.Floor(from.Y));
            Point b = new Point((int)MathF.Floor(to.X), (int)MathF.Floor(to.Y));
            return Pathfinder.FindPath(map, a, b, 9000);
        }

        /// <returns>true — yo'l tugadi (yoki yo'l topilmadi).</returns>
        private static bool MoveAlong(Rioter r, float dt, float hurry)
        {
            CitizenUnit u = r.Unit;
            if (r.Path == null) return true;
            float speed = MathF.Max(0.9f, u.Speed * hurry);
            float step = speed * dt;
            while (r.PathIndex < r.Path.Count)
            {
                Point tile = r.Path[r.PathIndex];
                Vector2 target = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
                Vector2 delta = target - u.Position;
                float distance = delta.Length();
                if (distance <= MathF.Max(0.03f, step))
                {
                    u.Position = target;
                    r.PathIndex++;
                    step -= distance;
                    if (step <= 0f) return false;
                    continue;
                }
                u.DriveMove(delta, speed);
                u.Position += delta / distance * step;
                return false;
            }
            u.DriveIdle();
            return true;
        }

        private static bool StepTo(CitizenUnit u, Vector2 to, float dt, float speed)
        {
            Vector2 d = to - u.Position;
            float dist = d.Length();
            if (dist < 0.04f) { u.DriveIdle(); return true; }
            float step = MathF.Min(dist, speed * dt);
            u.DriveMove(d, speed);
            u.Position += d / dist * step;
            return false;
        }

        private static void BeginReturnPath(Rioter r, MapManager map)
        {
            r.Path = PathTo(map, r.Unit.Position, r.Home);
            r.PathIndex = r.Path != null && r.Path.Count > 1 ? 1 : 0;
        }

        private void BeginReturn(Rioter r, MapManager map)
        {
            r.Mode = Mode.Returning;
            r.Unit.SetActivity(CitizenActivityIcon.None);
            if (r.Unit.Hidden) { r.Path = null; return; } // hali uydan chiqmagan edi
            BeginReturnPath(r, map);
            if (_rng.Next(4) == 0) r.Unit.Say("Xudoga shukur, soliq kamaydi!", 2.5f);
        }

        private static void Release(CitizenUnit u)
        {
            u.Hidden = false;
            u.SetActivity(CitizenActivityIcon.None);
            u.DriveIdle();
            u.ExternallyControlled = false;
        }

        // ==================================================================
        // Toshlar
        // ==================================================================

        private void ThrowStone(CitizenUnit u)
        {
            if (_palace == null) return;
            // Otish harakati: qo'lni ko'tarib, oldinga silkitish
            u.AttackKind = Unit.AttackOverhead;
            u.AttackTimer = Unit.SwingTime;
            // Nishon: saroyning old devori va tomi
            float tx = _palace.Position.X + 0.4f + (float)_rng.NextDouble() * (_palace.Size.X - 0.8f);
            float ty = _palace.Position.Y + _palace.Size.Y - 0.2f - (float)_rng.NextDouble() * 1.2f;
            Vector2 to = new Vector2(tx, ty);
            float dist = Vector2.Distance(u.Position, to);
            _stones.Add(new Stone
            {
                From = u.Position, To = to, H0 = 18f, H1 = 14f + (float)_rng.NextDouble() * 34f,
                T = -0.12f, // qo'l ko'tarilguncha kechikish
                Duration = 0.45f + dist * 0.09f, Arc = 18f + dist * 3.5f
            });
        }

        private void UpdateProjectiles(float dt)
        {
            if (dt <= 0f) return;
            for (int i = _stones.Count - 1; i >= 0; i--)
            {
                Stone s = _stones[i];
                s.T += dt;
                if (s.T >= s.Duration)
                {
                    _dust.Add(new Dust { World = StoneWorld(s, 1f), Seed = (float)_rng.NextDouble() * 100f });
                    _stones.RemoveAt(i);
                }
            }
            for (int i = _dust.Count - 1; i >= 0; i--)
            {
                _dust[i].T += dt;
                if (_dust[i].T > 0.5f) _dust.RemoveAt(i);
            }
        }

        private static Vector2 StoneWorld(Stone s, float k)
        {
            Vector2 ground = IsoMath.TileToWorld(Vector2.Lerp(s.From, s.To, k));
            float h = MathHelper.Lerp(s.H0, s.H1, k) + s.Arc * 4f * k * (1f - k);
            return ground - new Vector2(0, h);
        }

        /// <summary>Uchayotgan toshlarning yerdagi soyalari (binolardan oldin chiziladi).</summary>
        public void DrawShadows(SpriteBatch sb, Texture2D px)
        {
            foreach (Stone s in _stones)
            {
                if (s.T < 0f) continue;
                float k = MathHelper.Clamp(s.T / s.Duration, 0f, 1f);
                Vector2 g = IsoMath.TileToWorld(Vector2.Lerp(s.From, s.To, k));
                sb.Draw(px, new Rectangle((int)g.X - 2, (int)g.Y - 1, 4, 2), new Color(0, 0, 0, 70));
            }
        }

        /// <summary>Toshlar va zarba changi — hamma narsaning ustida (toshlar saroy tepasidan uchadi).</summary>
        public void DrawAir(SpriteBatch sb, Texture2D px)
        {
            foreach (Stone s in _stones)
            {
                if (s.T < 0f) continue;
                float k = MathHelper.Clamp(s.T / s.Duration, 0f, 1f);
                // Uchish izi (havodagi chang chizig'i)
                for (int tr = 1; tr <= 3; tr++)
                {
                    float kk = k - tr * 0.06f;
                    if (kk <= 0f) break;
                    Vector2 tw = StoneWorld(s, kk);
                    sb.Draw(px, new Rectangle((int)tw.X - 1, (int)tw.Y - 1, 2, 2), new Color(210, 196, 170, 150 - tr * 40));
                }
                Vector2 w = StoneWorld(s, k);
                int x = (int)MathF.Round(w.X), y = (int)MathF.Round(w.Y);
                Color dark = new Color(46, 40, 38), body = new Color(108, 98, 90), lite = new Color(176, 166, 150);
                sb.Draw(px, new Rectangle(x - 3, y - 2, 6, 4), dark);
                sb.Draw(px, new Rectangle(x - 2, y - 3, 4, 6), dark);
                sb.Draw(px, new Rectangle(x - 2, y - 2, 4, 4), body);
                sb.Draw(px, new Rectangle(x - 1, y - 2, 2, 1), lite);
            }
            foreach (Dust d in _dust)
            {
                float t = d.T / 0.5f;
                int a = (int)(170 * (1f - t));
                for (int i = 0; i < 6; i++)
                {
                    float ang = d.Seed + i * 1.047f;
                    float r = 2f + t * 7f;
                    Vector2 p = d.World + new Vector2(MathF.Cos(ang) * r, MathF.Sin(ang) * r * 0.6f - t * 3f);
                    int sz = i % 2 == 0 ? 2 : 1;
                    sb.Draw(px, new Rectangle((int)p.X, (int)p.Y, sz, sz), new Color(200, 184, 150, a));
                }
                if (t < 0.25f) // chaqnash: bo'lakchalar
                    sb.Draw(px, new Rectangle((int)d.World.X - 1, (int)d.World.Y - 1, 3, 3), new Color(120, 110, 100, 220));
            }
        }
    }
}
