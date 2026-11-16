using System.Collections.Generic;

namespace AndijonSaltanatlar.Shared.DTOs
{
    public sealed class RegisterRequest { public string Username { get; set; } = ""; public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
    public sealed class LoginRequest { public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
    public sealed class AuthResponse { public string Token { get; set; } = ""; public string Username { get; set; } = ""; public int? KingdomId { get; set; } }

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
