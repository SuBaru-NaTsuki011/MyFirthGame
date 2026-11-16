using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Server.Services;
using AndijonSaltanatlar.Shared.DTOs;
using AndijonSaltanatlar.Shared.Farming;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>PROMPT 19A — guruch yetishtirish va o'yinchilar savdosi (server mantiqi, haqiqiy EF Core + SQLite).</summary>
    public class OnlineTradeTests : IDisposable
    {
        private sealed class FakeClock : IServerClock
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);
            public void Advance(double seconds) => UtcNow = UtcNow.AddSeconds(seconds);
        }

        private readonly SqliteConnection _conn;
        private readonly FakeClock _clock = new FakeClock();

        public OnlineTradeTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
            using GameDbContext db = NewDb();
            db.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private GameDbContext NewDb() => new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_conn).Options);
        private OnlineGameService Svc(GameDbContext db) => new OnlineGameService(db, _clock);
        private static string Id() => Guid.NewGuid().ToString("N");

        private async Task<Kingdom> Player(string name, int coin = 500)
        {
            using GameDbContext db = NewDb();
            var u = new AppUser { Username = name, Email = name + "@t", PasswordHash = "x" };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            Kingdom k = await Svc(db).JoinAsync(u.Id, new JoinWorldRequest { WorldId = 1 });
            await db.KingdomResources.Where(r => r.KingdomId == k.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.Coin, coin));
            return k;
        }

        private async Task<OnlineStateDto> Do(Kingdom k, Func<OnlineGameService, Task<ActionResultDto>> act, bool expectOk = true)
        {
            using GameDbContext db = NewDb();
            ActionResultDto r = await act(Svc(db));
            Assert.True(r.Success == expectOk, r.MessageUz);
            return r.State;
        }

        private async Task<int> MatureField(Kingdom k)
        {
            OnlineStateDto s = await Do(k, sv => sv.BuildFieldAsync(k, Id()));
            int field = s.Fields[0].Id;
            await Do(k, sv => sv.PlantAsync(k, Id(), field));
            _clock.Advance(RiceFarmingRules.PlantedToMatureMinutes / RiceFarmingRules.GameMinutesPerRealSecond + 1);
            return field;
        }

        [Fact]
        public void Rules_StagesFollowServerTime()
        {
            Assert.Equal(RiceFieldState.Planted, RiceFarmingRules.Evaluate(RiceFieldState.Planted, 100, 100));
            Assert.Equal(RiceFieldState.Growing, RiceFarmingRules.Evaluate(RiceFieldState.Planted, 100, 100 + RiceFarmingRules.PlantedToGrowingMinutes));
            Assert.Equal(RiceFieldState.Mature, RiceFarmingRules.Evaluate(RiceFieldState.Planted, 100, 100 + RiceFarmingRules.PlantedToMatureMinutes));
            Assert.Equal(RiceFieldState.Harvested, RiceFarmingRules.Evaluate(RiceFieldState.Harvested, 0, 99999));
            Assert.Equal(1f, RiceFarmingRules.Progress(RiceFieldState.Planted, 0, 99999));
            Assert.False(RiceFarmingRules.HasWaterFor(4, 1));
            Assert.True(RiceFarmingRules.HasWaterFor(3, 1));
        }

        [Fact]
        public async Task Rice_GrowsThenHarvestedOnlyOnce()
        {
            Kingdom k = await Player("a");
            OnlineStateDto s = await Do(k, sv => sv.BuildFieldAsync(k, Id()));
            int field = s.Fields[0].Id;
            await Do(k, sv => sv.PlantAsync(k, Id(), field));
            await Do(k, sv => sv.HarvestAsync(k, Id(), field), expectOk: false); // hali yetilmagan

            _clock.Advance(RiceFarmingRules.PlantedToGrowingMinutes / RiceFarmingRules.GameMinutesPerRealSecond + 1);
            using (GameDbContext db = NewDb()) Assert.Equal(RiceFieldState.Growing, (await Svc(db).StateAsync(k)).Fields[0].State);

            _clock.Advance(RiceFarmingRules.PlantedToMatureMinutes / RiceFarmingRules.GameMinutesPerRealSecond);
            s = await Do(k, sv => sv.HarvestAsync(k, Id(), field));
            Assert.Equal(RiceFarmingRules.HarvestYield, s.Rice);
            s = await Do(k, sv => sv.HarvestAsync(k, Id(), field), expectOk: false);
            Assert.Equal(RiceFarmingRules.HarvestYield, s.Rice);
        }

        [Fact]
        public async Task Plant_RequiresWater()
        {
            Kingdom k = await Player("w", coin: 5000);
            OnlineStateDto s = null;
            for (int i = 0; i < 5; i++) s = await Do(k, sv => sv.BuildFieldAsync(k, Id()));
            for (int i = 0; i < 4; i++) { int f = s.Fields[i].Id; await Do(k, sv => sv.PlantAsync(k, Id(), f)); }
            int fifth = s.Fields[4].Id;
            await Do(k, sv => sv.PlantAsync(k, Id(), fifth), expectOk: false);   // 1 ariq = 4 maydon
            await Do(k, sv => sv.BuildWaterAsync(k, Id()));
            await Do(k, sv => sv.PlantAsync(k, Id(), fifth));
        }

        [Fact]
        public async Task DuplicateCommandId_IsNotAppliedTwice()
        {
            Kingdom k = await Player("d");
            string cmd = Id();
            OnlineStateDto first = await Do(k, sv => sv.BuildFieldAsync(k, cmd));
            using GameDbContext db = NewDb();
            ActionResultDto again = await Svc(db).BuildFieldAsync(k, cmd);
            Assert.True(again.Duplicate);
            Assert.Equal(first.Coin, again.State.Coin);
            Assert.Single(again.State.Fields);
        }

        [Fact]
        public async Task Trade_TransfersCoinAndRice_AndCannotRepeat()
        {
            Kingdom seller = await Player("seller");
            Kingdom buyer = await Player("buyer");
            int field = await MatureField(seller);
            await Do(seller, sv => sv.HarvestAsync(seller, Id(), field));
            OnlineStateDto s = await Do(seller, sv => sv.CreateOfferAsync(seller, new CreateOfferRequest { CommandId = Id(), Quantity = 100, TotalPrice = 500 }));
            Assert.Equal(100, s.RiceReserved);
            Assert.Equal(20, s.Rice);
            // Rezervdagi guruchni boshqa taklifga qo'yib bo'lmaydi.
            await Do(seller, sv => sv.CreateOfferAsync(seller, new CreateOfferRequest { CommandId = Id(), Quantity = 100, TotalPrice = 500 }), expectOk: false);

            int offerId;
            using (GameDbContext db = NewDb()) offerId = (await Svc(db).OffersAsync(buyer)).Single().Id;
            int sellerCoin = s.Coin;
            OnlineStateDto b = await Do(buyer, sv => sv.BuyAsync(buyer, new BuyOfferRequest { CommandId = Id(), OfferId = offerId }));
            Assert.Equal(0, b.Coin);           // tanga karvon yetguncha rezervda
            Assert.Equal(0, b.Rice);           // mol hali yo'lda (PROMPT 19B: karvon orqali)
            Assert.Single(b.Caravans);
            await Do(buyer, sv => sv.BuyAsync(buyer, new BuyOfferRequest { CommandId = Id(), OfferId = offerId }), expectOk: false);

            // Karvon yetib keladi — mol xaridorga, pul sotuvchiga; ikkinchi yetkazish hech narsa qilmaydi.
            _clock.Advance(CaravanRules.MaxTravelSeconds + 1);
            using (GameDbContext d1 = NewDb()) Assert.Single(await Svc(d1).DeliverDueAsync());
            using (GameDbContext d2 = NewDb()) Assert.Empty(await Svc(d2).DeliverDueAsync());
            using (GameDbContext d3 = NewDb()) Assert.Equal(100, (await Svc(d3).StateAsync(buyer)).Rice);

            using GameDbContext check = NewDb();
            OnlineStateDto after = await Svc(check).StateAsync(seller);
            Assert.Equal(sellerCoin + 500, after.Coin);
            Assert.Equal(0, after.RiceReserved);
            Assert.Equal(1, await check.TradeTransactions.CountAsync(t => t.OfferId == offerId));
        }

        [Fact]
        public async Task Trade_FailsWithoutCoins_AndOfferStaysActive()
        {
            Kingdom seller = await Player("s2");
            Kingdom poor = await Player("poor", coin: 100);
            int field = await MatureField(seller);
            await Do(seller, sv => sv.HarvestAsync(seller, Id(), field));
            await Do(seller, sv => sv.CreateOfferAsync(seller, new CreateOfferRequest { CommandId = Id(), Quantity = 50, TotalPrice = 400 }));
            int offerId;
            using (GameDbContext db = NewDb()) offerId = (await Svc(db).OffersAsync(poor)).Single().Id;
            OnlineStateDto p = await Do(poor, sv => sv.BuyAsync(poor, new BuyOfferRequest { CommandId = Id(), OfferId = offerId }), expectOk: false);
            Assert.Equal(100, p.Coin);
            Assert.Equal(0, p.Rice);
            using GameDbContext check = NewDb();
            Assert.Equal(OfferStatus.Active, (await check.MarketOffers.SingleAsync(o => o.Id == offerId)).Status);
            // O'z taklifini sotib bo'lmaydi; bekor qilinsa guruch omborga qaytadi.
            await Do(seller, sv => sv.BuyAsync(seller, new BuyOfferRequest { CommandId = Id(), OfferId = offerId }), expectOk: false);
            OnlineStateDto s = await Do(seller, sv => sv.CancelOfferAsync(seller, Id(), offerId));
            Assert.Equal(RiceFarmingRules.HarvestYield, s.Rice);
            Assert.Equal(0, s.RiceReserved);
        }
    }
}
