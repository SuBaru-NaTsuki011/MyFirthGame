using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AndijonSaltanatlar.Server.Authentication;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Server.Hubs;
using AndijonSaltanatlar.Server.Services;
using AndijonSaltanatlar.Server.Simulation;

var builder = WebApplication.CreateBuilder(args);

// --- Ma'lumotlar bazasi (PostgreSQL / EF Core) ---
// Provider: "Postgres" (production / docker-compose) yoki "Sqlite" (PostgreSQL o'rnatilmagan lokal LAN sinovi).
// Ikkalasida ham ma'lumot diskda saqlanadi — server qayta ishga tushganda yo'qolmaydi.
string provider = builder.Configuration["Database:Provider"] ?? "Postgres";
if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    string sqlite = builder.Configuration["Database:Sqlite"] ?? "Data Source=andijon_server.db";
    builder.Services.AddDbContext<GameDbContext>(opt => opt.UseSqlite(sqlite));
}
else
{
    string connStr = builder.Configuration.GetConnectionString("Default")
        ?? "Host=localhost;Port=5432;Database=andijon;Username=andijon;Password=andijon";
    builder.Services.AddDbContext<GameDbContext>(opt => opt.UseNpgsql(connStr));
}

// --- Parol hashlash (ASP.NET Identity) ---
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

// --- JWT autentifikatsiya ---
string jwtKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("ANDIJON_JWT_KEY") ?? "";
if (jwtKey.Length < 32) jwtKey = "DEV-ONLY-INSECURE-KEY-change-me-in-production-32b"; // faqat lokal dev uchun
builder.Services.AddSingleton(new JwtTokenService(jwtKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        // SignalR: tokenni query-string'dan olish
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gamehub"))
                    ctx.Token = accessToken;
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<BuildingService>();
builder.Services.AddScoped<AiPlanValidationService>();
builder.Services.AddSingleton<IServerClock, SystemClock>();
builder.Services.AddScoped<OnlineGameService>();
builder.Services.AddScoped<KingdomService>();
builder.Services.AddScoped<DiplomacyService>();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHostedService<WorldSimulationService>();

var app = builder.Build();

// --- Bazani yaratish (migratsiyalar o'rniga dev uchun EnsureCreated) ---
// DB mavjud bo'lmasa server ishdan to'xtamaydi — ogohlantirish yoziladi.
try
{
    using IServiceScope scope = app.Services.CreateScope();
    GameDbContext db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    db.Database.EnsureCreated();
    // Mavjud bazaga yangi jadval/ustunlarni qo'shadi (ma'lumot saqlanadi).
    SchemaUpgrader.Upgrade(db, app.Logger);
    app.Logger.LogInformation("Ma'lumotlar bazasiga ulanildi ({Provider}).", provider);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Ma'lumotlar bazasiga ulanib bo'lmadi. Server ishlaydi, lekin auth/saqlash funksiyalari DB talab qiladi. PostgreSQL'ni ishga tushiring (docker compose up).");
}

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => "Andijon: Saltanatlar — Server ishlayapti. API: /api/auth, /api/game, /api/online, Hub: /gamehub");
app.MapGet("/health", () => Results.Ok(new { ok = true, provider, utc = DateTime.UtcNow }));
app.MapControllers();
app.MapHub<GameHub>("/gamehub");

app.Run();
