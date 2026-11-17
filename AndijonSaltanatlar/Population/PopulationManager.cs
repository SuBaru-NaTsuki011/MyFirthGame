using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Economy;
using AndijonSaltanatlar.Government;

namespace AndijonSaltanatlar.Population
{
    /// <summary>
    /// Shahar aholisi: fuqarolar, oilalar, kasblar, ehtiyojlar va baxt. Har o'yin kunida bir marta
    /// hisoblanadi. PROMPT 17 da kengaytirildi:
    ///  - har bir fuqaro ANIQ ish joyiga (bino) biriktiriladi va bu biriktiruv kunma-kun barqaror;
    ///  - Save/Load fuqaroning shaxsini (Id, ism, oila, uy, ish joyi) saqlaydi;
    ///  - yangi qurilgan bo'sh uyga yangi oila ko'chib keladi (yangi mahalla to'ladi);
    ///  - oilaning oziq-ovqat bilan ta'minlanishi (Household.FedRatio) baxt va ochlikka ta'sir qiladi.
    /// </summary>
    public sealed class PopulationManager
    {
        private static readonly string[] GivenNames =
        {
            "Bobur", "Alisher", "Zulfiya", "Nodira", "Tohir", "Oydin", "Sardor", "Gulnora", "Jahongir", "Dilnoza",
            "Akbar", "Malika", "Ulug'bek", "Sevara", "Qodir", "Ra'no", "Abdulloh", "Mohira", "Rustam", "Lola",
            "Anvar", "Shahlo", "Ibrohim", "Nargiza"
        };

        private static readonly string[] FamilyNames = { "Andijoniy", "Asakaviy", "Marg'iloniy", "Farg'oniy", "Qo'qoniy", "Namangoniy" };

        /// <summary>Ishga joylash tartibi (muhimlik bo'yicha).</summary>
        private static readonly CitizenProfession[] JobOrder =
        {
            CitizenProfession.Dehqon, CitizenProfession.Bogbon, CitizenProfession.Savdogar, CitizenProfession.Tashuvchi,
            CitizenProfession.Soqchi, CitizenProfession.Amaldor, CitizenProfession.Muallim
        };

        private readonly Random _rng = new Random(1560);
        private readonly List<Citizen> _citizens = new List<Citizen>();
        private readonly List<Household> _households = new List<Household>();
        private readonly Dictionary<int, Citizen> _byId = new Dictionary<int, Citizen>();
        private readonly Dictionary<int, Household> _hhById = new Dictionary<int, Household>();
        private readonly Dictionary<CitizenProfession, int> _profCount = new Dictionary<CitizenProfession, int>();
        private int _nextCitizenId = 1;
        private int _nextHouseholdId = 1;

        public bool Initialized { get; private set; }
        public int Count => _citizens.Count;
        public int Workers { get; private set; }
        public int Unemployed => Count - Workers;
        public int Families => _households.Count;
        public int HousingCapacity { get; private set; }
        public int HousingUsed => Count;
        /// <summary>Shahardagi jami ish o'rinlari (band + bo'sh).</summary>
        public int JobCapacity { get; private set; }
        public int FreeJobs => Math.Max(0, JobCapacity - Workers);
        public float AvgHappiness { get; private set; } = 70f;
        public float AvgHealth { get; private set; } = 100f;
        public float AvgEducation { get; private set; }
        public float AvgSecurity { get; private set; }

        public IReadOnlyList<Citizen> Citizens => _citizens;
        public IReadOnlyList<Household> Households => _households;

        public Citizen GetById(int id) => _byId.TryGetValue(id, out Citizen c) ? c : null;
        public Household GetHousehold(int id) => _hhById.TryGetValue(id, out Household h) ? h : null;

        public int CountProfession(CitizenProfession p) => _profCount.TryGetValue(p, out int v) ? v : 0;

        /// <summary>Bino turi nechta va qaysi kasbdagi ish o'rni beradi (0 = ish joyi emas).</summary>
        public static int JobSlots(BuildingType type, out CitizenProfession profession)
        {
            switch (type)
            {
                case BuildingType.Farm: profession = CitizenProfession.Dehqon; return 6;
                case BuildingType.Garden: profession = CitizenProfession.Bogbon; return 5;
                case BuildingType.Market: profession = CitizenProfession.Savdogar; return 4;
                case BuildingType.Caravanserai: profession = CitizenProfession.Tashuvchi; return 3;
                case BuildingType.Barracks: profession = CitizenProfession.Soqchi; return 6;
                case BuildingType.MainHall: profession = CitizenProfession.Amaldor; return 4;
                case BuildingType.Madrasa: profession = CitizenProfession.Muallim; return 3;
                default: profession = CitizenProfession.None; return 0;
            }
        }

