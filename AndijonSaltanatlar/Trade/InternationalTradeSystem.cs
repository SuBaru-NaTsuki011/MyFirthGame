using System.Collections.Generic;
using AndijonSaltanatlar.Kingdoms;

namespace AndijonSaltanatlar.Trade
{
    /// <summary>
    /// Davlatlararo savdoni boshqaradi: shartnoma takliflari, qabul/rad, va faol bitimlar bo'yicha
    /// har strategik tikда tanga oqimi. Bir xil bitim ikki marta ishlamaydi; mablag' yetmasa — bajarilmaydi.
    /// </summary>
    public sealed class InternationalTradeSystem
    {
        private int _nextId = 1;
        public List<TradeAgreement> Agreements { get; } = new List<TradeAgreement>();

        public TradeAgreement Propose(int seller, int buyer, string resource, int qty, int price, int duration)
        {
            var a = new TradeAgreement
            {
                Id = _nextId++, SellerKingdomId = seller, BuyerKingdomId = buyer,
                ResourceType = resource, Quantity = qty, Price = price, Duration = duration,
                Status = TradeStatus.Pending
            };
            Agreements.Add(a);
            return a;
        }

        public void SetStatus(TradeAgreement a, TradeStatus s) => a.Status = s;

        public int ActiveCount
        {
            get { int n = 0; foreach (var a in Agreements) if (a.Status == TradeStatus.Active) n++; return n; }
        }

        /// <summary>Faol bitimlarni bir kun davomida qayta ishlaydi: tanga xaridordan sotuvchiga.</summary>
        public void Tick(KingdomManager kingdoms)
        {
            foreach (TradeAgreement a in Agreements)
            {
                if (a.Status != TradeStatus.Active) continue;
                Kingdom buyer = kingdoms.Get(a.BuyerKingdomId);
                Kingdom seller = kingdoms.Get(a.SellerKingdomId);
                if (buyer == null || seller == null) { a.Status = TradeStatus.Expired; continue; }

                if (buyer.Treasury >= a.Price)
                {
                    buyer.Treasury -= a.Price;
                    seller.Treasury += a.Price;
                    // Ikkala tomon ham savdodan foyda ko'radi (iqtisodiy jonlanish)
                    seller.EconomicStrength = System.Math.Min(100, seller.EconomicStrength + 1);
                    buyer.EconomicStrength = System.Math.Min(100, buyer.EconomicStrength + 1);
                }
                a.Duration--;
                if (a.Duration <= 0) a.Status = TradeStatus.Expired;
            }
        }
    }
}
