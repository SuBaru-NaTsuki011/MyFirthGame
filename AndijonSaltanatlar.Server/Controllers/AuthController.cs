using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AndijonSaltanatlar.Server.Authentication;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Server.Controllers
{
    /// <summary>
    /// ESKI login/parol kirishi. O'yinda kirish endi FAQAT Google orqali (GoogleAuthController).
    /// Bu endpointlar standart holatda o'chiq; faqat avtomatik sinov skriptlari uchun
    /// Auth:AllowPasswordLogin=true bilan yoqiladi (production'da yoqilmaydi).
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly IPasswordHasher<AppUser> _hasher;
        private readonly JwtTokenService _jwt;
        private readonly bool _passwordAllowed;

        public AuthController(GameDbContext db, IPasswordHasher<AppUser> hasher, JwtTokenService jwt, IConfiguration cfg)
        {
            _db = db; _hasher = hasher; _jwt = jwt;
            _passwordAllowed = cfg.GetValue<bool>("Auth:AllowPasswordLogin");
        }

        private IActionResult GoogleOnly()
            => StatusCode(403, new { messageUz = "Kirish faqat Google hisobi orqali." });

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            if (!_passwordAllowed) return GoogleOnly();
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Username))
                return BadRequest(new { messageUz = "Barcha maydonlar to'ldirilishi shart." });
            if (_db.Users.Any(u => u.Email == req.Email))
                return Conflict(new { messageUz = "Bu email allaqachon ro'yxatdan o'tgan." });

            var user = new AppUser { Username = req.Username, Email = req.Email };
            user.PasswordHash = _hasher.HashPassword(user, req.Password);
            _db.Users.Add(user);
            _db.SaveChanges();

            return Ok(new AuthResponse { Token = _jwt.Create(user.Id, user.Username), Username = user.Username, KingdomId = user.KingdomId });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if (!_passwordAllowed) return GoogleOnly();
            // Email yoki foydalanuvchi nomi bilan kirish mumkin (telefonda yozish qulayroq).
            string login = (req.Email ?? "").Trim();
            AppUser user = _db.Users.FirstOrDefault(u => u.Email == login || u.Username == login);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash)) return Unauthorized(new { messageUz = "Email yoki parol noto'g'ri." });
            if (_hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
                return Unauthorized(new { messageUz = "Email yoki parol noto'g'ri." });

            user.LastLoginAt = DateTime.UtcNow;
            _db.SaveChanges();
            return Ok(new AuthResponse { Token = _jwt.Create(user.Id, user.Username), Username = user.Username, KingdomId = user.KingdomId });
        }
    }
}
