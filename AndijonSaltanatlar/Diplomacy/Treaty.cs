namespace AndijonSaltanatlar.Diplomacy
{
    public enum TreatyType { TradePact, Alliance, Peace }

    /// <summary>Ikki saltanat o'rtasidagi shartnoma (ittifoq, savdo bitimi, tinchlik).</summary>
    public sealed class Treaty
    {
        public int Id { get; set; }
        public TreatyType Type { get; set; }
        public int KingdomA { get; set; }
        public int KingdomB { get; set; }
        public int SinceDay { get; set; }
        public bool Active { get; set; } = true;
    }
}