        public static string ProfessionName(CitizenProfession p)
        {
            switch (p)
            {
                case CitizenProfession.Dehqon: return "Dehqon";
                case CitizenProfession.Bogbon: return "Bog'bon";
                case CitizenProfession.Savdogar: return "Savdogar";
                case CitizenProfession.Hunarmand: return "Hunarmand";
                case CitizenProfession.Quruvchi: return "Quruvchi";
                case CitizenProfession.Tabib: return "Tabib";
                case CitizenProfession.Muallim: return "Muallim";
                case CitizenProfession.Soqchi: return "Soqchi";
                case CitizenProfession.Amaldor: return "Amaldor";
                case CitizenProfession.Tashuvchi: return "Karvonchi";
                default: return "Ishsiz";
            }
        }

        // ------------------------------------------------------------------
        // Boshlang'ich aholi
        // ------------------------------------------------------------------
        public void Initialize(BuildingManager buildings)
        {
            ClearAll();
            ComputeHousing(buildings);
            int target = Math.Min(42, Math.Max(14, HousingCapacity * 3 / 5));
            foreach (Building b in buildings.Buildings)
            {
                if (Count >= target) break;
                if (b.Type == BuildingType.House) CreateHousehold(b.Id, b.PopulationCapacity, -1);
            }
            AssignJobs(buildings);
            RecomputeStats(buildings);
            Initialized = true;
        }

        private void ClearAll()
        {
            _citizens.Clear();
            _households.Clear();
            _byId.Clear();
            _hhById.Clear();
            _nextCitizenId = 1;
            _nextHouseholdId = 1;
        }

        private Household CreateHousehold(int homeId, int cap, int members)
        {
            if (cap <= 0) cap = 5;
            var hh = new Household { Id = _nextHouseholdId++, HomeBuildingId = homeId, FamilyName = Pick(FamilyNames) };
            int n = members > 0 ? members : Math.Max(1, Math.Min(cap, 2 + _rng.Next(cap >= 4 ? 3 : 2)));
            _households.Add(hh);
            _hhById[hh.Id] = hh;
            for (int i = 0; i < n; i++) AddMember(hh, 16 + _rng.Next(50), 64 + _rng.Next(16));
            return hh;
        }

        private Citizen AddMember(Household hh, int age, float happiness)
        {
            var c = new Citizen
            {
                Id = _nextCitizenId++,
                HouseholdId = hh.Id,
                HomeBuildingId = hh.HomeBuildingId,
                Name = Pick(GivenNames) + " " + hh.FamilyName,
                Age = age,
                Happiness = happiness,
                CurrentState = CitizenState.AtHome
            };
            hh.MemberIds.Add(c.Id);
            _citizens.Add(c);
            _byId[c.Id] = c;
            return c;
        }

        private string Pick(string[] a) => a[_rng.Next(a.Length)];

        private void ComputeHousing(BuildingManager b)
        {
            int cap = 0;
            foreach (Building x in b.Buildings) cap += x.PopulationCapacity;
            HousingCapacity = cap;
        }

        // ------------------------------------------------------------------
        // Ish joylari — har bir fuqaro aniq binoga biriktiriladi (barqaror)
        // ------------------------------------------------------------------
        public void AssignJobs(BuildingManager b)
        {
            // 1) Har bir ish beruvchi binodagi bo'sh o'rinlar.
            var free = new Dictionary<int, int>();
            var profOf = new Dictionary<int, CitizenProfession>();
            var order = new List<Building>();
            int capacity = 0;
            foreach (Building x in b.Buildings)
            {
                int slots = JobSlots(x.Type, out CitizenProfession p);
                if (slots <= 0) continue;
                free[x.Id] = slots;
                profOf[x.Id] = p;
                order.Add(x);
                capacity += slots;
            }
            JobCapacity = capacity;

            // 2) Mavjud ish joyi hali ham bor bo'lsa — fuqaro o'z ishida qoladi.
            foreach (Citizen c in _citizens)
            {
                int wid = c.WorkplaceBuildingId ?? 0;
                if (c.IsEmployed && wid != 0 && free.TryGetValue(wid, out int left) && left > 0 && profOf[wid] == c.Profession)
                {
                    free[wid] = left - 1;
                    c.CurrentState = CitizenState.Working;
                }
                else
                {
                    c.IsEmployed = false;
                    c.Profession = CitizenProfession.None;
                    c.WorkplaceBuildingId = null;
                }
            }

            // 3) Ishsizlarni bo'sh o'rinlarga (kasblar muhimligi tartibida) joylash.
            foreach (Citizen c in _citizens)
            {
                if (c.IsEmployed) continue;
                foreach (CitizenProfession p in JobOrder)
                {
                    Building target = null;
                    foreach (Building x in order)
                        if (profOf[x.Id] == p && free[x.Id] > 0) { target = x; break; }
                    if (target == null) continue;
                    free[target.Id]--;
                    c.Profession = p;
                    c.IsEmployed = true;
                    c.WorkplaceBuildingId = target.Id;
                    c.CurrentState = CitizenState.Working;
                    break;
                }
            }
            RecountProfessions();
        }

