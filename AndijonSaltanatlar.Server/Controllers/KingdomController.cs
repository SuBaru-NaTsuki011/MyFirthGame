using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Server.Hubs;
using AndijonSaltanatlar.Server.Services;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Server.Controllers
{
    /// <summary>
    /// PROMPT 19B: Saltanat boshqaruvi — hududlar (dunyo xaritasi), diplomatik birlashish, shaharlar, hokimlar.
    /// Saltanat har doim JWT dagi foydalanuvchidan aniqlanadi; hudud/shahar egaligi serverda tekshiriladi.
    /// </summary>
    [ApiController]
    [Route("api/kingdom")]
    [Authorize]
    public class KingdomController : ControllerBase
    {
        private readonly OnlineGameService _online;
        private readonly KingdomService _kingdom;
        private readonly IHubContext<GameHub> _hub;

        public KingdomController(OnlineGameService online, KingdomService kingdom, IHubContext<GameHub> hub)
        {
            _online = online; _kingdom = kingdom; _hub = hub;
        }

        private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : 0;

        private async Task<Kingdom> Me() => await _online.KingdomOfUserAsync(UserId);

        [HttpGet("overview")]
        public async Task<IActionResult> Overview()
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            return Ok(await _kingdom.OverviewAsync(k));
        }

        [HttpGet("regions")]
        public async Task<IActionResult> Regions()
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            return Ok(await _kingdom.RegionsAsync(k));
        }

        [HttpPost("envoy")]
        public async Task<IActionResult> Envoy([FromBody] RegionActionRequest req)
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            return Ok(await _kingdom.SendEnvoyAsync(k, req?.CommandId, req?.RegionId ?? 0));
        }

        /// <summary>Diplomatik birlashish: muvaffaqiyatli bo'lsa dunyodagi BARCHA clientlar xaritani yangilaydi.</summary>
        [HttpPost("annex")]
        public async Task<IActionResult> Annex([FromBody] RegionActionRequest req)
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            var (result, region, oldOwner) = await _kingdom.AnnexAsync(k, req?.CommandId, req?.RegionId ?? 0);
            if (result.Success && !result.Duplicate && region != null)
                await _hub.Clients.Group(GameHub.WorldGroup(k.WorldId)).SendAsync("TerritoryTransferred", k.WorldId, region.Id, k.Id, oldOwner, region.Name + " endi " + k.Name + " tarkibida");
            return Ok(result);
        }

        [HttpPost("governor/mode")]
        public async Task<IActionResult> GovernorMode([FromBody] GovernorRequest req) => await Act(k => _kingdom.SetGovernorModeAsync(k, req));

        [HttpPost("governor/analyze")]
        public async Task<IActionResult> GovernorAnalyze([FromBody] GovernorRequest req) => await Act(k => _kingdom.GovernorAnalyzeAsync(k, req));

        [HttpPost("governor/approve")]
        public async Task<IActionResult> GovernorApprove([FromBody] GovernorRequest req) => await Act(k => _kingdom.ApprovePlanAsync(k, req));

        [HttpPost("governor/reject")]
        public async Task<IActionResult> GovernorReject([FromBody] GovernorRequest req) => await Act(k => _kingdom.RejectPlanAsync(k, req));

        private async Task<IActionResult> Act(System.Func<Kingdom, Task<ActionResultDto>> action)
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            if (!ModelState.IsValid) return BadRequest(ActionResultDto.Fail("So'rov noto'g'ri"));
            return Ok(await action(k));
        }
    }
}
