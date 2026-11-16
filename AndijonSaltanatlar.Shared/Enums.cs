namespace AndijonSaltanatlar.Shared
{
    /// <summary>Client va server o'rtasida umumiy buyruq turlari (server-authoritative).</summary>
    public enum CommandType
    {
        CreateKingdom,
        PlaceBuilding,
        UpgradeBuilding,
        UnlockLand,
        ConstructRoad,
        ChangeTaxRate,
        StartResearch,
        CreateTradeOffer,
        AcceptTradeOffer,
        RejectTradeOffer
    }

    /// <summary>Umumiy bino turlari (shared). Client ichidagi BuildingType bilan mos keladi.</summary>
    public enum BuildingKind
    {
        House, Market, Tower, Wall, Gate, MainHall, Garden, Barracks, Madrasa, Mosque, Caravanserai
    }

    public enum ResourceKind { Coin, Food, Wood, Stone }

    public enum DiplomaticStance { War, Cold, Neutral, Friendly, Allied }

    public enum TradeStatus { Pending, Active, Rejected, Expired }
}
