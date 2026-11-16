namespace AndijonSaltanatlar.Trade
{
    public enum TradeStatus { Pending, Active, Rejected, Expired }

    /// <summary>
    /// Davlatlararo savdo shartnomasi. Faol bo'lsa, har strategik tikда xaridor sotuvchiga narx
    /// to'laydi va resurs oladi. Mablag'/resurs yetmasa bajarilmaydi.
    /// </summary>
    public sealed class TradeAgreement
    {
        public int Id { get; set; }
        public int SellerKingdomId { get; set; }
        public int BuyerKingdomId { get; set; }
        public string ResourceType { get; set; } = "Oziq-ovqat";
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int Duration { get; set; }   // qolgan kunlar
        public TradeStatus Status { get; set; } = TradeStatus.Pending;
    }
}