        private void RecountProfessions()
        {
            _profCount.Clear();
            Workers = 0;
            foreach (Citizen c in _citizens)
            {
                if (c.IsEmployed) Workers++;
                _profCount[c.Profession] = CountProfession(c.Profession) + 1;
            }
        }

        // ------------------------------------------------------------------
        // Kunlik hisob
        // ------------------------------------------------------------------
        public void DailyTick(BuildingManager b, ResourceManager res, GovernmentPolicy policy,
                              float healthBonus = 0f, float eduBonus = 0f, float stabilityBonus = 0f)
        {
            ComputeHousing(b);
            bool housed = HousingUsed <= HousingCapacity;
            bool cityHasFood = res.Get(ResourceType.Food) > 0;
            int guards = b.Count(BuildingType.Barracks) + b.Count(BuildingType.Tower);
            int schools = b.Count(BuildingType.Madrasa);
            float taxPain = policy.PopulationTax * 0.6f;

            foreach (Citizen c in _citizens)
            {
                // Oilaning bugungi ta'minoti (1 = to'q, 0.5 = yarim ulush, 0 = och). Bozor/oila iqtisodi belgilaydi.
                Household hh = GetHousehold(c.HouseholdId);
                float fed = cityHasFood ? (hh != null ? hh.FedRatio : 1f) : 0f;
                float comfort = hh != null ? hh.Comfort : 0f;

                if (fed >= 0.99f) c.Hunger = Math.Max(0f, c.Hunger - 20f);
                else if (fed >= 0.45f) c.Hunger = Approach(c.Hunger, 45f, 12f);
                else c.Hunger = Math.Min(100f, c.Hunger + 25f);

                c.Energy = Math.Min(100f, c.Energy + 10f);
                float eduTarget = (schools > 0 ? Math.Min(100, 55 + schools * 10) : 20) * (1f + eduBonus);
                c.Education = Approach(c.Education, Math.Min(100f, eduTarget), 4f);
                c.Health = Approach(c.Health, (fed >= 0.45f ? 90 : 55) + healthBonus * 20f, 6f);

                float target = 50f;
                target += c.IsEmployed ? 14 : -16;
                target += housed ? 10 : -22;
                target += fed >= 0.99f ? 10 : (fed >= 0.45f ? -4 : -20);
                target += guards > 0 ? 6 : -4;
                target += stabilityBonus * 20f;
                target += comfort * 4f;
                target -= taxPain;
                target -= c.Hunger * 0.2f;
                c.Happiness = Clamp100(Approach(c.Happiness, target, 8f));
                c.CurrentState = c.IsEmployed ? CitizenState.Working : CitizenState.Idle;
            }

            foreach (Household hh in _households)
            {
                float sum = 0; int n = 0;
                foreach (int id in hh.MemberIds) { Citizen m = GetById(id); if (m != null) { sum += m.Happiness; n++; } }
                if (n > 0) hh.Happiness = sum / n;
            }
            RecomputeStats(b);
        }

        public void MonthlyGrowth(BuildingManager b, ResourceManager res)
        {
            ComputeHousing(b);
            int room = HousingCapacity - HousingUsed;
            if (AvgHappiness > 58f && res.Get(ResourceType.Food) > Count && room > 0)
            {
                int n = Math.Max(1, Math.Min(room, room / 4 + 1));
                for (int i = 0; i < n; i++) AddImmigrant(b);
            }
            else if (AvgHappiness < 30f && Count > 6)
            {
                int n = 1 + _rng.Next(2);
                for (int i = 0; i < n && _citizens.Count > 6; i++) RemoveCitizen(_citizens[_citizens.Count - 1]);
            }
            AssignJobs(b);
            RecomputeStats(b);
        }

