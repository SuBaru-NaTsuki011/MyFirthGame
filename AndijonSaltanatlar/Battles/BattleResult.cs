namespace AndijonSaltanatlar.Battles
{
    public enum BattleOutcome { Galaba, Maglubiyat, Durang, Chekinish }

    /// <summary>Jang davomidagi bitta hodisa — animatsiya tizimi shunga mos vizual ko'rsatadi.</summary>
    public sealed class BattleEvent
    {
        public string Text { get; set; } = "";
    }

    /// <summary>Strategik jang natijasi — GameState ma'lumotlaridan hisoblanadi (hardcode emas).</summary>
    public sealed class BattleResult
    {
        public int Day { get; set; }
        public string AttackerName { get; set; } = "";
        public string DefenderName { get; set; } = "";
        public string RegionName { get; set; } = "";
        public int AttackerStrength { get; set; }
        public int DefenderStrength { get; set; }
        public int AttackerLosses { get; set; }
        public int DefenderLosses { get; set; }
        public BattleOutcome Outcome { get; set; }
        public int DurationDays { get; set; }
        public int CoinChange { get; set; }
        public string DiplomaticResult { get; set; } = "";
        public string TerritoryResult { get; set; } = "";
        public string Summary { get; set; } = "";

        public static string OutcomeName(BattleOutcome o)
        {
            switch (o)
            {
                case BattleOutcome.Galaba: return "G'ALABA";
                case BattleOutcome.Maglubiyat: return "MAG'LUBIYAT";
                case BattleOutcome.Durang: return "DURANG";
                default: return "CHEKINISH";
            }
        }
    }
}
