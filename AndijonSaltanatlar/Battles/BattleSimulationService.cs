using System;

namespace AndijonSaltanatlar.Battles
{
    /// <summary>
    /// Strategik jangning mavhum (statistik) simulyatsiyasi. Natija qo'shin kuchi, hudud mudofaa
    /// bonusi va kichik tasodifiy omilga bog'liq. Belgilangan seed bilan takrorlanuvchan.
    /// </summary>
    public static class BattleSimulationService
    {
        public static BattleResult Simulate(string attacker, string defender, string region,
            int attackerStrength, int defenderStrength, int defenseBonus, int day, Random rng)
        {
            int effectiveDefense = defenderStrength + defenseBonus;
            float a = attackerStrength * (0.9f + (float)rng.NextDouble() * 0.3f);
            float d = effectiveDefense * (0.9f + (float)rng.NextDouble() * 0.3f);

            var r = new BattleResult
            {
                Day = day, AttackerName = attacker, DefenderName = defender, RegionName = region,
                AttackerStrength = attackerStrength, DefenderStrength = effectiveDefense,
                DurationDays = 1 + rng.Next(3)
            };

            float ratio = a / (d + 1f);
            if (ratio > 1.25f) r.Outcome = BattleOutcome.Galaba;
            else if (ratio < 0.8f) r.Outcome = BattleOutcome.Maglubiyat;
            else r.Outcome = rng.NextDouble() < 0.5 ? BattleOutcome.Durang : BattleOutcome.Chekinish;

            float total = a + d + 1f;
            r.AttackerLosses = (int)(attackerStrength * (0.12f + 0.28f * (d / total)));
            r.DefenderLosses = (int)(effectiveDefense * (0.12f + 0.28f * (a / total)));
            return r;
        }
    }
}
