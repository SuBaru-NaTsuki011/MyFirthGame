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
    public class DiplomacyTests : IDisposable
    {
        private sealed class FakeClock : IServerClock { public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc); }

        private readonly SqliteConnection _conn;
        private readonly FakeClock _clock = new FakeClock();

        public DiplomacyTests()
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

        private async Task<Kingdom> Player(string name, int coin = 2000)
        {
            using GameDbContext db = NewDb();
            var u = new AppUser { Username = name, Email = name + "@t", PasswordHash = "x" };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            Kingdom k = await Svc(db).JoinAsync(u.Id, new JoinWorldRequest { WorldId = 1 });
            await db.KingdomResources.Where(r => r.KingdomId == k.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.Coin, coin).SetProperty(r => r.Wood, 100).SetProperty(r => r.Stone, 50));
            return k;
        }

        [Fact]
        public async Task Alliance_CreateInviteAccept_MembersAndNews()
        {
            Kingdom a = await Player("asaka");
            Kingdom b = await Player("shahrixon");

            using (GameDbContext db = NewDb())
            {
                var dip = Dip(db);
                var svc = Svc(db);
                Kingdom ka = await svc.KingdomOfUserAsync((await db.Users.FirstAsync(u => u.Username == "asaka")).Id);
                Kingdom kb = await svc.KingdomOfUserAsync((await db.Users.FirstAsync(u => u.Username == "shahrixon")).Id);
                ActionResultDto r = await dip.CreateAllianceAsync(ka, new CreateAllianceRequest { CommandId = Id(), Name = "Vodiy ittifoqi", Emblem = 1 });
                Assert.True(r.Success, r.MessageUz);
                AllianceDto mine = await dip.MyAllianceAsync(ka);
                Assert.NotNull(mine);
                Assert.Equal(1, mine.Members.Count);

                ActionResultDto inv = await dip.InviteAsync(ka, new InviteToAllianceRequest { CommandId = Id(), AllianceId = mine.Id, ToKingdomId = kb.Id });
                Assert.True(inv.Success, inv.MessageUz);

                var invs = await dip.InvitationsAsync(kb);
                Assert.Single(invs);
                ActionResultDto acc = await dip.RespondInvitationAsync(kb, new RespondInvitationRequest { CommandId = Id(), InvitationId = invs[0].Id, Accept = true });
                Assert.True(acc.Success, acc.MessageUz);

                AllianceDto mineB = await dip.MyAllianceAsync(kb);
                Assert.NotNull(mineB);
                Assert.Equal(mine.Id, mineB.Id);
                Assert.Equal(2, mineB.Members.Count);
            }

            using (GameDbContext db = NewDb())
            {
                var news = await Dip(db).NewsAsync(1);
                Assert.Contains(news, n => n.Type == "alliance_created");
                Assert.Contains(news, n => n.Type == "alliance_joined");
            }
        }

        [Fact]
        public async Task Contribution_IdempotentByCommand()
        {
            Kingdom a = await Player("a2");
            using GameDbContext db = NewDb();
            var dip = Dip(db);
            var svc = Svc(db);
            Kingdom ka = await svc.KingdomOfUserAsync((await db.Users.FirstAsync(u => u.Username == "a2")).Id);
            await dip.CreateAllianceAsync(ka, new CreateAllianceRequest { CommandId = Id(), Name = "T", Emblem = 1 });
            AllianceDto mine = await dip.MyAllianceAsync(ka);

            string cmd = Id();
            ActionResultDto r1 = await dip.ContributeAsync(ka, new ContributeAllianceRequest { CommandId = cmd, AllianceId = mine.Id, ProjectId = 0, Resource = "Coin", Amount = 100 });
            Assert.True(r1.Success, r1.MessageUz);
            ActionResultDto r2 = await dip.ContributeAsync(ka, new ContributeAllianceRequest { CommandId = cmd, AllianceId = mine.Id, ProjectId = 0, Resource = "Coin", Amount = 100 });
            Assert.True(r2.Duplicate);

            AllianceDto after = await dip.MyAllianceAsync(ka);
            Assert.Equal(100, after.TreasuryCoin);
        }

        [Fact]
        public async Task Project_CompletesWhenFunded()
        {
            Kingdom a = await Player("p1");
            using GameDbContext db = NewDb();
            var dip = Dip(db);
            var svc = Svc(db);
            Kingdom ka = await svc.KingdomOfUserAsync((await db.Users.FirstAsync(u => u.Username == "p1")).Id);
            await dip.CreateAllianceAsync(ka, new CreateAllianceRequest { CommandId = Id(), Name = "P", Emblem = 1 });
            AllianceDto mine = await dip.MyAllianceAsync(ka);
            await dip.CreateProjectAsync(ka, new CreateAllianceProjectRequest { CommandId = Id(), AllianceId = mine.Id, Type = 0 });

            var projects = await dip.ProjectsAsync(ka, mine.Id);
            Assert.Single(projects);
            int pid = projects[0].Id;
            int needCoin = projects[0].ReqCoin;
            int needWood = projects[0].ReqWood;
            await dip.ContributeAsync(ka, new ContributeAllianceRequest { CommandId = Id(), AllianceId = mine.Id, ProjectId = pid, Resource = "Coin", Amount = needCoin });
            await dip.ContributeAsync(ka, new ContributeAllianceRequest { CommandId = Id() + "w", AllianceId = mine.Id, ProjectId = pid, Resource = "Wood", Amount = needWood });
            var after = await dip.ProjectsAsync(ka, mine.Id);
            Assert.Equal(1, after[0].Status); // completed
        }

        [Fact]
        public async Task TerritoryAgreement_TransfersRegionAndCity()
        {
            Kingdom seller = await Player("seller2", coin: 5000);
            Kingdom buyer = await Player("buyer2", coin: 5000);
            using GameDbContext db = NewDb();
            var svc = Svc(db);
            var dip = Dip(db);
            Kingdom ks = await svc.KingdomOfUserAsync((await db.Users.FirstAsync(u => u.Username == "seller2")).Id);
            Kingdom kb = await svc.KingdomOfUserAsync((await db.Users.FirstAsync(u => u.Username == "buyer2")).Id);

            // Sotuvchi avval ikkinchi hududga ega bo'ladi (poytaxt yoki yagona hududni topshirib bo'lmaydi — PROMPT 20 auditi).
            RegionEntity region = await db.Regions.AsNoTracking().FirstAsync(r => r.OwnerKingdomId == 0);
            var (annex, _, _) = await new KingdomService(svc).AnnexAsync(ks, Id(), region.Id);
            Assert.True(annex.Success, annex.MessageUz);
            CityEntity city = await db.Cities.AsNoTracking().FirstAsync(c => c.RegionId == region.Id);

            // propose territory transfer from seller to buyer, price 100
            await dip.ProposeAgreementAsync(ks, new ProposeAgreementRequest { CommandId = Id(), Type = 2, ToKingdomId = kb.Id, RegionId = region.Id, Price = 100 });

            var ags = await dip.AgreementsAsync(kb);
            Assert.Single(ags);
            int agId = ags[0].Id;
            ActionResultDto res = await dip.RespondAgreementAsync(kb, new RespondAgreementRequest { CommandId = Id(), AgreementId = agId, Accept = true });
            Assert.True(res.Success, res.MessageUz);

            RegionEntity afterR = await db.Regions.AsNoTracking().FirstAsync(r => r.Id == region.Id);
            Assert.Equal(kb.Id, afterR.OwnerKingdomId);
            CityEntity afterC = await db.Cities.AsNoTracking().FirstAsync(c => c.Id == city.Id);
            Assert.Equal(kb.Id, afterC.KingdomId);
        }

        [Fact]
        public async Task News_PublishedOnAllianceCreate()
        {
            Kingdom a = await Player("n1");
            using GameDbContext db = NewDb();
            var dip = Dip(db);
            var svc = Svc(db);
            Kingdom ka = await svc.KingdomOfUserAsync((await db.Users.FirstAsync(u => u.Username == "n1")).Id);
            await dip.CreateAllianceAsync(ka, new CreateAllianceRequest { CommandId = Id(), Name = "N", Emblem = 1 });
            var news = await dip.NewsAsync(1);
            Assert.Contains(news, n => n.TextUz.Contains("ittifoqini tuzdi"));
        }
    }
}
