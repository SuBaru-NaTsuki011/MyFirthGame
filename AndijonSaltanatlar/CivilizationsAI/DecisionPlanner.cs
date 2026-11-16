using System;
using AndijonSaltanatlar.Kingdoms;
using AndijonSaltanatlar.World;

namespace AndijonSaltanatlar.CivilizationsAI
{
    public enum AIDecision { DevelopEconomy, Stabilize, Expand, Diplomacy, Militarize }

    /// <summary>
    /// Evaluatorlar bergan ballarni taqqoslab, eng foydali qarorni tanlaydi. Natijaga oz miqdorda
    /// tasodifiylik qo'shiladi (belgilangan seed — takrorlanuvchan).
    /// </summary>
    public sealed class DecisionPlanner
    {
        private readonly EconomyEvaluator _econ = new EconomyEvaluator();
        private readonly StabilityEvaluator _stab = new StabilityEvaluator();
        private readonly ExpansionEvaluator _exp = new ExpansionEvaluator();
        private readonly DiplomacyEvaluator _dip = new DiplomacyEvaluator();
        private readonly MilitaryEvaluator _mil = new MilitaryEvaluator();

        public AIDecision Plan(Kingdom k, WorldSimulation w, Random rng)
        {
            float best = float.MinValue;
            AIDecision choice = AIDecision.DevelopEconomy;

            void Consider(AIDecision d, float score)
            {
                score += (float)rng.NextDouble() * 0.1f;
                if (score > best) { best = score; choice = d; }
            }

            Consider(AIDecision.DevelopEconomy, _econ.Evaluate(k, w));
            Consider(AIDecision.Stabilize, _stab.Evaluate(k, w));
            Consider(AIDecision.Expand, _exp.Evaluate(k, w));
            Consider(AIDecision.Diplomacy, _dip.Evaluate(k, w));
            Consider(AIDecision.Militarize, _mil.Evaluate(k, w));

            return choice;
        }
    }
}
