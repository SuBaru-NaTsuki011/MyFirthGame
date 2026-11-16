using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Server.Hubs;
using AndijonSaltanatlar.Server.Services;
using AndijonSaltanatlar.Shared;
using AndijonSaltanatlar.Shared.Commands;
using AndijonSaltanatlar.Shared.DTOs;
using AndijonSaltanatlar.Shared.Planning;

namespace AndijonSaltanatlar.Server.Controllers
{
    [ApiController]
    [Route("api/game")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly BuildingService _buildings;
        private readonly AiPlanValidationService _planValidation;
        private readonly IHubContext<GameHub> _hub;

        public GameController(GameDbContext db, BuildingService buildings,
            AiPlanValidationService planValidation, IHubContext<GameHub> hub)
        {
            _db = db; _buildings = buildings; _planValidation = planValidation; _hub = hub;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [HttpPost("create-kingdom")]
        public IActionResult CreateKingdom([FromBody] CreateKingdomRequest req)
        {
            AppUser user = _db.Users.FirstOrDefault(u => u.Id == UserId);
            if (user == null) return Unauthorized();
            if (user.KingdomId != null) return Conflict(new { messageUz = "Sizda allaqachon saltanat bor." });

            World world = _db.Worlds.FirstOrDefault(w => w.Id == req.WorldId) ?? CreateDefaultWorld(req.WorldId);

            var kingdom = new Kingdom
            {
                WorldId = world.Id,
                Name = string.IsNullOrWhiteSpace(req.KingdomName) ? "Asaka Saltanati" : req.KingdomName,
                RulerName = string.IsNullOrWhiteSpace(req.RulerName) ? "Hukmdor" : req.RulerName,
                IsAi = false, OwnerUserId = user.Id,
                ColorR = 70, ColorG = 130, ColorB = 220
            };
            _db.Kingdoms.Add(kingdom);
            _db.SaveChanges();

            _db.KingdomResources.Add(new KingdomResource { KingdomId = kingdom.Id });
            _db.Cities.Add(new CityEntity { KingdomId = kingdom.Id, Name = "Asaka" });
            user.KingdomId = kingdom.Id;
            _db.SaveChanges();

            return Ok(ToDto(kingdom));
        }

        [HttpPost("command")]
        public async Task<IActionResult> Command([FromBody] GameCommand cmd)
        {
            Kingdom kingdom = _db.Kingdoms.FirstOrDefault(k => k.Id == cmd.KingdomId);
            if (kingdom == null) return NotFound(new { messageUz = "Saltanat topilmadi." });
            if (kingdom.OwnerUserId != UserId) return Forbid();

            // Idempotentlik: bir buyruq ikki marta bajarilmaydi
            if (!string.IsNullOrEmpty(cmd.CommandId) && _db.GameCommands.Any(gc => gc.CommandId == cmd.CommandId))
                return Ok(CommandResult.Ok(kingdom.StateVersion, "Buyruq allaqachon bajarilgan."));

            CommandResult result = cmd.Type switch
            {
                CommandType.PlaceBuilding => _buildings.PlaceBuilding(_db, kingdom, cmd),
                _ => CommandResult.Fail("Bu buyruq hali qo'llab-quvvatlanmaydi.")
            };

            if (result.Success)
            {
                if (!string.IsNullOrEmpty(cmd.CommandId))
                    _db.GameCommands.Add(new GameCommandEntity { CommandId = cmd.CommandId, KingdomId = kingdom.Id, WorldId = kingdom.WorldId });
                _db.SaveChanges();
                await _hub.Clients.Group(GameHub.WorldGroup(kingdom.WorldId))
                    .SendAsync("StateChanged", new { kingdomId = kingdom.Id, version = kingdom.StateVersion });
            }
            return Ok(result);
        }

        /// <summary>
        /// AI blueprintini serverda tekshiradi. Bu hech narsa qurmaydi va resurs yechmaydi —
        /// faqat egalik, texnologiya, bandlik va budjetni tasdiqlaydi.
        /// </summary>
        [HttpPost("validate-plan")]
        public IActionResult ValidatePlan([FromBody] AiPlanValidationRequest request)
        {
            if (request == null) return BadRequest(new { messageUz = "Reja yuborilmadi." });
            Kingdom kingdom = _db.Kingdoms.FirstOrDefault(k => k.Id == request.KingdomId);
            if (kingdom == null) return NotFound(new { messageUz = "Saltanat topilmadi." });
            if (kingdom.OwnerUserId != UserId) return Forbid();

            AiPlanValidationResult result = _planValidation.Validate(_db, kingdom, request);
            return Ok(result);
        }

        [HttpGet("state")]
        public IActionResult State()
        {
            AppUser user = _db.Users.FirstOrDefault(u => u.Id == UserId);
            if (user?.KingdomId == null) return NotFound(new { messageUz = "Saltanat topilmadi." });
            Kingdom kingdom = _db.Kingdoms.FirstOrDefault(k => k.Id == user.KingdomId);
            if (kingdom == null) return NotFound();

            KingdomResource res = _db.KingdomResources.FirstOrDefault(r => r.KingdomId == kingdom.Id) ?? new KingdomResource();
            World world = _db.Worlds.FirstOrDefault(w => w.Id == kingdom.WorldId) ?? new World();

            var dto = new WorldStateDto
            {
                WorldId = kingdom.WorldId, StateVersion = kingdom.StateVersion, Day = world.Day,
                PlayerResources = new ResourceDto { Coin = res.Coin, Food = res.Food, Wood = res.Wood, Stone = res.Stone }
            };
            foreach (Kingdom k in _db.Kingdoms.Where(k => k.WorldId == kingdom.WorldId)) dto.Kingdoms.Add(ToDto(k));
            foreach (BuildingEntity b in _db.Buildings.Where(b => b.KingdomId == kingdom.Id))
                dto.PlayerBuildings.Add(new BuildingDto { Id = b.Id, Kind = (BuildingKind)b.Kind, X = b.X, Y = b.Y, Level = b.Level, BuildProgress = b.BuildProgress });
            return Ok(dto);
        }

        private World CreateDefaultWorld(int id)
        {
            var w = new World { Name = "Andijon — 1-dunyo" };
            _db.Worlds.Add(w);
            _db.SaveChanges();
            return w;
        }

        private static KingdomDto ToDto(Kingdom k) => new()
        {
            Id = k.Id, Name = k.Name, RulerName = k.RulerName, WorldId = k.WorldId, IsAi = k.IsAi,
            ColorR = k.ColorR, ColorG = k.ColorG, ColorB = k.ColorB, Population = k.Population, StateVersion = k.StateVersion
        };
    }
}
