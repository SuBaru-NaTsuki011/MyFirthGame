using System.Collections.Generic;

namespace AndijonSaltanatlar.Technology
{
    /// <summary>Texnologiya effektining turi — type-safe (satr emas).</summary>
    public enum TechEffectType
    {
        FoodProductionBonus,
        TradeIncomeBonus,
        ResearchProductionBonus,
        BuildingConstructionBonus,
        PopulationHealthBonus,
        EducationBonus,
        CityStabilityBonus,
        CulturalInfluenceBonus,
        TaxEfficiencyBonus
    }

    public sealed class TechnologyEffect
    {
        public TechEffectType Type { get; set; }
        public float Value { get; set; }
        public TechnologyEffect() { }
        public TechnologyEffect(TechEffectType type, float value) { Type = type; Value = value; }
    }

    public sealed class TechnologyDefinition
    {
        public string Id { get; set; } = "";
        public string NameUz { get; set; } = "";
        public string DescriptionUz { get; set; } = "";
        public string Category { get; set; } = "";
        public string EraId { get; set; } = "";
        public int ResearchCost { get; set; }
        public List<string> PrerequisiteIds { get; set; } = new List<string>();
        public List<string> UnlockBuildingIds { get; set; } = new List<string>();
        public List<string> UnlockUnitIds { get; set; } = new List<string>();
        public List<TechnologyEffect> Effects { get; set; } = new List<TechnologyEffect>();
    }

    /// <summary>
    /// Boshlang'ich 18 texnologiya katalogi — o'zaro bog'langan asiklik grafik. Qiymatlar o'yin
    /// balansi uchun; tarixiy kashfiyot sanalari emas.
    /// </summary>
    public static class TechnologyCatalog
    {
        public static readonly List<TechnologyDefinition> All = Build();
        private static readonly Dictionary<string, TechnologyDefinition> _byId = Index();

        public static TechnologyDefinition Get(string id) => _byId.TryGetValue(id, out var d) ? d : null;

        private static Dictionary<string, TechnologyDefinition> Index()
        {
            var d = new Dictionary<string, TechnologyDefinition>();
            foreach (TechnologyDefinition t in All) d[t.Id] = t;
            return d;
        }

