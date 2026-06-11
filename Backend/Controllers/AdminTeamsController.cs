using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Team;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/teams")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminTeamsController : ControllerBase
    {
        private readonly IAdminTeamService _adminTeamService;

        public AdminTeamsController(IAdminTeamService adminTeamService)
        {
            _adminTeamService = adminTeamService;
        }

        private Guid? CurrentUserId
        {
            get
            {
                var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(value, out var userId) ? userId : null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTeams()
            => Ok(await _adminTeamService.GetTeamsAsync());

        [HttpPut("{teamId}/approve")]
        public async Task<IActionResult> ApproveTeam(Guid teamId)
            => (await _adminTeamService.ApproveTeamAsync(teamId, CurrentUserId)).ToActionResult(this);

        [HttpPut("{teamId}/reject")]
        public async Task<IActionResult> RejectTeam(Guid teamId, [FromBody] TeamDecisionRequest? request = null)
            => (await _adminTeamService.RejectTeamAsync(teamId, request, CurrentUserId)).ToActionResult(this);

        [HttpPut("{teamId}/eliminate")]
        public async Task<IActionResult> EliminateTeam(Guid teamId, [FromBody] EliminateTeamRequest request)
            => (await _adminTeamService.EliminateTeamAsync(teamId, request, CurrentUserId)).ToActionResult(this);

        [HttpDelete("{teamId}")]
        public async Task<IActionResult> DeleteTeam(Guid teamId)
            => (await _adminTeamService.DeleteTeamAsync(teamId, CurrentUserId)).ToActionResult(this);
    }
}