        /// <summary>
        /// Yangi kelgan fuqaro: avval bo'sh turgan uyga yangi oila bo'lib joylashadi (yangi mahalla shunday
        /// to'ladi), bo'sh uy bo'lmasa — eng kam a'zoli, joyi bor oilaga qo'shiladi.
        /// </summary>
        private void AddImmigrant(BuildingManager b)
        {
            var occupied = new HashSet<int>();
            foreach (Household h in _households) if (h.HomeBuildingId.HasValue) occupied.Add(h.HomeBuildingId.Value);
            if (b != null)
                foreach (Building x in b.Buildings)
                    if (x.Type == BuildingType.House && x.BuildProgress >= 1f && !occupied.Contains(x.Id))
                    {
                        CreateHousehold(x.Id, x.PopulationCapacity, 1);
                        _citizens[_citizens.Count - 1].Happiness = 62f;
                        return;
                    }

            Household best = null;
            foreach (Household h in _households)
                if (h.MemberIds.Count < 5 && (best == null || h.MemberIds.Count < best.MemberIds.Count)) best = h;
            if (best == null && _households.Count > 0) best = _households[_rng.Next(_households.Count)];
            if (best != null) AddMember(best, 16 + _rng.Next(30), 60f);
        }

        private void RemoveCitizen(Citizen c)
        {
            _citizens.Remove(c);
            _byId.Remove(c.Id);
            Household hh = GetHousehold(c.HouseholdId);
            if (hh == null) return;
            hh.MemberIds.Remove(c.Id);
            if (hh.MemberIds.Count == 0) { _households.Remove(hh); _hhById.Remove(hh.Id); }
        }

        public void AdjustHappiness(float delta)
        {
            foreach (Citizen c in _citizens) c.Happiness = Clamp100(c.Happiness + delta);
            RecomputeStats(null);
        }

        private void RecomputeStats(BuildingManager b)
        {
            if (_citizens.Count == 0)
            {
                AvgHappiness = AvgHealth = 50f;
                AvgEducation = 0f;
                return;
            }
            float h = 0, he = 0, ed = 0;
            foreach (Citizen c in _citizens) { h += c.Happiness; he += c.Health; ed += c.Education; }
            AvgHappiness = h / _citizens.Count;
            AvgHealth = he / _citizens.Count;
            AvgEducation = ed / _citizens.Count;
            if (b != null) AvgSecurity = Math.Min(100, b.Count(BuildingType.Barracks) * 20 + b.Count(BuildingType.Tower) * 6);
        }

        private static float Approach(float cur, float target, float step)
        {
            if (cur < target) return Math.Min(target, cur + step);
            if (cur > target) return Math.Max(target, cur - step);
            return cur;
        }

        private static float Clamp100(float v) => v < 0f ? 0f : (v > 100f ? 100f : v);

        // ------------------------------------------------------------------
        // Save / Load — fuqaroning shaxsi saqlanadi
        // ------------------------------------------------------------------
        public List<CitizenSnapshot> Snapshot(BuildingManager b = null)
        {
            var list = new List<CitizenSnapshot>();
            foreach (Citizen c in _citizens)
            {
                var s = new CitizenSnapshot
                {
                    Id = c.Id, Name = c.Name, HouseholdId = c.HouseholdId,
                    Profession = (int)c.Profession, Age = c.Age, Happiness = c.Happiness, Health = c.Health,
                    Education = c.Education, Hunger = c.Hunger, Energy = c.Energy, Money = c.PersonalMoney,
                    Employed = c.IsEmployed
                };
                Building work = FindBuilding(b, c.WorkplaceBuildingId);
                if (work != null) { s.WorkX = work.Position.X; s.WorkY = work.Position.Y; }
                list.Add(s);
            }
            return list;
        }

        public List<HouseholdSnapshot> SnapshotHouseholds(BuildingManager b)
        {
            var list = new List<HouseholdSnapshot>();
            foreach (Household h in _households)
            {
                var s = new HouseholdSnapshot
                {
                    Id = h.Id, FamilyName = h.FamilyName, Money = h.SharedMoney, FoodReserve = h.FoodReserve,
                    FedRatio = h.FedRatio, Comfort = h.Comfort, LastIncome = h.LastIncome, LastExpense = h.LastExpense
                };
                Building home = FindBuilding(b, h.HomeBuildingId);
                if (home != null) { s.HomeX = home.Position.X; s.HomeY = home.Position.Y; }
                list.Add(s);
            }
            return list;
        }

