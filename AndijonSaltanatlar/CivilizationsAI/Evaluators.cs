using AndijonSaltanatlar.Kingdoms;
using AndijonSaltanatlar.World;

namespace AndijonSaltanatlar.CivilizationsAI
{
    /// <summary>Har bir evaluator davlat holatini baholab, tegishli qarorga 0..1 ball beradi.</summary>

    public sealed class EconomyEvaluator
    {
        public float Evaluate(Kingdom k, WorldSimulation w)
        {
            float need = (100 - k.EconomicStrength) / 100f;
            float poor = k.Treasury < 200 ? 0.3f : 0f;
            return (need * 0.6f + poor) * (0.4f + k.AI.EconomicFocus);
        }
    }

    public sealed class StabilityEvaluator
    {
        public float Evaluate(Kingdom k, WorldSimulation w)
        {
            float need = (100 - k.Stability) / 100f;
            return need * (0.3f + k.AI.StabilityFocus) + (k.Stability < 40 ? 0.2f : 0f);
        }
    }

    public sealed class ExpansionEvaluator
    {
        public float Evaluate(Kingdom k, WorldSimulation w)
        {
            if (!w.HasNeutralRegion()) return 0f;
            if (k.Treasury < 220) return 0f;              // kengayish qimmat — zaxira kerak
            float power = k.MilitaryStrength / 100f;
            return k.AI.ExpansionFocus * (0.3f + 0.5f * k.AI.RiskTolerance) * (0.5f + 0.5f * power) * 0.7f;
        }
    }

    public sealed class DiplomacyEvaluator
    {
        public float Evaluate(Kingdom k, WorldSimulation w)
        {
            int rel = w.Diplomacy.Get(k.Id, w.Kingdoms.PlayerKingdomId);
            float wantFriend = rel < 40 ? 0.3f : 0.1f;
            return k.AI.DiplomacyFocus * 0.6f + wantFriend;
        }
    }

    public sealed class MilitaryEvaluator
    {
        public float Evaluate(Kingdom k, WorldSimulation w)
        {
            int rel = w.Diplomacy.Get(k.Id, w.Kingdoms.PlayerKingdomId);
            float threat = rel < -10 ? 0.3f : 0f;
            float weak = k.MilitaryStrength < 90 ? 0.3f : 0f;
            if (k.Treasury < 80) return 0f;
            return (threat + weak) * (0.4f + k.AI.ExpansionFocus) + 0.1f * k.AI.RiskTolerance;
        }
    }
}
