using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Team;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/teams")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminTeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminTeamsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task RemoveTeamLeaderRoleIfNoApprovedTeamsAsync(Guid leaderId)
        {
            var hasApprovedTeam = await _context.Teams.AnyAsync(t =>
                t.LeaderId == leaderId &&
                t.Status == TeamStatus.Approved);

            if (hasApprovedTeam)
                return;

            var leader = await _userManager.FindByIdAsync(leaderId.ToString());
            if (leader == null || !await _userManager.IsInRoleAsync(leader, "TeamLeader"))
                return;

            await _userManager.RemoveFromRoleAsync(leader, "TeamLeader");
            await _userManager.UpdateSecurityStampAsync(leader);
        }

        [HttpGet]
        public async Task<IActionResult> GetTeams()
        {
            var teams = await _context.Teams
                .Include(t => t.Category)
                .Include(t => t.CurrentRound)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.TeamId,
                    t.TeamName,
                    t.LeaderId,
                    t.CreatedAt,
                    t.StatusReason,
                    t.EliminationReason,
                    t.EliminatedAt,
                    status = t.Status.ToString(),
                    category = new
                    {
                        t.Category!.CategoryId,
                        t.Category.CategoryName
                    },
                    currentRound = t.CurrentRound == null ? null : new
                    {
                        t.CurrentRound.RoundId,
                        t.CurrentRound.RoundName
                    },
                    members = t.Members.Select(m => new
                    {
                        m.UserId,
                        m.User!.FullName,
                        m.User.Email
                    })
                })
                .ToListAsync();

            return Ok(teams);
        }

        [HttpPut("{teamId}/approve")]
        public async Task<IActionResult> ApproveTeam(Guid teamId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.Members.Count < 3 || team.Members.Count > 5)
                return BadRequest(new { message = "Team must have 3 to 5 members before approval." });

            team.Status = TeamStatus.Approved;
            team.StatusReason = null;

            foreach (var member in team.Members)
            {
                member.IsLeader = member.UserId == team.LeaderId;
                member.Role = member.IsLeader ? TeamMemberRole.Leader : TeamMemberRole.Member;
                _context.Notifications.Add(new Notification
                {
                    UserId = member.UserId,
                    Type = "TeamApproved",
                    Title = "Team approved",
                    Message = $"{team.TeamName} has been approved.",
                    Link = "/my-team"
                });
            }

            var leader = await _userManager.FindByIdAsync(team.LeaderId.ToString());
            if (leader != null && !await _userManager.IsInRoleAsync(leader, "TeamLeader"))
            {
                await _userManager.AddToRoleAsync(leader, "TeamLeader");
                await _userManager.UpdateSecurityStampAsync(leader);
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "TeamApproved",
                EntityType = "Team",
                EntityId = teamId,
                ActorUserId = GetCurrentUserIdOrNull()
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Team approved successfully." });
        }

        [HttpPut("{teamId}/reject")]
        public async Task<IActionResult> RejectTeam(Guid teamId, [FromBody] TeamDecisionRequest? request = null)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.Status != TeamStatus.Pending)
                return BadRequest(new { message = "Only pending teams can be rejected." });

            team.Status = TeamStatus.Rejected;
            team.StatusReason = request?.Reason;
            team.EliminationReason = null;
            team.EliminatedAt = null;

            foreach (var member in team.Members)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = member.UserId,
                    Type = "TeamRejected",
                    Title = "Team rejected",
                    Message = request?.Reason,
                    Link = "/my-team"
                });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "TeamRejected",
                EntityType = "Team",
                EntityId = teamId,
                Details = request?.Reason,
                ActorUserId = GetCurrentUserIdOrNull()
            });

            await _context.SaveChangesAsync();
            await RemoveTeamLeaderRoleIfNoApprovedTeamsAsync(team.LeaderId);

            return Ok(new { message = "Team rejected successfully." });
        }
        [HttpPut("{teamId}/eliminate")]
        public async Task<IActionResult> EliminateTeam(Guid teamId, [FromBody] EliminateTeamRequest request)
        {
            var team = await _context.Teams
                .Include(t => t.Category)
                .Include(t => t.CurrentRound)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.Status == TeamStatus.Eliminated)
                return BadRequest(new { message = "Team is already eliminated." });

            if (team.Status == TeamStatus.Pending || team.Status == TeamStatus.Rejected)
                return BadRequest(new { message = "Only approved or active teams can be eliminated." });

            team.Status = TeamStatus.Eliminated;
            team.EliminationReason = request.Reason;
            team.StatusReason = request.Reason;
            team.EliminatedAt = DateTime.UtcNow;

            var members = await _context.TeamMembers.Where(m => m.TeamId == teamId).ToListAsync();
            foreach (var member in members)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = member.UserId,
                    Type = "TeamEliminated",
                    Title = "Team eliminated",
                    Message = request.Reason,
                    Link = "/my-team"
                });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "TeamEliminated",
                EntityType = "Team",
                EntityId = teamId,
                Details = request.Reason,
                ActorUserId = GetCurrentUserIdOrNull()
            });

            await _context.SaveChangesAsync();
            await RemoveTeamLeaderRoleIfNoApprovedTeamsAsync(team.LeaderId);

            return Ok(new
            {
                message = "Team eliminated successfully.",
                team = new
                {
                    team.TeamId,
                    team.TeamName,
                    status = team.Status.ToString(),
                    team.EliminationReason,
                    team.EliminatedAt,
                    category = team.Category == null ? null : new
                    {
                        team.Category.CategoryId,
                        team.Category.CategoryName
                    },
                    currentRound = team.CurrentRound == null ? null : new
                    {
                        team.CurrentRound.RoundId,
                        team.CurrentRound.RoundName
                    }
                }
            });
        }

        [HttpDelete("{teamId}")]
        public async Task<IActionResult> DeleteTeam(Guid teamId)
        {
            var team = await _context.Teams
                .Include(t => t.Submissions)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.Submissions.Any())
                return BadRequest(new { message = "Cannot delete team because it already has submissions." });

            var leaderId = team.LeaderId;

            _context.Teams.Remove(team);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "TeamDeleted",
                EntityType = "Team",
                EntityId = teamId,
                ActorUserId = GetCurrentUserIdOrNull()
            });
            await _context.SaveChangesAsync();
            await RemoveTeamLeaderRoleIfNoApprovedTeamsAsync(leaderId);

            return Ok(new { message = "Team deleted successfully." });
        }

        private Guid? GetCurrentUserIdOrNull()
        {
            var value = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
