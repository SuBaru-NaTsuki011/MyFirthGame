using System;

namespace AndijonSaltanatlar.Government
{
    /// <summary>
    /// Hukmdor siyosati — soliq stavkalari (foizda, 0..40). Iqtisodiyot va aholi baxti
    /// shu qiymatlarga bog'liq. UI orqali o'zgartiriladi, Save/Load'da saqlanadi.
    /// </summary>
    public sealed class GovernmentPolicy
    {
        public const int Min = 0, Max = 40, Step = 5;

        public int PopulationTax { get; set; } = 10; // aholi solig'i
        public int TradeTax { get; set; } = 10;       // savdo solig'i
        public int ProductionTax { get; set; } = 10;  // ishlab chiqarish solig'i

        public void AdjustPopulation(int d) => PopulationTax = Clamp(PopulationTax + d);
        public void AdjustTrade(int d) => TradeTax = Clamp(TradeTax + d);
        public void AdjustProduction(int d) => ProductionTax = Clamp(ProductionTax + d);

        private static int Clamp(int v) => Math.Max(Min, Math.Min(Max, v));
    }
}
