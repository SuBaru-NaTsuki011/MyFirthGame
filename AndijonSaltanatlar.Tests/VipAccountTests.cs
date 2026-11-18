using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AndijonSaltanatlar.Server.Authentication;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>VIP hisoblar: Google email bo'yicha aniqlanadi, bazada saqlanadi va sessiya javobida qaytadi.</summary>
    public class VipAccountTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly GameDbContext _db;
        private readonly VipOptions _vip = new VipOptions
        {
            Accounts = new System.Collections.Generic.List<string>
            {
                "anvarovmuhammadamin011",
                "anvarovmuhammadamin2011@gmail.com"
            }
        };

        public VipAccountTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
            _db = new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_conn).Options);
            _db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _db.Dispose();
            _conn.Dispose();
        }

        private static GoogleIdentity G(string sub, string email) => new GoogleIdentity
        {
            Sub = sub, Email = email, Name = "Muhammadamin", EmailVerified = true
        };

        [Fact]
        public void Vip_MatchesLocalPartAndFullEmail_AndPersists()
        {
            AppUser a = GoogleLoginService.FindOrCreateUser(_db, G("vip-local", "anvarovmuhammadamin011@gmail.com"), _vip);
            AppUser b = GoogleLoginService.FindOrCreateUser(_db, G("vip-full", "anvarovmuhammadamin2011@gmail.com"), _vip);
            AppUser c = GoogleLoginService.FindOrCreateUser(_db, G("plain", "boshqa@gmail.com"), _vip);

            Assert.True(a.IsVip);
            Assert.True(b.IsVip);
            Assert.False(c.IsVip);

            // Boshqa qurilmada qayta kirish: VIP belgisi bazadan tiklanadi.
            AppUser again = GoogleLoginService.FindOrCreateUser(_db, G("vip-local", "anvarovmuhammadamin011@gmail.com"));
            Assert.True(again.IsVip);

            var sessions = new SessionTokenService(new JwtTokenService(new string('k', 32)));
            AuthResponse auth = sessions.Issue(_db, again, "test-device");
            Assert.True(auth.IsVip);
        }

        [Fact]
        public void Vip_RemovedFromConfig_IsClearedOnNextLogin()
        {
            AppUser a = GoogleLoginService.FindOrCreateUser(_db, G("vip-remove", "anvarovmuhammadamin011@gmail.com"), _vip);
            Assert.True(a.IsVip);

            var empty = new VipOptions();
            AppUser again = GoogleLoginService.FindOrCreateUser(_db, G("vip-remove", "anvarovmuhammadamin011@gmail.com"), empty);
            Assert.False(again.IsVip);
        }
    }
}
