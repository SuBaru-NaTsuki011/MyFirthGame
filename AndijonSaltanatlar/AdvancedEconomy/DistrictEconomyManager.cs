using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.LandExpansion;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.AdvancedEconomy
{
    /// <summary>Fuqaro/mahalla muammolari kalitlari (dialog shartlari, hisobotlar va vazifalar shularga tayanadi).</summary>
    public static class ConcernKeys
    {
        public const string Unemployed = "unemployed";
        public const string Poverty = "poverty";
        public const string FoodPrice = "food_price";
        public const string Housing = "housing";
        public const string NoRoad = "no_road";
        public const string NoMarket = "no_market";
        public const string NoSchool = "no_school";
        public const string HighTax = "high_tax";
        public const string Security = "security";

        public static readonly string[] All = { Unemployed, Poverty, Housing, FoodPrice, HighTax, NoRoad, NoMarket, Security, NoSchool };

        public static string LabelUz(string key)
        {
            switch (key)
            {
                case Unemployed: return "Ish o'rinlari yetishmaydi";
                case Poverty: return "Oziq-ovqatga pul yetmaydi";
                case FoodPrice: return "Oziq-ovqat qimmatladi";
                case Housing: return "Uy-joy tor";
                case NoRoad: return "Mahallaga yo'l kerak";
                case NoMarket: return "Bozor yetishmaydi";
                case NoSchool: return "Maktab yo'q";
                case HighTax: return "Soliq og'ir";
                case Security: return "Xavfsizlik sust";
                default: return "Muammo yo'q";
            }
        }

        /// <summary>Muammoning ustuvorligi (katta = muhimroq).</summary>
        public static int Weight(string key)
        {
            switch (key)
            {
                case Unemployed: return 95;
                case Poverty: return 90;
                case Housing: return 85;
                case FoodPrice: return 75;
                case HighTax: return 70;
                case NoRoad: return 65;
                case NoMarket: return 60;
                case Security: return 55;
                case NoSchool: return 50;
                default: return 0;
            }
        }
    }

    /// <summary>Bitta mahallaning iqtisodiy va ijtimoiy statistikasi (har o'yin kunida qayta hisoblanadi).</summary>
    public sealed class DistrictStats
    {
        public int Id;
        public string NameUz = "";
        /// <summary>Mahalla markazi (tile) — kamerani fokuslash uchun.</summary>
        public int CenterX, CenterY;
        public bool IsOpen = true;

        public int Population;
        public int Households;
        public int Houses;
        public int HousingCapacity;
        public int Jobs;
        public int Employed;
        public int Unemployed;
        public int Markets;
        /// <summary>Mahalliy ishlab chiqarish (kunlik oziq-ovqat birligi, bog'lar hisobidan).</summary>
        public int LocalProduction;
        public int HousesConnected;
        public float Happiness, Health, Education, Security;
        public float AvgSavings;
        public int UnderfedHouseholds;

        /// <summary>Haqiqiy ehtiyojlar (ConcernKeys), muhimlik bo'yicha saralangan.</summary>
        public List<string> Needs = new List<string>();
        /// <summary>Har bir muammodan xavotirdagi fuqarolar soni.</summary>
        public Dictionary<string, int> ConcernCounts = new Dictionary<string, int>();

        public int RoadConnectionPercent => Houses <= 0 ? 100 : HousesConnected * 100 / Houses;
        public int EmploymentPercent => Population <= 0 ? 0 : Employed * 100 / Population;

        /// <summary>Rivojlanish darajasi 0..5.</summary>
        public int DevelopmentLevel
        {
            get
            {
                if (Population == 0) return 0;
                int lv = 1;
                if (Population >= 10) lv++;
                if (Markets >= 1 || Jobs >= Population / 2) lv++;
                if (RoadConnectionPercent >= 80 && Happiness >= 60) lv++;
                if (Needs.Count == 0 && Happiness >= 70) lv++;
                return Math.Min(5, lv);
            }
        }

        public string DevelopmentName
        {
            get
            {
                switch (DevelopmentLevel)
                {
                    case 0: return "Bo'sh hudud";
                    case 1: return "Yangi mahalla";
                    case 2: return "Rivojlanayotgan";
                    case 3: return "O'rta";
                    case 4: return "Rivojlangan";
                    default: return "Gullagan";
                }
            }
        }

        public int CountOf(string concern) => ConcernCounts.TryGetValue(concern, out int v) ? v : 0;
    }

    /// <summary>
    /// Mahallalar iqtisodiyoti. Mahalla = markaziy shahar (Id 0) yoki shahar yer uchastkasi (LandParcel.Id).
    /// Statistika faqat haqiqiy fuqaro, bino va yo'l ma'lumotlaridan hisoblanadi.
    /// </summary>
    public sealed class DistrictEconomyManager
    {
        public const int CentralId = 0;
        public const string CentralName = "Markaziy shahar";
        /// <summary>Uy shu masofa (tile) ichida ko'cha/maydonga yaqin bo'lsa "yo'lga ulangan" hisoblanadi.</summary>
        public const int RoadRadius = 4;

        private readonly List<DistrictStats> _districts = new List<DistrictStats>();
        private readonly Dictionary<int, int> _buildingDistrict = new Dictionary<int, int>();
        private readonly HashSet<int> _unconnectedHouses = new HashSet<int>();

        public IReadOnlyList<DistrictStats> Districts => _districts;

        public DistrictStats Get(int id)
        {
            foreach (DistrictStats d in _districts) if (d.Id == id) return d;
            return null;
        }

        public int DistrictOfBuilding(int buildingId) => _buildingDistrict.TryGetValue(buildingId, out int d) ? d : CentralId;

        public int DistrictOfCitizen(Citizen c) => c != null && c.HomeBuildingId.HasValue ? DistrictOfBuilding(c.HomeBuildingId.Value) : CentralId;

        public string NameOf(int id) => Get(id)?.NameUz ?? CentralName;

        public bool IsHouseConnected(int buildingId) => !_unconnectedHouses.Contains(buildingId);

        public static int DistrictAt(CityLandExpansionManager land, int x, int y)
        {
            LandParcel p = land?.ParcelAt(x, y);
            return p?.Id ?? CentralId;
        }

        /// <summary>Uy atrofida (RoadRadius ichida) ko'cha yoki maydon bormi.</summary>
        public static bool IsRoadConnected(Building house, Func<int, int, bool> isRoad, int radius = RoadRadius)
        {
            if (isRoad == null) return true;
            int size = house.Size.X;
            for (int y = house.Position.Y - radius; y < house.Position.Y + size + radius; y++)
                for (int x = house.Position.X - radius; x < house.Position.X + size + radius; x++)
                    if (isRoad(x, y)) return true;
            return false;
        }

        /// <summary>Barcha mahallalar statistikasini qayta hisoblaydi (kuniga bir marta yoki holat o'zgarganda).</summary>
        public void Recompute(BuildingManager b, PopulationManager pop, CityLandExpansionManager land,
                              AdvancedEconomyManager econ, Func<int, int, bool> isRoad, int populationTax)
        {
            _districts.Clear();
            _buildingDistrict.Clear();
            _unconnectedHouses.Clear();

            DistrictStats central = new DistrictStats { Id = CentralId, NameUz = CentralName, CenterX = 32, CenterY = 32 };
            _districts.Add(central);
            if (land != null)
                foreach (LandParcel p in land.Parcels)
                    _districts.Add(new DistrictStats
                    {
                        Id = p.Id, NameUz = p.NameUz, CenterX = p.GridBounds.Center.X, CenterY = p.GridBounds.Center.Y,
                        IsOpen = p.State == LandParcelState.Open
                    });

            // --- Binolar ---
            var securityPoints = new Dictionary<int, int>();
            foreach (Building x in b.Buildings)
            {
                int half = x.Size.X / 2;
                int did = DistrictAt(land, x.Position.X + half, x.Position.Y + half);
                _buildingDistrict[x.Id] = did;
                DistrictStats d = Get(did) ?? central;
                if (x.BuildProgress < 1f) continue;

                d.HousingCapacity += x.PopulationCapacity;
                d.Jobs += PopulationManager.JobSlots(x.Type, out _);
                switch (x.Type)
                {
                    case BuildingType.House:
                        d.Houses++;
                        if (IsRoadConnected(x, isRoad)) d.HousesConnected++;
                        else _unconnectedHouses.Add(x.Id);
                        break;
                    case BuildingType.Market: d.Markets++; break;
                    case BuildingType.Garden: d.LocalProduction += 2; break;
                    case BuildingType.Barracks: securityPoints[did] = securityPoints.GetValueOrDefault(did) + 20; break;
                    case BuildingType.Tower: securityPoints[did] = securityPoints.GetValueOrDefault(did) + 6; break;
                }
            }

            // --- Fuqarolar ---
            foreach (Citizen c in pop.Citizens)
            {
                DistrictStats d = Get(DistrictOfCitizen(c)) ?? central;
                d.Population++;
                d.Happiness += c.Happiness; d.Health += c.Health; d.Education += c.Education;
                if (c.IsEmployed)
                {
                    d.Employed++;
                    // Bog'bon o'z mahallasida yoki boshqa joyda ishlashidan qat'i nazar, hosil bog' joylashgan mahallaga yoziladi.
                    if (c.Profession == CitizenProfession.Bogbon && c.WorkplaceBuildingId.HasValue)
                    {
                        DistrictStats wd = Get(DistrictOfBuilding(c.WorkplaceBuildingId.Value));
                        if (wd != null) wd.LocalProduction += 5;
                    }
                }
                else d.Unemployed++;
            }
            foreach (Household hh in pop.Households)
            {
                if (hh.MemberIds.Count == 0) continue;
                DistrictStats d = Get(hh.HomeBuildingId.HasValue ? DistrictOfBuilding(hh.HomeBuildingId.Value) : CentralId) ?? central;
                d.Households++;
                d.AvgSavings += hh.SharedMoney;
                if (hh.FedRatio < 0.99f) d.UnderfedHouseholds++;
            }

            bool cityHousingFull = pop.HousingUsed >= pop.HousingCapacity && pop.Count > 0;
            bool noSchool = b.Count(BuildingType.Madrasa) == 0;
            float foodRatio = econ != null ? econ.Prices.Get(ProductType.Food).PriceRatio : 1f;
            float highPrice = econ != null ? econ.Config.HighPriceRatio : 1.35f;
            float unemploymentShare = econ != null ? econ.Config.UnemploymentConcernShare : 0.2f;
            float marketShortage = econ != null ? econ.MarketShortageShare : 0f;
            int cityMarkets = b.Count(BuildingType.Market);

            foreach (DistrictStats d in _districts)
            {
                if (d.Population > 0)
                {
                    d.Happiness /= d.Population; d.Health /= d.Population; d.Education /= d.Population;
                }
                if (d.Households > 0) d.AvgSavings /= d.Households;
                d.Security = Math.Min(100, securityPoints.GetValueOrDefault(d.Id));
                // Markaziy qal'a qo'shini butun shaharni qisman qo'riqlaydi.
                if (d.Id != CentralId) d.Security = Math.Min(100, d.Security + pop.AvgSecurity * 0.4f);
                if (d.Population == 0) continue;

                // ---- Haqiqiy ehtiyojlar ----
                bool jobsLacking = d.Unemployed >= 3 && d.Unemployed >= d.Population * unemploymentShare && pop.FreeJobs < d.Unemployed;
                if (jobsLacking) AddNeed(d, ConcernKeys.Unemployed, d.Unemployed);
                if (d.UnderfedHouseholds > 0 && d.UnderfedHouseholds * 5 >= d.Households)
                    AddNeed(d, ConcernKeys.Poverty, CountMembersUnderfed(pop, d.Id));
                if (d.Population > d.HousingCapacity || cityHousingFull) AddNeed(d, ConcernKeys.Housing, Math.Max(1, d.Population - d.HousingCapacity));
                if (foodRatio >= highPrice) AddNeed(d, ConcernKeys.FoodPrice, d.Population);
                if (populationTax >= 25) AddNeed(d, ConcernKeys.HighTax, d.Population);
                if (d.Houses > d.HousesConnected) AddNeed(d, ConcernKeys.NoRoad, CountResidentsUnconnected(pop, d.Id));
                // Mahallada bozor yo'q va eng yaqin bozor ham uzoqda (yoki shaharda umuman bozor yo'q).
                MarketState nearest = econ?.Merchants.Nearest(d.CenterX, d.CenterY);
                int marketDistance = nearest == null ? int.MaxValue : Math.Abs(nearest.X - d.CenterX) + Math.Abs(nearest.Y - d.CenterY);
                bool farFromMarket = d.Markets == 0 && (cityMarkets == 0 || (d.Population >= 6 && marketDistance > 20));
                if (farFromMarket || marketShortage >= 0.15f) AddNeed(d, ConcernKeys.NoMarket, d.Population);
                if (d.Security < 20f && d.Population >= 8) AddNeed(d, ConcernKeys.Security, d.Population);
                if (noSchool && d.Education < 30f && d.Population >= 10) AddNeed(d, ConcernKeys.NoSchool, d.Population);

                d.Needs.Sort((a, c2) => ConcernKeys.Weight(c2).CompareTo(ConcernKeys.Weight(a)));
            }
        }

        private void AddNeed(DistrictStats d, string key, int affected)
        {
            if (!d.Needs.Contains(key)) d.Needs.Add(key);
            d.ConcernCounts[key] = Math.Max(1, Math.Min(d.Population, affected));
        }

        private int CountMembersUnderfed(PopulationManager pop, int districtId)
        {
            int n = 0;
            foreach (Household hh in pop.Households)
            {
                if (hh.FedRatio >= 0.99f || hh.MemberIds.Count == 0) continue;
                int did = hh.HomeBuildingId.HasValue ? DistrictOfBuilding(hh.HomeBuildingId.Value) : CentralId;
                if (did == districtId) n += hh.MemberIds.Count;
            }
            return n;
        }

        private int CountResidentsUnconnected(PopulationManager pop, int districtId)
        {
            int n = 0;
            foreach (Citizen c in pop.Citizens)
                if (c.HomeBuildingId.HasValue && _unconnectedHouses.Contains(c.HomeBuildingId.Value)
                    && DistrictOfBuilding(c.HomeBuildingId.Value) == districtId) n++;
            return n;
        }
    }
}
