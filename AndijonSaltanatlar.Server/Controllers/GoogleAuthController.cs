using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AndijonSaltanatlar.Server.Authentication;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Server.Controllers
{
    /// <summary>
    /// Google orqali kirish — o'yindagi YAGONA kirish usuli.
    ///
    /// Oqim (Windows va Android uchun bir xil, deep-link yoki client secret talab qilmaydi):
    ///   1. O'yin  POST /api/auth/google/start          → { sessionId, pollKey, loginUrl }
    ///   2. O'yin tizim brauzerida loginUrl'ni ochadi  → GET /auth/google/login?s=… → Google hisob tanlash
    ///   3. Google → GET /auth/google/callback?code&amp;state → server kodni almashtiradi, o'yinchini topadi/yaratadi
    ///   4. O'yin har 2 soniyada GET /api/auth/google/poll?s&amp;k → JWT token (bir marta beriladi)
    /// Client secret faqat serverda saqlanadi.
    /// </summary>
    [ApiController]
    public class GoogleAuthController : ControllerBase
    {
        private readonly GoogleLoginService _google;
        private readonly GameDbContext _db;
        private readonly SessionTokenService _sessions;
        private readonly VipOptions _vip;
        private readonly ILogger<GoogleAuthController> _log;

        public GoogleAuthController(GoogleLoginService google, GameDbContext db, SessionTokenService sessions, VipOptions vip, ILogger<GoogleAuthController> log)
        {
            _google = google; _db = db; _sessions = sessions; _vip = vip; _log = log;
        }

        private string Device
        {
            get { string ua = Request.Headers.UserAgent.ToString(); return ua.Length > 0 ? ua : "game"; }
        }

        private string RequestBaseUrl => Request.Scheme + "://" + Request.Host.Value;

        [HttpPost("api/auth/google/start")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
        public IActionResult Start()
        {
            if (!_google.Options.IsConfigured && !_google.Options.DevLogin)
                return StatusCode(503, new { messageUz = "Serverda Google kirish sozlanmagan (Google:ClientId / ClientSecret)." });
            GoogleLoginSession s = _google.Create();
            string baseUrl = string.IsNullOrWhiteSpace(_google.Options.PublicBaseUrl) ? RequestBaseUrl : _google.Options.PublicBaseUrl.TrimEnd('/');
            return Ok(new GoogleLoginStartResponse
            {
                SessionId = s.Id,
                PollKey = s.PollKey,
                LoginUrl = baseUrl + "/auth/google/login?s=" + Uri.EscapeDataString(s.Id),
                ExpiresInSeconds = (int)GoogleLoginService.SessionLifetime.TotalSeconds
            });
        }

        /// <summary>O'yin shu yerdan Web Client ID'ni oladi (APK'ni qayta yig'masdan sozlash mumkin).</summary>
        [HttpGet("api/auth/google/config")]
        public IActionResult Config() => Ok(new GoogleClientConfig
        {
            WebClientId = _google.Options.IsConfigured ? _google.Options.ClientId : "",
            Configured = _google.Options.IsConfigured,
            DevLogin = _google.Options.DevLogin
        });

        /// <summary>
        /// Telefondagi Google akkaunt tanlash oynasi (Android Credential Manager) bergan ID token bilan kirish.
        /// Token imzosi serverda tekshiriladi; muvaffaqiyatli bo'lsa o'yin tokeni qaytadi.
        /// </summary>
        [HttpPost("api/auth/google/idtoken")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
        public async Task<IActionResult> IdToken([FromBody] GoogleIdTokenRequest req)
        {
            if (!_google.Options.IsConfigured)
                return StatusCode(503, new { messageUz = "Serverda Google kirish sozlanmagan." });
            if (string.IsNullOrWhiteSpace(req?.IdToken))
                return BadRequest(new { messageUz = "Google token yuborilmadi." });
            GoogleIdentity id;
            try { id = await _google.ValidateClientIdTokenAsync(req.IdToken); }
            catch (Exception ex)
            {
                _log.LogWarning("Google ID token rad etildi: {Msg}", ex.Message);
                return Unauthorized(new { messageUz = "Google hisobini tasdiqlab bo'lmadi." });
            }
            AppUser user = GoogleLoginService.FindOrCreateUser(_db, id, _vip);
            var tmp = new GoogleLoginSession();
            _google.Complete(tmp, user, _sessions, _db, Device);
            return Ok(new GoogleLoginPollResponse { Status = "ok", Auth = tmp.Auth, DisplayName = tmp.DisplayName, Email = tmp.Email });
        }

        [HttpGet("api/auth/google/poll")]
        public IActionResult Poll([FromQuery] string s, [FromQuery] string k)
            => Ok(_google.Poll(s, k));

        /// <summary>Saqlangan token hali yaroqlimi (o'yin ishga tushganda avtomatik kirish uchun).</summary>
        [Authorize]
        [HttpGet("api/auth/me")]
        public IActionResult Me()
        {
            int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            AppUser u = _db.Users.FirstOrDefault(x => x.Id == id);
            if (u == null) return Unauthorized(new { messageUz = "Hisob topilmadi." });
            return Ok(new MeResponse { UserId = u.Id, Username = u.Username, DisplayName = u.DisplayName ?? u.Username, Email = u.Email, KingdomId = u.KingdomId, NeedsProfile = !u.ProfileCompleted, AvatarId = u.AvatarId, IsVip = u.IsVip });
        }

        /// <summary>Brauzer shu yerdan Google'ga yo'naltiriladi (yoki sinov rejimida sinov sahifasi ko'rsatiladi).</summary>
        [HttpGet("auth/google/login")]
        public IActionResult Login([FromQuery] string s)
        {
            GoogleLoginSession session = _google.Get(s);
            if (session == null) return Html(LoginPages.Message("Kirish havolasi eskirgan", "O'yinga qaytib, «Google bilan kirish» tugmasini qayta bosing.", false), 410);

            if (_google.Options.IsConfigured)
                return Redirect(_google.AuthorizeUrl(session.Id, _google.RedirectUri(RequestBaseUrl)));
            if (_google.Options.DevLogin)
                return Html(LoginPages.DevLogin(session.Id, null, !string.IsNullOrEmpty(_google.Options.DevLoginCode)));
            return Html(LoginPages.Message("Google sozlanmagan", "Server administratori Google:ClientId va Google:ClientSecret sozlamalarini kiritishi kerak.", false), 503);
        }

        [HttpGet("auth/google/callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, [FromQuery] string error)
        {
            GoogleLoginSession session = _google.Get(state);
            if (session == null) return Html(LoginPages.Message("Kirish havolasi eskirgan", "O'yinga qaytib, qaytadan urinib ko'ring.", false), 410);
            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
            {
                GoogleLoginService.Fail(session, "Google kirishni bekor qildi.");
                return Html(LoginPages.Message("Kirish bekor qilindi", "O'yinga qaytib, qaytadan urinib ko'rishingiz mumkin.", false));
            }
            try
            {
                GoogleIdentity id = await _google.ExchangeCodeAsync(code, _google.RedirectUri(RequestBaseUrl), session);
                AppUser user = GoogleLoginService.FindOrCreateUser(_db, id, _vip);
                _google.Complete(session, user, _sessions, _db, "browser");
                return Html(LoginPages.Success(user.DisplayName ?? user.Username));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Google kirish xatosi");
                GoogleLoginService.Fail(session, "Google bilan kirib bo'lmadi. Qaytadan urinib ko'ring.");
                return Html(LoginPages.Message("Kirib bo'lmadi", "Google javobini tekshirib bo'lmadi. O'yinga qaytib, qaytadan urinib ko'ring.", false), 502);
            }
        }

        /// <summary>FAQAT sinov rejimi (Google:DevLogin=true): haqiqiy Google o'rniga kiritilgan email bilan kirish.</summary>
        [HttpPost("auth/google/dev")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult DevCallback([FromForm] string s, [FromForm] string email, [FromForm] string name, [FromForm] string code)
        {
            if (!_google.Options.DevLogin) return NotFound();
            GoogleLoginSession session = _google.Get(s);
            if (session == null) return Html(LoginPages.Message("Kirish havolasi eskirgan", "O'yinga qaytib, qaytadan urinib ko'ring.", false), 410);
            bool needCode = !string.IsNullOrEmpty(_google.Options.DevLoginCode);
            if (needCode && !string.Equals((code ?? "").Trim(), _google.Options.DevLoginCode, StringComparison.OrdinalIgnoreCase))
                return Html(LoginPages.DevLogin(session.Id, "Taklif kodi noto'g'ri.", needCode), 403);
            email = (email ?? "").Trim().ToLowerInvariant();
            if (email.Length < 5 || !email.Contains('@'))
                return Html(LoginPages.DevLogin(session.Id, "Email manzilini to'g'ri kiriting.", needCode), 400);

            var id = new GoogleIdentity { Sub = "dev:" + email, Email = email, Name = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim(), EmailVerified = true };
            AppUser user = GoogleLoginService.FindOrCreateUser(_db, id, _vip);
            _google.Complete(session, user, _sessions, _db, "dev");
            return Html(LoginPages.Success(user.DisplayName ?? user.Username));
        }

        private ContentResult Html(string html, int status = 200)
            => new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = status };
    }

    /// <summary>Brauzerda ko'rinadigan kichik HTML sahifalar (telefon va kompyuter uchun moslashuvchan).</summary>
    public static class LoginPages
    {
        private const string Style = @"<style>
:root{--bg:#121826;--card:#1c2436;--line:#2c3854;--gold:#e0b45a;--text:#e9edf5;--muted:#9aa6bd;--ok:#6cc887;--bad:#e2766b}
*{box-sizing:border-box}body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;
background:radial-gradient(circle at 50% 0,#1f2a44,var(--bg) 60%);font-family:'Segoe UI',Roboto,'Noto Sans',sans-serif;color:var(--text);padding:16px}
.card{width:100%;max-width:420px;background:var(--card);border:1px solid var(--line);border-top:4px solid var(--gold);border-radius:14px;padding:28px 24px;text-align:center;box-shadow:0 12px 40px #0008}
h1{font-size:22px;margin:0 0 4px;color:var(--gold);letter-spacing:.5px}h2{font-size:18px;margin:18px 0 8px}
p{color:var(--muted);line-height:1.5;margin:6px 0}.icon{font-size:44px;margin:8px 0}
.ok{color:var(--ok)}.bad{color:var(--bad)}
label{display:block;text-align:left;font-size:13px;color:var(--muted);margin:14px 0 6px}
input{width:100%;padding:12px 14px;border-radius:10px;border:1px solid var(--line);background:#141b2b;color:var(--text);font-size:16px}
button,.btn{display:inline-block;width:100%;margin-top:18px;padding:13px;border:0;border-radius:10px;background:#fff;color:#1f1f1f;font-size:16px;font-weight:600;cursor:pointer;text-decoration:none}
.g{display:inline-flex;align-items:center;justify-content:center;gap:10px}
.dev{background:#3a2a12;border:1px solid #6b4d1c;color:#f3cf8a;border-radius:8px;padding:8px 10px;font-size:13px;margin-top:12px}
.err{color:var(--bad);font-size:14px;margin-top:10px}
</style>";

        private static string Page(string title, string body)
            => "<!doctype html><html lang=\"uz\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
               + "<title>" + WebUtility.HtmlEncode(title) + "</title>" + Style + "</head><body><main class=\"card\">"
               + "<h1>ANDIJON: SALTANATLAR</h1>" + body + "</main></body></html>";

        private const string GLogo = "<svg width=\"20\" height=\"20\" viewBox=\"0 0 48 48\" aria-hidden=\"true\"><path fill=\"#EA4335\" d=\"M24 9.5c3.5 0 6.6 1.2 9.1 3.6l6.8-6.8C35.8 2.4 30.3 0 24 0 14.6 0 6.6 5.4 2.7 13.3l7.9 6.1C12.5 13.6 17.8 9.5 24 9.5z\"/><path fill=\"#4285F4\" d=\"M46.1 24.5c0-1.6-.1-3.1-.4-4.5H24v9h12.4c-.5 2.9-2.2 5.3-4.6 7l7.4 5.7c4.3-4 6.9-9.9 6.9-17.2z\"/><path fill=\"#FBBC05\" d=\"M10.6 28.6c-.5-1.4-.8-3-.8-4.6s.3-3.2.8-4.6l-7.9-6.1C1 16.6 0 20.2 0 24s1 7.4 2.7 10.7l7.9-6.1z\"/><path fill=\"#34A853\" d=\"M24 48c6.5 0 11.9-2.1 15.9-5.8l-7.4-5.7c-2.1 1.4-4.8 2.3-8.5 2.3-6.2 0-11.5-4.1-13.4-9.9l-7.9 6.1C6.6 42.6 14.6 48 24 48z\"/></svg>";

        public static string DevLogin(string sessionId, string error = null, bool needCode = false)
            => Page("Google bilan kirish (sinov)",
                "<p>Hisobingizni tanlang</p>"
                + "<div class=\"dev\" id=\"dev-banner\">SINOV REJIMI — haqiqiy Google emas. Serverda Google:ClientId sozlangach, bu sahifa o'rniga Google oynasi ochiladi.</div>"
                + "<form method=\"post\" action=\"/auth/google/dev\">"
                + "<input type=\"hidden\" name=\"s\" value=\"" + WebUtility.HtmlEncode(sessionId) + "\">"
                + "<label for=\"email\">Google email</label><input id=\"email\" name=\"email\" type=\"email\" placeholder=\"ism@gmail.com\" required autocomplete=\"email\">"
                + "<label for=\"name\">Ism</label><input id=\"name\" name=\"name\" type=\"text\" placeholder=\"Ismingiz\" autocomplete=\"name\">"
                + (needCode ? "<label for=\"code\">Taklif kodi</label><input id=\"code\" name=\"code\" type=\"text\" placeholder=\"Do'stingiz bergan kod\" required autocomplete=\"off\">" : "")
                + (error != null ? "<div class=\"err\" role=\"alert\">" + WebUtility.HtmlEncode(error) + "</div>" : "")
                + "<button type=\"submit\" class=\"g\" id=\"continue\">" + GLogo + "<span>Davom etish</span></button>"
                + "</form>");

        public static string Success(string name)
            => Page("Kirish muvaffaqiyatli",
                "<div class=\"icon ok\">&#10004;</div><h2 id=\"result\">Xush kelibsiz, " + WebUtility.HtmlEncode(name) + "!</h2>"
                + "<p>Kirish muvaffaqiyatli. Endi o'yinga qayting — saltanatingiz qolgan joyidan davom etadi.</p>"
                + "<a class=\"btn\" id=\"back\" href=\"uz.andijon.saltanatlar://auth\" style=\"display:none\">O'yinga qaytish</a>"
                + "<p style=\"font-size:13px;margin-top:16px\">Bu oynani yopishingiz mumkin.</p>"
                + "<script>if(/Android/i.test(navigator.userAgent)){var b=document.getElementById('back');b.style.display='inline-block';setTimeout(function(){location.href=b.href},600);}else{setTimeout(function(){try{window.close()}catch(e){}},2500);}</script>");

        public static string Message(string title, string text, bool ok)
            => Page(title, "<div class=\"icon " + (ok ? "ok" : "bad") + "\">" + (ok ? "&#10004;" : "&#9888;") + "</div><h2 id=\"result\">"
                           + WebUtility.HtmlEncode(title) + "</h2><p>" + WebUtility.HtmlEncode(text) + "</p>");
    }
}
