using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Team;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [Route("api/teams")]
    [ApiController]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
            => (await _teamService.CreateTeamAsync(CurrentUserId, request)).ToActionResult(this);

        [HttpGet("my-team")]
        public async Task<IActionResult> GetMyTeam()
            => Ok(await _teamService.GetMyTeamAsync(CurrentUserId));

        [HttpPut("{teamId}")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> UpdateTeam(Guid teamId, [FromBody] UpdateTeamRequest request)
            => (await _teamService.UpdateTeamAsync(teamId, CurrentUserId, User.IsInRole("Admin"), request)).ToActionResult(this);

        [HttpPost("{teamId}/members")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> AddMember(Guid teamId, [FromBody] AddTeamMemberRequest request)
            => (await _teamService.AddMemberAsync(teamId, CurrentUserId, request)).ToActionResult(this);

        [HttpPost("{teamId}/invites")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> InviteMember(Guid teamId, [FromBody] InviteTeamMemberRequest request)
            => (await _teamService.InviteMemberAsync(teamId, CurrentUserId, request)).ToActionResult(this);

        [HttpGet("invites/pending")]
        public async Task<IActionResult> GetPendingInvites()
            => Ok(await _teamService.GetPendingInvitesAsync(CurrentUserId));

        [HttpPost("invites/{inviteId}/accept")]
        public async Task<IActionResult> AcceptInvite(Guid inviteId)
            => (await _teamService.RespondToInviteAsync(inviteId, CurrentUserId, accept: true)).ToActionResult(this);

        [HttpPost("invites/{inviteId}/reject")]
        public async Task<IActionResult> RejectInvite(Guid inviteId)
            => (await _teamService.RespondToInviteAsync(inviteId, CurrentUserId, accept: false)).ToActionResult(this);

        [HttpDelete("{teamId}/invites/{inviteId}")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> CancelInvite(Guid teamId, Guid inviteId)
            => (await _teamService.CancelInviteAsync(teamId, inviteId, CurrentUserId)).ToActionResult(this);

        [HttpDelete("{teamId}/members/{studentCode}")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> RemoveMember(Guid teamId, string studentCode)
            => (await _teamService.RemoveMemberAsync(teamId, studentCode, CurrentUserId)).ToActionResult(this);

        [HttpPost("{teamId}/leave")]
        public async Task<IActionResult> LeaveTeam(Guid teamId)
            => (await _teamService.LeaveTeamAsync(teamId, CurrentUserId)).ToActionResult(this);

        [HttpPost("{teamId}/disband")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> DisbandTeam(Guid teamId)
            => (await _teamService.DisbandTeamAsync(teamId, CurrentUserId)).ToActionResult(this);

        [HttpPost("{teamId}/transfer-leadership")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> TransferLeadership(Guid teamId, [FromBody] TransferLeadershipRequest request)
            => (await _teamService.TransferLeadershipAsync(teamId, CurrentUserId, request)).ToActionResult(this);
    }
}
