using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Units;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AndijonSaltanatlar.Combat
{
    /// <summary>
    /// Jang tizimi: "ICHIGA KIRISH" (istalgan jangchi yoki fuqaroni o'yinchi o'zi boshqaradi) va
    /// "BOSQIN" (shaharga tashqaridan dushman hujumi, himoyachilarning avtomatik jangi).
    ///
    /// Asosiy qoidalar:
    ///  - Unit boshqarilayotganda (o'yinchi yoki AI) uning o'z xatti-harakati (patrul/post) to'xtaydi
    ///    va jang tugagach o'z joyiga yo'l topib (A*) qaytadi — patrul yo'nalishlari buzilmaydi.
    ///  - Shahar himoyachilari va aholi o'lmaydi: HP tugasa yiqiladi, bosqin tugagach o'rnidan turadi.
    ///    (Fuqaro figuralari PopulationManager'dagi haqiqiy fuqarolarga bog'langan — ro'yxatdan o'chirilmaydi.)
    ///  - Bosqinchilar vaqtinchalik: yengilganda dunyodan olib tashlanadi, Save'ga yozilmaydi.
    ///  - Jang real vaqtda kechadi (o'yin tezligi x2/x4 ga bog'liq emas), pauzada to'xtaydi.
    /// </summary>
    public sealed class CombatSystem
    {
        // --- Sozlamalar ---
        private const float PlayerWalkSpeed = 2.5f;     // tile/s
        private const float PlayerSprintSpeed = 3.7f;
        private const float PlayerAttackCooldown = 0.42f;
        private const float PlayerDamageMultiplier = 1.6f;
        private const float DefenderSpeed = 1.8f;
        private const float GuardAggro = 7f;            // postdagi soqchi shu radiusda jangga kiradi
        private const float PatrolAggro = 11f;
        private const float DisengageRange = 15f;
        private const float RaiderAggro = 6f;
        private const float RaiderPossessedAggro = 9f;
        private static readonly Vector2 CityCenter = new Vector2(31.5f, 31.5f);

        private readonly MapManager _map;
        private readonly UnitManager _units;
        private readonly GameStateManager _state;
        private readonly Random _rng = new Random();

        private enum AgentMode { Possessed, Fighting, Raiding, Returning, Escort }

        private sealed class Agent
        {
            public Unit Unit;
            public AgentMode Mode;
            public Vector2 Home;
            public Unit Target;
            public float ThinkTimer;
            public float LootTimer;
            public List<Point> Path;
            public int PathIndex;
            public Point PathGoal;
            public float RepathTimer;
            public Vector2 EscortOffset;
        }

        // ==================================================================
        // SHOH SHAHARDA (saroydan chiqish, soqchilar hamrohligi)
        // ==================================================================

        private ShahUnit _shah;

        /// <summary>Shoh hozir shaharda (saroydan tashqarida) yuribdimi.</summary>
        public bool ShahInWorld => _shah != null;

        /// <summary>
        /// Shohni saroy eshigi oldida dunyoga chiqaradi va o'yinchi uni boshqaradi.
        /// Saroy yaqinidagi ikki soqchi shohga hamroh bo'ladi.
        /// </summary>
        public ShahUnit SpawnShahAndPossess(Vector2 palaceDoor)
        {
            if (_shah == null)
            {
                _shah = _units.Add(new ShahUnit(palaceDoor));
                _agents[_shah] = new Agent { Unit = _shah, Mode = AgentMode.Possessed, Home = palaceDoor };

                // Eng yaqin ikki soqchi (avval nayzalilar) — hamrohlar
                var guards = new List<Unit>();
                foreach (Unit u in _units.Units)
                    if (IsSoldier(u) && !u.IsDown && u != Possessed && Vector2.Distance(u.Position, palaceDoor) < 16f) guards.Add(u);
                guards.Sort((a, b) =>
                {
                    int pa = a is SpearmanUnit ? 0 : 1, pb = b is SpearmanUnit ? 0 : 1;
                    return pa != pb ? pa.CompareTo(pb) : Vector2.Distance(a.Position, palaceDoor).CompareTo(Vector2.Distance(b.Position, palaceDoor));
                });
                Vector2[] offsets = { new Vector2(-0.9f, 0.5f), new Vector2(0.5f, -0.9f) };
                for (int i = 0; i < guards.Count && i < 2; i++)
                {
                    Unit g = guards[i];
                    if (!_agents.TryGetValue(g, out Agent ga))
                    {
                        ga = new Agent { Unit = g, Home = g.Position };
                        _agents[g] = ga;
                    }
                    g.ExternallyControlled = true;
                    ga.Mode = AgentMode.Escort;
                    ga.Target = _shah;
                    ga.EscortOffset = offsets[i];
                    ga.Path = null;
                }
            }
            TryPossess(_shah);
            _state.ShowMessage("Shoh " + ShahUnit.ShahName + " hazratlari shahar sayriga chiqdilar! Soqchilar hamroh.");
            return _shah;
        }

        private void UpdateEscort(Agent a, float dt)
        {
            Unit lord = a.Target;
            if (lord == null || lord.IsDown || !_units.Units.Contains(lord)) { BeginReturn(a); return; }
            Vector2 goal = lord.Position + a.EscortOffset;
            if (!Walk(goal)) goal = lord.Position;
            float dist = Vector2.Distance(a.Unit.Position, goal);
            float speed = dist > 3f ? 4.0f : dist > 1.2f ? 3.0f : 1.6f; // orqada qolsa yuguradi
            if (MoveAgent(a, goal, speed, dt, 0.35f)) a.Unit.SetFacing(lord.FacingTile);
        }

        private readonly Dictionary<Unit, Agent> _agents = new Dictionary<Unit, Agent>();
        private readonly List<RaiderUnit> _raiders = new List<RaiderUnit>();
        private readonly Dictionary<Unit, float> _corpseTimers = new Dictionary<Unit, float>();
        private readonly Dictionary<Unit, float> _reviveTimers = new Dictionary<Unit, float>();

        private struct FloatText { public Vector2 World; public string Text; public Color Color; public float T; }
        private readonly List<FloatText> _floats = new List<FloatText>();

        // --- Bosqin holati ---
        public bool RaidActive { get; private set; }
        public bool RaidIncoming => _warnTimer > 0f;
        private float _warnTimer;
        private int _raidNumber;
        private int _raidTotal;
        private int _lootStolen;
        private int _gateIndex;
        private string _resultText;
        private bool _resultVictory;
        private float _resultTimer;
        private float _defenderThink;

        private static readonly string[] GateNames = { "Shimoliy", "Janubiy", "G'arbiy", "Sharqiy" };

        private bool _justPossessed;

        /// <summary>O'yinchi hozir boshqarayotgan unit (yoki null).</summary>
        public Unit Possessed { get; private set; }

        public CombatSystem(MapManager map, UnitManager units, GameStateManager state)
        {
            _map = map;
            _units = units;
            _state = state;
        }

        // ==================================================================
        // ICHIGA KIRISH
        // ==================================================================

        public bool TryPossess(Unit u)
        {
            if (u == null)
            {
                _state.ShowMessage("Avval jangchi yoki fuqaroni tanlang (ustiga bosing), keyin F", true);
                return false;
            }
            if (u.IsDown)
            {
                _state.ShowMessage(u.Name + " yiqilgan — boshqasini tanlang", true);
                return false;
            }
            if (Possessed != null) Release(false);

            Possessed = u;
            _justPossessed = true; // shu kadrdagi F/Q/E bosilishi darhol chiqarib yubormasin
            u.ExternallyControlled = true;
            u.DriveIdle();
            if (_agents.TryGetValue(u, out Agent a)) a.Mode = AgentMode.Possessed; // uy joyi saqlanadi
            else _agents[u] = new Agent { Unit = u, Mode = AgentMode.Possessed, Home = u.Position };
            _state.Select(u);
            _state.ShowMessage("Siz endi: " + u.Name + ".  WASD — yurish, Space/sichqoncha — zarba, F — chiqish");
            return true;
        }

        /// <summary>Boshqaruvni AI'ga qaytaradi: bosqin paytida jangga, aks holda o'z joyiga.</summary>
        public void Release(bool announce = true)
        {
            Unit u = Possessed;
            if (u == null) return;
            Possessed = null;
            if (_agents.TryGetValue(u, out Agent a))
            {
                if (u is RaiderUnit) a.Mode = AgentMode.Raiding;
                else if (RaidActive && IsSoldier(u)) { a.Mode = AgentMode.Fighting; a.Target = null; }
                else BeginReturn(a);
            }
            u.DriveIdle();
            if (u is ShahUnit) _state.ShowMessage("Shoh hazratlari saroyga qaytmoqdalar...");
            else if (announce) _state.ShowMessage("Kuzatuv rejimi. Jangchini tanlab F bosing — yana ichiga kirasiz");
        }

        private void CyclePossession(int dir)
        {
            if (Possessed == null) return;
            var list = new List<Unit>();
            foreach (Unit u in _units.Units)
            {
                if (u.IsDown || u.Team != Possessed.Team) continue;
                if (u != Possessed && u is CitizenUnit) continue; // almashtirishda faqat jangchilar
                list.Add(u);
            }
            if (list.Count <= 1) { _state.ShowMessage("Boshqa jangchi yo'q", true); return; }
            int i = list.IndexOf(Possessed);
            int next = ((i + dir) % list.Count + list.Count) % list.Count;
            TryPossess(list[next]);
        }

        private void UpdatePossessed(float dt, InputManager input, CameraManager camera)
        {
            Unit p = Possessed;
            if (p == null) return;
            if (p.IsDown)
            {
                _state.ShowMessage(p.Name + " yiqildi! Boshqa jangchini tanlab F bosing", true);
                Release(false);
                return;
            }
            if (input == null || _justPossessed) { _justPossessed = false; p.DriveIdle(); return; }

            if (input.KeyPressed(Keys.F) || input.KeyPressed(Keys.Escape)) { Release(); return; }
            if (input.KeyPressed(Keys.E)) { CyclePossession(1); return; }
            if (input.KeyPressed(Keys.Q)) { CyclePossession(-1); return; }

            // Ekran yo'nalishidagi harakat (W — ekranda yuqoriga) -> tile yo'nalishi
            Vector2 screen = Vector2.Zero;
            if (input.KeyDown(Keys.A) || input.KeyDown(Keys.Left)) screen.X -= 1;
            if (input.KeyDown(Keys.D) || input.KeyDown(Keys.Right)) screen.X += 1;
            if (input.KeyDown(Keys.W) || input.KeyDown(Keys.Up)) screen.Y -= 1;
            if (input.KeyDown(Keys.S) || input.KeyDown(Keys.Down)) screen.Y += 1;

            if (screen != Vector2.Zero && dt > 0f)
            {
                screen.Normalize();
                Vector2 tileDir = IsoMath.WorldToTileF(screen * 32f);
                tileDir.Normalize();
                float speed = input.KeyDown(Keys.LeftShift) || input.KeyDown(Keys.RightShift) ? PlayerSprintSpeed : PlayerWalkSpeed;
                p.DriveMove(tileDir, speed);
                MoveWithCollision(p, tileDir * speed * dt);
            }
            else p.DriveIdle();

            if (_comboWindow > 0f) _comboWindow -= dt;

            bool mouseAttack = input.LeftPressed && !input.MouseOverUI;
            bool attack = input.KeyPressed(Keys.Space) || mouseAttack;
            bool spin = input.KeyPressed(Keys.R) || (input.RightClicked && !input.MouseOverUI);
            if ((attack || spin) && p.AttackCooldown <= 0f && dt > 0f)
            {
                if ((mouseAttack || input.RightClicked) && camera != null)
                {
                    // Sichqoncha tomonga qarab uradi
                    Vector2 aim = IsoMath.WorldToTileF(camera.ScreenToWorld(input.MouseVector)) - p.Position;
                    p.SetFacing(aim);
                }
                int kind;
                if (spin) kind = Unit.AttackSpin;
                else
                {
                    // Kombo: tez-tez bosilsa KESISH -> CHOPISH -> SANCHISH navbatma-navbat
                    _comboStep = _comboWindow > 0f ? (_comboStep + 1) % 3 : 0;
                    kind = _comboStep;
                }
                PlayerSwing(p, kind);
                _comboWindow = 0.85f;
            }
        }

        private int _comboStep;
        private float _comboWindow;

        private static readonly string[] AttackNames = { "KESISH", "CHOPISH", "SANCHISH!", "AYLANMA!" };

        private void PlayerSwing(Unit p, int kind)
        {
            p.AttackKind = kind;
            p.AttackTimer = Unit.SwingTime;
            // Har bir zarba: [kutish, zarar koeffitsienti, qo'shimcha masofa, nishonlar soni, orqaga uloqtirish]
            float cooldown, mult, extra, knock;
            int maxTargets;
            switch (kind)
            {
                case Unit.AttackOverhead: cooldown = 0.42f; mult = 1.3f; extra = 0.35f; maxTargets = 2; knock = 0.2f; break;
                case Unit.AttackThrust: cooldown = 0.5f; mult = 1.7f; extra = 0.8f; maxTargets = 1; knock = 0.5f; break;
                case Unit.AttackSpin: cooldown = 1.0f; mult = 0.95f; extra = 0.55f; maxTargets = 8; knock = 0.35f; break;
                default: cooldown = PlayerAttackCooldown * 0.8f; mult = 1f; extra = 0.35f; maxTargets = 2; knock = 0.08f; break;
            }
            p.AttackCooldown = cooldown;
            if (kind >= Unit.AttackOverhead)
                AddFloat(p.Position + new Vector2(-0.2f, -0.2f), AttackNames[kind], new Color(255, 214, 90));

            int baseDmg = Math.Max(4, p.Attack);
            float reach = p.AttackRange + extra;
            Vector2 facing = p.FacingTile;
            facing.Normalize();

            // Nishonlar: aylanmada — atrofdagi hammasi, boshqalarida — oldidagi eng yaqinlari
            var targets = new List<(Unit u, float d)>();
            foreach (Unit u in _units.Units)
            {
                if (u.Team == p.Team || u.IsDown) continue;
                Vector2 to = u.Position - p.Position;
                float d = to.Length();
                if (d > reach) continue;
                if (kind != Unit.AttackSpin && d > 0.3f && Vector2.Dot(to / d, facing) < -0.1f) continue; // orqadagilarga tegmaydi
                targets.Add((u, d));
            }
            targets.Sort((a, b) => a.d.CompareTo(b.d));
            for (int i = 0; i < targets.Count && i < maxTargets; i++)
            {
                Unit t = targets[i].u;
                int dmg = (int)(baseDmg * PlayerDamageMultiplier * mult) + _rng.Next(0, 5);
                Hit(p, t, dmg);
                if (!t.IsDown && knock > 0f)
                {
                    Vector2 away = t.Position - p.Position;
                    if (away.LengthSquared() > 0.0001f) { away.Normalize(); MoveWithCollision(t, away * knock); }
                }
            }
        }

        // ==================================================================
        // BOSQIN
        // ==================================================================

        /// <summary>Bosqinni boshlaydi (ogohlantirish bilan). manual=true — B tugmasi.</summary>
        public void TriggerRaid(bool manual)
        {
            if (RaidActive || RaidIncoming)
            {
                if (manual) _state.ShowMessage("Bosqin allaqachon davom etmoqda!", true);
                return;
            }
            _gateIndex = _rng.Next(4);
            _warnTimer = manual ? 3f : 6f;
            _state.ShowMessage("DIQQAT! " + GateNames[_gateIndex] + " darvoza tomondan dushman yaqinlashmoqda!", true);
        }

        /// <summary>Sinov/ishga tushirish parametri uchun: ogohlantirishsiz darhol bosqin.</summary>
        public void StartRaidNow(int gate)
        {
            if (RaidActive) return;
            _gateIndex = ((gate % 4) + 4) % 4;
            _warnTimer = 0f;
            SpawnRaid();
        }

        /// <summary>Berilgan tile'ga eng yaqin (yiqilmagan) shahar jangchisi ichiga kiradi.</summary>
        public bool PossessNearestSoldier(Vector2 tile)
        {
            Unit best = null;
            float bestD = float.MaxValue;
            foreach (Unit u in _units.Units)
            {
                if (!IsSoldier(u) || u.IsDown) continue;
                float d = Vector2.Distance(u.Position, tile);
                if (d < bestD) { bestD = d; best = u; }
            }
            return TryPossess(best);
        }

        private void SpawnRaid()
        {
            _raidNumber++;
            int count = Math.Min(14, 4 + 2 * _raidNumber);
            _raidTotal = count;
            _lootStolen = 0;
            RaidActive = true;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = SpawnPosition(_gateIndex, i);
                bool chief = i == 0;
                var r = _units.Add(new RaiderUnit(pos, chief));
                _raiders.Add(r);
                _agents[r] = new Agent { Unit = r, Mode = AgentMode.Raiding, Home = pos, ThinkTimer = (float)_rng.NextDouble() * 0.4f };
            }
            _state.ShowMessage("BOSQIN! " + count + " ta dushman shaharga yurdi. Himoyaga!", true);
        }

        /// <summary>Bosqinchilar bosh ko'cha (31-32 qatorlar) bo'ylab darvoza tashqarisida saf tortadi.</summary>
        private static Vector2 SpawnPosition(int gate, int i)
        {
            float lane = i % 2;          // ko'chaning ikki qatori
            float row = (i / 2) * 0.9f;  // orqaga saf
            switch (gate)
            {
                case 0: return new Vector2(31.5f + lane, 2.5f + row);   // shimol
                case 1: return new Vector2(31.5f + lane, 61.5f - row);  // janub
                case 2: return new Vector2(2.5f + row, 31.5f + lane);   // g'arb
                default: return new Vector2(61.5f - row, 31.5f + lane); // sharq
            }
        }

        private void EndRaid(bool victory)
        {
            RaidActive = false;
            if (Possessed is RaiderUnit) Release(false);

            if (victory)
            {
                int reward = 30 + 12 * _raidTotal;
                _state.Coins += reward;
                _resultText = "G'ALABA! Bosqin qaytarildi.  +" + reward + " tanga";
            }
            else
            {
                int loss = _state.Coins / 5;
                _state.Coins -= loss;
                _resultText = "MAG'LUBIYAT... Bosqinchilar " + (loss + _lootStolen) + " tanga talab ketdi";
            }
            _resultVictory = victory;
            _resultTimer = 6f;
            _state.ShowMessage(_resultText, !victory);

            // Bosqinchilarni olib tashlash
            foreach (RaiderUnit r in _raiders) RemoveUnit(r);
            _raiders.Clear();
            _corpseTimers.Clear();

            // Himoyachilar o'rnidan turadi va o'z joyiga qaytadi
            foreach (Unit u in _units.Units)
                if (u.Team == UnitTeam.Player && u.IsDown) u.Revive(victory ? 0.6f : 0.35f);
            foreach (Agent a in _agents.Values)
                if (a.Mode == AgentMode.Fighting) BeginReturn(a);
        }

        // ==================================================================
        // UPDATE
        // ==================================================================

        /// <param name="dt">Real vaqt (pauzada 0).</param>
        /// <param name="input">Boshqaruv kiritishi (panel/dialog ochiq bo'lsa null).</param>
        public void Update(float dt, InputManager input, CameraManager camera)
        {
            // Floating matnlar va natija banneri real vaqtda so'nadi
            for (int i = _floats.Count - 1; i >= 0; i--)
            {
                FloatText f = _floats[i];
                f.T += dt;
                if (f.T > 1f) _floats.RemoveAt(i); else _floats[i] = f;
            }
            if (_resultTimer > 0f) _resultTimer -= dt;

            UpdatePossessed(dt, input, camera);
            if (dt <= 0f) return;

            // Avtomatik bosqinlar o'chirilgan (o'yinchi talabi) — bosqin faqat B tugmasi bilan chaqiriladi.
            if (RaidIncoming)
            {
                _warnTimer -= dt;
                if (_warnTimer <= 0f) { _warnTimer = 0f; SpawnRaid(); }
            }

            if (RaidActive) UpdateDefenderAwareness(dt);

            // Agentlar (AI boshqaruvidagi unitlar)
            var snapshot = new List<Agent>(_agents.Values);
            foreach (Agent a in snapshot)
            {
                Unit u = a.Unit;
                if (u == Possessed) continue;
                if (u.IsDown) { u.DriveIdle(); continue; }
                switch (a.Mode)
                {
                    case AgentMode.Fighting: UpdateFighter(a, dt); break;
                    case AgentMode.Raiding: UpdateRaider(a, dt); break;
                    case AgentMode.Returning: UpdateReturning(a, dt); break;
                    case AgentMode.Escort: UpdateEscort(a, dt); break;
                }
            }
            Separate();

            // Yiqilgan bosqinchilar biroz yotib, keyin yo'qoladi
            if (_corpseTimers.Count > 0)
            {
                var dead = new List<Unit>();
                foreach (var kv in _corpseTimers) if (kv.Value - dt <= 0f) dead.Add(kv.Key);
                foreach (Unit d in new List<Unit>(_corpseTimers.Keys)) _corpseTimers[d] -= dt;
                foreach (Unit d in dead) { _corpseTimers.Remove(d); _raiders.Remove(d as RaiderUnit); RemoveUnit(d); }
            }

            // Bosqin natijasi
            if (RaidActive)
            {
                int aliveRaiders = 0;
                foreach (RaiderUnit r in _raiders) if (!r.IsDown) aliveRaiders++;
                int aliveDefenders = 0;
                foreach (Unit u in _units.Units) if (IsSoldier(u) && !u.IsDown) aliveDefenders++;
                if (aliveRaiders == 0) EndRaid(true);
                else if (aliveDefenders == 0) EndRaid(false);
            }
            else
            {
                // Bosqindan tashqari (masalan bosqinchi ichida bo'lganda) yiqilganlar tez tiklanadi
                foreach (Unit u in _units.Units)
                {
                    if (u.Team != UnitTeam.Player || !u.IsDown) continue;
                    _reviveTimers.TryGetValue(u, out float t);
                    t += dt;
                    if (t >= 4f) { u.Revive(0.5f); _reviveTimers.Remove(u); }
                    else _reviveTimers[u] = t;
                }
            }
        }

        /// <summary>Shahar jangchilari yaqin dushmanni sezsa jangga kiradi.</summary>
        private void UpdateDefenderAwareness(float dt)
        {
            _defenderThink -= dt;
            if (_defenderThink > 0f) return;
            _defenderThink = 0.35f;
            foreach (Unit u in _units.Units)
            {
                if (!IsSoldier(u) || u.IsDown || u == Possessed) continue;
                float aggro = u is GuardUnit ? GuardAggro : PatrolAggro;
                Unit enemy = NearestEnemy(u, aggro);
                if (enemy == null) continue;
                if (!_agents.TryGetValue(u, out Agent a))
                {
                    a = new Agent { Unit = u, Home = u.Position };
                    _agents[u] = a;
                    u.ExternallyControlled = true;
                }
                if (a.Mode != AgentMode.Possessed) { a.Mode = AgentMode.Fighting; if (a.Target == null || a.Target.IsDown) a.Target = enemy; }
            }
        }

        private void UpdateFighter(Agent a, float dt)
        {
            Unit u = a.Unit;
            a.ThinkTimer -= dt;
            if (a.Target == null || a.Target.IsDown || a.ThinkTimer <= 0f)
            {
                a.ThinkTimer = 0.5f;
                a.Target = NearestEnemy(u, DisengageRange);
            }
            if (a.Target == null) { BeginReturn(a); return; }
            ChaseAndStrike(a, a.Target, DefenderSpeed, dt);
        }

        private void UpdateRaider(Agent a, float dt)
        {
            Unit u = a.Unit;
            a.ThinkTimer -= dt;
            if (a.Target == null || a.Target.IsDown || a.ThinkTimer <= 0f)
            {
                a.ThinkTimer = 0.4f;
                Unit t = null;
                float best = RaiderAggro;
                foreach (Unit o in _units.Units)
                {
                    if (o.Team == UnitTeam.Enemy || o.IsDown) continue;
                    bool possessedTarget = o == Possessed;
                    if (!possessedTarget && !IsSoldier(o)) continue; // tinch aholiga tegmaydi
                    float range = possessedTarget ? RaiderPossessedAggro : RaiderAggro;
                    float d = Vector2.Distance(o.Position, u.Position);
                    if (d <= range && (t == null || d < best)) { best = d; t = o; }
                }
                a.Target = t;
            }

            float speed = ((RaiderUnit)u).MoveSpeed;
            if (a.Target != null) { ChaseAndStrike(a, a.Target, speed, dt); return; }

            // Shahar markaziga yurish, yetgach — talash
            if (MoveAgent(a, CityCenter, speed, dt, 2.2f))
            {
                a.LootTimer -= dt;
                if (a.LootTimer <= 0f)
                {
                    a.LootTimer = 1.2f;
                    int take = Math.Min(_state.Coins, ((RaiderUnit)u).IsChief ? 6 : 3);
                    if (take > 0)
                    {
                        _state.Coins -= take;
                        _lootStolen += take;
                        AddFloat(u.Position, "-" + take, new Color(255, 210, 90));
                    }
                }
            }
        }

        private void UpdateReturning(Agent a, float dt)
        {
            Unit u = a.Unit;
            if (MoveAgent(a, a.Home, u is ShahUnit ? 1.8f : 1.4f, dt, 0.06f))
            {
                if (u is ShahUnit)
                {
                    // Shoh saroyga kirib, yana taxtga o'tiradi
                    RemoveUnit(u);
                    _shah = null;
                    _state.ShowMessage("Shoh hazratlari saroyga qaytdilar");
                    return;
                }
                u.Position = a.Home;
                u.ExternallyControlled = false;
                u.DriveIdle();
                _agents.Remove(u);
            }
        }

        private void BeginReturn(Agent a)
        {
            a.Mode = AgentMode.Returning;
            a.Target = null;
            a.Path = null;
        }

        /// <summary>Nishonga yaqinlashadi; masofaga kirsa to'xtab uradi.</summary>
        private void ChaseAndStrike(Agent a, Unit target, float speed, float dt)
        {
            Unit u = a.Unit;
            float dist = Vector2.Distance(u.Position, target.Position);
            if (dist <= u.AttackRange)
            {
                u.DriveIdle();
                u.SetFacing(target.Position - u.Position);
                if (u.AttackCooldown <= 0f)
                {
                    // AI ham har xil uradi: nayzali — sanchadi, sardor — tepadan chopadi, qolganlar aralash
                    u.AttackKind = u is SpearmanUnit ? Unit.AttackThrust
                        : u is RaiderUnit rc && rc.IsChief ? (_rng.Next(3) == 0 ? Unit.AttackSlash : Unit.AttackOverhead)
                        : _rng.Next(3);
                    u.AttackTimer = Unit.SwingTime;
                    u.AttackCooldown = 1.0f + (float)_rng.NextDouble() * 0.35f;
                    int dmg = Math.Max(2, u.Attack + _rng.Next(-2, 3));
                    Hit(u, target, dmg);
                }
                return;
            }
            MoveAgent(a, target.Position, speed, dt, u.AttackRange * 0.85f);
        }

        private void Hit(Unit attacker, Unit victim, int dmg)
        {
            if (victim.IsDown) return;
            victim.HitPoints = Math.Max(0, victim.HitPoints - dmg);
            victim.HurtTimer = 0.18f;
            bool byPlayer = attacker == Possessed;
            AddFloat(victim.Position, dmg.ToString(), byPlayer ? new Color(255, 236, 120) : victim.Team == UnitTeam.Enemy ? Color.White : new Color(255, 120, 110));
            if (!victim.IsDown) return;

            if (victim is RaiderUnit rv)
            {
                _corpseTimers[victim] = 2.2f;
                if (_agents.TryGetValue(victim, out _)) _agents.Remove(victim);
                if (rv.IsChief) _state.ShowMessage("Bosqinchilar sardori yiqildi!" + (byPlayer ? " Ofarin, botir!" : ""));
                else if (byPlayer) AddFloat(victim.Position + new Vector2(-0.3f, -0.3f), "YIQITDINGIZ!", new Color(255, 214, 90));
            }
            else
            {
                AddFloat(victim.Position, victim.Name + " yiqildi", new Color(255, 140, 120));
            }
        }

        // ==================================================================
        // HARAKAT VA YO'L TOPISH
        // ==================================================================

        private bool Walk(Vector2 p) => _map.IsWalkable((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y));

        /// <summary>To'siqqa tegsa devor bo'ylab sirpanadi. Unit to'siq ichida qolib ketgan bo'lsa — chiqib ketishiga ruxsat.</summary>
        private bool MoveWithCollision(Unit u, Vector2 delta)
        {
            Vector2 p = u.Position;
            Vector2 np = Clamp(p + delta);
            if (!Walk(p) || Walk(np)) { u.Position = np; return true; }
            Vector2 nx = Clamp(new Vector2(p.X + delta.X, p.Y));
            if (MathF.Abs(delta.X) > 0.0001f && Walk(nx)) { u.Position = nx; return true; }
            Vector2 ny = Clamp(new Vector2(p.X, p.Y + delta.Y));
            if (MathF.Abs(delta.Y) > 0.0001f && Walk(ny)) { u.Position = ny; return true; }
            return false;
        }

        private Vector2 Clamp(Vector2 p) => new Vector2(
            MathHelper.Clamp(p.X, 0.3f, _map.Width - 0.3f), MathHelper.Clamp(p.Y, 0.3f, _map.Height - 0.3f));

        /// <summary>a dan b gacha to'g'ri chiziq to'siqsizmi (oxirgi qismi tekshirilmaydi — nishon to'siq yonida turishi mumkin).</summary>
        private bool Clear(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            if (d < 0.6f) return true;
            int n = (int)((d - 0.5f) / 0.25f) + 1;
            for (int i = 1; i <= n; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i * 0.25f / d);
                if (!Walk(p)) return false;
            }
            return true;
        }

        private Point NearestWalkable(Point t)
        {
            if (_map.IsWalkable(t.X, t.Y)) return t;
            for (int r = 1; r <= 3; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                        if (_map.IsWalkable(t.X + dx, t.Y + dy)) return new Point(t.X + dx, t.Y + dy);
            return t;
        }

        /// <summary>Agentni maqsadga olib boradi (to'g'ri yo'l bo'lsa — to'g'ri, aks holda A*). Yetib kelganda true.</summary>
        private bool MoveAgent(Agent a, Vector2 goal, float speed, float dt, float stopDist)
        {
            Unit u = a.Unit;
            Vector2 pos = u.Position;
            if (Vector2.Distance(pos, goal) <= stopDist) { u.DriveIdle(); return true; }

            Vector2 next = goal;
            if (!Clear(pos, goal))
            {
                Point goalTile = NearestWalkable(new Point((int)MathF.Floor(goal.X), (int)MathF.Floor(goal.Y)));
                a.RepathTimer -= dt;
                if (a.Path == null || a.RepathTimer <= 0f || a.PathGoal != goalTile)
                {
                    Point start = NearestWalkable(new Point((int)MathF.Floor(pos.X), (int)MathF.Floor(pos.Y)));
                    a.Path = Pathfinder.FindPath(_map, start, goalTile, 4000);
                    a.PathGoal = goalTile;
                    a.PathIndex = a.Path != null && a.Path.Count > 1 ? 1 : 0;
                    a.RepathTimer = 0.8f + (float)_rng.NextDouble() * 0.3f;
                }
                if (a.Path != null && a.PathIndex < a.Path.Count)
                {
                    Point wp = a.Path[a.PathIndex];
                    next = new Vector2(wp.X + 0.5f, wp.Y + 0.5f);
                    if (Vector2.Distance(pos, next) < 0.2f && a.PathIndex < a.Path.Count - 1)
                    {
                        a.PathIndex++;
                        wp = a.Path[a.PathIndex];
                        next = new Vector2(wp.X + 0.5f, wp.Y + 0.5f);
                    }
                }
            }
            else a.Path = null;

            Vector2 dir = next - pos;
            float len = dir.Length();
            if (len < 0.0001f) { u.DriveIdle(); return false; }
            dir /= len;
            u.DriveMove(dir, speed);
            if (!MoveWithCollision(u, dir * MathF.Min(speed * dt, len))) a.RepathTimer = 0f; // tiqilib qoldi — yo'lni qayta hisoblash
            return false;
        }

        /// <summary>Jangdagi unitlar bir nuqtaga to'planib qolmasligi uchun yengil itarilish.</summary>
        private void Separate()
        {
            var list = new List<Unit>();
            foreach (Agent a in _agents.Values) if (!a.Unit.IsDown && a.Mode != AgentMode.Returning) list.Add(a.Unit);
            for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                {
                    Vector2 d = list[j].Position - list[i].Position;
                    float l = d.Length();
                    if (l >= 0.45f) continue;
                    if (l < 0.001f) { d = new Vector2(0.01f * (j - i), 0.01f); l = d.Length(); }
                    Vector2 push = d / l * (0.45f - l) * 0.5f;
                    if (list[i] != Possessed) MoveWithCollision(list[i], -push);
                    if (list[j] != Possessed) MoveWithCollision(list[j], push);
                }
        }

        // ==================================================================
        // YORDAMCHI
        // ==================================================================

        private static bool IsSoldier(Unit u) => u.Team == UnitTeam.Player && !(u is CitizenUnit) && !(u is ShahUnit);

        private Unit NearestEnemy(Unit from, float range)
        {
            Unit best = null;
            float bestD = range;
            foreach (Unit o in _units.Units)
            {
                if (o.Team == from.Team || o.IsDown) continue;
                float d = Vector2.Distance(o.Position, from.Position);
                if (d <= bestD) { bestD = d; best = o; }
            }
            return best;
        }

        private void RemoveUnit(Unit u)
        {
            if (u == null) return;
            if (Possessed == u) Release(false);
            if (u == _shah) _shah = null;
            _agents.Remove(u);
            _units.Remove(u);
            if (_state.Selected == u) _state.Select(null);
        }

        private void AddFloat(Vector2 tile, string text, Color c)
        {
            _floats.Add(new FloatText { World = IsoMath.TileToWorld(tile) + new Vector2(_rng.Next(-6, 7), -46), Text = text, Color = c });
        }

        // ==================================================================
        // CHIZISH
        // ==================================================================

        /// <summary>Oyoq ostidagi halqalar (world batch, binolardan oldin).</summary>
        public void DrawGroundMarkers(SpriteBatch sb)
        {
            foreach (RaiderUnit r in _raiders)
                if (!r.IsDown) _map.DrawMarker(sb, IsoMath.TileToWorld(r.Position), new Color(220, 50, 40, 170), 0.42f);
            if (Possessed != null)
            {
                float pulse = 0.5f + 0.06f * MathF.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds * 6f);
                _map.DrawMarker(sb, IsoMath.TileToWorld(Possessed.Position), new Color(255, 214, 90, 240), pulse);
            }
        }

        /// <summary>Ekran qatlami: HP chiziqlari, zarba raqamlari, bosqin banneri, boshqaruv paneli.</summary>
        public void DrawHud(SpriteBatch sb, Texture2D px, PixelFont font, CameraManager cam, int vw, int vh)
        {
            // --- HP chiziqlari (jangdagi yoki jarohatlangan unitlar) ---
            foreach (Unit u in _units.Units)
            {
                bool show = u.Team == UnitTeam.Enemy || u == Possessed || (u.HitPoints < u.MaxHitPoints && u.MaxHitPoints > 0) || _agents.ContainsKey(u) && RaidActive;
                if (!show || u.IsDown) continue;
                Vector2 s = cam.WorldToScreen(IsoMath.TileToWorld(u.Position) + new Vector2(0, -52));
                if (s.X < -40 || s.Y < -20 || s.X > vw + 40 || s.Y > vh + 20) continue;
                int w = 30, h = 4;
                var r = new Rectangle((int)s.X - w / 2, (int)s.Y, w, h);
                sb.Draw(px, new Rectangle(r.X - 1, r.Y - 1, w + 2, h + 2), new Color(10, 10, 12, 200));
                float f = u.MaxHitPoints > 0 ? (float)u.HitPoints / u.MaxHitPoints : 0f;
                Color c = u.Team == UnitTeam.Enemy ? new Color(220, 60, 50) : u == Possessed ? new Color(255, 210, 80) : new Color(90, 200, 110);
                sb.Draw(px, new Rectangle(r.X, r.Y, (int)(w * f), h), c);
            }

            // --- Qilich izlari (har bir zarba turi o'z shaklida) ---
            foreach (Unit u in _units.Units)
                if (u.AttackTimer > 0f && !u.IsDown) DrawSlash(sb, px, cam, u);

            // --- Zarba raqamlari ---
            foreach (FloatText f in _floats)
            {
                Vector2 s = cam.WorldToScreen(f.World + new Vector2(0, -22f * f.T));
                float alpha = f.T < 0.7f ? 1f : 1f - (f.T - 0.7f) / 0.3f;
                Vector2 size = font.Measure(f.Text, 2);
                Vector2 p = new Vector2(MathF.Round(s.X - size.X / 2), MathF.Round(s.Y));
                font.Draw(sb, f.Text, p + new Vector2(1, 1), Color.Black * (0.7f * alpha), 2);
                font.Draw(sb, f.Text, p, f.Color * alpha, 2);
            }

            // --- Bosqin banneri ---
            if (RaidIncoming)
            {
                string t = "DUSHMAN YAQINLASHMOQDA!  " + GateNames[_gateIndex].ToUpperInvariant() + " DARVOZA  " + (int)MathF.Ceiling(_warnTimer);
                Banner(sb, px, font, vw, t, new Color(150, 30, 26, 230), Color.White);
            }
            else if (RaidActive)
            {
                int alive = 0;
                foreach (RaiderUnit r in _raiders) if (!r.IsDown) alive++;
                int def = 0;
                foreach (Unit u in _units.Units) if (IsSoldier(u) && !u.IsDown) def++;
                string t = "BOSQIN!  Dushman: " + alive + "/" + _raidTotal + "   Himoyachilar: " + def + (_lootStolen > 0 ? "   Talangan: " + _lootStolen : "");
                Banner(sb, px, font, vw, t, new Color(110, 24, 22, 220), new Color(255, 230, 200));
                if (Possessed == null)
                    font.DrawCentered(sb, "Jangchini tanlang va F bosing - o'zingiz jangga kiring!", new Vector2(vw / 2f, 92), new Color(255, 220, 150), 2);
            }
            else if (_resultTimer > 0f && _resultText != null)
            {
                Banner(sb, px, font, vw, _resultText, _resultVictory ? new Color(30, 100, 50, 225) : new Color(90, 30, 30, 225), Color.White);
            }

            // --- Boshqaruv paneli ---
            if (Possessed != null) DrawPossessionPanel(sb, px, font, vw, vh);
        }

        /// <summary>
        /// Qilich izi: KESISH — gorizontal yoy, CHOPISH — tepadan pastga yoy, SANCHISH — oldinga chiziq,
        /// AYLANMA — to'liq aylana. Iz zarba jarayoniga qarab chiziladi va so'nib boradi.
        /// </summary>
        private void DrawSlash(SpriteBatch sb, Texture2D px, CameraManager cam, Unit u)
        {
            float p = u.AttackProgress;
            float z = cam.Zoom;
            Vector2 center = cam.WorldToScreen(IsoMath.TileToWorld(u.Position) + new Vector2(0, -22));
            Vector2 f = u.FacingWorld;
            float a0 = MathF.Atan2(f.Y, f.X);
            Color col = u == Possessed ? new Color(255, 244, 200) : u.Team == UnitTeam.Enemy ? new Color(255, 170, 160) : new Color(225, 230, 240);
            const int n = 18;
            bool havePrev = false;
            Vector2 prev = Vector2.Zero;

            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                if (t > p) break;
                float alpha = MathHelper.Clamp(1f - (p - t) * 2.6f, 0f, 1f);
                Vector2 pt;
                switch (u.AttackKind)
                {
                    case Unit.AttackOverhead:
                    {
                        float s = f.X >= 0 ? 1f : -1f;
                        float ang = -MathF.PI / 2f + s * t * 2.3f;
                        pt = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 22f * z + f * 4f * z;
                        break;
                    }
                    case Unit.AttackThrust:
                        pt = center + f * (6f + 30f * t) * z + new Vector2(0, 6f * z);
                        break;
                    case Unit.AttackSpin:
                    {
                        float ang = a0 + t * MathF.PI * 2f;
                        pt = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.55f) * 26f * z + new Vector2(0, 10f * z);
                        break;
                    }
                    default:
                    {
                        float ang = a0 - 1.25f + 2.5f * t;
                        pt = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * 20f * z + new Vector2(0, 6f * z);
                        break;
                    }
                }
                // Yaxlit yoy: oldingi nuqtadan shu nuqtagacha qalinligi so'nishga mos chiziq (boshi qalin, dumi ingichka)
                if (havePrev && alpha > 0.02f)
                {
                    Vector2 d = pt - prev;
                    float len = d.Length();
                    if (len > 0.01f)
                    {
                        float thick = (1.2f + 3.2f * alpha) * z;
                        float ang = MathF.Atan2(d.Y, d.X);
                        sb.Draw(px, prev, null, Color.Black * (0.35f * alpha), ang, new Vector2(0f, 0.5f), new Vector2(len + 1f, thick + 2f), SpriteEffects.None, 0f);
                        sb.Draw(px, prev, null, col * (0.95f * alpha), ang, new Vector2(0f, 0.5f), new Vector2(len + 1f, thick), SpriteEffects.None, 0f);
                    }
                }
                prev = pt;
                havePrev = true;
            }
        }

        private static void Banner(SpriteBatch sb, Texture2D px, PixelFont font, int vw, string text, Color bg, Color fg)
        {
            Vector2 size = font.Measure(text, 2);
            int w = (int)size.X + 40, h = 30;
            var r = new Rectangle(vw / 2 - w / 2, 44, w, h);
            sb.Draw(px, new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), new Color(20, 12, 10, 220));
            sb.Draw(px, r, bg);
            font.DrawCentered(sb, text, r.Center.ToVector2(), fg, 2);
        }

        private void DrawPossessionPanel(SpriteBatch sb, Texture2D px, PixelFont font, int vw, int vh)
        {
            Unit p = Possessed;
            int w = Math.Min(vw - 32, 620), h = 74;
            var r = new Rectangle(vw / 2 - w / 2, vh - 44 - h - 10, w, h);
            sb.Draw(px, new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), new Color(216, 167, 83, 230));
            sb.Draw(px, r, new Color(24, 20, 18, 235));

            int x = r.X + 14, y = r.Y + 10;
            font.Draw(sb, "SIZ: " + p.Name.ToUpperInvariant(), new Vector2(x, y), new Color(255, 214, 90), 2);
            string atk = "Hujum " + Math.Max(4, p.Attack);
            font.DrawRight(sb, atk, new Vector2(r.Right - 14, y), new Color(220, 200, 170), 2);
            y += 20;

            // HP
            int bw = w - 28, bh = 10;
            sb.Draw(px, new Rectangle(x - 1, y - 1, bw + 2, bh + 2), Color.Black);
            float f = p.MaxHitPoints > 0 ? (float)p.HitPoints / p.MaxHitPoints : 0f;
            sb.Draw(px, new Rectangle(x, y, (int)(bw * f), bh), f > 0.35f ? new Color(90, 200, 110) : new Color(220, 70, 50));
            font.DrawCentered(sb, p.HitPoints + " / " + p.MaxHitPoints, new Vector2(x + bw / 2f, y + bh / 2f + 1), Color.White, 1);
            if (p.AttackCooldown > 0f)
                sb.Draw(px, new Rectangle(x, y + bh + 2, (int)(bw * (p.AttackCooldown / PlayerAttackCooldown)), 2), new Color(255, 214, 90, 180));
            y += 20;

            font.Draw(sb, "WASD yurish | SHIFT yugurish | SPACE kombo | R aylanma | ENTER binoga kirish | Q/E boshqasi | F chiqish",
                new Vector2(x, y), new Color(190, 180, 160), 1);
        }
    }
}
