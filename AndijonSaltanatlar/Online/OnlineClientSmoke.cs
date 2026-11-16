using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AndijonSaltanatlar.Shared.DTOs;
using AndijonSaltanatlar.Shared.Farming;

namespace AndijonSaltanatlar.Online
{
    /// <summary>
    /// PROMPT 19A — o'yin clientining online zanjiri (UI tugmalari chaqiradigan AYNAN shu OnlineClient metodlari):
    ///   Sotuvchi (Windows):  login → maydon → ekish → o'sish → hosil → 100 guruchni 500 tangaga sotuvga qo'yish
    ///   Xaridor (2-client):  login → xuddi shu dunyo → bozor → sotib olish
    ///   Sotuvchi:            SignalR orqali "guruchingiz sotildi" xabarini oladi, balansi yangilanadi.
    /// Ishga tushirish: AndijonSaltanatlar.exe --online-smoke natija.txt --online-server http://IP:5080
    /// </summary>
    public static class OnlineClientSmoke
    {
        public static string Run(string server)
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail = "")
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS" : "FAIL") + " | " + name + (detail.Length > 0 ? "  [" + detail + "]" : ""));
            }
            sb.AppendLine("ONLINE CLIENT SMOKE - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + server);
            string tag = DateTime.Now.ToString("HHmmss");
            var seller = new OnlineClient();
            var buyer = new OnlineClient();
            try
            {
                // Har ikki client xuddi o'yindagidek: buyruq → fon vazifasi → Pump() orqali natija.
                bool Wait(OnlineClient c, Func<bool> cond, int seconds)
                {
                    DateTime end = DateTime.UtcNow.AddSeconds(seconds);
                    while (DateTime.UtcNow < end)
                    {
                        c.Pump();
                        if (!c.Busy && cond()) return true;
                        Thread.Sleep(100);
                    }
                    c.Pump();
                    return cond();
                }
                bool Idle(OnlineClient c) => Wait(c, () => true, 20);

                seller.Login(server, "win_" + tag, "Parol123!", register: true);
                Check("Windows client serverga ulandi va dunyoga kirdi", Wait(seller, () => seller.LoggedIn, 20), seller.Message);
                Check("Windows client real vaqt kanaliga (SignalR) ulandi", Wait(seller, () => seller.HubConnected, 15));

                seller.BuildField(); Idle(seller);
                Check("Ekin maydoni qurildi", seller.State.Fields.Count == 1, seller.Message);
                int field = seller.State.Fields[0].Id;
                seller.Plant(field); Idle(seller);
                Check("Guruch ekildi", seller.State.Fields[0].State == RiceFieldState.Planted, seller.Message);

                bool sawGrowing = false;
                bool mature = Wait(seller, () =>
                {
                    if (!seller.Busy && DateTime.UtcNow.Second % 5 == 0) seller.QueueRefresh();
                    RiceFieldState st = seller.State.Fields[0].State;
                    if (st == RiceFieldState.Growing) sawGrowing = true;
                    return st == RiceFieldState.Mature;
                }, RiceFarmingRules.SecondsToMature(seller.State.Fields[0].PlantedAtGameMinute, seller.State.ServerGameMinute) + 40);
                Check("Guruch o'sdi (server vaqti bo'yicha)", sawGrowing);
                Check("Guruch yetildi", mature);

                seller.Harvest(field); Idle(seller);
                Check("Hosil omborga qo'shildi", seller.State.Rice == RiceFarmingRules.HarvestYield, "guruch=" + seller.State.Rice);
                seller.Harvest(field); Idle(seller);
                Check("Ikkinchi marta yig'ib bo'lmaydi", seller.MessageIsError && seller.State.Rice == RiceFarmingRules.HarvestYield, seller.Message);

                seller.CreateOffer(100, 500); Idle(seller);
                Check("100 guruch 500 tangaga sotuvga qo'yildi", seller.State.RiceReserved == 100 && seller.Offers.Any(o => o.IsMine && o.Quantity == 100), seller.Message);
                int sellerCoins = seller.State.Coin;

                buyer.Login(server, "android_" + tag, "Parol123!", register: true);
                Check("Ikkinchi client (xaridor) xuddi shu dunyoga kirdi", Wait(buyer, () => buyer.LoggedIn, 20) && buyer.State.WorldId == seller.State.WorldId, "world=" + buyer.State?.WorldId);
                Wait(buyer, () => buyer.HubConnected, 15);
                MarketOfferDto offer = buyer.Offers.FirstOrDefault(o => o.SellerKingdomId == seller.State.KingdomId);
                Check("Xaridor bozorda taklifni ko'rdi", offer != null && offer.Quantity == 100 && offer.TotalPrice == 500);
                int buyerCoins = buyer.State.Coin;
                buyer.Buy(offer?.Id ?? 0); Idle(buyer);
                Check("Sotib olindi: tanga rezervda, karvon yo'lga chiqdi", !buyer.MessageIsError && buyer.State.Coin == buyerCoins - 500 && buyer.State.Caravans.Any(c => c.Status == 0), buyer.Message);
                buyer.Buy(offer?.Id ?? 0); Idle(buyer);
                Check("Xuddi shu taklifni qayta sotib bo'lmaydi", buyer.MessageIsError && buyer.State.Coin == buyerCoins - 500, buyer.Message);

                bool notified = Wait(seller, () => seller.Notification != null, 10);
                Check("Sotuvchi SignalR orqali 'sotildi' xabarini oldi", notified, seller.Notification ?? "");
                seller.Notification = null;

                // Karvon server vaqti bo'yicha yuradi; yetib kelganda server ikkala clientga xabar yuboradi.
                bool delivered = Wait(buyer, () => buyer.State.Rice == 100, CaravanRules.MaxTravelSeconds + 30);
                Check("Karvon yetib keldi: xaridor omboriga +100 guruch", delivered, "guruch=" + buyer.State.Rice);
                Wait(seller, () => seller.State.Coin == sellerCoins + 500, 30);
                Check("Sotuvchi clientida tanga yangilandi (+500, real vaqt)", seller.State.Coin == sellerCoins + 500 && seller.State.RiceReserved == 0, sellerCoins + " -> " + seller.State.Coin + " · " + (seller.Notification ?? ""));
            }
            catch (Exception ex)
            {
                Check("Kutilmagan xatosiz", false, ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                seller.Dispose();
                buyer.Dispose();
            }
            sb.AppendLine("NATIJA: " + pass + " PASS / " + fail + " FAIL");
            return sb.ToString();
        }

        public static void RunToFile(string server, string path)
        {
            string text = Run(server);
            try { File.WriteAllText(path, text); } catch { }
            Console.WriteLine(text);
        }
    }
}
