namespace AndijonSaltanatlar.Battles
{
    public enum BattleState { None, Preparing, Fighting, Finished }

    /// <summary>Faol strategik jang (animatsiya sahnasi uchun holat).</summary>
    public sealed class Battle
    {
        public int AttackerKingdomId { get; set; }
        public int DefenderKingdomId { get; set; }
        public int RegionId { get; set; }
        public BattleState State { get; set; } = BattleState.None;
        public BattleResult Result { get; set; }
    }
}
