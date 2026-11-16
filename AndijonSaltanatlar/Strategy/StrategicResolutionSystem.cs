using System;
using AndijonSaltanatlar.Kingdoms;

namespace AndijonSaltanatlar.Strategy
{
    /// <summary>Strategik mojaroning mavhum tavsifi (kim, qayerda).</summary>
    public sealed class StrategicConflict
    {
        public int AttackerId { get; set; }
        public int DefenderId { get; set; }
        public int TargetRegionId { get; set; }
    }

    /// <summary>
    /// Harbiy mojarolarni mavhum (taktik jangsiz) hal qiladi: harbiy kuch, iqtisod va barqarorlikni
    /// taqqoslaydi, oz tasodifiylik qo'shadi. Natija hudud egaligi va munosabatlarga ta'sir qiladi.
    /// </summary>
    public static class StrategicResolutionSystem
    {
        public static bool Resolve(Kingdom attacker, Kingdom defender, Random rng, out string desc)
        {
            float a = attacker.MilitaryStrength + attacker.EconomicStrength * 0.3f + (float)rng.NextDouble() * 25f;
            float d = defender.MilitaryStrength + defender.Stability * 0.3f + (float)rng.NextDouble() * 25f;
            bool win = a >= d;
            desc = win
                ? attacker.Name + " strategik ustunlikka erishdi."
                : defender.Name + " mudofaani ushlab qoldi.";
            return win;
        }
    }
}
