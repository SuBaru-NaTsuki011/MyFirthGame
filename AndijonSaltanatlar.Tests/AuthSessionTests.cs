using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AndijonSaltanatlar.Server.Authentication;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>PROMPT 26: Google identifikatori, sessiyalar (rotation, bekor qilish), PKCE/nonce, taxallus.</summary>
    public class AuthSessionTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly SessionTokenService _sessions = new SessionTokenService(new JwtTokenService("test-key-0123456789-0123456789-0123456789"));

        public AuthSessionTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
            using GameDbContext db = NewDb();
            db.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();
        private GameDbContext NewDb() => new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_conn).Options);

        private static GoogleIdentity G(string sub, string email, string name = "Ali Valiyev") =>
            new GoogleIdentity { Sub = sub, Email = email, Name = name, EmailVerified = true };

        [Fact]
        public void SameGoogleSub_SamePlayer_DifferentSub_DifferentPlayer()
        {
            using GameDbContext db = NewDb();
            AppUser a1 = GoogleLoginService.FindOrCreateUser(db, G("sub-A", "a@gmail.com"));   // Windows
            AppUser a2 = GoogleLoginService.FindOrCreateUser(db, G("sub-A", "a@gmail.com"));   // Android, o'sha akkaunt
            AppUser b = GoogleLoginService.FindOrCreateUser(db, G("sub-B", "b@gmail.com"));
            Assert.Equal(a1.Id, a2.Id);
            Assert.NotEqual(a1.Id, b.Id);
            Assert.Equal(1, db.Users.Count(u => u.GoogleSub == "sub-A"));
            // Yangi o'yinchi: haqiqiy ism majburan ko'rsatilmaydi, taxallus so'raladi
            Assert.Null(a1.DisplayName);
            Assert.False(a1.ProfileCompleted);
        }

        [Fact]
        public void RefreshToken_Rotates_OldTokenReuse_RevokesSession()
        {
            using GameDbContext db = NewDb();
            AppUser u = GoogleLoginService.FindOrCreateUser(db, G("sub-R", "r@gmail.com"));
            AuthResponse first = _sessions.Issue(db, u, "pc");
            Assert.False(string.IsNullOrEmpty(first.RefreshToken));
            Assert.True(first.NeedsProfile);
            // Bazada token ochiq holda saqlanmaydi
            Assert.DoesNotContain(db.PlayerSessions, s => s.RefreshTokenHash == first.RefreshToken);

            AuthResponse second = _sessions.Refresh(db, first.RefreshToken);
            Assert.NotNull(second);
            Assert.NotEqual(first.RefreshToken, second.RefreshToken);

            // Eski token qayta ishlatildi — rad etiladi va sessiya bekor qilinadi (yangi token ham ishlamaydi)
            Assert.Null(_sessions.Refresh(db, first.RefreshToken));
            Assert.Null(_sessions.Refresh(db, second.RefreshToken));
            Assert.Null(_sessions.Refresh(db, "noto'g'ri-token"));
        }

        [Fact]
        public void Logout_RevokesOnlyThisDevice()
        {
            using GameDbContext db = NewDb();
            AppUser u = GoogleLoginService.FindOrCreateUser(db, G("sub-L", "l@gmail.com"));
            AuthResponse pc = _sessions.Issue(db, u, "pc");
            AuthResponse phone = _sessions.Issue(db, u, "android");
            string pcSid = db.PlayerSessions.First(s => s.DeviceLabel == "pc").Id;
            Assert.True(_sessions.Revoke(db, pcSid, u.Id));
            Assert.True(SessionTokenService.IsRevoked(db, pcSid));
            Assert.Null(_sessions.Refresh(db, pc.RefreshToken));
            Assert.NotNull(_sessions.Refresh(db, phone.RefreshToken));   // telefon sessiyasi ishlayveradi
            // Begona o'yinchi boshqaning sessiyasini bekor qila olmaydi
            string phoneSid = db.PlayerSessions.First(s => s.DeviceLabel == "android").Id;
            Assert.False(_sessions.Revoke(db, phoneSid, u.Id + 999));
        }

        [Fact]
        public void IdToken_WrongNonceOrAudience_Rejected_PkceChallengeIsS256()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            string Tok(object p) => B64("{\"alg\":\"RS256\"}") + "." + B64(JsonSerializer.Serialize(p)) + ".sig";
            long exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
            string ok = Tok(new { iss = "https://accounts.google.com", aud = "client-1", exp, sub = "s1", nonce = "N1" });
            Assert.Equal("s1", GoogleLoginService.ParseIdToken(ok, "client-1", "N1").Sub);
            Assert.Throws<InvalidOperationException>(() => GoogleLoginService.ParseIdToken(ok, "client-1", "BOSHQA"));
            Assert.Throws<InvalidOperationException>(() => GoogleLoginService.ParseIdToken(ok, "client-2", "N1"));
            // SHA-256 + base64url (Python hashlib bilan mustaqil hisoblangan qiymat)
            Assert.Equal("ERSvixDpBjZOzRsXzGBcA4PV4MzOhD5GmJKLZ_xYY5s", GoogleLoginService.Challenge("dBjftJeZ4CVP-mB0aJ7MBVbQ8Sg3G9TSkKQ8pX1Z6Hk"));
        }

        [Fact]
        public void Nickname_Rules()
        {
            Assert.Null(ProfileRules.Validate(ProfileRules.Clean("  Bobur  Mirzo ")));
            Assert.Null(ProfileRules.Validate("O'tkir_2"));
            Assert.NotNull(ProfileRules.Validate("ab"));
            Assert.NotNull(ProfileRules.Validate("juda_uzun_taxallus_123"));
            Assert.NotNull(ProfileRules.Validate("ali@gmail"));
            Assert.Equal("Bobur Mirzo", ProfileRules.Clean("  Bobur   Mirzo "));
        }
    }
}
