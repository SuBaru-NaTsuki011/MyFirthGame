using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Server.Authentication
{
    /// <summary>
    /// O'yin sessiyalari: qisqa muddatli kirish tokeni (JWT, 1 soat, "sid" bilan) + server boshqaradigan uzoq muddatli
    /// yangilash tokeni (60 kun). Yangilash tokeni bazada faqat SHA-256 xeshi bilan saqlanadi va har ishlatilganda
    /// almashtiriladi (rotation). Eski token qayta ishlatilsa — o'g'irlangan deb hisoblanib, sessiya bekor qilinadi.
    /// Chiqishda sessiya bekor qilinadi; bekor qilingan sessiyaning JWT'si ham darhol rad etiladi.
    /// </summary>
    public sealed class SessionTokenService
    {
        public static readonly TimeSpan AccessLifetime = TimeSpan.FromHours(1);
        public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(60);

        private readonly JwtTokenService _jwt;
        // Bekor qilingan sessiyalar keshi (har so'rovda bazaga murojaat qilmaslik uchun)
        private static readonly ConcurrentDictionary<string, bool> Revoked = new ConcurrentDictionary<string, bool>();

        public SessionTokenService(JwtTokenService jwt) { _jwt = jwt; }

        public static string Hash(string token)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(token ?? "")));
        }

        private static string NewSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        /// <summary>Yangi sessiya (kirishda) — kirish va yangilash tokenlari.</summary>
        public AuthResponse Issue(GameDbContext db, AppUser user, string device)
        {
            string refresh = NewSecret();
            var s = new PlayerSessionEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = user.Id,
                RefreshTokenHash = Hash(refresh),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow + RefreshLifetime,
                DeviceLabel = Clip(device, 60)
            };
            db.PlayerSessions.Add(s);
            db.SaveChanges();
            return Build(user, s.Id, refresh);
        }

        /// <summary>Yangilash: eski token almashtiriladi. null — token yaroqsiz/muddati o'tgan/bekor qilingan.</summary>
        public AuthResponse Refresh(GameDbContext db, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return null;
            string h = Hash(refreshToken);
            PlayerSessionEntity s = db.PlayerSessions.FirstOrDefault(x => x.RefreshTokenHash == h);
            if (s == null)
            {
                // Avval almashtirilgan (eski) token qayta ishlatildi — sessiyani bekor qilamiz
                PlayerSessionEntity stolen = db.PlayerSessions.FirstOrDefault(x => x.PreviousTokenHash == h);
                if (stolen != null && stolen.RevokedAt == null) { stolen.RevokedAt = DateTime.UtcNow; db.SaveChanges(); Revoked[stolen.Id] = true; }
                return null;
            }
            if (s.RevokedAt != null || s.ExpiresAt < DateTime.UtcNow) return null;
            AppUser user = db.Users.FirstOrDefault(u => u.Id == s.UserId);
            if (user == null) return null;
            string next = NewSecret();
            s.PreviousTokenHash = s.RefreshTokenHash;
            s.RefreshTokenHash = Hash(next);
            s.ExpiresAt = DateTime.UtcNow + RefreshLifetime;
            s.LastUsedAt = DateTime.UtcNow;
            user.LastLoginAt = DateTime.UtcNow;
            db.SaveChanges();
            return Build(user, s.Id, next);
        }

        /// <summary>Shu qurilmadan chiqish — sessiya bekor qilinadi (o'yin progressi o'chirilmaydi).</summary>
        public bool Revoke(GameDbContext db, string sessionId, int userId)
        {
            PlayerSessionEntity s = db.PlayerSessions.FirstOrDefault(x => x.Id == sessionId && x.UserId == userId);
            if (s == null) return false;
            if (s.RevokedAt == null) { s.RevokedAt = DateTime.UtcNow; db.SaveChanges(); }
            Revoked[s.Id] = true;
            return true;
        }

        /// <summary>JWT tekshiruvida: sessiya bekor qilinganmi.</summary>
        public static bool IsRevoked(GameDbContext db, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return false;   // eski (sid'siz) tokenlar
            if (Revoked.TryGetValue(sessionId, out bool r)) return r;
            PlayerSessionEntity s = db.PlayerSessions.FirstOrDefault(x => x.Id == sessionId);
            bool rev = s == null || s.RevokedAt != null;
            Revoked[sessionId] = rev;
            return rev;
        }

        private AuthResponse Build(AppUser user, string sessionId, string refresh) => new AuthResponse
        {
            Token = _jwt.Create(user.Id, user.Username, sessionId, AccessLifetime),
            Username = user.Username,
            KingdomId = user.KingdomId,
            RefreshToken = refresh,
            ExpiresInSeconds = (int)AccessLifetime.TotalSeconds,
            DisplayName = user.DisplayName ?? user.Username,
            NeedsProfile = !user.ProfileCompleted,
            AvatarId = user.AvatarId,
            IsVip = user.IsVip
        };

        private static string Clip(string s, int n) => string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s.Substring(0, n);
    }
}
