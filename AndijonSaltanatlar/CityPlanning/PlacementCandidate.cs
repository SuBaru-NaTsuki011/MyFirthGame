using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.CityPlanning
{
    /// <summary>
    /// Bitta joylashuv nomzodi — PROMPT 15, 7-bo'lim. AI bir nechta nomzodni solishtiradi va
    /// eng yuqori umumiy ballni tanlaydi. Ballar 0..100 oralig'ida.
    /// </summary>
    public sealed class PlacementCandidate
    {
        public Point Origin { get; set; }
        public int Size { get; set; }
        public bool IsValid { get; set; }
        public string RejectReasonUz { get; set; } = "";

        public int AccessibilityScore { get; set; }
        public int EfficiencyScore { get; set; }
        public int NeighborhoodCompatibilityScore { get; set; }
        public int ExpansionPotentialScore { get; set; }
        public int VisualHarmonyScore { get; set; }
        public int ConstructionCostScore { get; set; }

        /// <summary>Og'irlikli umumiy ball. Har bir mezon 0..100.</summary>
        public int TotalScore { get; set; }

        public static int Clamp(int v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
