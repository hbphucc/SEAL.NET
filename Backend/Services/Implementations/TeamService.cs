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
    public class TeamService : ITeamService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeamService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ServiceResult> CreateTeamAsync(Guid leaderId, CreateTeamRequest request)
        {
            var requestedStudentCodes = (request.MemberStudentCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpper())
                .Distinct()
                .ToList();

            var leader = await _userManager.FindByIdAsync(leaderId.ToString());
            if (leader == null)
                return ServiceResult.Unauthorized(new { message = "User not found." });

            if (!leader.IsApproved)
                return ServiceResult.BadRequest(new { message = "Your account has not been approved yet." });

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);

            if (category == null)
                return ServiceResult.NotFound(new { message = "Category not found." });

            var eventId = category.EventId;
            if (!category.Event!.IsPublished || category.Event.IsArchived)
                return ServiceResult.BadRequest(new { message = "Cannot create a team for an unpublished or archived event." });

            if (category.Event.RegistrationClosedAt != null)
                return ServiceResult.BadRequest(new { message = "Cannot create teams after registration is closed." });

            var members = await _context.Users
                .Where(u =>
                    u.StudentCode != null &&
                    requestedStudentCodes.Contains(u.StudentCode.ToUpper()))
                .ToListAsync();

            var foundStudentCodes = members
                .Where(u => u.StudentCode != null)
                .Select(u => u.StudentCode!.ToUpper())
                .ToList();

            var missingStudentCodes = requestedStudentCodes.Except(foundStudentCodes).ToList();

            if (missingStudentCodes.Any())
                return ServiceResult.BadRequest(new { message = $"One or more student codes are invalid or not found: {string.Join(", ", missingStudentCodes)}" });

            var allMemberIds = members
                .Select(u => u.Id)
                .Append(leaderId)
                .Distinct()
                .ToList();

            if (allMemberIds.Count < 3 || allMemberIds.Count > 5)
                return ServiceResult.BadRequest(new { message = "A team must have 3 to 5 members including the leader." });

            var existingUsers = await _context.Users
                .Where(u => allMemberIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            if (existingUsers.Count != allMemberIds.Count)
                return ServiceResult.BadRequest(new { message = "One or more members do not exist." });

            var unapprovedUsers = await _context.Users
                .Where(u => allMemberIds.Contains(u.Id) && !u.IsApproved)
                .Select(u => u.Email)
                .ToListAsync();

            if (unapprovedUsers.Any())
                return ServiceResult.BadRequest(new
                {
                    message = "One or more members have not been approved.",
                    users = unapprovedUsers
                });

            var categoryIdsInSameEvent = await _context.Categories
                .Where(c => c.EventId == eventId)
                .Select(c => c.CategoryId)
                .ToListAsync();

            var alreadyJoined = await _context.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm =>
                    allMemberIds.Contains(tm.UserId) &&
                    categoryIdsInSameEvent.Contains(tm.Team!.CategoryId))
                .Select(tm => new
                {
                    tm.UserId,
                    tm.User!.Email,
                    tm.Team!.TeamName
                })
                .ToListAsync();

            if (alreadyJoined.Any())
                return ServiceResult.BadRequest(new
                {
                    message = "One or more members already joined a team in this event.",
                    users = alreadyJoined
                });

            var duplicateTeamName = await _context.Teams.AnyAsync(t =>
                t.CategoryId == request.CategoryId &&
                t.TeamName.ToLower() == request.TeamName.ToLower());

            if (duplicateTeamName)
                return ServiceResult.BadRequest(new { message = "Team name already exists in this category." });

            var firstRound = await _context.Rounds
                .Where(r => r.EventId == eventId)
                .OrderBy(r => r.RoundOrder)
                .FirstOrDefaultAsync();

            var team = new Team
            {
                TeamName = request.TeamName,
                LeaderId = leaderId,
                CategoryId = request.CategoryId,
                CurrentRoundId = firstRound?.RoundId,
                Status = TeamStatus.Pending
            };

            _context.Teams.Add(team);

            foreach (var memberId in allMemberIds)
            {
                _context.TeamMembers.Add(new TeamMember
                {
                    TeamId = team.TeamId,
                    UserId = memberId,
                    IsLeader = memberId == leaderId,
                    Role = memberId == leaderId ? TeamMemberRole.Leader : TeamMemberRole.Member
                });
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new
            {
                message = "Team registered successfully and is waiting for approval.",
                teamId = team.TeamId,
                teamName = team.TeamName,
                status = team.Status.ToString()
            });
        }

        public async Task<TeamDetailDto?> GetMyTeamAsync(Guid userId)
        {
            var team = await _context.TeamMembers
                .Where(tm => tm.UserId == userId)
                .Include(tm => tm.Team!)
                    .ThenInclude(t => t.Category)
                .Include(tm => tm.Team!)
                    .ThenInclude(t => t.CurrentRound)
                .Include(tm => tm.Team!)
                    .ThenInclude(t => t.Members)
                        .ThenInclude(m => m.User)
                .Select(tm => tm.Team)
                .FirstOrDefaultAsync();

            if (team == null)
                return null;

            return new TeamDetailDto
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                Status = team.Status.ToString(),
                LeaderId = team.LeaderId,
                Category = new TeamCategoryInfo
                {
                    CategoryId = team.Category!.CategoryId,
                    CategoryName = team.Category.CategoryName
                },
                CurrentRound = team.CurrentRound == null ? null : new TeamRoundInfo
                {
                    RoundId = team.CurrentRound.RoundId,
                    RoundName = team.CurrentRound.RoundName
                },
                Members = team.Members.Select(m => new TeamMemberInfo
                {
                    UserId = m.UserId,
                    StudentCode = m.User!.StudentCode,
                    FullName = m.User.FullName,
                    Email = m.User.Email,
                    Role = m.Role.ToString(),
                    IsLeader = m.IsLeader
                }).ToList()
            };
        }

        public async Task<ServiceResult> UpdateTeamAsync(Guid teamId, Guid currentUserId, bool isAdmin, UpdateTeamRequest request)
        {
            var team = await _context.Teams
                .Include(t => t.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return ServiceResult.Forbidden();

            if (team.Category.Event.JudgingStartedAt != null && !isAdmin)
                return ServiceResult.BadRequest(new { message = "Team cannot be modified after judging starts." });

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);
            if (category == null)
                return ServiceResult.NotFound(new { message = "Category not found." });
            if (category.EventId != team.Category.EventId)
                return ServiceResult.BadRequest(new { message = "Team category must stay within the same event." });

            var duplicateTeamName = await _context.Teams.AnyAsync(t =>
                t.TeamId != teamId &&
                t.CategoryId == request.CategoryId &&
                t.TeamName.ToLower() == request.TeamName.ToLower());
            if (duplicateTeamName)
                return ServiceResult.BadRequest(new { message = "Team name already exists in this category." });

            team.TeamName = request.TeamName;
            team.Description = request.Description;
            team.CategoryId = request.CategoryId;
            await AddAuditAsync("TeamUpdated", "Team", teamId, null, currentUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Team updated successfully." });
        }

        public async Task<ServiceResult> AddMemberAsync(Guid teamId, Guid currentUserId, AddTeamMemberRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentCode))
                return ServiceResult.BadRequest(new { message = "Student code is required." });

            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return ServiceResult.Forbidden();

            if (team.Status != TeamStatus.Pending)
                return ServiceResult.BadRequest(new { message = "Use the invite flow to add members after initial team creation." });

            if (team.Members.Count >= 5)
                return ServiceResult.BadRequest(new { message = "A team can have maximum 5 members." });

            var user = await FindUserByStudentCodeAsync(request.StudentCode);

            if (user == null)
                return ServiceResult.NotFound(new { message = $"User with Student Code '{request.StudentCode}' was not found." });

            if (!user.IsApproved)
                return ServiceResult.BadRequest(new { message = "This user has not been approved yet." });

            if (user.Id == currentUserId)
                return ServiceResult.BadRequest(new { message = "Leader is already part of the team." });

            var alreadyInTeam = team.Members.Any(m => m.UserId == user.Id);

            if (alreadyInTeam)
                return ServiceResult.BadRequest(new { message = "User is already in this team." });

            var eventId = team.Category!.EventId;

            var categoryIdsInSameEvent = await _context.Categories
                .Where(c => c.EventId == eventId)
                .Select(c => c.CategoryId)
                .ToListAsync();

            var alreadyJoinedEvent = await _context.TeamMembers
                .Include(tm => tm.Team)
                .AnyAsync(tm =>
                    tm.UserId == user.Id &&
                    categoryIdsInSameEvent.Contains(tm.Team!.CategoryId));

            if (alreadyJoinedEvent)
                return ServiceResult.BadRequest(new { message = "User already joined another team in this event." });

            _context.TeamMembers.Add(new TeamMember
            {
                TeamId = team.TeamId,
                UserId = user.Id,
                IsLeader = false,
                Role = TeamMemberRole.Member
            });

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Member added successfully." });
        }

        public async Task<ServiceResult> InviteMemberAsync(Guid teamId, Guid currentUserId, InviteTeamMemberRequest request)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });
            if (team.LeaderId != currentUserId)
                return ServiceResult.Forbidden();
            if (team.Category.Event.RegistrationClosedAt != null || team.Category.Event.JudgingStartedAt != null)
                return ServiceResult.BadRequest(new { message = "Cannot invite members after registration closes or judging starts." });
            if (team.Members.Count >= 5)
                return ServiceResult.BadRequest(new { message = "A team can have maximum 5 members." });

            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(request.StudentCode))
            {
                user = await FindUserByStudentCodeAsync(request.StudentCode);
            }
            else if (!string.IsNullOrWhiteSpace(request.Email))
            {
                user = await _userManager.FindByEmailAsync(request.Email);
            }
            else
            {
                return ServiceResult.BadRequest(new { message = "Student code or email is required." });
            }

            if (user == null)
                return ServiceResult.NotFound(new { message = $"User with Student Code '{request.StudentCode}' was not found." });
            if (!user.IsApproved)
                return ServiceResult.BadRequest(new { message = "This user has not been approved yet." });
            if (user.Id == currentUserId)
                return ServiceResult.BadRequest(new { message = "Leader is already part of the team." });

            var categoryIdsInSameEvent = await _context.Categories
                .Where(c => c.EventId == team.Category.EventId)
                .Select(c => c.CategoryId)
                .ToListAsync();
            var alreadyJoinedEvent = await _context.TeamMembers
                .Include(tm => tm.Team)
                .AnyAsync(tm => tm.UserId == user.Id && categoryIdsInSameEvent.Contains(tm.Team!.CategoryId));
            if (alreadyJoinedEvent)
                return ServiceResult.BadRequest(new { message = "User already joined a team in this event." });

            var hasPendingInvite = await _context.TeamInvites.AnyAsync(i =>
                i.TeamId == teamId &&
                i.InvitedUserId == user.Id &&
                i.Status == TeamInviteStatus.Pending &&
                i.ExpiresAt > DateTime.UtcNow);
            if (hasPendingInvite)
                return ServiceResult.BadRequest(new { message = "User already has a pending invite for this team." });

            var invite = new TeamInvite
            {
                TeamId = teamId,
                InvitedUserId = user.Id,
                InvitedByUserId = currentUserId
            };

            _context.TeamInvites.Add(invite);
            AddNotification(user.Id, "InviteReceived", "Team invite received", $"You were invited to join {team.TeamName}.", "/my-team");
            await AddAuditAsync("TeamInviteCreated", "TeamInvite", invite.TeamInviteId, $"TeamId={teamId}", currentUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Invite sent successfully.", invite.TeamInviteId });
        }

        public async Task<List<PendingInviteDto>> GetPendingInvitesAsync(Guid userId)
        {
            var now = DateTime.UtcNow;
            return await _context.TeamInvites
                .Include(i => i.Team)
                    .ThenInclude(t => t.Category)
                .Where(i => i.InvitedUserId == userId && i.Status == TeamInviteStatus.Pending && i.ExpiresAt > now)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new PendingInviteDto
                {
                    TeamInviteId = i.TeamInviteId,
                    CreatedAt = i.CreatedAt,
                    ExpiresAt = i.ExpiresAt,
                    Status = i.Status.ToString(),
                    Team = new PendingInviteTeamInfo
                    {
                        TeamId = i.Team.TeamId,
                        TeamName = i.Team.TeamName,
                        Category = i.Team.Category.CategoryName
                    }
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> CancelInviteAsync(Guid teamId, Guid inviteId, Guid currentUserId)
        {
            var invite = await _context.TeamInvites
                .Include(i => i.Team)
                .FirstOrDefaultAsync(i => i.TeamInviteId == inviteId && i.TeamId == teamId);
            if (invite == null)
                return ServiceResult.NotFound(new { message = "Invite not found." });
            if (invite.Team.LeaderId != currentUserId)
                return ServiceResult.Forbidden();
            if (invite.Status != TeamInviteStatus.Pending)
                return ServiceResult.BadRequest(new { message = "Only pending invites can be cancelled." });

            invite.Status = TeamInviteStatus.Cancelled;
            invite.RespondedAt = DateTime.UtcNow;
            AddNotification(invite.InvitedUserId, "InviteCancelled", "Team invite cancelled", $"The invite to join {invite.Team.TeamName} was cancelled.", "/my-team");
            await AddAuditAsync("TeamInviteCancelled", "TeamInvite", invite.TeamInviteId, null, currentUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Invite cancelled successfully." });
        }

        public async Task<ServiceResult> RemoveMemberAsync(Guid teamId, string studentCode, Guid currentUserId)
        {
            var user = await FindUserByStudentCodeAsync(studentCode);
            if (user == null)
                return ServiceResult.NotFound(new { message = $"User with Student Code '{studentCode}' was not found." });

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return ServiceResult.Forbidden();

            if (team.Status != TeamStatus.Pending)
                return ServiceResult.BadRequest(new { message = "Cannot modify members after team approval." });

            if (team.LeaderId == user.Id)
                return ServiceResult.BadRequest(new { message = "Leader cannot be removed from the team." });

            var member = team.Members.FirstOrDefault(m => m.UserId == user.Id);
            if (member == null)
                return ServiceResult.NotFound(new { message = "Member not found in this team." });

            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Member removed successfully." });
        }

        public async Task<ServiceResult> LeaveTeamAsync(Guid teamId, Guid currentUserId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);
            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });
            if (team.Category.Event.JudgingStartedAt != null)
                return ServiceResult.BadRequest(new { message = "Team membership cannot be changed after judging starts." });
            if (team.LeaderId == currentUserId)
                return ServiceResult.BadRequest(new { message = "Leader must transfer leadership or disband the team before leaving." });

            var member = team.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (member == null)
                return ServiceResult.NotFound(new { message = "You are not a member of this team." });

            _context.TeamMembers.Remove(member);
            await AddAuditAsync("TeamLeft", "Team", teamId, null, currentUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Left team successfully." });
        }

        public async Task<ServiceResult> DisbandTeamAsync(Guid teamId, Guid currentUserId)
        {
            var team = await _context.Teams
                .Include(t => t.Submissions)
                .Include(t => t.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);
            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });
            if (team.LeaderId != currentUserId)
                return ServiceResult.Forbidden();
            if (team.Category.Event.JudgingStartedAt != null)
                return ServiceResult.BadRequest(new { message = "Team cannot be disbanded after judging starts." });
            if (team.Submissions.Any())
                return ServiceResult.BadRequest(new { message = "Cannot disband a team with submissions. Withdraw submissions first." });

            team.Status = TeamStatus.Withdrawn;
            team.StatusReason = "Disbanded by leader";
            await AddAuditAsync("TeamDisbanded", "Team", teamId, null, currentUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Team disbanded successfully." });
        }

        public async Task<ServiceResult> TransferLeadershipAsync(Guid teamId, Guid currentUserId, TransferLeadershipRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewLeaderStudentCode))
                return ServiceResult.BadRequest(new { message = "New leader student code is required." });

            var normalizedStudentCode = request.NewLeaderStudentCode.Trim().ToUpper();

            var team = await _context.Teams
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return ServiceResult.Forbidden();

            var newLeader = team.Members.FirstOrDefault(m =>
                m.User != null &&
                m.User.StudentCode != null &&
                m.User.StudentCode.ToUpper() == normalizedStudentCode);

            if (newLeader == null)
                return ServiceResult.BadRequest(new { message = "New leader must be a current team member." });

            if (newLeader.UserId == currentUserId)
                return ServiceResult.BadRequest(new { message = "You are already the team leader." });

            foreach (var member in team.Members)
            {
                member.IsLeader = member.UserId == newLeader.UserId;
                member.Role = member.IsLeader
                    ? TeamMemberRole.Leader
                    : TeamMemberRole.Member;
            }

            team.LeaderId = newLeader.UserId;

            await AddAuditAsync(
                "TeamLeadershipTransferred",
                "Team",
                teamId,
                $"NewLeaderStudentCode={normalizedStudentCode}",
                currentUserId
            );

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Leadership transferred successfully." });
        }

        public async Task<ServiceResult> RespondToInviteAsync(Guid inviteId, Guid currentUserId, bool accept)
        {
            var invite = await _context.TeamInvites
                .Include(i => i.Team)
                    .ThenInclude(t => t.Members)
                .Include(i => i.Team)
                    .ThenInclude(t => t.Category)
                        .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(i => i.TeamInviteId == inviteId && i.InvitedUserId == currentUserId);

            if (invite == null)
                return ServiceResult.NotFound(new { message = "Invite not found." });
            if (invite.Status != TeamInviteStatus.Pending || invite.ExpiresAt <= DateTime.UtcNow)
                return ServiceResult.BadRequest(new { message = "Invite is no longer pending." });

            if (!accept)
            {
                invite.Status = TeamInviteStatus.Rejected;
                invite.RespondedAt = DateTime.UtcNow;
                AddNotification(invite.Team.LeaderId, "InviteRejected", "Team invite rejected", "A user rejected your team invite.", "/my-team");
                await AddAuditAsync("TeamInviteRejected", "TeamInvite", inviteId, null, currentUserId);
                await _context.SaveChangesAsync();
                return ServiceResult.Ok(new { message = "Invite rejected." });
            }

            if (invite.Team.Category.Event.RegistrationClosedAt != null || invite.Team.Category.Event.JudgingStartedAt != null)
                return ServiceResult.BadRequest(new { message = "Cannot accept invites after registration closes or judging starts." });
            if (invite.Team.Members.Count >= 5)
                return ServiceResult.BadRequest(new { message = "Team is already full." });

            var categoryIdsInSameEvent = await _context.Categories
                .Where(c => c.EventId == invite.Team.Category.EventId)
                .Select(c => c.CategoryId)
                .ToListAsync();
            var alreadyJoinedEvent = await _context.TeamMembers
                .Include(tm => tm.Team)
                .AnyAsync(tm => tm.UserId == currentUserId && categoryIdsInSameEvent.Contains(tm.Team!.CategoryId));
            if (alreadyJoinedEvent)
                return ServiceResult.BadRequest(new { message = "You already joined a team in this event." });

            invite.Status = TeamInviteStatus.Accepted;
            invite.RespondedAt = DateTime.UtcNow;
            _context.TeamMembers.Add(new TeamMember
            {
                TeamId = invite.TeamId,
                UserId = currentUserId,
                IsLeader = false,
                Role = TeamMemberRole.Member
            });
            AddNotification(invite.Team.LeaderId, "InviteAccepted", "Team invite accepted", "A user accepted your team invite.", "/my-team");
            await AddAuditAsync("TeamInviteAccepted", "TeamInvite", inviteId, null, currentUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Invite accepted." });
        }

        private async Task<ApplicationUser?> FindUserByStudentCodeAsync(string studentCode)
        {
            var normalizedStudentCode = studentCode.Trim().ToUpper();

            return await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.StudentCode != null &&
                    u.StudentCode.ToUpper() == normalizedStudentCode);
        }

        private void AddNotification(Guid userId, string type, string title, string? message, string? link)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Link = link
            });
        }

        private async Task AddAuditAsync(string action, string entityType, Guid entityId, string? details, Guid actorUserId)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                ActorUserId = actorUserId
            });
            await Task.CompletedTask;
        }
    }
}
