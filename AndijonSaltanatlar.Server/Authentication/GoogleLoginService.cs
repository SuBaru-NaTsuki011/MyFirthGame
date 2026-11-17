using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Server.Authentication
{
    /// <summary>
    /// Google sozlamalari (appsettings.json → "Google" bo'limi yoki muhit o'zgaruvchilari Google__ClientId ...).
    ///   ClientId / ClientSecret — Google Cloud Console → APIs &amp; Services → Credentials → OAuth client (Web application).
    ///   PublicBaseUrl — server tashqaridan ko'rinadigan manzil (masalan https://andijon.uz). Google'ga
    ///                   redirect_uri = PublicBaseUrl + "/auth/google/callback" sifatida beriladi va Console'da ham
    ///                   aynan shu manzil "Authorized redirect URIs" ro'yxatiga qo'shilishi shart.
    ///   DevLogin — FAQAT lokal sinov: haqiqiy Google o'rniga sinov sahifasi (har qanday email bilan kirish).
    ///              Production'da hech qachon yoqilmaydi.
    /// </summary>
    public sealed class GoogleAuthOptions
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string PublicBaseUrl { get; set; } = "";
        public bool DevLogin { get; set; }
        /// <summary>
        /// Sinov kirishi internetga ochilganda: sahifada shu taklif kodi so'raladi (begona odam kira olmaydi).
        /// Bo'sh bo'lsa kod so'ralmaydi (faqat lokal sinov uchun).
        /// </summary>
        public string DevLoginCode { get; set; } = "";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

        public static GoogleAuthOptions From(IConfiguration cfg)
        {
            var o = new GoogleAuthOptions();
            cfg.GetSection("Google").Bind(o);
            return o;
        }
    }

    /// <summary>Google'dan olingan (yoki sinov sahifasida kiritilgan) shaxs.</summary>
    public sealed class GoogleIdentity
    {
        public string Sub { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public bool EmailVerified { get; set; }
    }

    /// <summary>
    /// Kirish seansi: o'yin clienti /api/auth/google/start bilan yaratadi, brauzer Google orqali tasdiqlaydi,
    /// client esa /api/auth/google/poll bilan natijani oladi. Seans faqat xotirada va 10 daqiqa yashaydi.
    /// SessionId brauzer manzilida ko'rinadi, PollKey esa faqat clientda — boshqa odam tokenni o'g'irlay olmaydi.
    /// </summary>
    public sealed class GoogleLoginSession
    {
        public string Id { get; init; } = "";
        public string PollKey { get; init; } = "";
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
        public string Status { get; set; } = "pending";
        public string MessageUz { get; set; } = "";
        public AuthResponse Auth { get; set; }
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool Delivered { get; set; }
    }

    public sealed class GoogleLoginService
    {
        public static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(10);
        private const string AuthorizeEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        private readonly ConcurrentDictionary<string, GoogleLoginSession> _sessions = new();
        private readonly IHttpClientFactory _http;
        public GoogleAuthOptions Options { get; }

        public GoogleLoginService(GoogleAuthOptions options, IHttpClientFactory http)
        {
            Options = options;
            _http = http;
        }

        public static string RandomToken(int bytes = 24)
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public GoogleLoginSession Create()
        {
            Cleanup();
            var s = new GoogleLoginSession { Id = RandomToken(), PollKey = RandomToken() };
            _sessions[s.Id] = s;
            return s;
        }

        public GoogleLoginSession Get(string id)
        {
            if (string.IsNullOrEmpty(id) || !_sessions.TryGetValue(id, out GoogleLoginSession s)) return null;
            if (DateTime.UtcNow - s.CreatedUtc > SessionLifetime) { _sessions.TryRemove(id, out _); return null; }
            return s;
        }

        /// <summary>Client natijani oladi. Muvaffaqiyatli token bir marta beriladi va seans o'chiriladi.</summary>
        public GoogleLoginPollResponse Poll(string id, string pollKey)
        {
            GoogleLoginSession s = Get(id);
            if (s == null) return new GoogleLoginPollResponse { Status = "expired", MessageUz = "Kirish vaqti tugadi. Qaytadan urinib ko'ring." };
            if (!FixedEquals(s.PollKey, pollKey)) return new GoogleLoginPollResponse { Status = "error", MessageUz = "Noto'g'ri kirish kaliti." };
            var resp = new GoogleLoginPollResponse { Status = s.Status, MessageUz = s.MessageUz, DisplayName = s.DisplayName, Email = s.Email };
            if (s.Status == "ok")
            {
                resp.Auth = s.Auth;
                _sessions.TryRemove(id, out _);
            }
            else if (s.Status == "error") _sessions.TryRemove(id, out _);
            return resp;
        }

        private static bool FixedEquals(string a, string b)
            => a != null && b != null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

        private void Cleanup()
        {
            DateTime now = DateTime.UtcNow;
            foreach (KeyValuePair<string, GoogleLoginSession> kv in _sessions)
                if (now - kv.Value.CreatedUtc > SessionLifetime) _sessions.TryRemove(kv.Key, out _);
        }

        public string RedirectUri(string requestBaseUrl)
        {
            string baseUrl = string.IsNullOrWhiteSpace(Options.PublicBaseUrl) ? requestBaseUrl : Options.PublicBaseUrl;
            return baseUrl.TrimEnd('/') + "/auth/google/callback";
        }

        public string AuthorizeUrl(string sessionId, string redirectUri)
        {
            var q = new Dictionary<string, string>
            {
                ["client_id"] = Options.ClientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = "openid email profile",
                ["state"] = sessionId,
                ["prompt"] = "select_account",
                ["access_type"] = "online"
            };
            return AuthorizeEndpoint + "?" + string.Join("&", q.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));
        }

        /// <summary>
        /// Avtorizatsiya kodini Google token endpoint'ida id_token'ga almashtiradi.
        /// id_token to'g'ridan-to'g'ri Google'dan TLS orqali (client secret bilan) olingani uchun OpenID Connect
        /// (3.1.3.7, 6-band) bo'yicha imzo tekshiruvi shart emas; aud, iss, exp va email_verified tekshiriladi.
        /// </summary>
        public async Task<GoogleIdentity> ExchangeCodeAsync(string code, string redirectUri)
        {
            HttpClient http = _http.CreateClient("google");
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = Options.ClientId,
                ["client_secret"] = Options.ClientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            });
            HttpResponseMessage resp = await http.PostAsync(TokenEndpoint, form);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("Google token xatosi: " + (int)resp.StatusCode);
            using JsonDocument doc = JsonDocument.Parse(body);
            string idToken = doc.RootElement.GetProperty("id_token").GetString();
            return ParseIdToken(idToken, Options.ClientId);
        }

        /// <summary>
        /// Telefon (Android akkaunt tanlash oynasi) yuborgan ID tokenni tekshiradi. Token clientdan kelgani uchun
        /// imzo Google'ning ochiq kalitlari bilan to'liq tekshiriladi (Google.Apis.Auth), shuningdek aud (Web Client ID),
        /// iss va muddat. Qalbaki yoki boshqa ilova uchun berilgan token rad etiladi.
        /// </summary>
        public async Task<GoogleIdentity> ValidateClientIdTokenAsync(string idToken)
        {
            if (!Options.IsConfigured) throw new InvalidOperationException("Google sozlanmagan");
            var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken,
                new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings { Audience = new[] { Options.ClientId } });
            return new GoogleIdentity { Sub = payload.Subject, Email = payload.Email ?? "", Name = payload.Name ?? "", EmailVerified = payload.EmailVerified };
        }

        public static GoogleIdentity ParseIdToken(string idToken, string expectedAudience)
        {
            string[] parts = (idToken ?? "").Split('.');
            if (parts.Length < 2) throw new InvalidOperationException("id_token noto'g'ri formatda");
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using JsonDocument doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            JsonElement r = doc.RootElement;

            string iss = r.TryGetProperty("iss", out JsonElement i) ? i.GetString() : "";
            if (iss != "accounts.google.com" && iss != "https://accounts.google.com") throw new InvalidOperationException("id_token: noto'g'ri iss");
            string aud = r.TryGetProperty("aud", out JsonElement a) ? a.GetString() : "";
            if (aud != expectedAudience) throw new InvalidOperationException("id_token: noto'g'ri aud");
            long exp = r.TryGetProperty("exp", out JsonElement e) ? e.GetInt64() : 0;
            if (DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow.AddMinutes(-5)) throw new InvalidOperationException("id_token muddati o'tgan");

            var id = new GoogleIdentity
            {
                Sub = r.TryGetProperty("sub", out JsonElement s) ? s.GetString() : "",
                Email = r.TryGetProperty("email", out JsonElement em) ? em.GetString() : "",
                Name = r.TryGetProperty("name", out JsonElement n) ? n.GetString() : "",
                EmailVerified = r.TryGetProperty("email_verified", out JsonElement ev) &&
                                (ev.ValueKind == JsonValueKind.True || (ev.ValueKind == JsonValueKind.String && ev.GetString() == "true"))
            };
            if (string.IsNullOrEmpty(id.Sub)) throw new InvalidOperationException("id_token: sub yo'q");
            return id;
        }

        /// <summary>
        /// Google shaxsi bo'yicha foydalanuvchini topadi yoki yaratadi. Avval GoogleSub bo'yicha, keyin tasdiqlangan
        /// email bo'yicha (eski hisobni bog'lash). Yangi o'yinchiga noyob o'yin nomi beriladi.
        /// </summary>
        public static AppUser FindOrCreateUser(GameDbContext db, GoogleIdentity g)
        {
            AppUser user = db.Users.FirstOrDefault(u => u.GoogleSub == g.Sub);
            if (user == null && g.EmailVerified && !string.IsNullOrEmpty(g.Email))
            {
                string email = g.Email.Trim().ToLowerInvariant();
                // Parol bilan ochilgan eski akkaunt yoki sinov sahifasi (DevLogin) orqali shu email bilan ochilgan
                // akkaunt — haqiqiy Google (email tasdiqlangan) birinchi kirishda shu akkauntni o'ziga oladi:
                // saltanat va bulutdagi saqlash yo'qolmaydi.
                string devSub = "dev:" + email;
                user = db.Users.FirstOrDefault(u => u.Email == email && (u.GoogleSub == null || u.GoogleSub == devSub));
                if (user != null) user.GoogleSub = g.Sub;
            }
            if (user == null)
            {
                string email = string.IsNullOrEmpty(g.Email) ? g.Sub + "@google.local" : g.Email.Trim().ToLowerInvariant();
                if (db.Users.Any(u => u.Email == email)) email = g.Sub + "@google.local";
                user = new AppUser
                {
                    GoogleSub = g.Sub,
                    Email = email,
                    Username = UniqueUsername(db, g),
                    PasswordHash = "",
                    DisplayName = string.IsNullOrWhiteSpace(g.Name) ? null : g.Name.Trim()
                };
                db.Users.Add(user);
            }
            else if (!string.IsNullOrWhiteSpace(g.Name)) user.DisplayName = g.Name.Trim();
            user.LastLoginAt = DateTime.UtcNow;
            db.SaveChanges();
            return user;
        }

        private static string UniqueUsername(GameDbContext db, GoogleIdentity g)
        {
            string raw = !string.IsNullOrWhiteSpace(g.Name) ? g.Name : (g.Email ?? "").Split('@')[0];
            var sb = new StringBuilder();
            foreach (char c in raw.ToLowerInvariant())
            {
                if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9') sb.Append(c);
                else if ((c == ' ' || c == '.' || c == '_' || c == '-') && sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
                if (sb.Length >= 16) break;
            }
            string baseName = sb.ToString().Trim('_');
            if (baseName.Length < 3) baseName = "hukmdor";
            string name = baseName;
            for (int n = 2; db.Users.Any(u => u.Username == name); n++) name = baseName + n;
            return name;
        }

        public void Complete(GoogleLoginSession s, AppUser user, JwtTokenService jwt)
        {
            s.Auth = new AuthResponse { Token = jwt.Create(user.Id, user.Username), Username = user.Username, KingdomId = user.KingdomId };
            s.DisplayName = user.DisplayName ?? user.Username;
            s.Email = user.Email;
            s.Status = "ok";
        }

        public static void Fail(GoogleLoginSession s, string messageUz)
        {
            if (s == null) return;
            s.Status = "error";
            s.MessageUz = messageUz;
        }
    }
}
