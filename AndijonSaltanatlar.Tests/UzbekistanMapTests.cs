using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Server.Services;
using AndijonSaltanatlar.Shared.DTOs;
using AndijonSaltanatlar.Shared.Geo;
using AndijonSaltanatlar.Shared.Heraldry;
using AndijonSaltanatlar.Shared.War;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>O'zbekiston xaritasi (viloyat/hududlar), boshlang'ich yurtni tanlash, bayroqlar va eski dunyoni ko'chirish.</summary>
    public class UzbekistanMapTests : IDisposable
    {
        private sealed class FakeClock : IServerClock
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 17, 10, 0, 0, DateTimeKind.Utc);
        }

        private readonly SqliteConnection _conn;
        private readonly FakeClock _clock = new FakeClock();

        public UzbekistanMapTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
            using GameDbContext db = NewDb();
            db.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private GameDbContext NewDb() => new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_conn).Options);

        private async Task<Kingdom> Join(string name, bool choose)
        {
            using GameDbContext db = NewDb();
            var u = new AppUser { Username = name, Email = name + "@t", PasswordHash = "x" };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            return await new OnlineGameService(db, _clock).JoinAsync(u.Id, new JoinWorldRequest { WorldId = 1, ChooseStart = choose });
        }

        // ------------------------------------------------------------------ Xarita ma'lumotlari
        [Fact]
        public void Catalog_EveryRegionInsideUzbekistan_UniqueAndNotOnTopOfEachOther()
        {
            Assert.True(UzMap.Regions.Length >= 80);
            Assert.Equal(UzMap.Regions.Length, UzMap.Regions.Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach (MapRegionSeed r in UzMap.Regions)
            {
                Assert.True(UzMap.InsideCountry(UzMap.X(r.Lon), UzMap.Y(r.Lat)), r.Name + " chegaradan tashqarida");
                Assert.NotNull(UzMap.Province(r.Province));
            }
            for (int i = 0; i < UzMap.Regions.Length; i++)
                for (int j = i + 1; j < UzMap.Regions.Length; j++)
                {
                    MapRegionSeed a = UzMap.Regions[i], b = UzMap.Regions[j];
                    double km = UzMap.DistanceKm(UzMap.X(a.Lon), UzMap.Y(a.Lat), UzMap.X(b.Lon), UzMap.Y(b.Lat));
                    Assert.True(km > 7, a.Name + " va " + b.Name + " juda yaqin: " + km.ToString("0.0") + " km");
                }
            // Har bir viloyatda kamida 4 hudud, har bir AI saltanatda poytaxt bor
            foreach (MapProvince p in UzMap.Provinces) Assert.True(UzMap.Regions.Count(r => r.Province == p.Key) >= 4, p.Name);
            foreach (MapKingdomSeed k in UzMap.Kingdoms)
            {
                Assert.Contains(UzMap.Regions, r => r.Owner == k.Key);
                Assert.True(FlagSpec.TryParse(k.Flag, out _), k.Name);
            }
        }

        [Fact]
        public void MarchTime_DependsOnRealDistance()
        {
            MapRegionSeed andijon = UzMap.Find("Andijon"), asaka = UzMap.Find("Asaka"), toshkent = UzMap.Find("Toshkent"), nukus = UzMap.Find("Nukus");
            int Near(MapRegionSeed a, MapRegionSeed b) => WarRules.MarchSeconds(UzMap.X(a.Lon), UzMap.Y(a.Lat), UzMap.X(b.Lon), UzMap.Y(b.Lat));
            double km = UzMap.DistanceKm(UzMap.X(andijon.Lon), UzMap.Y(andijon.Lat), UzMap.X(toshkent.Lon), UzMap.Y(toshkent.Lat));
            Assert.InRange(km, 230, 300);                       // haqiqiy masofa ~260 km
            Assert.InRange(Near(andijon, asaka), 20, 25);       // qo'shni tuman
            Assert.InRange(Near(andijon, toshkent), 50, 75);
            Assert.InRange(Near(andijon, nukus), 160, WarRules.MaxMarchSeconds);
        }

        // ------------------------------------------------------------------ Yangi dunyo va yurt tanlash
        [Fact]
        public async Task NewWorld_HasAllRegionsProvincesAndKingdoms()
        {
            await Join("birinchi", false);
            using GameDbContext db = NewDb();
            Assert.Equal(UzMap.Kingdoms.Length, await db.Kingdoms.CountAsync(k => k.IsAi));
            var regions = await db.Regions.ToListAsync();
            Assert.Equal(UzMap.Regions.Length, regions.Count);
            Assert.All(regions, r => Assert.False(string.IsNullOrEmpty(r.Province)));
            Assert.Equal(regions.Count, await db.Cities.CountAsync());
            Assert.Equal(UzMap.Version, (await db.Worlds.SingleAsync()).MapVersion);
            // Har bir AI saltanatning bitta poytaxti
            foreach (Kingdom k in await db.Kingdoms.Where(k => k.IsAi).ToListAsync())
                Assert.Equal(1, await db.Cities.CountAsync(c => c.KingdomId == k.Id && c.IsCapital));
        }

        [Fact]
        public async Task ChooseStart_OnlyFreeRegion_OnlyOnce_WithNameAndFlag()
        {
            Kingdom k = await Join("tanlovchi", true);
            using (GameDbContext db = NewDb())
            {
                var svc = new OnlineGameService(db, _clock);
                Assert.True((await svc.StateAsync(k)).NeedsHome);
                StartRegionsDto starts = await svc.StartRegionsAsync(k);
                Assert.Equal(UzMap.Regions.Length, starts.Regions.Count);
                StartRegionDto taken = starts.Regions.First(r => r.Name == "Toshkent");   // Qo'qon xonligiga tegishli
                Assert.False(taken.Free);
                Assert.NotNull(await svc.ChooseStartAsync(k, new ChooseStartRequest { RegionId = taken.RegionId }));
                Assert.NotNull(await svc.ChooseStartAsync(k, new ChooseStartRequest { RegionId = starts.Regions.First(r => r.Free).RegionId, Flag = "99-1-1-1-1-1" }));

                StartRegionDto samarqandYoni = starts.Regions.First(r => r.Name == "Urgut");
                Assert.True(samarqandYoni.Free);
                Assert.Null(await svc.ChooseStartAsync(k, new ChooseStartRequest { RegionId = samarqandYoni.RegionId, KingdomName = "Urgut sultonligi", Flag = "10-3-6-2-4-6" }));
            }
            using (GameDbContext db = NewDb())
            {
                Kingdom fresh = await db.Kingdoms.SingleAsync(x => x.Id == k.Id);
                Assert.Equal("Urgut sultonligi", fresh.Name);
                Assert.Equal("10-3-6-2-4-6", fresh.Flag);
                CityEntity cap = await db.Cities.SingleAsync(c => c.KingdomId == k.Id);
                Assert.True(cap.IsCapital);
                Assert.Equal("Urgut", cap.Name);
                Assert.Equal(k.Id, (await db.Regions.SingleAsync(r => r.Name == "Urgut")).OwnerKingdomId);
                var svc = new OnlineGameService(db, _clock);
                Assert.False((await svc.StateAsync(fresh)).NeedsHome);
                // Ikkinchi marta tanlab bo'lmaydi
                int other = (await svc.StartRegionsAsync(fresh)).Regions.First(r => r.Free).RegionId;
                Assert.NotNull(await svc.ChooseStartAsync(fresh, new ChooseStartRequest { RegionId = other }));
            }
            // Boshqa o'yinchi band hududni ololmaydi
            Kingdom second = await Join("ikkinchi", true);
            using (GameDbContext db = NewDb())
            {
                var svc = new OnlineGameService(db, _clock);
                int urgut = (await db.Regions.SingleAsync(r => r.Name == "Urgut")).Id;
                Assert.NotNull(await svc.ChooseStartAsync(second, new ChooseStartRequest { RegionId = urgut }));
            }
        }

        [Fact]
        public async Task OldClient_GetsAutomaticHome_AndRejoinDoesNotMoveIt()
        {
            Kingdom k = await Join("eski", false);
            using GameDbContext db = NewDb();
            CityEntity cap = await db.Cities.SingleAsync(c => c.KingdomId == k.Id);
            Assert.Equal("Asaka", cap.Name);
            Assert.True(cap.IsCapital);
        }

        [Fact]
        public async Task Flag_ValidatedAndSaved()
        {
            Kingdom k = await Join("bayroqdor", false);
            using GameDbContext db = NewDb();
            var svc = new OnlineGameService(db, _clock);
            Assert.NotNull(await svc.SetFlagAsync(k, new SetFlagRequest { Flag = "salom" }));
            Assert.NotNull(await svc.SetFlagAsync(k, new SetFlagRequest { Flag = "1-2-3-4-5-99" }));
            Assert.Null(await svc.SetFlagAsync(k, new SetFlagRequest { Flag = "6-5-0-0-4-6" }));
            Assert.Equal("6-5-0-0-4-6", (await db.Kingdoms.AsNoTracking().SingleAsync(x => x.Id == k.Id)).Flag);
            Assert.Equal("6-5-0-0-4-6", (await svc.StateAsync(k)).Flag);

            Assert.True(FlagSpec.TryParse("6-5-0-0-4-6", out FlagSpec f));
            Assert.Equal("6-5-0-0-4-6", f.Encode());
            Assert.Equal(f.EmblemColor, new FlagSpec { Pattern = 0, C1 = 1, Emblem = 9, EmblemColor = 6 }.ColorIndexAt(0.5, 0.5)); // doira markazi
            Assert.Equal(1, new FlagSpec { Pattern = 0, C1 = 1, Emblem = 9, EmblemColor = 6 }.ColorIndexAt(0.03, 0.03));          // burchak — fon
        }

        // ------------------------------------------------------------------ Eski dunyoni ko'chirish
        [Fact]
        public async Task LegacyFerganaWorld_MigratedWithoutLosingOwnership()
        {
            int playerId, playerCityId, legacyId;
            using (GameDbContext db = NewDb())
            {
                db.Worlds.Add(new AndijonSaltanatlar.Server.Data.World { Id = 1, Name = "Eski dunyo", MapVersion = 0 });
                var ai = new Kingdom { WorldId = 1, Name = "Andijon xonligi", RulerName = "Umarbek", IsAi = true };
                var player = new Kingdom { WorldId = 1, Name = "Asaka saltanati", RulerName = "p", IsAi = false };
                db.Kingdoms.AddRange(ai, player);
                await db.SaveChangesAsync();
                playerId = player.Id;
                var asaka = new RegionEntity { WorldId = 1, Name = "Asaka", OwnerKingdomId = player.Id, Latitude = 55, Longitude = 52 };
                var andijon = new RegionEntity { WorldId = 1, Name = "Andijon", OwnerKingdomId = ai.Id, Latitude = 60, Longitude = 40 };
                var legacy = new RegionEntity { WorldId = 1, Name = "Yangi yurt 77", OwnerKingdomId = player.Id, Latitude = 30, Longitude = 70 };
                db.Regions.AddRange(asaka, andijon, legacy);
                await db.SaveChangesAsync();
                legacyId = legacy.Id;
                var c = new CityEntity { KingdomId = player.Id, RegionId = asaka.Id, Name = "Asaka", Population = 77, IsCapital = true };
                db.Cities.AddRange(c, new CityEntity { KingdomId = ai.Id, RegionId = andijon.Id, Name = "Andijon", Population = 500, IsCapital = true },
                    new CityEntity { KingdomId = player.Id, RegionId = legacy.Id, Name = "Yangi yurt 77", Population = 40 });
                await db.SaveChangesAsync();
                playerCityId = c.Id;
            }
            using (GameDbContext db = NewDb()) await new OnlineGameService(db, _clock).EnsureWorldAsync(1);
            using (GameDbContext db = NewDb()) await new OnlineGameService(db, _clock).EnsureWorldAsync(1); // ikkinchi marta — o'zgarishsiz

            using (GameDbContext db = NewDb())
            {
                var regions = await db.Regions.ToListAsync();
                Assert.Equal(UzMap.Regions.Length + 1, regions.Count);                     // katalog + "Yangi yurt 77"
                Assert.Equal(1, regions.Count(r => r.Name == "Asaka"));
                RegionEntity asaka = regions.Single(r => r.Name == "Asaka");
                Assert.Equal(playerId, asaka.OwnerKingdomId);                               // egalik saqlandi
                Assert.Equal("an", asaka.Province);
                Assert.Equal(UzMap.X(UzMap.Find("Asaka").Lon), (float)asaka.Latitude, 3);
                CityEntity city = await db.Cities.SingleAsync(c => c.Id == playerCityId);
                Assert.Equal(77, city.Population);                                          // aholi saqlandi
                RegionEntity legacy = regions.Single(r => r.Id == legacyId);
                Assert.True(UzMap.InsideCountry((float)legacy.Latitude, (float)legacy.Longitude));
                Assert.False(string.IsNullOrEmpty(legacy.Province));
                Assert.Equal(1, await db.Kingdoms.CountAsync(k => k.Name == "Andijon xonligi")); // AI qayta yaratilmadi
                Assert.Equal(UzMap.Kingdoms.Length, await db.Kingdoms.CountAsync(k => k.IsAi));
                Assert.Equal(1, await db.Cities.CountAsync(c => c.KingdomId == playerId && c.IsCapital));
            }
        }
    }
}
