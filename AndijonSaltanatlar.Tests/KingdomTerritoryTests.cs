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
    /// <summary>PROMPT 19B — hudud transferi, aholi integratsiyasi, ko'p shahar, hokimlar, ekinlar (haqiqiy EF Core + SQLite).</summary>
    public class KingdomTerritoryTests : IDisposable
    {
        private sealed class FakeClock : IServerClock
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);
        }

        private readonly SqliteConnection _conn;
        private readonly FakeClock _clock = new FakeClock();

        public KingdomTerritoryTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
            using GameDbContext db = NewDb();
            db.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private GameDbContext NewDb() => new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_conn).Options);
        private static string Id() => Guid.NewGuid().ToString("N");

        private async Task<Kingdom> Player(string name, int coin)
        {
            using GameDbContext db = NewDb();
            var u = new AppUser { Username = name, Email = name + "@t", PasswordHash = "x" };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            Kingdom k = await new OnlineGameService(db, _clock).JoinAsync(u.Id, new JoinWorldRequest { WorldId = 1 });
            await db.KingdomResources.Where(r => r.KingdomId == k.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.Coin, coin));
            return k;
        }

        private KingdomService Kingdoms(GameDbContext db) => new KingdomService(new OnlineGameService(db, _clock));

        private async Task<RegionEntity> Region(string name)
        {
            using GameDbContext db = NewDb();
            return await db.Regions.AsNoTracking().SingleAsync(r => r.Name == name);
        }

        [Fact]
        public async Task Join_CreatesHomeRegionAndCapital_WorldSeeded()
        {
            Kingdom k = await Player("asaka", 500);
            using GameDbContext db = NewDb();
            Assert.Equal(2, await db.Kingdoms.CountAsync(x => x.IsAi));
            CityEntity cap = await db.Cities.SingleAsync(c => c.KingdomId == k.Id);
            Assert.True(cap.IsCapital);
            Assert.Equal("Asaka", cap.Name);
        }

        [Fact]
        public async Task Annex_RequiresRelation_ThenTransfersCitiesBuildingsAndPopulationOnce()
        {
            Kingdom me = await Player("asaka", 5000);
            RegionEntity shahrixon = await Region("Shahrixon");
            int oldOwner = shahrixon.OwnerKingdomId;
            int cityPop;
            using (GameDbContext db = NewDb())
            {
                CityEntity c = await db.Cities.SingleAsync(x => x.RegionId == shahrixon.Id);
                cityPop = c.Population;
                db.Buildings.Add(new BuildingEntity { KingdomId = oldOwner, CityId = c.Id, Kind = 0, X = 1, Y = 1 });
                await db.SaveChangesAsync();
            }

            using (GameDbContext db = NewDb())
            {
                var (r, _, _) = await Kingdoms(db).AnnexAsync(me, Id(), shahrixon.Id);
                Assert.False(r.Success); // munosabat yetarli emas
            }
            using (GameDbContext db = NewDb()) Assert.True((await Kingdoms(db).SendEnvoyAsync(me, Id(), shahrixon.Id)).Success);
            using (GameDbContext db = NewDb()) Assert.True((await Kingdoms(db).SendEnvoyAsync(me, Id(), shahrixon.Id)).Success);

            int popBefore;
            using (GameDbContext db = NewDb()) popBefore = (await Kingdoms(db).OverviewAsync(me)).Population;
            using (GameDbContext db = NewDb())
            {
                var (r, region, prev) = await Kingdoms(db).AnnexAsync(me, Id(), shahrixon.Id);
                Assert.True(r.Success, r.MessageUz);
                Assert.Equal(oldOwner, prev);
            }

            using GameDbContext check = NewDb();
            Assert.Equal(me.Id, (await check.Regions.SingleAsync(x => x.Id == shahrixon.Id)).OwnerKingdomId);
            CityEntity moved = await check.Cities.SingleAsync(x => x.RegionId == shahrixon.Id);
            Assert.Equal(me.Id, moved.KingdomId);
            Assert.Equal(cityPop, moved.Population);                                  // aholi saqlandi
            Assert.Equal(me.Id, (await check.Buildings.SingleAsync(b => b.CityId == moved.Id)).KingdomId); // binolar saqlandi
            KingdomOverviewDto ov = await Kingdoms(check).OverviewAsync(me);
            Assert.Equal(popBefore + cityPop, ov.Population);                       // bir marta qo'shildi
            Assert.Equal(2, ov.CityCount);
            int totalAll = await check.Cities.SumAsync(c => c.Population);
            int totalByOwner = (await check.Cities.Where(c => c.KingdomId != 0).GroupBy(c => c.KingdomId).Select(g => g.Sum(c => c.Population)).ToListAsync()).Sum()
                               + await check.Cities.Where(c => c.KingdomId == 0).SumAsync(c => c.Population);
            Assert.Equal(totalAll, totalByOwner);                                   // hech kim ikki marta sanalmaydi
        }

        [Fact]
        public async Task ConcurrentAnnex_OnlyOneKingdomWins()
        {
            Kingdom a = await Player("a", 5000);
            Kingdom b = await Player("b", 5000);
            RegionEntity neutral = await Region("Paxtaobod"); // mustaqil — munosabat shart emas
            using GameDbContext d1 = NewDb();
            using GameDbContext d2 = NewDb();
            var t1 = Kingdoms(d1).AnnexAsync(a, Id(), neutral.Id);
            var t2 = Kingdoms(d2).AnnexAsync(b, Id(), neutral.Id);
            var results = await Task.WhenAll(t1, t2);
            Assert.Equal(1, results.Count(r => r.result.Success));
            using GameDbContext check = NewDb();
            int owner = (await check.Regions.SingleAsync(r => r.Id == neutral.Id)).OwnerKingdomId;
            Assert.True(owner == a.Id || owner == b.Id);
            Assert.Equal(owner, (await check.Cities.SingleAsync(c => c.RegionId == neutral.Id)).KingdomId);
        }

        [Fact]
        public async Task Annex_LastAiRegionIsProtected_PlayerRegionBlocked()
        {
            Kingdom me = await Player("me", 50000);
            Kingdom other = await Player("other", 500);
            using (GameDbContext db = NewDb())
            {
                RegionEntity home = await db.Regions.SingleAsync(r => r.OwnerKingdomId == other.Id);
                var (r, _, _) = await Kingdoms(db).AnnexAsync(me, Id(), home.Id);
                Assert.False(r.Success);
            }
            RegionEntity quva = await Region("Quva");
            RegionEntity margilon = await Region("Marg'ilon");
            for (int i = 0; i < 2; i++) using (GameDbContext db = NewDb()) await Kingdoms(db).SendEnvoyAsync(me, Id(), quva.Id);
            using (GameDbContext db = NewDb()) Assert.True((await Kingdoms(db).AnnexAsync(me, Id(), quva.Id)).result.Success);
            using (GameDbContext db = NewDb()) Assert.False((await Kingdoms(db).AnnexAsync(me, Id(), margilon.Id)).result.Success); // so'nggi hudud
        }

        [Fact]
        public async Task Governor_PlansOnlyWithPermission_AndBuildsAfterApproval()
        {
            Kingdom me = await Player("gov", 5000);
            int cityId;
            using (GameDbContext db = NewDb())
            {
                CityEntity c = await db.Cities.SingleAsync(x => x.KingdomId == me.Id);
                c.Population = 58; c.HousingCapacity = 60; c.Jobs = 10;
                await db.SaveChangesAsync();
                cityId = c.Id;
            }
            using (GameDbContext db = NewDb()) Assert.False((await Kingdoms(db).GovernorAnalyzeAsync(me, new GovernorRequest { CommandId = Id(), CityId = cityId })).Success); // Manual
            using (GameDbContext db = NewDb()) await Kingdoms(db).SetGovernorModeAsync(me, new GovernorRequest { CommandId = Id(), CityId = cityId, Mode = GovernorMode.Advisory });
            using (GameDbContext db = NewDb()) Assert.True((await Kingdoms(db).GovernorAnalyzeAsync(me, new GovernorRequest { CommandId = Id(), CityId = cityId })).Success);

            GovernorPlanEntity plan;
            using (GameDbContext db = NewDb()) plan = await db.GovernorPlans.SingleAsync(p => p.CityId == cityId && p.Status == 0);
            Assert.True(plan.Houses > 0 && plan.Markets > 0);
            // Maslahat rejimida bajarilmaydi, tanga yechilmaydi.
            using (GameDbContext db = NewDb()) Assert.False((await Kingdoms(db).ApprovePlanAsync(me, new GovernorRequest { CommandId = Id(), PlanId = plan.Id })).Success);
            using (GameDbContext db = NewDb()) await Kingdoms(db).SetGovernorModeAsync(me, new GovernorRequest { CommandId = Id(), CityId = cityId, Mode = GovernorMode.ApprovedAutomation });
            ActionResultDto ok;
            using (GameDbContext db = NewDb()) ok = await Kingdoms(db).ApprovePlanAsync(me, new GovernorRequest { CommandId = Id(), PlanId = plan.Id });
            Assert.True(ok.Success, ok.MessageUz);
            Assert.Equal(5000 - plan.Cost, ok.State.Coin);
            using (GameDbContext db = NewDb())
            {
                Assert.Equal(plan.Houses + plan.Markets + plan.Gardens, await db.Buildings.CountAsync(b => b.CityId == cityId));
                Assert.False((await Kingdoms(db).ApprovePlanAsync(me, new GovernorRequest { CommandId = Id(), PlanId = plan.Id })).Success); // ikki marta emas
            }
            var city = new CityEntity { Population = 58, HousingCapacity = 60, Jobs = 10 };
            KingdomService.ApplyBuildingEffect(city, AndijonSaltanatlar.Shared.BuildingKind.House);
            Assert.Equal(65, city.HousingCapacity);
        }

        [Fact]
        public async Task Wheat_GrowsWithItsOwnTimeline_AndGoesToInventory()
        {
            Kingdom me = await Player("farmer", 1000);
            OnlineStateDto s;
            using (GameDbContext db = NewDb()) s = (await new OnlineGameService(db, _clock).BuildFieldAsync(me, Id())).State;
            int field = s.Fields[0].Id;
            using (GameDbContext db = NewDb()) Assert.True((await new OnlineGameService(db, _clock).PlantCropAsync(me, Id(), field, "Wheat")).Success);
            _clock.UtcNow = _clock.UtcNow.AddSeconds(CropCatalog.Get(CropType.Wheat).MatureAtMinutes / RiceFarmingRules.GameMinutesPerRealSecond + 1);
            using GameDbContext d = NewDb();
            ActionResultDto r = await new OnlineGameService(d, _clock).HarvestAsync(me, Id(), field);
            Assert.True(r.Success, r.MessageUz);
            Assert.Equal(CropCatalog.Get(CropType.Wheat).Yield, r.State.Inventory["Wheat"]);
            Assert.Equal(0, r.State.Rice);
        }
    }
}
