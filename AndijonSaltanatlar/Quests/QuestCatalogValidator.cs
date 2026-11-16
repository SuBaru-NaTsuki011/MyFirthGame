using System.Collections.Generic;
using System.Linq;

namespace AndijonSaltanatlar.Quests
{
    /// <summary>
    /// Katalog yaxlitligini tekshiradi — PROMPT 13, test 19 & 20: noto'g'ri prerequisite va
    /// bog'lanishlardagi cycle'ni aniqlaydi. Yuklashdan oldin ishlatiladi.
    /// </summary>
    public static class QuestCatalogValidator
    {
        /// <summary>Mavjud bo'lmagan prerequisite'ga ishora qiluvchi vazifalar ro'yxati (xato matnlari).</summary>
        public static List<string> FindMissingPrerequisites(IEnumerable<QuestDefinition> quests)
        {
            var list = quests.ToList();
            var ids = new HashSet<string>(list.Select(q => q.QuestId));
            var errors = new List<string>();
            foreach (var q in list)
                foreach (var pre in q.PrerequisiteQuestIds)
                    if (!ids.Contains(pre))
                        errors.Add($"'{q.QuestId}' mavjud bo'lmagan prerequisite'ga ishora qiladi: '{pre}'");
            return errors;
        }

        /// <summary>Prerequisite grafida cycle bormi? (DFS, rang bilan).</summary>
        public static bool HasCycle(IEnumerable<QuestDefinition> quests)
        {
            var map = quests.Where(q => !string.IsNullOrEmpty(q.QuestId))
                            .GroupBy(q => q.QuestId).ToDictionary(g => g.Key, g => g.First());
            var color = new Dictionary<string, int>(); // 0=oq,1=kulrang,2=qora
            foreach (var id in map.Keys)
                if (Dfs(id, map, color)) return true;
            return false;
        }

        private static bool Dfs(string id, Dictionary<string, QuestDefinition> map, Dictionary<string, int> color)
        {
            if (!map.ContainsKey(id)) return false;
            color.TryGetValue(id, out int c);
            if (c == 1) return true;   // kulrang — orqaga qirra => cycle
            if (c == 2) return false;  // qora — tekshirilgan
            color[id] = 1;
            foreach (var pre in map[id].PrerequisiteQuestIds)
                if (Dfs(pre, map, color)) return true;
            color[id] = 2;
            return false;
        }

        /// <summary>Katalog to'liq yaroqlimi (prerequisite'lar mavjud va cycle yo'q).</summary>
        public static bool IsValid(IEnumerable<QuestDefinition> quests, out List<string> errors)
        {
            var list = quests.ToList();
            errors = FindMissingPrerequisites(list);
            if (HasCycle(list)) errors.Add("Prerequisite bog'lanishlarida cycle aniqlandi.");
            return errors.Count == 0;
        }
    }
}
