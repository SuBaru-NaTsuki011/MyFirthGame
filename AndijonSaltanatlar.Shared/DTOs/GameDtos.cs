using System.Collections.Generic;

namespace AndijonSaltanatlar.Shared.DTOs
{
    public sealed class RegisterRequest { public string Username { get; set; } = ""; public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
    public sealed class LoginRequest { public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
    public sealed class AuthResponse { public string Token { get; set; } = ""; public string Username { get; set; } = ""; public int? KingdomId { get; set; } }

    // --- Google orqali kirish (brauzer + so'rov/poll oqimi, Windows va Android uchun bir xil) ---
    public sealed class GoogleLoginStartResponse
    {
        public string SessionId { get; set; } = "";
        /// <summary>Faqat o'yin clienti biladigan maxfiy kalit (brauzer manzilida yo'q) — natijani olish uchun.</summary>
        public string PollKey { get; set; } = "";
        /// <summary>Brauzerda ochiladigan manzil.</summary>
        public string LoginUrl { get; set; } = "";
        public int ExpiresInSeconds { get; set; }
    }

    public sealed class GoogleLoginPollResponse
    {
        /// <summary>"pending" | "ok" | "error" | "expired"</summary>
        public string Status { get; set; } = "pending";
        public string MessageUz { get; set; } = "";
        public AuthResponse Auth { get; set; }
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
    }

    /// <summary>Client uchun Google sozlamasi: telefondagi akkaunt tanlash oynasiga Web Client ID kerak.</summary>
    public sealed class GoogleClientConfig
    {
        /// <summary>Google Cloud'dagi "Web application" Client ID (server tokenni shu auditoriya bilan tekshiradi).</summary>
        public string WebClientId { get; set; } = "";
        /// <summary>Haqiqiy Google sozlangan (aks holda faqat sinov sahifasi).</summary>
        public bool Configured { get; set; }
        public bool DevLogin { get; set; }
    }

    /// <summary>Telefondagi Google akkaunt tanlash oynasidan olingan ID token.</summary>
    public sealed class GoogleIdTokenRequest { public string IdToken { get; set; } = ""; }

    public sealed class MeResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public int? KingdomId { get; set; }
    }

    // --- Bulutdagi saqlash ---
    public sealed class CloudSaveDto
    {
        public string Json { get; set; } = "";
        public long Version { get; set; }
        public System.DateTime UpdatedAt { get; set; }
        public string Device { get; set; } = "";
    }

    public sealed class CloudSaveUploadRequest
    {
        public string Json { get; set; } = "";
        /// <summary>Client oxirgi ko'rgan versiya. 0 — birinchi saqlash. Mos kelmasa server 409 qaytaradi.</summary>
        public long BaseVersion { get; set; }
        public bool Force { get; set; }
        public string Device { get; set; } = "";
    }

    public sealed class CreateKingdomRequest { public string RulerName { get; set; } = ""; public string KingdomName { get; set; } = ""; public int WorldId { get; set; } = 1; }

    public sealed class KingdomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string RulerName { get; set; } = "";
        public int WorldId { get; set; }
        public bool IsAi { get; set; }
        public int ColorR { get; set; }
        public int ColorG { get; set; }
        public int ColorB { get; set; }
        public int CityCount { get; set; }
        public int Population { get; set; }
        public long StateVersion { get; set; }
    }

    public sealed class ResourceDto { public int Coin { get; set; } public int Food { get; set; } public int Wood { get; set; } public int Stone { get; set; } }

    public sealed class BuildingDto
    {
        public int Id { get; set; }
        public AndijonSaltanatlar.Shared.BuildingKind Kind { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Level { get; set; } = 1;
        public float BuildProgress { get; set; } = 1f;
    }

    /// <summary>Dunyo yoki shahar holati snapshot'i (client ko'rsatish uchun).</summary>
    public sealed class WorldStateDto
    {
        public int WorldId { get; set; }
        public long StateVersion { get; set; }
        public int Day { get; set; }
        public List<KingdomDto> Kingdoms { get; set; } = new();
        public ResourceDto PlayerResources { get; set; } = new();
        public List<BuildingDto> PlayerBuildings { get; set; } = new();
    }

    public sealed class NotificationDto { public int Id { get; set; } public string TitleUz { get; set; } = ""; public string BodyUz { get; set; } = ""; public bool Read { get; set; } }
}
