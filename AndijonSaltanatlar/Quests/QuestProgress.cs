using System;
using System.Collections.Generic;

namespace AndijonSaltanatlar.Quests
{
    /// <summary>
    /// Bitta vazifaning runtime progressi (saqlanadigan holat). Katalog (QuestDefinition) alohida.
    /// </summary>
    public class QuestProgress
    {
        public string QuestId { get; set; } = "";
        public QuestStatus Status { get; set; } = QuestStatus.Locked;

        /// <summary>Har bir objective bo'yicha joriy progress (objective indeksi -> qiymat).</summary>
        public Dictionary<int, int> ObjectiveProgress { get; set; } = new();

        /// <summary>
        /// Qayta hisoblanmaslik uchun: ushbu vazifa uchun allaqachon qayta ishlangan event id'lar.
        /// PROMPT 13, 6-bo'lim: "Bir xil event ikki marta kelganda progress ikki marta ortmasin".
        /// </summary>
        public HashSet<string> ProcessedEventIds { get; set; } = new();

        /// <summary>CountExisting=false bo'lganda bazaviy (vazifa boshlangandagi) qiymatlar.</summary>
        public Dictionary<int, int> Baseline { get; set; } = new();

        public DateTime? ActivatedAtUtc { get; set; }
        public bool RewardClaimed { get; set; } = false;

        public int GetProgress(int objectiveIndex) =>
            ObjectiveProgress.TryGetValue(objectiveIndex, out int v) ? v : 0;
    }
}
