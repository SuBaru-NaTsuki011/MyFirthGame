using System.Text;
using AndijonSaltanatlar.SmartConstruction;

namespace AndijonSaltanatlar.Forecasting
{
    /// <summary>
    /// Yakunlangan reja natijasi — PROMPT 15, 2 va 16-bo'limlar. Reja tasdiqlanganda olingan
    /// boshlang'ich ko'rsatkichlar bilan qurilishdan keyingi haqiqiy ko'rsatkichlarni solishtiradi.
    /// Bu prognoz emas: qiymatlar GameState'dan olinadi.
    /// </summary>
    public sealed class DevelopmentImpact
    {
        public int HousingBefore { get; set; }
        public int HousingAfter { get; set; }
        public int PopulationBefore { get; set; }
        public int PopulationAfter { get; set; }
        public int BuiltBuildings { get; set; }
        public int BuiltRoads { get; set; }
        public int FailedSteps { get; set; }
        public int Spent { get; set; }

        public int HousingDelta => HousingAfter - HousingBefore;
        public int PopulationDelta => PopulationAfter - PopulationBefore;
        public bool HasBaseline => HousingBefore > 0 || PopulationBefore > 0;

        public string SummaryUz
        {
            get
            {
                if (!HasBaseline)
                    return $"{BuiltBuildings} bino, {BuiltRoads} yo'l katagi qurildi; {Spent} tanga sarflandi.";

                var sb = new StringBuilder();
                sb.Append("Sig'im ").Append(HousingBefore).Append("->").Append(HousingAfter)
                  .Append(" (").Append(HousingDelta >= 0 ? "+" : "").Append(HousingDelta).Append("), ");
                sb.Append("aholi ").Append(PopulationBefore).Append("->").Append(PopulationAfter)
                  .Append(" (").Append(PopulationDelta >= 0 ? "+" : "").Append(PopulationDelta).Append("). ");
                sb.Append(BuiltBuildings).Append(" bino, ").Append(BuiltRoads).Append(" yo'l katagi, ")
                  .Append(Spent).Append(" tanga.");
                if (FailedSteps > 0) sb.Append(' ').Append(FailedSteps).Append(" qadam o'tkazib yuborildi.");
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Rivojlanish natijasini baholovchi — reja qadamlarini va qurilishdan keyingi shahar holatini
    /// birlashtiradi. Faqat o'qish; hech qanday resurs yoki GameState'ni o'zgartirmaydi.
    /// </summary>
    public static class DevelopmentImpactEvaluator
    {
        public static DevelopmentImpact Evaluate(ApprovedConstructionPlan plan,
            int housingAfter, int populationAfter)
        {
            var impact = new DevelopmentImpact
            {
                HousingAfter = housingAfter,
                PopulationAfter = populationAfter
            };
            if (plan == null) return impact;

            impact.HousingBefore = plan.HousingCapacityAtApproval;
            impact.PopulationBefore = plan.PopulationAtApproval;
            impact.Spent = plan.SpentSoFar;
            foreach (PlanStep step in plan.Steps)
            {
                if (step.Status == StepStatus.Done)
                {
                    if (step.Kind == StepKind.Building) impact.BuiltBuildings++;
                    else impact.BuiltRoads++;
                }
                else if (step.Status == StepStatus.Failed) impact.FailedSteps++;
            }
            return impact;
        }
    }
}
