using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Team;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class AdminTeamService : IAdminTeamService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminTeamService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<AdminTeamDto>> GetTeamsAsync()
        {
            return await _context.Teams
                .Include(t => t.Category)
                .Include(t => t.CurrentRound)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new AdminTeamDto
                {
                    TeamId = t.TeamId,
                    TeamName = t.TeamName,
                    LeaderId = t.LeaderId,
                    CreatedAt = t.CreatedAt,
                    EliminationReason = t.EliminationReason,
                    EliminatedAt = t.EliminatedAt,
                    Status = t.Status.ToString(),
                    Category = new AdminTeamCategoryInfo
                    {
                        CategoryId = t.Category!.CategoryId,
                        CategoryName = t.Category.CategoryName
                    },
                    CurrentRound = t.CurrentRound == null ? null : new AdminTeamRoundInfo
                    {
                        RoundId = t.CurrentRound.RoundId,
                        RoundName = t.CurrentRound.RoundName
                    },
                    Members = t.Members.Select(m => new AdminTeamMemberInfo
                    {
                        UserId = m.UserId,
                        FullName = m.User!.FullName,
                        Email = m.User.Email
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> ApproveTeamAsync(Guid teamId, Guid? actorUserId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            if (team.Members.Count < 3 || team.Members.Count > 5)
                return ServiceResult.BadRequest(new { message = "Team must have 3 to 5 members before approval." });

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
                ActorUserId = actorUserId
            });

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Team approved successfully." });
        }

        public async Task<ServiceResult> RejectTeamAsync(Guid teamId, TeamDecisionRequest? request, Guid? actorUserId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            team.Status = TeamStatus.Eliminated;
            team.StatusReason = request?.Reason;
            team.EliminationReason = request?.Reason;
            team.EliminatedAt = DateTime.UtcNow;

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
                ActorUserId = actorUserId
            });

            await _context.SaveChangesAsync();
            await RemoveTeamLeaderRoleIfNoApprovedTeamsAsync(team.LeaderId);

            return ServiceResult.Ok(new { message = "Team rejected successfully." });
        }

        public async Task<ServiceResult> EliminateTeamAsync(Guid teamId, EliminateTeamRequest request, Guid? actorUserId)
        {
            var team = await _context.Teams
                .Include(t => t.Category)
                .Include(t => t.CurrentRound)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            if (team.Status == TeamStatus.Eliminated)
                return ServiceResult.BadRequest(new { message = "Team is already eliminated." });

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
                ActorUserId = actorUserId
            });

            await _context.SaveChangesAsync();
            await RemoveTeamLeaderRoleIfNoApprovedTeamsAsync(team.LeaderId);

            return ServiceResult.Ok(new
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

        public async Task<ServiceResult> DeleteTeamAsync(Guid teamId, Guid? actorUserId)
        {
            var team = await _context.Teams
                .Include(t => t.Submissions)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            if (team.Submissions.Any())
                return ServiceResult.BadRequest(new { message = "Cannot delete team because it already has submissions." });

            var leaderId = team.LeaderId;

            _context.Teams.Remove(team);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "TeamDeleted",
                EntityType = "Team",
                EntityId = teamId,
                ActorUserId = actorUserId
            });
            await _context.SaveChangesAsync();
            await RemoveTeamLeaderRoleIfNoApprovedTeamsAsync(leaderId);

            return ServiceResult.Ok(new { message = "Team deleted successfully." });
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
    }
}
