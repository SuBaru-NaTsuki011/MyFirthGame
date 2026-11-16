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
    [ApiController]
    [Route("api/diplomacy")]
    [Authorize]
    public class DiplomacyController : ControllerBase
    {
        private readonly OnlineGameService _online;
        private readonly DiplomacyService _dipl;
        private readonly IHubContext<GameHub> _hub;

        public DiplomacyController(OnlineGameService online, DiplomacyService dipl, IHubContext<GameHub> hub)
        {
            _online = online; _dipl = dipl; _hub = hub;
        }

        private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : 0;
        private async Task<Kingdom> Me() => await _online.KingdomOfUserAsync(UserId);

        private async Task<IActionResult> Act(System.Func<Kingdom, Task<ActionResultDto>> action)
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            if (!ModelState.IsValid) return BadRequest(ActionResultDto.Fail("So'rov noto'g'ri"));
            return Ok(await action(k));
        }

        // ---- Ittifoqlar ----
        [HttpGet("alliance/mine")]
        public async Task<IActionResult> MyAlliance()
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            return Ok(await _dipl.MyAllianceAsync(k));
        }

        [HttpGet("alliance/invitations")]
        public async Task<IActionResult> Invitations()
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            return Ok(await _dipl.InvitationsAsync(k));
        }

        [HttpPost("alliance/create")]
        public async Task<IActionResult> CreateAlliance([FromBody] CreateAllianceRequest req)
            => await Act(k => _dipl.CreateAllianceAsync(k, req));

        [HttpPost("alliance/invite")]
        public async Task<IActionResult> Invite([FromBody] InviteToAllianceRequest req)
            => await Act(k => _dipl.InviteAsync(k, req));

        [HttpPost("alliance/respond")]
        public async Task<IActionResult> RespondInvitation([FromBody] RespondInvitationRequest req)
            => await Act(k => _dipl.RespondInvitationAsync(k, req));

        [HttpPost("alliance/leave")]
        public async Task<IActionResult> Leave([FromBody] LeaveAllianceRequest req)
            => await Act(k => _dipl.LeaveAllianceAsync(k, req));

        /// <summary>Asoschi a'zoga rol beradi (Vakil / A'zo / asoschilikni topshirish).</summary>
        [HttpPost("alliance/role")]
        public async Task<IActionResult> SetRole([FromBody] SetAllianceRoleRequest req)
            => await Act(k => _dipl.SetRoleAsync(k, req));

        [HttpGet("alliance/list")]
        public async Task<IActionResult> ListAlliances()
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            return Ok(await _dipl.AlliancesInWorldAsync(k.WorldId, k));
        }

        // ---- Loyihalar ----
        [HttpPost("alliance/project/create")]
        public async Task<IActionResult> CreateProject([FromBody] CreateAllianceProjectRequest req)
            => await Act(k => _dipl.CreateProjectAsync(k, req));

        [HttpPost("alliance/contribute")]
        public async Task<IActionResult> Contribute([FromBody] ContributeAllianceRequest req)
            => await Act(k => _dipl.ContributeAsync(k, req));

        [HttpGet("alliance/projects/{allianceId:int}")]
        public async Task<IActionResult> Projects(int allianceId)
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            return Ok(await _dipl.ProjectsAsync(k, allianceId));
        }

        // ---- Kelishuvlar ----
        [HttpPost("agreement/propose")]
        public async Task<IActionResult> Propose([FromBody] ProposeAgreementRequest req)
            => await Act(k => _dipl.ProposeAgreementAsync(k, req));

        [HttpPost("agreement/respond")]
        public async Task<IActionResult> Respond([FromBody] RespondAgreementRequest req)
            => await Act(k => _dipl.RespondAgreementAsync(k, req));

        [HttpGet("agreement/list")]
        public async Task<IActionResult> Agreements()
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            return Ok(await _dipl.AgreementsAsync(k));
        }

        // ---- Dunyo yangiliklari ----
        [HttpGet("news")]
        public async Task<IActionResult> News([FromQuery] int take = 30)
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            return Ok(await _dipl.NewsAsync(k.WorldId, take));
        }

        // ---- Ittifoq bozori (offers with allianceId) ----
        [HttpGet("market/offers/{allianceId:int}")]
        public async Task<IActionResult> AllianceOffers(int allianceId)
        {
            Kingdom k = await Me();
            if (k == null) return NotFound(new { messageUz = "Avval dunyoga qo'shiling" });
            // Faqat ittifoq a'zosi o'z ittifoqi bozorini ko'radi.
            AllianceDto mine = await _dipl.MyAllianceAsync(k);
            if (mine == null || mine.Id != allianceId) return Forbid();
            return Ok(await _online.OffersAsync(k, allianceId));
        }
    }
}