        private static Building FindBuilding(BuildingManager b, int? id)
        {
            if (b == null || !id.HasValue) return null;
            foreach (Building x in b.Buildings) if (x.Id == id.Value) return x;
            return null;
        }

        public void Restore(List<CitizenSnapshot> snap, BuildingManager b, List<HouseholdSnapshot> households = null)
        {
            if (snap == null || snap.Count == 0) return;
            bool identity = households != null && households.Count > 0 && snap[0].Id > 0;
            ClearAll();
            if (identity) RestoreIdentity(snap, households, b);
            else RestoreLegacy(snap, b);
            ComputeHousing(b);
            AssignJobs(b);
            RecomputeStats(b);
            Initialized = true;
        }

        /// <summary>PROMPT 17 formati: har bir fuqaro o'z Id, ismi, oilasi, uyi va ish joyi bilan tiklanadi.</summary>
        private void RestoreIdentity(List<CitizenSnapshot> snap, List<HouseholdSnapshot> households, BuildingManager b)
        {
            foreach (HouseholdSnapshot hs in households)
            {
                var hh = new Household
                {
                    Id = hs.Id, FamilyName = hs.FamilyName ?? "", SharedMoney = hs.Money, FoodReserve = hs.FoodReserve,
                    FedRatio = hs.FedRatio, Comfort = hs.Comfort, LastIncome = hs.LastIncome, LastExpense = hs.LastExpense
                };
                Building home = hs.HomeX >= 0 ? b.GetAt(hs.HomeX, hs.HomeY) : null;
                hh.HomeBuildingId = home?.Id;
                _households.Add(hh);
                _hhById[hh.Id] = hh;
                _nextHouseholdId = Math.Max(_nextHouseholdId, hh.Id + 1);
            }
            foreach (CitizenSnapshot s in snap)
            {
                Household hh = GetHousehold(s.HouseholdId);
                if (hh == null) continue; // buzuq yozuv — o'tkazib yuboramiz
                var c = new Citizen
                {
                    Id = s.Id, Name = s.Name ?? "", HouseholdId = hh.Id, HomeBuildingId = hh.HomeBuildingId,
                    Age = s.Age, Happiness = s.Happiness, Health = s.Health, Education = s.Education,
                    Hunger = s.Hunger, Energy = s.Energy, PersonalMoney = s.Money,
                    IsEmployed = s.Employed, Profession = (CitizenProfession)s.Profession
                };
                Building work = s.WorkX >= 0 ? b.GetAt(s.WorkX, s.WorkY) : null;
                c.WorkplaceBuildingId = work?.Id;
                if (work == null) { c.IsEmployed = false; c.Profession = CitizenProfession.None; }
                hh.MemberIds.Add(c.Id);
                _citizens.Add(c);
                _byId[c.Id] = c;
                _nextCitizenId = Math.Max(_nextCitizenId, c.Id + 1);
            }
        }

        /// <summary>Eski (PROMPT 17 gacha) save: shaxs ma'lumoti yo'q — oilalar qayta tuziladi.</summary>
        private void RestoreLegacy(List<CitizenSnapshot> snap, BuildingManager b)
        {
            var houses = new List<int>();
            foreach (Building x in b.Buildings) if (x.Type == BuildingType.House) houses.Add(x.Id);
            Household hh = null;
            int inHouse = 0, hi = 0;
            foreach (CitizenSnapshot s in snap)
            {
                if (hh == null || inHouse >= 5)
                {
                    hh = new Household
                    {
                        Id = _nextHouseholdId++, FamilyName = Pick(FamilyNames),
                        HomeBuildingId = houses.Count > 0 ? houses[hi++ % houses.Count] : (int?)null
                    };
                    _households.Add(hh);
                    _hhById[hh.Id] = hh;
                    inHouse = 0;
                }
                Citizen c = AddMember(hh, s.Age, s.Happiness);
                c.Health = s.Health; c.Education = s.Education; c.Hunger = s.Hunger; c.Energy = s.Energy;
                c.PersonalMoney = s.Money;
                inHouse++;
            }
        }
    }
}
