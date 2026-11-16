using System.Collections.Generic;
using AndijonSaltanatlar.Shared.Farming;

namespace AndijonSaltanatlar.Shared.DTOs
{
    /// <summary>Dunyoga qo'shilish (saltanat yo'q bo'lsa yaratiladi).</summary>
    public sealed class JoinWorldRequest
    {
        public int WorldId { get; set; } = 1;
        public string KingdomName { get; set; } = "";
    }

    public sealed class RiceFieldDto
    {
        public int Id { get; set; }
        public int Slot { get; set; }
        /// <summary>Server vaqti bo'yicha HISOBLANGAN holat.</summary>
        public RiceFieldState State { get; set; }
        public long PlantedAtGameMinute { get; set; }
        /// <summary>Ekin turi (PROMPT 19B): Rice, Wheat, Cotton, Fruit, Vegetable.</summary>
        public string Crop { get; set; } = "Rice";
        public float Progress { get; set; }
        public int SecondsToMature { get; set; }
    }

    public sealed class MarketOfferDto
    {
        public int Id { get; set; }
        public int SellerKingdomId { get; set; }
        public string SellerName { get; set; } = "";
        public string Product { get; set; } = "Rice";
        public int Quantity { get; set; }
        public int TotalPrice { get; set; }
        public bool IsMine { get; set; }
    }

    /// <summary>O'yinchining online holati (server tasdiqlagan yagona haqiqat).</summary>
    public sealed class OnlineStateDto
    {
        public int WorldId { get; set; }
        public string WorldName { get; set; } = "";
        public int KingdomId { get; set; }
        public string KingdomName { get; set; } = "";
        public string Username { get; set; } = "";
        public int Coin { get; set; }
        public int Rice { get; set; }
        /// <summary>Sotuvga qo'yilgan (rezervdagi) guruch.</summary>
        public int RiceReserved { get; set; }
        public int WaterSources { get; set; }
        public long ServerGameMinute { get; set; }
        public int Day { get; set; }
        public long StateVersion { get; set; }
        public List<RiceFieldDto> Fields { get; set; } = new();
        public List<string> PlayersOnline { get; set; } = new();
        /// <summary>PROMPT 19B: barcha mahsulotlar ombori (kalit = mahsulot, qiymat = mavjud miqdor). Rice ham shu yerda.</summary>
        public Dictionary<string, int> Inventory { get; set; } = new();
        public Dictionary<string, int> Reserved { get; set; } = new();
        public List<CaravanDto> Caravans { get; set; } = new();
        public int CityCount { get; set; }
        public int Population { get; set; }
    }

    public sealed class FieldActionRequest
    {
        public string CommandId { get; set; } = "";
        public int FieldId { get; set; }
    }

    public sealed class CreateOfferRequest
    {
        public string CommandId { get; set; } = "";
        /// <summary>PROMPT 19B: mahsulot turi (bo'sh = Rice, 19A bilan mos).</summary>
        public string Product { get; set; } = "Rice";
        public int Quantity { get; set; }
        public int TotalPrice { get; set; }
        /// <summary>PROMPT 20: 0 = umumiy bozor; aks holda faqat shu ittifoq a'zolari.</summary>
        public int AllianceId { get; set; }
    }

    public sealed class BuyOfferRequest
    {
        public string CommandId { get; set; } = "";
        public int OfferId { get; set; }
    }

    public sealed class ActionResultDto
    {
        public bool Success { get; set; }
        public string MessageUz { get; set; } = "";
        /// <summary>Buyruq avval bajarilganmi (idempotent takror).</summary>
        public bool Duplicate { get; set; }
        public OnlineStateDto State { get; set; }

        public static ActionResultDto Fail(string msg) => new() { Success = false, MessageUz = msg };
    }

    public sealed class TradeRecordDto
    {
        public int Id { get; set; }
        public int OfferId { get; set; }
        public string SellerName { get; set; } = "";
        public string BuyerName { get; set; } = "";
        public int Quantity { get; set; }
        public int TotalPrice { get; set; }
        public string AtUtc { get; set; } = "";
        public string Product { get; set; } = "Rice";
        /// <summary>0 = karvon yo'lda, 1 = yetkazildi.</summary>
        public int DeliveryStatus { get; set; }
    }
}
