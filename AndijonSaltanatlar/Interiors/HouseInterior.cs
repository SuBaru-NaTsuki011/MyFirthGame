using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Population;
using AndijonSaltanatlar.Units;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AndijonSaltanatlar.Interiors
{
    /// <summary>
    /// BINOGA KIRISH: o'yinchi boshqarayotgan odam (jangchi, fuqaro yoki shoh) bino yonida ENTER bossa,
    /// uning ichki ko'rinishi ochiladi:
    ///   - UY: yotoqxona + oshxona, uy turiga mos dizayn; ichkarida shu uyning HAQIQIY oilasi (PopulationManager).
    ///   - SAROY: taxt zali — taxtda shoh, soqchilar, vazir, kotib. Saroyga hamma kira oladi.
    ///     Shoh yonida F bosilsa — o'yinchi shohga aylanib, shaharga sayrga chiqadi (BecomeShahRequested).
    ///
    /// Shahar simulyatsiyasi orqada davom etadi. Boshqarilayotgan unitning shahardagi Position'i
    /// ichkarida o'zgarmaydi; chiqishda u bino eshigi oldiga qo'yiladi.
    /// </summary>
    public sealed class HouseInterior
    {
        private const int N = InteriorArt.N;
        private const float WalkSpeed = 2.1f, RunSpeed = 3.1f;

        private readonly AssetManager _assets;
        private readonly MapManager _map;
        private readonly BuildingManager _buildings;
        private readonly GameStateManager _state;

        private sealed class Prop
        {
            public Texture2D Tex;
            public Point Tile;
            public int Size;
            public float Depth => Tile.X + Tile.Y + Size;
            public Vector2 DrawPos => WorldObjectExtensions.AnchorBottomCenter(Tex, Tile, Size);
        }

        private sealed class Resident
        {
            public string Name;      // to'liq ism
            public string Short;     // uzoqdan ko'rinadigan qisqa ism
            public string Info;      // yaqinlashganda: yosh, kasb
            public string Line;      // salomlashish
            public Vector2 Pos;
            public Texture2D Tex;
            public float Scale = 1f;
            public float Lift;       // taxtda o'tirgan shoh — balandroq
            public bool IsShah;
            public bool FixedFacing; // taxtdagi shoh doim zalga qaraydi
        }

        private readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        private readonly Dictionary<int, (Texture2D shell, Vector2 origin)> _shells = new Dictionary<int, (Texture2D, Vector2)>();
        private readonly List<Prop> _props = new List<Prop>();
        private readonly List<Resident> _residents = new List<Resident>();
        private readonly bool[,] _blocked = new bool[N, N];
        private Texture2D _shell;
        private Vector2 _shellOrigin;
        private Texture2D _glow;

        private Unit _avatar;
        private Vector2 _pos;
        private bool _leftDoor;
        private float _time;
        private string _title = "";
        private string _subtitle = "";
        private bool _palace;
        private int _doorX0, _doorX1;

        // ---- Uy ichidagi harakatlar: dasturxon atrofida o'tirish / ovqatlanish, to'shakda yotish ----
        public enum Pose { Stand, Sit, Eat, Lie }
        public Pose CurrentPose { get; private set; } = Pose.Stand;
        public const int MealFood = 5;
        public const float EatTime = 3f;
        private Vector2 _standPos;          // o'tirish/yotishdan oldingi joy (turganda qaytadi)
        private Vector2 _tableCenter = new Vector2(6f, 5f);
        private Vector2 _bedPos = new Vector2(1.0f, 6.0f);
        private float _poseTime;
        private Rectangle _act1, _act2;     // ekrandagi harakat tugmalari (sensorli ekran uchun ham)
        private string _act1Label = "", _act2Label = "";
        private static readonly Vector2[] Seats = { new Vector2(5.5f, 6.4f), new Vector2(6.5f, 6.4f), new Vector2(7.45f, 4.5f), new Vector2(7.45f, 5.5f) };

        public bool Active { get; private set; }
        public Building House { get; private set; }
        public bool IsPalace => _palace;
        /// <summary>Boshqarilayotgan odam yonidagi (kirsa bo'ladigan) bino — ENTER ko'rsatmasi uchun.</summary>
        public Building NearbyHouse { get; private set; }
        /// <summary>Saroyda shoh yonida F bosildi — Game1 shohni shaharga chiqaradi.</summary>
        public bool BecomeShahRequested { get; set; }
        /// <summary>Shoh hozir shaharda bo'lsa, taxt bo'sh turadi (Game1 har kadr o'rnatadi).</summary>
        public bool ShahAway { get; set; }

        public HouseInterior(AssetManager assets, MapManager map, BuildingManager buildings, GameStateManager state)
        {
            _assets = assets;
            _map = map;
            _buildings = buildings;
            _state = state;
        }

        internal static bool IsEnterable(Building b) =>
            b != null && b.BuildProgress >= 1f
            && b.Type != BuildingType.Garden && b.Type != BuildingType.Tower
            && b.Type != BuildingType.Wall && b.Type != BuildingType.Gate;

        // ==================================================================
        // Shahardagi binoni topish
        // ==================================================================

        /// <summary>Har kadr: boshqarilayotgan odam yonida kirsa bo'ladigan bino bormi (null — tekshirmaslik).</summary>
        private Unit _nearbyWho;

        /// <summary>Saroyga faqat soqchilar, jangchilar va shoh kiradi — oddiy fuqaro kira olmaydi.</summary>
        public static bool MayEnter(Building b, Unit who) => !(b != null && b.Type == BuildingType.MainHall && who is CitizenUnit);

        public void UpdateNearby(Unit who)
        {
            _nearbyWho = who;
            NearbyHouse = null;
            if (who == null || who.IsDown) return;
            float best = 0.9f;
            foreach (Building b in _buildings.Buildings)
            {
                if (!IsEnterable(b)) continue;
                float dx = MathF.Max(0f, MathF.Max(b.Position.X - who.Position.X, who.Position.X - (b.Position.X + b.Size.X)));
                float dy = MathF.Max(0f, MathF.Max(b.Position.Y - who.Position.Y, who.Position.Y - (b.Position.Y + b.Size.Y)));
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d <= best) { best = d; NearbyHouse = b; }
            }
        }

        // ==================================================================
        // Kirish / chiqish
        // ==================================================================

        public void Enter(Building building, Unit avatar)
        {
            if (building == null || avatar == null) return;
            if (!MayEnter(building, avatar))
            {
                _state.ShowMessage("Saroyga faqat soqchilar va jangchilar kira oladi!", true);
                return;
            }
            House = building;
            _avatar = avatar;
            _palace = building.Type == BuildingType.MainHall;
            _glow ??= InteriorArt.Glow(96).ToTexture(_assets.GraphicsDevice);
            BecomeShahRequested = false;

            if (_palace)
            {
                _doorX0 = InteriorArt.PalaceDoorX0;
                _doorX1 = InteriorArt.PalaceDoorX1;
                LoadShell(100, () => InteriorArt.PalaceShell(out Vector2 o).WithOrigin(o));
                BuildPalaceLayout();
                BuildPalaceResidents();
                _pos = new Vector2(_doorX1 + 0.05f, 7.4f);
            }
            else
            {
                _doorX0 = _doorX1 = 6;
                int v = VariantOf(building);
                InteriorArt.Style s = InteriorArt.For(v);
                LoadShell(v, () => InteriorArt.Shell(s, out Vector2 o).WithOrigin(o));
                if (building.Type == BuildingType.House)
                {
                    BuildHouseLayout(v, s);
                    BuildHouseResidents(building);
                }
                else
                {
                    BuildPublicBuildingLayout(building, v, s);
                    BuildPublicBuildingResidents(building);
                }
                _pos = new Vector2(6.5f, 7.4f);
            }
            _leftDoor = false;
            CurrentPose = Pose.Stand;
            avatar.DriveIdle();
            avatar.SetFacing(new Vector2(-1, -1));
            Active = true;
            _state.ShowMessage(_title + ": xush kelibsiz!");
        }

        /// <summary>Binodan chiqish. newAvatar berilsa (shohga aylanish), tashqariga o'sha chiqadi.</summary>
        public void Exit(Unit newAvatar = null)
        {
            if (!Active) return;
            Active = false;
            CurrentPose = Pose.Stand;
            if (newAvatar != null) _avatar = newAvatar;
            if (_avatar != null)
            {
                _avatar.Position = OutsideDoor(House);
                _avatar.SetFacing(new Vector2(1, 1));
                _avatar.DriveIdle();
            }
            if (newAvatar == null) _state.ShowMessage(_palace ? "Saroydan chiqdingiz" : "Uydan chiqdingiz");
            _residents.Clear();
        }

        /// <summary>Bino eshigi oldidagi yuriladigan nuqta (uy: old hovli darvozasi, saroy: old tomon o'rtasi).</summary>
        public Vector2 OutsideDoor(Building h)
        {
            Point entrance = h.FrontEntranceTile;
            var preferred = new Vector2(entrance.X + 0.5f, entrance.Y + 0.5f);
            if (_map.IsWalkable((int)preferred.X, (int)preferred.Y)) return preferred;
            for (int r = 0; r <= 2; r++)
                for (int y = h.Position.Y - 1 - r; y <= h.Position.Y + h.Size.Y + r; y++)
                    for (int x = h.Position.X - 1 - r; x <= h.Position.X + h.Size.X + r; x++)
                    {
                        bool inside = x >= h.Position.X && x < h.Position.X + h.Size.X && y >= h.Position.Y && y < h.Position.Y + h.Size.Y;
                        if (!inside && _map.IsWalkable(x, y)) return new Vector2(x + 0.5f, y + 0.5f);
                    }
            return _avatar != null ? _avatar.Position : preferred;
        }

        private static int VariantOf(Building h)
        {
            string key = h.SpriteKey ?? "";
            char last = key.Length > 0 ? key[key.Length - 1] : '0';
            return char.IsDigit(last) ? (last - '0') % 4 : 0;
        }

        private void LoadShell(int key, Func<(Canvas c, Vector2 o)> make)
        {
            if (!_shells.TryGetValue(key, out var s))
            {
                var (c, o) = make();
                s = (c.ToTexture(_assets.GraphicsDevice), o);
                _shells[key] = s;
            }
            _shell = s.shell;
            _shellOrigin = s.origin;
        }

        private Texture2D Tex(string key, Func<Canvas> make)
        {
            if (!_tex.TryGetValue(key, out Texture2D t))
            {
                t = make().ToTexture(_assets.GraphicsDevice);
                _tex[key] = t;
            }
            return t;
        }

        private void ClearLayout()
        {
            _props.Clear();
            Array.Clear(_blocked, 0, _blocked.Length);
        }

        private void Add(Texture2D t, int x, int y, int size)
        {
            _props.Add(new Prop { Tex = t, Tile = new Point(x, y), Size = size });
            for (int yy = y; yy < y + size; yy++)
                for (int xx = x; xx < x + size; xx++) _blocked[xx, yy] = true;
        }

        // ------------------------------------------------------------------
        // UY
        // ------------------------------------------------------------------

        private void BuildHouseLayout(int v, InteriorArt.Style s)
        {
            ClearLayout();
            string k = "h" + v + "_";
            Texture2D partition = Tex(k + "part", () => InteriorArt.Partition(s));
            foreach (int y in new[] { 0, 1, 2, 5, 6, 7 }) Add(partition, 4, y, 1); // o'rtada o'tish joyi (y = 3..4)

            // YOTOQXONA
            Add(Tex(k + "chest", () => InteriorArt.Chest(s)), 1, 0, 1);
            Add(Tex(k + "bed", () => InteriorArt.Bed(s)), 0, 5, 2);
            if (v == 1 || v == 2) Add(Tex(k + "stairs", () => InteriorArt.Stairs(s)), 3, 0, 1); // bolaxonaga zinapoya
            else Add(Tex(k + "cradle", () => InteriorArt.Cradle(s)), 3, 6, 1);

            // OSHXONA
            Add(Tex("shelf", InteriorArt.Shelf), 5, 0, 1);
            Add(Tex("jars", InteriorArt.Jars), 6, 0, 1);
            Add(Tex("stove", InteriorArt.Stove), 7, 0, 1);
            Add(Tex("sacks", InteriorArt.Sacks), 7, 2, 1);
            Add(Tex(k + "table", () => InteriorArt.Table(s)), 5, 4, 2);
        }

        private void BuildPublicBuildingLayout(Building building, int v, InteriorArt.Style s)
        {
            ClearLayout();
            string k = "public_" + v + "_";
            if (building.Type == BuildingType.Barracks)
            {
                Texture2D bed = Tex(k + "bed", () => InteriorArt.Bed(s));
                Add(bed, 0, 0, 2);
                Add(bed, 0, 3, 2);
                Add(bed, 5, 0, 2);
                Add(bed, 5, 3, 2);
                Texture2D chest = Tex(k + "chest", () => InteriorArt.Chest(s));
                Add(chest, 1, 6, 1);
                Add(chest, 6, 6, 1);
                return;
            }

            Add(Tex(k + "table", () => InteriorArt.Table(s)), 3, 3, 2);
        }

        private void BuildPublicBuildingResidents(Building building)
        {
            _residents.Clear();
            _title = building.Name.ToUpperInvariant();
            _subtitle = "Bino ichki qismi";
        }

        private static readonly Vector2[] HouseSlots =
        {
            new Vector2(7.45f, 4.7f), new Vector2(7.5f, 1.35f), new Vector2(5.8f, 3.5f),
            new Vector2(2.5f, 2.9f), new Vector2(2.9f, 5.2f), new Vector2(1.7f, 3.9f)
        };

        private static readonly string[] Greetings =
        {
            "Assalomu alaykum! Xush kelibsiz!",
            "Marhamat, to'rga o'ting!",
            "Choy ichib keting, endi damlandi.",
            "Osh tayyor, dasturxonga marhamat!",
            "Uyimizga kelganingizdan xursandmiz.",
            "Yaxshi keldingizmi? Charchamadingizmi?"
        };

        private void BuildHouseResidents(Building house)
        {
            _residents.Clear();
            Household home = null;
            int households = 0;
            foreach (Household h in _state.Pop.Households)
            {
                if (h.HomeBuildingId != house.Id) continue;
                households++;
                home ??= h;
            }

            var members = new List<Citizen>();
            foreach (Citizen c in _state.Pop.Citizens)
                if (c.HomeBuildingId == house.Id || (home != null && c.HouseholdId == home.Id))
                    if (!members.Contains(c)) members.Add(c);

            string family = home != null && !string.IsNullOrWhiteSpace(home.FamilyName) ? home.FamilyName : null;
            _title = family != null ? family.ToUpperInvariant() + " XONADONI" : "UY";
            _subtitle = members.Count > 0
                ? members.Count + " kishi yashaydi" + (households > 1 ? " (" + households + " oila)" : "")
                : "Bu uyda hozircha hech kim yashamaydi";

            bool soldier = _avatar != null && !(_avatar is CitizenUnit);
            for (int i = 0; i < members.Count && i < HouseSlots.Length; i++)
            {
                Citizen c = members[i];
                string line = Greetings[(c.Id + i) % Greetings.Length];
                if (_avatar is ShahUnit && i == 0) line = "Hazratim! Kulbamizga qadam ranjida qildingiz!";
                else if (soldier && i == 0) line = "Soqchi aka, xush kelibsiz! Shahrimiz tinchmi?";
                string full = c.Name ?? "";
                int sp = full.IndexOf(' ');
                string prof = PopulationManager.ProfessionName(c.Profession);
                _residents.Add(new Resident
                {
                    Name = full,
                    Short = sp > 0 ? full.Substring(0, sp) : full,
                    Info = c.Age + " yosh" + (string.IsNullOrEmpty(prof) ? "" : " - " + prof),
                    Line = line,
                    Pos = HouseSlots[i],
                    Tex = _assets.Get(SpriteKeys.UnitCitizen + KindFor(c) + "_0"),
                    Scale = c.Age < 14 ? 0.72f : 1f
                });
            }
        }

        private static int KindFor(Citizen c)
        {
            switch (c.Profession)
            {
                case CitizenProfession.Savdogar: return CitizenArt.Merchant;
                case CitizenProfession.Dehqon:
                case CitizenProfession.Bogbon: return CitizenArt.Farmer;
                case CitizenProfession.Tashuvchi:
                case CitizenProfession.Muallim: return CitizenArt.Craftsman;
                case CitizenProfession.None: return c.Age < 14 ? CitizenArt.Citizen : CitizenArt.Buyer;
                default: return CitizenArt.Citizen;
            }
        }

        // ------------------------------------------------------------------
        // SAROY
        // ------------------------------------------------------------------

        private static readonly Vector2 ThroneSeat = new Vector2(4.0f, 1.05f);

        private void BuildPalaceLayout()
        {
            ClearLayout();
            InteriorArt.Style s = InteriorArt.PalaceStyle;
            Add(Tex("throne", InteriorArt.Throne), 3, 0, 2);
            Texture2D brazier = Tex("brazier", InteriorArt.Brazier);
            Add(brazier, 2, 1, 1);
            Add(brazier, 5, 1, 1);
            Texture2D column = Tex("column", InteriorArt.Column);
            Add(column, 1, 3, 1);
            Add(column, 6, 3, 1);
            Add(column, 1, 6, 1);
            Add(column, 6, 6, 1);
            Texture2D treasure = Tex("treasure", InteriorArt.Treasure);
            Add(treasure, 0, 0, 1);
            Add(treasure, 7, 0, 1);
            Add(Tex("palace_table", () => InteriorArt.Table(s)), 6, 4, 2); // shoh dasturxoni
        }

        private void BuildPalaceResidents()
        {
            _residents.Clear();
            bool asShah = _avatar is ShahUnit;
            bool shahHome = !ShahAway && !asShah;
            _title = "SHOH SAROYI";
            _subtitle = shahHome ? "Shoh " + ShahUnit.ShahName + " hazratlari taxtda" : asShah ? "Hazratim, saroyingizga xush kelibsiz" : "Shoh hazratlari shahar sayrida";

            Texture2D guard = _assets.Get(SpriteKeys.UnitSpearman + "_0");
            Texture2D vizier = _assets.Get(SpriteKeys.UnitCitizen + CitizenArt.Merchant + "_0");
            Texture2D scribe = _assets.Get(SpriteKeys.UnitCitizen + CitizenArt.Craftsman + "_0");
            bool soldier = _avatar != null && !(_avatar is CitizenUnit) && !asShah;

            if (shahHome)
            {
                _residents.Add(new Resident
                {
                    Name = "Shoh " + ShahUnit.ShahName,
                    Short = "SHOH",
                    Info = "Saltanat hukmdori",
                    Line = soldier ? "Botir navkarim, shahar osoyishtami?" : "Xush kelding, fuqarom! Arzing bo'lsa, ayt.",
                    Pos = ThroneSeat,
                    Tex = _assets.Get(SpriteKeys.UnitShah + "_0"),
                    Lift = 16,
                    IsShah = true,
                    FixedFacing = true
                });
            }
            string guardLine = asShah ? "Hazratim! Amringizga muntazirmiz!" : "Shoh huzuridasiz, odob saqlang!";
            _residents.Add(new Resident { Name = "Saroy soqchisi", Short = "Soqchi", Info = "Shoh qo'riqchisi", Line = guardLine, Pos = new Vector2(2.4f, 2.6f), Tex = guard });
            _residents.Add(new Resident { Name = "Saroy soqchisi", Short = "Soqchi", Info = "Shoh qo'riqchisi", Line = guardLine, Pos = new Vector2(5.6f, 2.6f), Tex = guard });
            _residents.Add(new Resident
            {
                Name = "Vazir Nizomiddin", Short = "Vazir", Info = "Bosh vazir",
                Line = asShah ? "Hazratim, xazina to'la, xalq tinch!" : shahHome ? "Hazratim bugun xush kayfiyatdalar." : "Shoh hazratlari shahar sayriga chiqdilar.",
                Pos = new Vector2(2.4f, 4.6f), Tex = vizier
            });
            _residents.Add(new Resident
            {
                Name = "Kotib Umar", Short = "Kotib", Info = "Saroy kotibi",
                Line = "Shohning farmonlarini men yozaman.",
                Pos = new Vector2(5.5f, 5.5f), Tex = scribe
            });
        }

        // ==================================================================
        // Update
        // ==================================================================

        private Resident NearestResident(float maxDist)
        {
            Resident near = null;
            float nd = maxDist;
            foreach (Resident r in _residents)
            {
                float d = Vector2.Distance(r.Pos, _pos);
                if (d < nd) { nd = d; near = r; }
            }
            return near;
        }

        public void Update(float dt, InputManager input)
        {
            if (!Active || _avatar == null) return;
            _time += dt;
            if (input == null) { _avatar.DriveIdle(); return; }

            // Saroyda shoh yonida F — shohga aylanib shaharga chiqish
            if (input.KeyPressed(Keys.F) && _palace)
            {
                Resident near = NearestResident(1.9f);
                if (near != null && near.IsShah) { BecomeShahRequested = true; return; }
            }
            if (input.KeyPressed(Keys.Escape) || input.KeyPressed(Keys.Enter) || input.KeyPressed(Keys.F))
            {
                Exit();
                return;
            }

            if (UpdatePose(dt, input)) return;

            Vector2 screen = Vector2.Zero;
            if (input.KeyDown(Keys.A) || input.KeyDown(Keys.Left)) screen.X -= 1;
            if (input.KeyDown(Keys.D) || input.KeyDown(Keys.Right)) screen.X += 1;
            if (input.KeyDown(Keys.W) || input.KeyDown(Keys.Up)) screen.Y -= 1;
            if (input.KeyDown(Keys.S) || input.KeyDown(Keys.Down)) screen.Y += 1;

            if (screen != Vector2.Zero && dt > 0f)
            {
                screen.Normalize();
                Vector2 dir = IsoMath.WorldToTileF(screen * 32f);
                dir.Normalize();
                float speed = input.KeyDown(Keys.LeftShift) || input.KeyDown(Keys.RightShift) ? RunSpeed : WalkSpeed;
                _avatar.DriveMove(dir, speed);
                Vector2 step = dir * speed * dt;
                if (!TryMove(_pos + step))
                    if (!TryMove(new Vector2(_pos.X + step.X, _pos.Y)))
                        TryMove(new Vector2(_pos.X, _pos.Y + step.Y));
            }
            else _avatar.DriveIdle();

            // Eshikka qaytib kelsa — tashqariga chiqadi
            Point tile = new Point((int)MathF.Floor(_pos.X), (int)MathF.Floor(_pos.Y));
            bool inDoorTile = tile.Y == N - 1 && tile.X >= _doorX0 && tile.X <= _doorX1;
            if (!inDoorTile) _leftDoor = true;
            else if (_leftDoor && _pos.Y > N - 0.45f) Exit();
        }

        // ------------------------------------------------------------------ harakatlar
        private bool IsHouse => House != null && House.Type == BuildingType.House && !_palace;
        private bool NearTable => IsHouse && Vector2.Distance(_pos, _tableCenter) < 1.9f;
        private bool NearBed => IsHouse && Vector2.Distance(_pos, _bedPos) < 1.8f;

        /// <summary>Hozir bajarsa bo'ladigan harakatlar (ekrandagi tugmalar va klaviatura ko'rsatmasi).</summary>
        private void ActionLabels(out string a1, out string a2)
        {
            a1 = a2 = "";
            switch (CurrentPose)
            {
                case Pose.Sit: a1 = "[E] OVQATLANISH"; a2 = "[Q] TURISH"; break;
                case Pose.Eat: a1 = "OVQATLANMOQDA..."; break;
                case Pose.Lie: a1 = "DAM OLMOQDA..."; a2 = "[Q] TURISH"; break;
                default:
                    if (NearTable) a1 = "[E] DASTURXONGA O'TIRISH";
                    else if (NearBed) a1 = "[E] YOTISH";
                    break;
            }
        }

        /// <summary>Sinov: avatarni dasturxon yoki to'shak yoniga qo'yib, holatni o'rnatadi.</summary>
        public void TestPose(string pose)
        {
            if (!IsHouse) return;
            switch ((pose ?? "").ToLowerInvariant())
            {
                case "sit": _pos = new Vector2(6.4f, 6.6f); SitDown(); break;
                case "eat": _pos = new Vector2(6.4f, 6.6f); SitDown(); _state.Food = Math.Max(_state.Food, MealFood); Eat(); _poseTime = 0.5f; break;
                case "lie": _pos = new Vector2(2.4f, 6.2f); LieDown(); _poseTime = 0.4f; break;
                case "table": _pos = new Vector2(6.4f, 6.8f); break;
            }
        }

        /// <summary>Asosiy harakat (E yoki 1-tugma).</summary>
        public void DoPrimary()
        {
            switch (CurrentPose)
            {
                case Pose.Stand:
                    if (NearTable) SitDown();
                    else if (NearBed) LieDown();
                    break;
                case Pose.Sit:
                    Eat();
                    break;
            }
        }

        /// <summary>O'rnidan turish (Q yoki 2-tugma).</summary>
        public void StandUp()
        {
            if (CurrentPose == Pose.Stand || CurrentPose == Pose.Eat) return;
            CurrentPose = Pose.Stand;
            _pos = _standPos;
            _avatar.DriveIdle();
        }

        private void SitDown()
        {
            _standPos = _pos;
            Vector2 best = Seats[0];
            foreach (Vector2 s in Seats) if (Vector2.Distance(s, _pos) < Vector2.Distance(best, _pos)) best = s;
            _pos = best;
            CurrentPose = Pose.Sit;
            _poseTime = 0f;
            _avatar.DriveIdle();
            _avatar.SetFacing(_tableCenter - _pos);
            _state.ShowMessage("Dasturxon atrofida o'tirdingiz. E — ovqatlanish, Q — turish");
        }

        private void Eat()
        {
            if (_state.Food < MealFood)
            {
                _state.ShowMessage("Uyda ovqat yo'q — omborda oziq-ovqat tugagan", true);
                return;
            }
            _state.Food -= MealFood;
            CurrentPose = Pose.Eat;
            _poseTime = 0f;
            _state.ShowMessage("Bismillah! Osh tortildi (-" + MealFood + " oziq-ovqat)");
        }

        private void LieDown()
        {
            _standPos = _pos;
            _pos = _bedPos;
            CurrentPose = Pose.Lie;
            _poseTime = 0f;
            _avatar.DriveIdle();
            _state.ShowMessage("To'shakda dam olyapsiz. Q — turish");
        }

        /// <summary>O'tirgan/yotgan holat. true — bu kadrda yurish yo'q.</summary>
        private bool UpdatePose(float dt, InputManager input)
        {
            ActionLabels(out _act1Label, out _act2Label);
            bool click1 = input.LeftPressed && _act1 != Rectangle.Empty && _act1.Contains(input.MousePosition);
            bool click2 = input.LeftPressed && _act2 != Rectangle.Empty && _act2.Contains(input.MousePosition);
            if (input.KeyPressed(Keys.E) || click1) DoPrimary();
            else if (input.KeyPressed(Keys.Q) || click2) StandUp();

            if (CurrentPose == Pose.Stand) return false;
            _poseTime += dt;
            _avatar.DriveIdle();
            if (CurrentPose == Pose.Eat && _poseTime >= EatTime)
            {
                CurrentPose = Pose.Sit;
                _poseTime = 0f;
                _avatar.HitPoints = Math.Min(_avatar.MaxHitPoints, _avatar.HitPoints + 40);
                _state.ShowMessage("Osh juda mazali bo'ldi! Kuch qaytdi.");
            }
            if (CurrentPose == Pose.Lie && _avatar.HitPoints < _avatar.MaxHitPoints && (int)(_poseTime * 2f) != (int)((_poseTime - dt) * 2f))
                _avatar.HitPoints = Math.Min(_avatar.MaxHitPoints, _avatar.HitPoints + 2);
            // Yurish tugmasi bosilsa — o'rnidan turadi (ovqat tugamaguncha emas)
            bool moveKey = input.KeyDown(Keys.W) || input.KeyDown(Keys.A) || input.KeyDown(Keys.S) || input.KeyDown(Keys.D) ||
                           input.KeyDown(Keys.Up) || input.KeyDown(Keys.Down) || input.KeyDown(Keys.Left) || input.KeyDown(Keys.Right);
            if (moveKey && CurrentPose != Pose.Eat) { StandUp(); return false; }
            return true;
        }

        private bool TryMove(Vector2 p)
        {
            bool inDoor = p.X >= _doorX0 + 0.15f && p.X <= _doorX1 + 0.85f;
            float maxY = inDoor ? N - 0.05f : N - 0.3f;
            if (p.X < 0.25f || p.Y < 0.3f || p.X > N - 0.3f || p.Y > maxY) return false;
            int tx = (int)MathF.Floor(p.X), ty = (int)MathF.Floor(MathF.Min(p.Y, N - 0.01f));
            if (_blocked[tx, ty]) return false;
            foreach (Resident r in _residents)
                if (!r.IsShah && Vector2.Distance(r.Pos, p) < 0.38f && Vector2.Distance(r.Pos, _pos) >= 0.38f) return false;
            _pos = p;
            return true;
        }

        // ==================================================================
        // Chizish
        // ==================================================================

        private Matrix Transform(int vw, int vh)
        {
            float roomW = N * 2 * GameConfig.HalfTileW + 40;
            float roomH = N * 2 * GameConfig.HalfTileH + InteriorArt.WallH + 40;
            float scale = MathHelper.Clamp(MathF.Min((vw - 32) / roomW, (vh - 130) / roomH), 0.9f, 2.4f);
            scale = MathF.Floor(scale * 4f) / 4f; // piksel-art aniq qolishi uchun
            Vector2 focus = new Vector2(0, N * GameConfig.HalfTileH - InteriorArt.WallH / 2f + 6);
            return Matrix.CreateTranslation(-focus.X, -focus.Y, 0) * Matrix.CreateScale(scale, scale, 1f)
                 * Matrix.CreateTranslation(MathF.Round(vw / 2f), MathF.Round(vh / 2f + 16), 0);
        }

        public void Draw(SpriteBatch sb, Texture2D px, PixelFont font, int vw, int vh, float night)
        {
            if (!Active) return;
            Matrix m = Transform(vw, vh);

            // --- Xona ---
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, m);
            sb.Draw(_shell, -_shellOrigin, Color.White);

            var items = new List<(float depth, int layer, Action draw)>();
            foreach (Prop p in _props)
            {
                Prop pp = p;
                items.Add((pp.Depth, 0, () => sb.Draw(pp.Tex, pp.DrawPos, Color.White)));
            }
            foreach (Resident r in _residents)
            {
                Resident rr = r;
                bool left = rr.FixedFacing ? false : FacesLeft(rr.Pos, _pos);
                items.Add((rr.Pos.X + rr.Pos.Y, 1, () => DrawPerson(sb, rr.Tex, rr.Pos, left, rr.Scale, rr.Lift)));
            }
            float avatarDepth = CurrentPose == Pose.Lie ? _bedPos.X + _bedPos.Y + 1.2f : _pos.X + _pos.Y;
            items.Add((avatarDepth, 1, () =>
            {
                Texture2D tex = _avatar.Texture;
                if (tex == null) return;
                Vector2 feet = IsoMath.TileToWorld(_pos);
                var origin = new Vector2(tex.Width / 2f, tex.Height - AssetManager.FootOf(tex));
                float k = AssetManager.ScaleOf(tex);
                SpriteEffects fx = _avatar.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                switch (CurrentPose)
                {
                    case Pose.Sit:
                    case Pose.Eat:
                        // O'tirgan: gavda pastroq (bukilgan), ovqatlanayotganda qo'l/bosh biroz qimirlaydi
                        float bob = CurrentPose == Pose.Eat ? MathF.Abs(MathF.Sin(_poseTime * 7f)) * 1.5f : 0f;
                        sb.Draw(tex, new Vector2(MathF.Round(feet.X), MathF.Round(feet.Y + 3 + bob)), null, Color.White, 0f, origin, new Vector2(k, 0.78f * k), fx, 0f);
                        break;
                    case Pose.Lie:
                        // Yotgan: to'shak ustida, yonboshlab
                        Vector2 bed = IsoMath.TileToWorld(_bedPos) + new Vector2(0, -10);
                        sb.Draw(tex, new Vector2(MathF.Round(bed.X), MathF.Round(bed.Y)), null, Color.White, -MathHelper.PiOver2 * 0.92f, origin, 0.95f * k, SpriteEffects.None, 0f);
                        break;
                    default:
                        sb.Draw(tex, new Vector2(MathF.Round(feet.X), MathF.Round(feet.Y)), null, Color.White, 0f, origin, k, fx, 0f);
                        break;
                }
            }));
            items.Sort((a, b) => a.depth != b.depth ? a.depth.CompareTo(b.depth) : a.layer.CompareTo(b.layer));
            foreach (var it in items) it.draw();
            sb.End();

            // --- Yorug'lik ---
            if (night > 0.05f)
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                sb.Draw(px, new Rectangle(0, 0, vw, vh), new Color(10, 8, 20) * (0.45f * night));
                sb.End();
            }
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, m);
            float flicker = 0.8f + 0.2f * MathF.Sin(_time * 9f) * MathF.Sin(_time * 3.7f + 1f);
            var glowOrigin = new Vector2(_glow.Width / 2f, _glow.Height / 2f);
            if (_palace)
            {
                foreach (Vector2 b in new[] { new Vector2(2.5f, 1.5f), new Vector2(5.5f, 1.5f) })
                {
                    Vector2 w = IsoMath.TileToWorld(b) + new Vector2(0, -34);
                    sb.Draw(_glow, w, null, new Color(255, 160, 60) * ((0.4f + 0.35f * night) * flicker), 0f, glowOrigin, 1.6f, SpriteEffects.None, 0f);
                }
            }
            else
            {
                Vector2 fire = IsoMath.TileToWorld(new Vector2(7.5f, 1.0f)) + new Vector2(0, -8);
                sb.Draw(_glow, fire, null, new Color(255, 140, 50) * ((0.35f + 0.4f * night) * flicker), 0f, glowOrigin, 1.5f, SpriteEffects.None, 0f);
                if (night > 0.05f)
                {
                    Vector2 lamp = IsoMath.TileToWorld(new Vector2(2.6f, 3.0f)) + new Vector2(0, -40);
                    sb.Draw(_glow, lamp, null, new Color(255, 200, 120) * (0.45f * night), 0f, glowOrigin, 3.2f, SpriteEffects.None, 0f);
                }
            }
            sb.End();

            // --- Ekran qatlami ---
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            Vector2 ToScreen(Vector2 tile, float lift) => Vector2.Transform(IsoMath.TileToWorld(tile) + new Vector2(0, -lift), m);

            Color roomLabel = new Color(240, 226, 196), roomBg = new Color(0, 0, 0, 110);
            if (_palace)
            {
                Label(sb, px, font, ToScreen(new Vector2(0.6f, 7.6f), -12), "TAXT ZALI", roomLabel, roomBg);
            }
            else
            {
                Label(sb, px, font, ToScreen(new Vector2(2.0f, 7.4f), -6), "YOTOQXONA", roomLabel, roomBg);
                Label(sb, px, font, ToScreen(new Vector2(7.7f, 6.6f), -14), "OSHXONA", roomLabel, roomBg);
            }
            Label(sb, px, font, ToScreen(new Vector2((_doorX0 + _doorX1 + 1) / 2f, N + 0.5f), -10), "CHIQISH", new Color(255, 226, 150), new Color(0, 0, 0, 130));

            Resident near = NearestResident(1.5f);
            if (near == null && _palace) { Resident s = NearestResident(1.9f); if (s != null && s.IsShah) near = s; }
            foreach (Resident r in _residents)
            {
                float lift = (r.Scale < 1f ? 40 : 54) + r.Lift + (r.IsShah ? 8 : 0);
                Vector2 sp = ToScreen(r.Pos, lift);
                bool isNear = r == near;
                string name = isNear ? r.Name : r.Short;
                Color nameCol = r.IsShah ? new Color(255, 214, 90) : isNear ? new Color(255, 220, 120) : new Color(230, 220, 200);
                Label(sb, px, font, sp, name, nameCol, new Color(0, 0, 0, 140));
                if (isNear)
                {
                    if (!string.IsNullOrEmpty(r.Info)) Label(sb, px, font, sp + new Vector2(0, 16), r.Info, new Color(200, 190, 170), new Color(0, 0, 0, 120));
                    Bubble(sb, px, font, sp + new Vector2(0, -26), r.Line);
                    if (r.IsShah) Label(sb, px, font, sp + new Vector2(0, 34), "[F] SHOH BO'LIB SHAHARGA CHIQISH", new Color(255, 214, 90), new Color(60, 20, 10, 220));
                }
            }

            // Sarlavha
            Vector2 tsize = font.Measure(_title, 3);
            Vector2 ssize = font.Measure(_subtitle, 2);
            int headW = (int)MathF.Max(tsize.X, ssize.X) + 48;
            var head = new Rectangle(vw / 2 - headW / 2, 10, headW, 60);
            sb.Draw(px, new Rectangle(head.X - 2, head.Y - 2, head.Width + 4, head.Height + 4), new Color(216, 167, 83, 230));
            sb.Draw(px, head, new Color(28, 22, 18, 235));
            font.DrawCentered(sb, _title, new Vector2(head.Center.X, head.Y + 19), new Color(255, 214, 90), 3);
            font.DrawCentered(sb, _subtitle, new Vector2(head.Center.X, head.Y + 44), new Color(210, 196, 170), 2);

            // Ovqat bug'i va uyqu "Zzz"
            if (CurrentPose == Pose.Eat)
                for (int i = 0; i < 3; i++)
                {
                    float u = (_poseTime * 0.8f + i / 3f) % 1f;
                    Vector2 sp = ToScreen(_tableCenter, 18 + u * 30) + new Vector2((i - 1) * 8 + MathF.Sin(_poseTime * 3f + i) * 3f, 0);
                    sb.Draw(px, new Rectangle((int)sp.X, (int)sp.Y, 4, 4), new Color(255, 255, 255) * (0.6f * (1f - u)));
                }
            if (CurrentPose == Pose.Lie)
            {
                float u = _poseTime * 0.6f % 1f;
                Vector2 zp = ToScreen(_bedPos, 40 + u * 26) + new Vector2(10 + u * 12, 0);
                font.Draw(sb, "Z", zp, new Color(230, 236, 255) * (1f - u), 2);
                font.Draw(sb, "z", zp + new Vector2(-12, 14), new Color(230, 236, 255) * (0.7f * (1f - u)), 2);
            }

            // Harakat tugmalari (klaviatura: E / Q; sensorli ekranda bosiladi)
            _act1 = _act2 = Rectangle.Empty;
            ActionLabels(out _act1Label, out _act2Label);
            int abh = 34, ay = vh - 92;
            int w1 = _act1Label.Length > 0 ? (int)font.Measure(_act1Label, 2).X + 28 : 0;
            int w2 = _act2Label.Length > 0 ? (int)font.Measure(_act2Label, 2).X + 28 : 0;
            int ax = vw / 2 - (w1 + w2 + (w1 > 0 && w2 > 0 ? 10 : 0)) / 2;
            if (w1 > 0)
            {
                _act1 = new Rectangle(ax, ay, w1, abh);
                sb.Draw(px, new Rectangle(_act1.X - 2, _act1.Y - 2, _act1.Width + 4, _act1.Height + 4), new Color(216, 167, 83, 240));
                sb.Draw(px, _act1, new Color(56, 42, 22, 245));
                font.DrawCentered(sb, _act1Label, _act1.Center.ToVector2(), new Color(255, 214, 90), 2);
                ax += w1 + 10;
            }
            if (w2 > 0)
            {
                _act2 = new Rectangle(ax, ay, w2, abh);
                sb.Draw(px, new Rectangle(_act2.X - 2, _act2.Y - 2, _act2.Width + 4, _act2.Height + 4), new Color(150, 116, 66, 240));
                sb.Draw(px, _act2, new Color(36, 28, 22, 245));
                font.DrawCentered(sb, _act2Label, _act2.Center.ToVector2(), new Color(234, 224, 202), 2);
            }

            string hint = "WASD yurish | SHIFT yugurish | ENTER / ESC yoki eshik - tashqariga chiqish";
            Vector2 hs = font.Measure(hint, 2);
            var hr = new Rectangle(vw / 2 - (int)hs.X / 2 - 16, vh - 40, (int)hs.X + 32, 28);
            sb.Draw(px, hr, new Color(20, 16, 14, 210));
            font.DrawCentered(sb, hint, hr.Center.ToVector2(), new Color(220, 205, 175), 2);

            if (_state.MessageTimer > 0f && !string.IsNullOrEmpty(_state.Message))
                Label(sb, px, font, new Vector2(vw / 2f, 90), _state.Message, _state.MessageIsError ? new Color(255, 140, 120) : new Color(150, 230, 150), new Color(0, 0, 0, 170));
            sb.End();
        }

        private static bool FacesLeft(Vector2 who, Vector2 target)
        {
            float sx = IsoMath.TileToWorld(target).X - IsoMath.TileToWorld(who).X;
            return sx < 0f;
        }

        private static void DrawPerson(SpriteBatch sb, Texture2D tex, Vector2 tile, bool flip, float scale, float lift)
        {
            Vector2 feet = IsoMath.TileToWorld(tile) + new Vector2(0, -lift);
            sb.Draw(tex, new Vector2(MathF.Round(feet.X), MathF.Round(feet.Y)), null, Color.White, 0f,
                new Vector2(tex.Width / 2f, tex.Height - AssetManager.FootOf(tex)), scale * AssetManager.ScaleOf(tex),
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        }

        private static void Label(SpriteBatch sb, Texture2D px, PixelFont font, Vector2 center, string text, Color fg, Color bg)
        {
            Vector2 s = font.Measure(text, 2);
            var r = new Rectangle((int)(center.X - s.X / 2 - 5), (int)(center.Y - s.Y / 2 - 3), (int)s.X + 10, (int)s.Y + 6);
            sb.Draw(px, r, bg);
            font.Draw(sb, text, new Vector2(r.X + 5, r.Y + 3), fg, 2);
        }

        private static void Bubble(SpriteBatch sb, Texture2D px, PixelFont font, Vector2 bottomCenter, string text)
        {
            Vector2 s = font.Measure(text, 2);
            var r = new Rectangle((int)(bottomCenter.X - s.X / 2 - 8), (int)(bottomCenter.Y - s.Y - 12), (int)s.X + 16, (int)s.Y + 12);
            sb.Draw(px, new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), new Color(60, 44, 30));
            sb.Draw(px, r, new Color(252, 246, 232));
            sb.Draw(px, new Rectangle((int)bottomCenter.X - 3, r.Bottom, 6, 5), new Color(252, 246, 232));
            font.Draw(sb, text, new Vector2(r.X + 8, r.Y + 6), new Color(50, 36, 26), 2);
        }

        /// <summary>Shahar ko'rinishida: bino yonida turganda "ENTER — kirish" ko'rsatmasi.</summary>
        public void DrawPrompt(SpriteBatch sb, Texture2D px, PixelFont font, CameraManager cam, int vw, int vh)
        {
            if (Active || NearbyHouse == null) return;
            Building h = NearbyHouse;
            Vector2 top = cam.WorldToScreen(IsoMath.TileToWorld(new Vector2(h.Position.X + h.Size.X, h.Position.Y + h.Size.Y)) + new Vector2(0, -h.Texture.Height - 6));
            bool denied = !MayEnter(h, _nearbyWho);
            string t = denied ? "SAROYGA FAQAT SOQCHILAR KIRADI"
                : h.Type == BuildingType.MainHall ? "[ENTER] SAROYGA KIRISH"
                : h.Type == BuildingType.GladiatorArena ? "[ENTER] JANG MAYDONIGA KIRISH"
                : "[ENTER] " + h.Name.ToUpperInvariant() + "GA KIRISH";
            Vector2 s = font.Measure(t, 2);
            var r = new Rectangle((int)(top.X - s.X / 2 - 8), (int)(top.Y - s.Y - 8), (int)s.X + 16, (int)s.Y + 10);
            r.X = Math.Clamp(r.X, 8, vw - r.Width - 8);
            r.Y = Math.Clamp(r.Y, 44, vh - r.Height - 140);
            sb.Draw(px, new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), new Color(216, 167, 83, 230));
            sb.Draw(px, r, new Color(28, 22, 18, 235));
            font.DrawCentered(sb, t, r.Center.ToVector2(), denied ? new Color(255, 130, 110) : new Color(255, 214, 90), 2);
        }
    }

    internal static class CanvasOriginExt
    {
        public static (Canvas, Vector2) WithOrigin(this Canvas c, Vector2 origin) => (c, origin);
    }
}
