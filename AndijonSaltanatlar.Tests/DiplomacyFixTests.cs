using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Server.Services;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>
    /// PROMPT 20 auditi paytida topilgan xatolar uchun regressiya testlari:
    /// hudud kelishuvi atomikligi va poytaxt himoyasi, hissalar yo'qolmasligi, loyiha bir marta yakunlanishi,
    /// tarqatilgan ittifoq xazinasi qaytishi, rollar, yangi saltanatning qurilish resurslari.
    /// </summary>
    public class DiplomacyFixTests : IDisposable
    {
        private sealed class FakeClock : IServerClock { public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 16, 10, 0, 0, DateTimeKind.Utc); }

        private readonly SqliteConnection _conn;
        private readonly FakeClock _clock = new FakeClock();

        public DiplomacyFixTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
            using GameDbContext db = NewDb();
            db.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private GameDbContext NewDb() => new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_conn).Options);
        private OnlineGameService Svc(GameDbContext db) => new OnlineGameService(db, _clock);
        private DiplomacyService Dip(GameDbContext db) => new DiplomacyService(Svc(db), null);
        private static string Id() => Guid.NewGuid().ToString("N");

        private async Task<Kingdom> Player(string name, int coin = 3000)
        {
            using GameDbContext db = NewDb();
            var u = new AppUser { Username = name, Email = name + "@t", PasswordHash = "x" };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            Kingdom k = await Svc(db).JoinAsync(u.Id, new JoinWorldRequest { WorldId = 1 });
            await db.KingdomResources.Where(r => r.KingdomId == k.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.Coin, coin));
            return k;
        }

        private async Task<int> AllianceWith(Kingdom founder, params Kingdom[] members)
        {
            using (GameDbContext db = NewDb()) Assert.True((await Dip(db).CreateAllianceAsync(founder, new CreateAllianceRequest { CommandId = Id(), Name = "Vodiy " + founder.Id })).Success);
            int aid;
            using (GameDbContext db = NewDb()) aid = (await Dip(db).MyAllianceAsync(founder)).Id;
            foreach (Kingdom m in members)
            {
                using (GameDbContext db = NewDb()) Assert.True((await Dip(db).InviteAsync(founder, new InviteToAllianceRequest { CommandId = Id(), AllianceId = aid, ToKingdomId = m.Id })).Success);
                int inv;
                using (GameDbContext db = NewDb()) inv = (await Dip(db).InvitationsAsync(m)).Single().Id;
                using (GameDbContext db = NewDb()) Assert.True((await Dip(db).RespondInvitationAsync(m, new RespondInvitationRequest { CommandId = Id(), InvitationId = inv, Accept = true })).Success);
            }
            return aid;
        }

        /// <summary>O'yinchiga ikkinchi (poytaxt bo'lmagan) hudud beradi — mustaqil hududni birlashtirib.</summary>
        private async Task<RegionEntity> SecondRegion(Kingdom k)
        {
            RegionEntity free;
            using (GameDbContext db = NewDb()) free = await db.Regions.AsNoTracking().FirstAsync(r => r.OwnerKingdomId == 0);
            using (GameDbContext db = NewDb())
            {
                var (r, _, _) = await new KingdomService(Svc(db)).AnnexAsync(k, Id(), free.Id);
                Assert.True(r.Success, r.MessageUz);
            }
            return free;
        }

        [Fact]
        public void NewKingdom_StartsWithBuildingResources()
        {
            var res = new KingdomResource();
            Assert.True(res.Wood > 0 && res.Stone > 0);
        }

        [Fact]
        public async Task TerritoryAgreement_CapitalAndLastRegionProtected()
        {
            Kingdom a = await Player("a");
            Kingdom b = await Player("b");
            int capitalRegion;
            using (GameDbContext db = NewDb()) capitalRegion = (await db.Cities.SingleAsync(c => c.KingdomId == a.Id && c.IsCapital)).RegionId;
            using (GameDbContext db = NewDb())
                Assert.True((await Dip(db).ProposeAgreementAsync(a, new ProposeAgreementRequest { CommandId = Id(), Type = 2, ToKingdomId = b.Id, RegionId = capitalRegion, Price = 100 })).Success);
            int agId;
            using (GameDbContext db = NewDb()) agId = (await db.DiplomaticAgreements.SingleAsync()).Id;
            using (GameDbContext db = NewDb())
            {
                ActionResultDto r = await Dip(db).RespondAgreementAsync(b, new RespondAgreementRequest { CommandId = Id(), AgreementId = agId, Accept = true });
                Assert.False(r.Success);
            }
            using GameDbContext check = NewDb();
            Assert.Equal(a.Id, (await check.Regions.SingleAsync(r => r.Id == capitalRegion)).OwnerKingdomId);
            Assert.Equal(3000, (await check.KingdomResources.SingleAsync(r => r.KingdomId == b.Id)).Coin); // pul yechilmadi
        }

        [Fact]
        public async Task TerritoryAgreement_TransfersOnce_EvenWithTwoAcceptedOffers()
        {
            Kingdom a = await Player("seller");
            Kingdom b = await Player("buyer1");
            Kingdom c = await Player("buyer2");
            RegionEntity region = await SecondRegion(a);
            using (GameDbContext db = NewDb()) await Dip(db).ProposeAgreementAsync(a, new ProposeAgreementRequest { CommandId = Id(), Type = 2, ToKingdomId = b.Id, RegionId = region.Id, Price = 200 });
            using (GameDbContext db = NewDb()) await Dip(db).ProposeAgreementAsync(a, new ProposeAgreementRequest { CommandId = Id(), Type = 2, ToKingdomId = c.Id, RegionId = region.Id, Price = 200 });
            int agB, agC;
            using (GameDbContext db = NewDb())
            {
                agB = (await db.DiplomaticAgreements.SingleAsync(x => x.ToKingdomId == b.Id)).Id;
                agC = (await db.DiplomaticAgreements.SingleAsync(x => x.ToKingdomId == c.Id)).Id;
            }
            ActionResultDto rb, rc;
            using (GameDbContext db = NewDb()) rb = await Dip(db).RespondAgreementAsync(b, new RespondAgreementRequest { CommandId = Id(), AgreementId = agB, Accept = true });
            using (GameDbContext db = NewDb()) rc = await Dip(db).RespondAgreementAsync(c, new RespondAgreementRequest { CommandId = Id(), AgreementId = agC, Accept = true });
            Assert.True(rb.Success, rb.MessageUz);
            Assert.False(rc.Success);

            using GameDbContext check = NewDb();
            Assert.Equal(b.Id, (await check.Regions.SingleAsync(r => r.Id == region.Id)).OwnerKingdomId);
            CityEntity city = await check.Cities.SingleAsync(x => x.RegionId == region.Id);
            Assert.Equal(b.Id, city.KingdomId);
            Assert.False(city.IsCapital);
            Assert.Equal(3000, (await check.KingdomResources.SingleAsync(r => r.KingdomId == c.Id)).Coin); // c dan pul yechilmadi
            Assert.Equal(2800, (await check.KingdomResources.SingleAsync(r => r.KingdomId == b.Id)).Coin);
        }

        [Fact]
        public async Task Contributions_AreCappedAndCompleteProjectOnce_WithBenefit()
        {
            Kingdom a = await Player("pa");
            Kingdom b = await Player("pb");
            int aid = await AllianceWith(a, b);
            using (GameDbContext db = NewDb()) Assert.True((await Dip(db).CreateProjectAsync(a, new CreateAllianceProjectRequest { CommandId = Id(), AllianceId = aid, Type = 0 })).Success);
            int pid;
            using (GameDbContext db = NewDb()) pid = (await db.AllianceProjects.SingleAsync()).Id;
            (int coin, int wood, _) = DiplomacyService.ProjectCost(0);

            // Windows: yog'och; Android: tanga (talabdan ko'p) — ortiqchasi o'yinchida qoladi.
            using (GameDbContext db = NewDb()) Assert.True((await Dip(db).ContributeAsync(a, new ContributeAllianceRequest { CommandId = Id(), AllianceId = aid, ProjectId = pid, Resource = "Wood", Amount = wood })).Success);
            using (GameDbContext db = NewDb()) Assert.True((await Dip(db).ContributeAsync(b, new ContributeAllianceRequest { CommandId = Id(), AllianceId = aid, ProjectId = pid, Resource = "Coin", Amount = coin + 500 })).Success);
            using (GameDbContext db = NewDb())
            {
                AllianceProjectEntity p = await db.AllianceProjects.SingleAsync();
                Assert.Equal(ProjectStatus.Completed, p.Status);
                Assert.Equal(coin, p.GotCoin);
                Assert.Equal(3000 - coin, (await db.KingdomResources.SingleAsync(r => r.KingdomId == b.Id)).Coin);
                int x = Math.Min(a.Id, b.Id), y = Math.Max(a.Id, b.Id);
                Assert.True((await db.DiplomaticRelations.SingleAsync(r => r.KingdomA == x && r.KingdomB == y)).Value >= 15); // savdo yo'li foydasi
                Assert.Equal(1, await db.WorldNews.CountAsync(n => n.Type == "project_done"));
            }
            // Yakunlangan loyihaga qo'shimcha hissa qabul qilinmaydi.
            using (GameDbContext db = NewDb()) Assert.False((await Dip(db).ContributeAsync(a, new ContributeAllianceRequest { CommandId = Id(), AllianceId = aid, ProjectId = pid, Resource = "Coin", Amount = 10 })).Success);
        }

        [Fact]
        public async Task ParallelContributions_AreNotLost()
        {
            Kingdom a = await Player("c1");
            Kingdom b = await Player("c2");
            int aid = await AllianceWith(a, b);
            using (GameDbContext db = NewDb()) await Dip(db).CreateProjectAsync(a, new CreateAllianceProjectRequest { CommandId = Id(), AllianceId = aid, Type = 2 });
            int pid;
            using (GameDbContext db = NewDb()) pid = (await db.AllianceProjects.SingleAsync()).Id;
            using GameDbContext d1 = NewDb();
            using GameDbContext d2 = NewDb();
            var results = await Task.WhenAll(
                Dip(d1).ContributeAsync(a, new ContributeAllianceRequest { CommandId = Id(), AllianceId = aid, ProjectId = pid, Resource = "Coin", Amount = 100 }),
                Dip(d2).ContributeAsync(b, new ContributeAllianceRequest { CommandId = Id(), AllianceId = aid, ProjectId = pid, Resource = "Coin", Amount = 100 }));
            using GameDbContext check = NewDb();
            int ok = results.Count(r => r.Success);
            Assert.Equal(ok * 100, (await check.AllianceProjects.SingleAsync()).GotCoin);           // hech bir hissa yo'qolmadi
            Assert.Equal(ok * 100, await check.AllianceContributions.SumAsync(x => x.Amount));
        }

        [Fact]
        public async Task DissolvedAlliance_ReturnsTreasuryToLastMember()
        {
            Kingdom a = await Player("solo");
            int aid = await AllianceWith(a);
            using (GameDbContext db = NewDb()) Assert.True((await Dip(db).ContributeAsync(a, new ContributeAllianceRequest { CommandId = Id(), AllianceId = aid, ProjectId = 0, Resource = "Coin", Amount = 400 })).Success);
            using (GameDbContext db = NewDb()) Assert.Equal(2600, (await db.KingdomResources.SingleAsync(r => r.KingdomId == a.Id)).Coin);
            using (GameDbContext db = NewDb()) Assert.True((await Dip(db).LeaveAllianceAsync(a, new LeaveAllianceRequest { CommandId = Id(), AllianceId = aid })).Success);
            using GameDbContext check = NewDb();
            Assert.Equal(3000, (await check.KingdomResources.SingleAsync(r => r.KingdomId == a.Id)).Coin);
            Assert.False(await check.Alliances.AnyAsync());
        }

        [Fact]
        public async Task AllianceMarketOffer_CannotBeBoughtByOutsider()
        {
            Kingdom a = await Player("am1");
            Kingdom b = await Player("am2");
            Kingdom outsider = await Player("am3");
            int aid = await AllianceWith(a, b);
            using (GameDbContext db = NewDb()) await db.KingdomResources.Where(r => r.KingdomId == a.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.Rice, 200));
            using (GameDbContext db = NewDb())
                Assert.True((await Svc(db).CreateOfferAsync(a, new CreateOfferRequest { CommandId = Id(), Product = "Rice", Quantity = 100, TotalPrice = 300, AllianceId = aid })).Success);
            int offerId;
            using (GameDbContext db = NewDb()) offerId = (await db.MarketOffers.SingleAsync()).Id;
            using (GameDbContext db = NewDb()) Assert.Empty(await Svc(db).OffersAsync(outsider));            // umumiy bozorda ko'rinmaydi
            using (GameDbContext db = NewDb()) Assert.False((await Svc(db).BuyAsync(outsider, new BuyOfferRequest { CommandId = Id(), OfferId = offerId })).Success);
            using (GameDbContext db = NewDb()) Assert.True((await Svc(db).BuyAsync(b, new BuyOfferRequest { CommandId = Id(), OfferId = offerId })).Success);
        }

        [Fact]
        public async Task Roles_OnlyFounderAssigns_EnvoyCanInvite()
        {
            Kingdom a = await Player("f");
            Kingdom b = await Player("m");
            Kingdom c = await Player("x");
            int aid = await AllianceWith(a, b);
            // A'zo taklif qila olmaydi
            using (GameDbContext db = NewDb()) Assert.False((await Dip(db).InviteAsync(b, new InviteToAllianceRequest { CommandId = Id(), AllianceId = aid, ToKingdomId = c.Id })).Success);
            // A'zo rol bera olmaydi
            using (GameDbContext db = NewDb()) Assert.False((await Dip(db).SetRoleAsync(b, new SetAllianceRoleRequest { CommandId = Id(), KingdomId = b.Id, Role = 1 })).Success);
            using (GameDbContext db = NewDb()) Assert.True((await Dip(db).SetRoleAsync(a, new SetAllianceRoleRequest { CommandId = Id(), KingdomId = b.Id, Role = 1 })).Success);
            using (GameDbContext db = NewDb()) Assert.True((await Dip(db).InviteAsync(b, new InviteToAllianceRequest { CommandId = Id(), AllianceId = aid, ToKingdomId = c.Id })).Success);
        }
    }
}
