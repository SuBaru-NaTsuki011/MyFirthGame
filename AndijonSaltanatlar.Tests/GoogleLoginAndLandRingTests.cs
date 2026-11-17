using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AndijonSaltanatlar.LandExpansion;
using AndijonSaltanatlar.SaveSystem;
using AndijonSaltanatlar.Server.Authentication;
using AndijonSaltanatlar.Server.Controllers;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>Google orqali kirish, bulutdagi saqlash va yer halqalari (4 tomon → keyingi 4 tomon).</summary>
    public class GoogleLoginAndLandRingTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public GoogleLoginAndLandRingTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
            using GameDbContext db = NewDb();
            db.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private GameDbContext NewDb() => new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_conn).Options);

        // ------------------------------------------------------------------ Yer halqalari
        [Fact]
        public void Land_EveryRingHasFourSides_AndRingsDoNotOverlap()
        {
            var land = new CityLandExpansionManager();
            land.Initialize();
            for (int r = 1; r <= land.RingCount; r++)
            {
                var ring = land.Parcels.Where(p => p.Ring == r).ToList();
                Assert.Equal(4, ring.Count);
                Assert.Equal(4, ring.Select(p => p.Side).Distinct().Count());
            }
            for (int i = 0; i < land.Parcels.Count; i++)
                for (int j = i + 1; j < land.Parcels.Count; j++)
                    Assert.False(land.Parcels[i].GridBounds.Intersects(land.Parcels[j].GridBounds),
                        land.Parcels[i].NameUz + " va " + land.Parcels[j].NameUz + " ustma-ust");
            // Tashqi halqa xarita chetigacha yetadi va xaritadan chiqmaydi.
            foreach (LandParcel p in land.Parcels)
            {
                Assert.True(p.GridBounds.Left >= 0 && p.GridBounds.Top >= 0);
                Assert.True(p.GridBounds.Right <= GameConfig.MapSize && p.GridBounds.Bottom <= GameConfig.MapSize);
            }
        }

        [Fact]
        public void Land_SecondRingOpensOnlyAfterAllFourSidesOpened()
        {
            var land = new CityLandExpansionManager();
            land.Initialize();
            land.RefreshStates(population: 100000);

            Assert.All(land.Parcels.Where(p => p.Ring == 1), p => Assert.Equal(LandParcelState.Open, p.State));
            Assert.Equal(2, land.CurrentRing);

            var ring2 = land.Parcels.Where(p => p.Ring == 2).ToList();
            Assert.All(ring2, p => Assert.Equal(LandParcelState.Unlockable, p.State));
            Assert.All(land.Parcels.Where(p => p.Ring == 3), p => { Assert.Equal(LandParcelState.Locked, p.State); Assert.True(p.WaitingForPreviousRing); });

            for (int i = 0; i < 3; i++) land.Unlock(ring2[i]);
            land.RefreshStates(100000);
            Assert.False(land.CanUnlockRing(land.Parcels.First(p => p.Ring == 3)));
            Assert.All(land.Parcels.Where(p => p.Ring == 3), p => Assert.Equal(LandParcelState.Locked, p.State));

            land.Unlock(ring2[3]);
            Assert.Equal(2, land.LastCompletedRing);
            land.RefreshStates(100000);
            Assert.Equal(3, land.CurrentRing);
            Assert.All(land.Parcels.Where(p => p.Ring == 3), p => Assert.Equal(LandParcelState.Unlockable, p.State));
            Assert.All(land.Parcels.Where(p => p.Ring == 4), p => Assert.True(p.WaitingForPreviousRing));
        }

        [Fact]
        public void Land_OuterTilesBuildableOnlyWhenOpened()
        {
            var land = new CityLandExpansionManager();
            land.Initialize();
            land.RefreshStates(0);
            int c = GameConfig.CityCenter;
            Assert.True(land.IsBuildable(c, c));                     // shahar markazi
            LandParcel north2 = land.Parcels.First(p => p.Ring == 2 && p.Side == LandSide.North);
            var t = north2.GridBounds.Center;
            Assert.False(land.IsBuildable(t.X, t.Y));
            land.Unlock(north2);
            Assert.True(land.IsBuildable(t.X, t.Y));
        }

        [Fact]
        public void Save_OldLayoutIsShiftedToNewMap()
        {
            var data = new SaveGameData { MapOffset = 0, CameraY = 100 };
            data.PlayerBuildings.Add(new SavedBuilding { X = 30, Y = 31 });
            data.Roads.Add(new SavedRoad { X = 5, Y = 6 });
            SaveManager.MigrateLayout(data, GameConfig.CityOffset, GameConfig.HalfTileH);
            Assert.Equal(30 + GameConfig.CityOffset, data.PlayerBuildings[0].X);
            Assert.Equal(6 + GameConfig.CityOffset, data.Roads[0].Y);
            Assert.Equal(100 + 2 * GameConfig.CityOffset * GameConfig.HalfTileH, data.CameraY);
            Assert.Equal(GameConfig.CityOffset, data.MapOffset);

            string json = SaveManager.ToJson(data);
            SaveGameData back = SaveManager.FromJson(json);
            Assert.Equal(data.PlayerBuildings[0].X, back.PlayerBuildings[0].X);
            Assert.Null(SaveManager.FromJson("{buzilgan"));
        }

        // ------------------------------------------------------------------ Google login
        private static string FakeIdToken(object payload)
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return B64("{\"alg\":\"RS256\"}") + "." + B64(JsonSerializer.Serialize(payload)) + ".sig";
        }

        [Fact]
        public void IdToken_ValidatesAudienceIssuerAndExpiry()
        {
            long exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
            GoogleIdentity id = GoogleLoginService.ParseIdToken(
                FakeIdToken(new { iss = "https://accounts.google.com", aud = "cid", exp, sub = "123", email = "a@gmail.com", email_verified = true, name = "Ali" }), "cid");
            Assert.Equal("123", id.Sub);
            Assert.True(id.EmailVerified);

            Assert.Throws<InvalidOperationException>(() => GoogleLoginService.ParseIdToken(
                FakeIdToken(new { iss = "https://accounts.google.com", aud = "boshqa", exp, sub = "1" }), "cid"));
            Assert.Throws<InvalidOperationException>(() => GoogleLoginService.ParseIdToken(
                FakeIdToken(new { iss = "https://evil.com", aud = "cid", exp, sub = "1" }), "cid"));
            Assert.Throws<InvalidOperationException>(() => GoogleLoginService.ParseIdToken(
                FakeIdToken(new { iss = "accounts.google.com", aud = "cid", exp = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds(), sub = "1" }), "cid"));
        }

        [Fact]
        public void FindOrCreate_SameGoogleAccountReturnsSameUser_UniqueUsernames()
        {
            using GameDbContext db = NewDb();
            var g = new GoogleIdentity { Sub = "s1", Email = "Ali@Gmail.com", Name = "Ali Valiyev", EmailVerified = true };
            AppUser a = GoogleLoginService.FindOrCreateUser(db, g);
            AppUser b = GoogleLoginService.FindOrCreateUser(db, g);
            Assert.Equal(a.Id, b.Id);
            Assert.Equal("ali@gmail.com", a.Email);
            Assert.Equal("ali_valiyev", a.Username);

            AppUser c = GoogleLoginService.FindOrCreateUser(db, new GoogleIdentity { Sub = "s2", Email = "x@gmail.com", Name = "Ali Valiyev", EmailVerified = true });
            Assert.NotEqual(a.Id, c.Id);
            Assert.Equal("ali_valiyev2", c.Username);
        }

        [Fact]
        public void Session_TokenOnlyWithCorrectPollKey_AndOnlyOnce()
        {
            var svc = new GoogleLoginService(new GoogleAuthOptions { DevLogin = true }, null);
            GoogleLoginSession s = svc.Create();
            Assert.Equal("pending", svc.Poll(s.Id, s.PollKey).Status);

            using GameDbContext db = NewDb();
            AppUser u = GoogleLoginService.FindOrCreateUser(db, new GoogleIdentity { Sub = "dev:t@t.uz", Email = "t@t.uz", Name = "T", EmailVerified = true });
            svc.Complete(s, u, new JwtTokenService(""));

            Assert.Equal("error", svc.Poll(s.Id, "noto'g'ri-kalit").Status);
            GoogleLoginPollResponse ok = svc.Poll(s.Id, s.PollKey);
            Assert.Equal("ok", ok.Status);
            Assert.False(string.IsNullOrEmpty(ok.Auth.Token));
            Assert.Equal("expired", svc.Poll(s.Id, s.PollKey).Status); // ikkinchi marta berilmaydi
        }

        [Fact]
        public async System.Threading.Tasks.Task NativeIdToken_ForgedTokenRejected_ConfigHidesIdWhenNotConfigured()
        {
            using GameDbContext db = NewDb();
            var log = Microsoft.Extensions.Logging.Abstractions.NullLogger<GoogleAuthController>.Instance;
            var configured = new GoogleLoginService(new GoogleAuthOptions { ClientId = "web.apps.googleusercontent.com", ClientSecret = "x" }, null);
            var c = new GoogleAuthController(configured, db, new JwtTokenService(""), log);

            var cfg = Assert.IsType<OkObjectResult>(c.Config());
            Assert.Equal("web.apps.googleusercontent.com", ((GoogleClientConfig)cfg.Value).WebClientId);

            // O'zimiz yasagan (Google imzolamagan) token — rad etiladi, foydalanuvchi yaratilmaydi.
            long exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
            string forged = FakeIdToken(new { iss = "https://accounts.google.com", aud = "web.apps.googleusercontent.com", exp, sub = "hacker", email = "h@gmail.com", email_verified = true });
            Assert.IsType<UnauthorizedObjectResult>(await c.IdToken(new GoogleIdTokenRequest { IdToken = forged }));
            Assert.False(db.Users.Any(u => u.GoogleSub == "hacker"));

            var dev = new GoogleAuthController(new GoogleLoginService(new GoogleAuthOptions { DevLogin = true }, null), db, new JwtTokenService(""), log);
            var devCfg = (GoogleClientConfig)Assert.IsType<OkObjectResult>(dev.Config()).Value;
            Assert.False(devCfg.Configured);
            Assert.Equal("", devCfg.WebClientId);
            Assert.Equal(503, Assert.IsType<ObjectResult>(await dev.IdToken(new GoogleIdTokenRequest { IdToken = forged })).StatusCode);
        }

        [Fact]
        public void DevLogin_InviteCode_RequiredWhenSet()
        {
            using GameDbContext db = NewDb();
            var log = Microsoft.Extensions.Logging.Abstractions.NullLogger<GoogleAuthController>.Instance;
            var svc = new GoogleLoginService(new GoogleAuthOptions { DevLogin = true, DevLoginCode = "K7PX2M" }, null);
            var c = new GoogleAuthController(svc, db, new JwtTokenService(""), log);
            GoogleLoginSession s = svc.Create();

            // Kod noto'g'ri yoki yo'q — 403, foydalanuvchi yaratilmaydi.
            Assert.Equal(403, Assert.IsType<ContentResult>(c.DevCallback(s.Id, "begona@test.uz", "Begona", "AAAAAA")).StatusCode);
            Assert.Equal(403, Assert.IsType<ContentResult>(c.DevCallback(s.Id, "begona@test.uz", "Begona", null)).StatusCode);
            Assert.False(db.Users.Any(u => u.GoogleSub == "dev:begona@test.uz"));

            // To'g'ri kod (katta-kichik harf farqi yo'q, bo'shliqlar kesiladi) — kirish yakunlanadi.
            Assert.Equal(200, Assert.IsType<ContentResult>(c.DevCallback(s.Id, "dost@test.uz", "Do'st", " k7px2m ")).StatusCode ?? 200);
            Assert.True(db.Users.Any(u => u.GoogleSub == "dev:dost@test.uz"));
        }

        [Fact]
        public void RealGoogle_TakesOverDevLoginAccountWithSameEmail()
        {
            using GameDbContext db = NewDb();
            AppUser dev = GoogleLoginService.FindOrCreateUser(db, new GoogleIdentity { Sub = "dev:ali@gmail.com", Email = "ali@gmail.com", Name = "ali", EmailVerified = true });
            AppUser real = GoogleLoginService.FindOrCreateUser(db, new GoogleIdentity { Sub = "1234567890", Email = "Ali@Gmail.com", Name = "Ali Valiyev", EmailVerified = true });
            Assert.Equal(dev.Id, real.Id);
            Assert.Equal("1234567890", real.GoogleSub);
            Assert.Equal(1, db.Users.Count(u => u.Email == "ali@gmail.com"));

            // Tasdiqlanmagan email bilan — boshqa akkauntni egallab bo'lmaydi.
            GoogleLoginService.FindOrCreateUser(db, new GoogleIdentity { Sub = "dev:vali@gmail.com", Email = "vali@gmail.com", EmailVerified = true });
            AppUser other = GoogleLoginService.FindOrCreateUser(db, new GoogleIdentity { Sub = "999", Email = "vali@gmail.com", EmailVerified = false });
            Assert.NotEqual("dev:vali@gmail.com", other.GoogleSub);
            Assert.Equal("dev:vali@gmail.com", db.Users.Single(u => u.Email == "vali@gmail.com").GoogleSub);
        }

        [Theory]
        [InlineData("abc-def.trycloudflare.com", "https://abc-def.trycloudflare.com")]
        [InlineData("192.168.1.5:5081", "http://192.168.1.5:5081")]
        [InlineData("localhost:5081/", "http://localhost:5081")]
        [InlineData("http://10.0.0.2:5081", "http://10.0.0.2:5081")]
        [InlineData(" https://o-yin.example.uz/ ", "https://o-yin.example.uz")]
        public void ServerUrl_InternetHostDefaultsToHttps(string input, string expected)
            => Assert.Equal(expected, AndijonSaltanatlar.Online.CloudSession.NormalizeUrl(input));

        // ------------------------------------------------------------------ Bulutdagi saqlash
        private SaveController Controller(GameDbContext db, int userId)
        {
            var c = new SaveController(db);
            c.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test"))
                }
            };
            return c;
        }

        [Fact]
        public void CloudSave_RoundTrip_AndStaleDeviceGetsConflict()
        {
            using GameDbContext db = NewDb();
            AppUser u = GoogleLoginService.FindOrCreateUser(db, new GoogleIdentity { Sub = "s9", Email = "s9@gmail.com", Name = "S", EmailVerified = true });
            SaveController c = Controller(db, u.Id);

            Assert.IsType<NotFoundObjectResult>(c.Get());
            var first = Assert.IsType<OkObjectResult>(c.Put(new CloudSaveUploadRequest { Json = "{\"Coins\":1}", BaseVersion = 0 }));
            Assert.Equal(1, ((CloudSaveDto)first.Value).Version);

            // Telefon (versiya 1) saqladi → 2.
            Assert.IsType<OkObjectResult>(c.Put(new CloudSaveUploadRequest { Json = "{\"Coins\":2}", BaseVersion = 1 }));
            // Eski holatdagi kompyuter (versiya 1) yangi progress ustidan yoza olmaydi.
            Assert.IsType<ConflictObjectResult>(c.Put(new CloudSaveUploadRequest { Json = "{\"Coins\":0}", BaseVersion = 1 }));

            var got = Assert.IsType<OkObjectResult>(c.Get());
            Assert.Equal("{\"Coins\":2}", ((CloudSaveDto)got.Value).Json);
            Assert.Equal(2, ((CloudSaveDto)got.Value).Version);
        }
    }
}
