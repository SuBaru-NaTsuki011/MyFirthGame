using System;
using System.Linq;
using System.Security.Claims;
using AndijonSaltanatlar.Server.Authentication;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AndijonSaltanatlar.Server.Controllers
{
    /// <summary>Sessiya: yangilash (rotation), chiqish (bekor qilish) va o'yinchi profili (taxallus, avatar).</summary>
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly SessionTokenService _sessions;

        public SessionController(GameDbContext db, SessionTokenService sessions) { _db = db; _sessions = sessions; }

        [HttpPost("api/auth/refresh")]
        [EnableRateLimiting("auth")]
        public IActionResult Refresh([FromBody] RefreshRequest req)
        {
            AuthResponse a = _sessions.Refresh(_db, req?.RefreshToken);
            if (a == null) return Unauthorized(new { messageUz = "Sessiya muddati tugadi. Qaytadan kiring." });
            return Ok(a);
        }

        /// <summary>
        /// Eski versiyadan o'tish: sessiyasiz (sid'siz) eski token bir marta yangi sessiyaga almashtiriladi —
        /// o'yinchi qayta kirmasdan shifrlangan sessiyaga o'tadi. Sessiyali token bilan chaqirilsa rad etiladi.
        /// </summary>
        [Authorize]
        [HttpPost("api/auth/upgrade")]
        [EnableRateLimiting("auth")]
        public IActionResult Upgrade()
        {
            if (!string.IsNullOrEmpty(User.FindFirstValue("sid"))) return BadRequest(new { messageUz = "Sessiya allaqachon mavjud." });
            int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            AppUser u = _db.Users.FirstOrDefault(x => x.Id == id);
            if (u == null) return Unauthorized();
            if (!u.ProfileCompleted && u.KingdomId != null) { if (string.IsNullOrWhiteSpace(u.DisplayName)) u.DisplayName = u.Username; u.ProfileCompleted = true; }
            return Ok(_sessions.Issue(_db, u, "upgrade"));
        }

        [Authorize]
        [HttpPost("api/auth/logout")]
        public IActionResult Logout()
        {
            int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            string sid = User.FindFirstValue("sid");
            if (!string.IsNullOrEmpty(sid)) _sessions.Revoke(_db, sid, id);
            return Ok(new { messageUz = "Qurilmadan chiqildi. O'yin progressi saqlangan." });
        }

        /// <summary>Yangi hukmdor: o'yinda ko'rinadigan taxallus va avatar (haqiqiy ism/email boshqalarga ko'rsatilmaydi).</summary>
        [Authorize]
        [HttpPost("api/auth/profile")]
        public IActionResult Profile([FromBody] ProfileRequest req)
        {
            int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            AppUser u = _db.Users.FirstOrDefault(x => x.Id == id);
            if (u == null) return Unauthorized();
            string nick = ProfileRules.Clean(req?.Nickname);
            string err = ProfileRules.Validate(nick);
            if (err != null) return BadRequest(new { messageUz = err });
            if (_db.Users.Any(x => x.Id != id && x.DisplayName == nick && x.ProfileCompleted))
                return Conflict(new { messageUz = "Bu taxallus band. Boshqasini tanlang." });
            u.DisplayName = nick;
            u.AvatarId = Math.Clamp(req.AvatarId, 0, ProfileRules.Avatars - 1);
            u.ProfileCompleted = true;
            // Saltanati bo'lsa hukmdor nomi ham taxallusga o'zgaradi
            foreach (var k in _db.Kingdoms.Where(k => k.OwnerUserId == id && !k.IsAi)) k.RulerName = nick;
            _db.SaveChanges();
            return Ok(new MeResponse { UserId = u.Id, Username = u.Username, DisplayName = u.DisplayName, KingdomId = u.KingdomId, NeedsProfile = false, AvatarId = u.AvatarId, IsVip = u.IsVip });
        }
    }
}
