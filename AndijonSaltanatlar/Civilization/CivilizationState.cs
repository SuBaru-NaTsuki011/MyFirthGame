using System.Collections.Generic;
using AndijonSaltanatlar.Technology;

namespace AndijonSaltanatlar.Civilization
{
    /// <summary>Ochilgan texnologiyalardan yig'ilgan bonuslar (type-safe).</summary>
    public sealed class CivilizationModifiers
    {
        private readonly Dictionary<TechEffectType, float> _v = new Dictionary<TechEffectType, float>();
        public float Get(TechEffectType t) => _v.TryGetValue(t, out float x) ? x : 0f;
        public void Add(TechEffectType t, float v) => _v[t] = Get(t) + v;
    }

    /// <summary>Bitta saltanatning ilmiy-madaniy holati: ilm/madaniyat ballari, ochilgan texnologiyalar, joriy tadqiqot.</summary>
    public sealed class CivilizationState
    {
        public int KingdomId;
        public int Science;              // ILM ballari (sarflanmagan zaxira)
        public int Culture;              // MADANIYAT ballari
        public readonly HashSet<string> Researched = new HashSet<string>();
        public string CurrentResearch = "";
        public int CurrentProgress;

        public bool IsResearched(string id) => Researched.Contains(id);
        public bool IsResearching => !string.IsNullOrEmpty(CurrentResearch);

        public bool CanResearch(string id)
        {
            TechnologyDefinition t = TechnologyCatalog.Get(id);
            if (t == null || IsResearched(id)) return false;
            foreach (string p in t.PrerequisiteIds) if (!IsResearched(p)) return false;
            return true;
        }

        public bool StartResearch(string id)
        {
            if (!CanResearch(id)) return false;
            CurrentResearch = id;
            CurrentProgress = 0;
            return true;
        }

        public CivilizationModifiers Modifiers()
        {
            var m = new CivilizationModifiers();
            foreach (string id in Researched)
            {
                TechnologyDefinition t = TechnologyCatalog.Get(id);
                if (t == null) continue;
                foreach (TechnologyEffect e in t.Effects) m.Add(e.Type, e.Value);
            }
            return m;
        }

        public bool IsBuildingUnlocked(string requiredTechId)
            => string.IsNullOrEmpty(requiredTechId) || Researched.Contains(requiredTechId);
    }
}
