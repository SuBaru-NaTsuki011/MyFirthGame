using System.Collections.Generic;
using AndijonSaltanatlar.Civilization;

namespace AndijonSaltanatlar.Technology
{
    /// <summary>Tadqiqot progressi va mavjud (o'rganish mumkin) texnologiyalarni boshqaradi.</summary>
    public static class ResearchManager
    {
        /// <summary>Joriy tadqiqotni berilgan ilm ballari bilan ilgari suradi. Yakunlangan texnologiya id'si yoki null.</summary>
        public static string Advance(CivilizationState s, int points)
        {
            if (!s.IsResearching) return null;
            TechnologyDefinition t = TechnologyCatalog.Get(s.CurrentResearch);
            if (t == null) { s.CurrentResearch = ""; return null; }
            s.CurrentProgress += points;
            if (s.CurrentProgress >= t.ResearchCost)
            {
                s.Researched.Add(t.Id);
                s.CurrentResearch = "";
                s.CurrentProgress = 0;
                return t.Id;
            }
            return null;
        }

        public static List<TechnologyDefinition> Available(CivilizationState s)
        {
            var list = new List<TechnologyDefinition>();
            foreach (TechnologyDefinition t in TechnologyCatalog.All)
                if (s.CanResearch(t.Id)) list.Add(t);
            return list;
        }
    }
}