        private static List<TechnologyDefinition> Build()
        {
            var list = new List<TechnologyDefinition>();

            void T(string id, string name, string cat, int cost, string[] prereq, string desc,
                   string[] unlockB, params TechnologyEffect[] effects)
            {
                list.Add(new TechnologyDefinition
                {
                    Id = id, NameUz = name, Category = cat, ResearchCost = cost, EraId = "temuriylar",
                    DescriptionUz = desc,
                    PrerequisiteIds = new List<string>(prereq ?? new string[0]),
                    UnlockBuildingIds = new List<string>(unlockB ?? new string[0]),
                    Effects = new List<TechnologyEffect>(effects)
                });
            }

            string[] N = new string[0];

            // ILM-FAN
            T("hisob-ilmi", "Hisob ilmi", "Ilm-fan", 60, N, "Matematik hisob — davlat boshqaruviga asos.", N,
                new TechnologyEffect(TechEffectType.TaxEfficiencyBonus, 0.15f));
            T("kitobat", "Kitobat", "Ilm-fan", 120, new[] { "hisob-ilmi" }, "Kitob ko'chirish — ilm-fan tez rivojlanadi.", N,
                new TechnologyEffect(TechEffectType.ResearchProductionBonus, 0.2f), new TechnologyEffect(TechEffectType.EducationBonus, 0.1f));
            T("astronomiya", "Astronomiya", "Ilm-fan", 200, new[] { "kitobat" }, "Yulduzlar ilmi — ilmiy salohiyat oshadi.", N,
                new TechnologyEffect(TechEffectType.ResearchProductionBonus, 0.15f));
            T("tibbiyot", "Tibbiyot", "Ilm-fan", 160, new[] { "kitobat" }, "Shifo ilmi — aholi sog'lig'i yaxshilanadi.", N,
                new TechnologyEffect(TechEffectType.PopulationHealthBonus, 0.2f));

            // ME'MORCHILIK
            T("gishtchilik", "G'ishtchilik", "Me'morchilik", 60, N, "Pishiq g'isht — qurilish mustahkamroq.", N,
                new TechnologyEffect(TechEffectType.BuildingConstructionBonus, 0.15f));
            T("shaharsozlik", "Shaharsozlik", "Me'morchilik", 120, new[] { "gishtchilik" }, "Shahar rejalashtirish — Saroy qurish ochiladi.", new[] { "MainHall" },
                new TechnologyEffect(TechEffectType.BuildingConstructionBonus, 0.1f), new TechnologyEffect(TechEffectType.CityStabilityBonus, 0.1f));
            T("me'morchilik", "Me'morchilik", "Me'morchilik", 200, new[] { "shaharsozlik" }, "Katta inshootlar — Madrasa qurish ochiladi.", new[] { "Madrasa" },
                new TechnologyEffect(TechEffectType.CulturalInfluenceBonus, 0.15f), new TechnologyEffect(TechEffectType.EducationBonus, 0.1f));
            T("gumbaz-qurilishi", "Gumbaz qurilishi", "Me'morchilik", 300, new[] { "me'morchilik" }, "Feruza gumbazlar — Jome masjidi qurish ochiladi.", new[] { "Mosque" },
                new TechnologyEffect(TechEffectType.CulturalInfluenceBonus, 0.25f));

            // IQTISODIYOT
            T("dehqonchilik", "Dehqonchilik usullari", "Iqtisodiyot", 60, N, "Ekin-tikin — oziq-ovqat ko'payadi.", N,
                new TechnologyEffect(TechEffectType.FoodProductionBonus, 0.15f));
            T("sugorish", "Sug'orish tizimi", "Iqtisodiyot", 120, new[] { "dehqonchilik" }, "Ariq va zovurlar — dehqonchilik samarali.", N,
                new TechnologyEffect(TechEffectType.FoodProductionBonus, 0.2f));
            T("bogdorchilik", "Bog'dorchilik", "Iqtisodiyot", 180, new[] { "sugorish" }, "Chorbog'lar — oziq-ovqat va madaniyat.", N,
                new TechnologyEffect(TechEffectType.FoodProductionBonus, 0.1f), new TechnologyEffect(TechEffectType.CulturalInfluenceBonus, 0.1f));
            T("hunarmandchilik", "Hunarmandchilik", "Iqtisodiyot", 120, new[] { "gishtchilik" }, "Ustaxonalar — ishlab chiqarish oshadi.", N,
                new TechnologyEffect(TechEffectType.TradeIncomeBonus, 0.15f));
            T("karvon-savdosi", "Karvon savdosi", "Iqtisodiyot", 160, new[] { "hunarmandchilik" }, "Ipak yo'li — Karvonsaroy qurish ochiladi.", new[] { "Caravanserai" },
                new TechnologyEffect(TechEffectType.TradeIncomeBonus, 0.2f));
            T("bozor-boshqaruvi", "Bozor boshqaruvi", "Iqtisodiyot", 220, new[] { "karvon-savdosi" }, "Savdo tartibi — bozor daromadi oshadi.", N,
                new TechnologyEffect(TechEffectType.TradeIncomeBonus, 0.2f));

            // MADANIYAT / BOSHQARUV
            T("xattotlik", "Xattotlik", "Madaniyat", 120, new[] { "hisob-ilmi" }, "Husnixat san'ati — madaniyat yuksaladi.", N,
                new TechnologyEffect(TechEffectType.CulturalInfluenceBonus, 0.2f));
            T("diplomatiya-ilmi", "Diplomatiya ilmi", "Boshqaruv", 160, new[] { "hisob-ilmi" }, "Elchilik odobi — diplomatik nufuz oshadi.", N,
                new TechnologyEffect(TechEffectType.CulturalInfluenceBonus, 0.1f), new TechnologyEffect(TechEffectType.CityStabilityBonus, 0.1f));
            T("davlat-moliyasi", "Davlat moliyasi", "Boshqaruv", 180, new[] { "hisob-ilmi" }, "Xazina tartibi — soliq samarali.", N,
                new TechnologyEffect(TechEffectType.TaxEfficiencyBonus, 0.2f));
            T("ma'muriy-boshqaruv", "Ma'muriy boshqaruv", "Boshqaruv", 240, new[] { "davlat-moliyasi" }, "Hududiy idora — saltanat barqaror.", N,
                new TechnologyEffect(TechEffectType.CityStabilityBonus, 0.2f));

            return list;
        }
    }

    /// <summary>Katalog butunligini tekshiradi: yo'q prereqlar va sikllar (asiklik grafik shart).</summary>
    public static class TechnologyValidator
    {
        public static bool Validate(out string error)
        {
            foreach (TechnologyDefinition t in TechnologyCatalog.All)
                foreach (string p in t.PrerequisiteIds)
                    if (TechnologyCatalog.Get(p) == null)
                    {
                        error = "Yo'q texnologiyaga havola: " + t.Id + " -> " + p;
                        return false;
                    }

            var state = new Dictionary<string, int>(); // 0=oq,1=kulrang,2=qora
            foreach (TechnologyDefinition t in TechnologyCatalog.All)
                if (HasCycle(t.Id, state))
                {
                    error = "Sikl topildi: " + t.Id;
                    return false;
                }
            error = "";
            return true;
        }

        private static bool HasCycle(string id, Dictionary<string, int> state)
        {
            state.TryGetValue(id, out int c);
            if (c == 1) return true;
            if (c == 2) return false;
            state[id] = 1;
            TechnologyDefinition t = TechnologyCatalog.Get(id);
            if (t != null)
                foreach (string p in t.PrerequisiteIds)
                    if (HasCycle(p, state)) return true;
            state[id] = 2;
            return false;
        }
    }
}
