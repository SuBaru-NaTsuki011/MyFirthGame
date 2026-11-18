using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Environment;
using AndijonSaltanatlar.Units;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// Cho'milish: issiq kunlarda fuqarolar yaqin atrofdagi suvga (hovuz, ariq) borib cho'miladi — chuqur joyda suzadi,
    /// sayoz ariqda kechib yuradi — keyin o'z yo'liga qaytadi. Yozda ko'proq, qishda hech kim. Yo'l topish suvdan ham
    /// o'tadi (kechib yoki suzib), shuning uchun boshqa odamlar ham kerak bo'lsa suvni kesib o'tadi.
    /// </summary>
    public sealed class SwimController
    {
        private enum Phase { Going, Swimming, Returning }

        private sealed class Swimmer
        {
            public CitizenUnit U;
            public List<Vector2> Saved;
            public float SavedPause;
            public Phase Phase;
            public Vector2 Spot;
            public float Time;
        }

        private static readonly string[] Lines = { "Suv zo'r!", "Salqin ekan!", "Kim tezroq suzadi?", "Issiqdan qutuldim", "Suv iliq-ku!" };
        private readonly List<Swimmer> _swimmers = new List<Swimmer>();
        private readonly Random _rng = new Random(77);
        private List<Point> _water;
        private MapManager _waterMap;
        private UnitManager _owner;
        private float _timer = 5f;

        public int Count => _swimmers.Count;
        public string Debug { get { int g = 0, s = 0, r = 0; foreach (Swimmer x in _swimmers) { if (x.Phase == Phase.Going) g++; else if (x.Phase == Phase.Swimming) s++; else r++; } return "SWIM g" + g + " s" + s + " r" + r + " water" + (_water?.Count ?? -1); } }
        /// <summary>Suvdagi birinchi odam (sinov kamerasi uchun).</summary>
        public Unit FirstInWater { get { foreach (Swimmer s in _swimmers) if (s.Phase == Phase.Swimming) return s.U; return null; } }
        /// <summary>Sinov: fasldan qat'i nazar cho'milish (--swim).</summary>
        public bool Force { get; set; }

        public bool IsSwimming(Unit u)
        {
            foreach (Swimmer s in _swimmers) if (s.U == u) return true;
            return false;
        }

        public void Update(float dt, MapManager map, UnitManager units, GameStateManager state, Unit possessed, Func<Unit, bool> busy)
        {
            if (!ReferenceEquals(units, _owner)) { _swimmers.Clear(); _owner = units; }
            if (!ReferenceEquals(map, _waterMap)) { _water = map.WaterTiles(); _waterMap = map; }
            if (_water == null || _water.Count == 0) return;

            Season s = state.Season;
            bool hot = SeasonVisuals.ComfortOf(s, state.Weather.Current, state.Clock.DayFraction) == Comfort.Issiq;
            int max = Force ? 8 : s == Season.Qish ? 0 : s == Season.Yoz ? (hot ? 6 : 4) : 1;

            // Tugaganlarni qaytarish
            for (int i = _swimmers.Count - 1; i >= 0; i--)
            {
                Swimmer sw = _swimmers[i];
                if (!units.Units.Contains(sw.U) || sw.U == possessed || sw.U.ExternallyControlled) { _swimmers.RemoveAt(i); continue; }
                Step(sw, dt, map, max == 0);
                if (sw.Phase == Phase.Returning && sw.Saved.Count > 0 && Vector2.Distance(sw.U.Position, sw.Saved[0]) < 0.8f)
                {
                    Restore(sw);
                    _swimmers.RemoveAt(i);
                }
            }

            _timer -= dt;
            if (_timer > 0f || _swimmers.Count >= max) return;
            _timer = Force ? 0.2f : 6f + (float)_rng.NextDouble() * 8f;
            // Suvga yaqin, bo'sh fuqaroni tanlash
            var pool = new List<CitizenUnit>();
            foreach (Unit u in units.Units)
                if (u is CitizenUnit c && !c.IsDown && !c.Hidden && !c.IsInsideBuilding && !c.ExternallyControlled && c != possessed
                    && c.PatrolPoints.Count >= 2 && !IsSwimming(c) && !(busy?.Invoke(c) ?? false))
                    pool.Add(c);
            if (pool.Count == 0) return;
            CitizenUnit pick = pool[_rng.Next(pool.Count)];
            Point? best = null; float bd = 18f * 18f;
            foreach (Point p in _water)
            {
                float d = Vector2.DistanceSquared(new Vector2(p.X + 0.5f, p.Y + 0.5f), pick.Position);
                if (d < bd) { bd = d; best = p; }
            }
            if (best == null) return;
            var sw2 = new Swimmer { U = pick, Saved = new List<Vector2>(pick.PatrolPoints), SavedPause = pick.PauseTime, Phase = Phase.Going, Spot = new Vector2(best.Value.X + 0.5f, best.Value.Y + 0.5f) };
            Route(pick, map, sw2.Spot);
            _swimmers.Add(sw2);
        }

        private void Step(Swimmer sw, float dt, MapManager map, bool winter)
        {
            CitizenUnit u = sw.U;
            switch (sw.Phase)
            {
                case Phase.Going:
                    if (Vector2.Distance(u.Position, sw.Spot) < 0.6f)
                    {
                        sw.Phase = Phase.Swimming;
                        sw.Time = 0f;
                        // Suv ichida bir necha nuqta bo'ylab suzadi
                        var pts = new List<Vector2> { u.Position };
                        Point c = new Point((int)sw.Spot.X, (int)sw.Spot.Y);
                        for (int dy = -2; dy <= 2; dy++)
                            for (int dx = -2; dx <= 2; dx++)
                                if ((dx != 0 || dy != 0) && map.IsWater(c.X + dx, c.Y + dy) && pts.Count < 5 && _rng.Next(3) == 0)
                                    pts.Add(new Vector2(c.X + dx + 0.5f, c.Y + dy + 0.5f));
                        if (pts.Count < 2) pts.Add(sw.Spot + new Vector2(0.2f, 0.2f));
                        u.PatrolPoints.Clear();
                        u.PatrolPoints.AddRange(pts);
                        u.PauseTime = 1.2f;
                        u.ResetRoute();
                        u.Say(Lines[_rng.Next(Lines.Length)], 3f);
                    }
                    break;
                case Phase.Swimming:
                    sw.Time += dt;
                    if (sw.Time > 9f && sw.Time - dt <= 9f) u.Say(Lines[_rng.Next(Lines.Length)], 3f);
                    if (sw.Time > 22f || winter)
                    {
                        sw.Phase = Phase.Returning;
                        if (sw.Saved.Count > 0) Route(u, map, sw.Saved[0]);
                    }
                    break;
            }
        }

        private static void Route(CitizenUnit u, MapManager map, Vector2 to)
        {
            u.PatrolPoints.Clear();
            u.PatrolPoints.Add(u.Position);
            u.PatrolPoints.Add(to);
            PatrolController.RouteUnit(map, u);
            u.PauseTime = 0f;
            u.ResetRoute();
        }

        private static void Restore(Swimmer sw)
        {
            sw.U.PatrolPoints.Clear();
            sw.U.PatrolPoints.AddRange(sw.Saved);
            sw.U.PauseTime = sw.SavedPause;
            sw.U.ResetRoute();
        }
    }
}
