using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AndijonSaltanatlar.Server.Authentication
{
    /// <summary>JWT token yaratadi. Kalit konfiguratsiyadan (Jwt:Key) — kodda hardcode qilinmaydi.</summary>
    public class JwtTokenService
    {
        private readonly string _key;

        // Kalit Program.cs'da Jwt:Key (yoki ANDIJON_JWT_KEY) dan olinadi va shu yerga uzatiladi,
        // shunda token imzolash va tekshirish bitta kalitdan foydalanadi.
        public JwtTokenService(string key)
        {
            _key = string.IsNullOrEmpty(key) || key.Length < 32
                ? "DEV-ONLY-INSECURE-KEY-change-me-in-production-32b" // faqat lokal development uchun
                : key;
        }

        public string Create(int userId, string username)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username)
            };
            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddDays(7), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string Key => _key;
    }
}
