using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Team;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [Route("api/teams")]
    [ApiController]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeamsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private Guid GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId!);
        }

        private async Task<ApplicationUser?> FindUserByStudentCodeAsync(string studentCode)
        {
            var normalizedStudentCode = studentCode.Trim().ToUpper();

            return await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.StudentCode != null &&
                    u.StudentCode.ToUpper() == normalizedStudentCode);
        }

        private static TeamMemberResponseDto MapTeamMemberResponse(TeamMember member)
        {
            return new TeamMemberResponseDto
            {
                UserId = member.UserId,
                StudentCode = member.User!.StudentCode,
                FullName = member.User.FullName,
                Email = member.User.Email ?? string.Empty,
                Role = member.Role.ToString(),
                IsLeader = member.IsLeader
            };
        }

        private static TeamResponseDto MapTeamResponse(Team team)
        {
            return new TeamResponseDto
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                Status = team.Status.ToString(),
                StatusReason = team.StatusReason,
                EliminationReason = team.EliminationReason,
                EliminatedAt = team.EliminatedAt,
                LeaderId = team.LeaderId,
                Category = new TeamCategoryResponseDto
                {
                    CategoryId = team.Category!.CategoryId,
                    CategoryName = team.Category.CategoryName
                },
                CurrentRound = team.CurrentRound == null ? null : new TeamRoundResponseDto
                {
                    RoundId = team.CurrentRound.RoundId,
                    RoundName = team.CurrentRound.RoundName
                },
                Members = team.Members.Select(MapTeamMemberResponse).ToList()
            };
        }

        private async Task AddTeamMemberDeniedAuditAsync(Guid teamId, string studentCode, string outcome)
        {
            await AddAuditAsync(
                "TeamMemberAddDenied",
                "Team",
                teamId,
                $"StudentCode={studentCode};Outcome={outcome}"
            );
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
        {
            var leaderId = GetCurrentUserId();

            var requestedStudentCodes = (request.MemberStudentCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpper())
                .Distinct()
                .ToList();

            var leader = await _userManager.FindByIdAsync(leaderId.ToString());
            if (leader == null)
                return Unauthorized(new { message = "User not found." });

            if (!leader.IsApproved)
                return BadRequest(new { message = "Your account has not been approved yet." });

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            var eventId = category.EventId;
            if (!category.Event!.IsPublished || category.Event.IsArchived)
                return BadRequest(new { message = "Cannot create a team for an unpublished or archived event." });

            if (category.Event.RegistrationClosedAt != null)
                return BadRequest(new { message = "Cannot create teams after registration is closed." });

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
                return BadRequest(new { message = $"One or more student codes are invalid or not found: {string.Join(", ", missingStudentCodes)}" });

            var allMemberIds = members
                .Select(u => u.Id)
                .Append(leaderId)
                .Distinct()
                .ToList();

            if (allMemberIds.Count < 3 || allMemberIds.Count > 5)
                return BadRequest(new { message = "A team must have 3 to 5 members including the leader." });

            var existingUsers = await _context.Users
                .Where(u => allMemberIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            if (existingUsers.Count != allMemberIds.Count)
                return BadRequest(new { message = "One or more members do not exist." });

            var unapprovedUsers = await _context.Users
                .Where(u => allMemberIds.Contains(u.Id) && !u.IsApproved)
                .Select(u => u.Email)
                .ToListAsync();

            if (unapprovedUsers.Any())
                return BadRequest(new
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
                return BadRequest(new
                {
                    message = "One or more members already joined a team in this event.",
                    users = alreadyJoined
                });

            var duplicateTeamName = await _context.Teams.AnyAsync(t =>
                t.CategoryId == request.CategoryId &&
                t.TeamName.ToLower() == request.TeamName.ToLower());

            if (duplicateTeamName)
                return BadRequest(new { message = "Team name already exists in this category." });

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

            return Ok(new
            {
                message = "Team registered successfully and is waiting for approval.",
                teamId = team.TeamId,
                teamName = team.TeamName,
                status = team.Status.ToString()
            });
        }

        [HttpGet("my-team")]
        public async Task<IActionResult> GetMyTeam()
        {
            var userId = GetCurrentUserId();

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
                return Ok(null);

            return Ok(MapTeamResponse(team));
        }

        [HttpPut("{teamId}")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> UpdateTeam(Guid teamId, [FromBody] UpdateTeamRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var team = await _context.Teams
                .Include(t => t.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return Forbid();

            if (team.Category.Event.JudgingStartedAt != null && !User.IsInRole("Admin"))
                return BadRequest(new { message = "Team cannot be modified after judging starts." });

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);
            if (category == null)
                return NotFound(new { message = "Category not found." });
            if (category.EventId != team.Category.EventId)
                return BadRequest(new { message = "Team category must stay within the same event." });

            var duplicateTeamName = await _context.Teams.AnyAsync(t =>
                t.TeamId != teamId &&
                t.CategoryId == request.CategoryId &&
                t.TeamName.ToLower() == request.TeamName.ToLower());
            if (duplicateTeamName)
                return BadRequest(new { message = "Team name already exists in this category." });

            team.TeamName = request.TeamName;
            team.Description = request.Description;
            team.CategoryId = request.CategoryId;
            await AddAuditAsync("TeamUpdated", "Team", teamId, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Team updated successfully." });
        }

        [HttpPost("{teamId}/members")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> AddMember(Guid teamId, [FromBody] AddTeamMemberRequest request)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(request.StudentCode))
                return BadRequest(new { message = "Student code is required." });

            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return Forbid();

            if (team.Status != TeamStatus.Pending)
                return BadRequest(new { message = "Use the invite flow to add members after initial team creation." });

            if (team.Members.Count >= 5)
                return BadRequest(new { message = "A team can have maximum 5 members." });

            var user = await FindUserByStudentCodeAsync(request.StudentCode);

            if (user == null)
                return NotFound(new { message = $"User with Student Code '{request.StudentCode}' was not found." });

            if (!user.IsApproved)
                return BadRequest(new { message = "This user has not been approved yet." });

            if (user.Id == currentUserId)
                return BadRequest(new { message = "Leader is already part of the team." });

            var alreadyInTeam = team.Members.Any(m => m.UserId == user.Id);

            if (alreadyInTeam)
                return BadRequest(new { message = "User is already in this team." });

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
                return BadRequest(new { message = "User already joined another team in this event." });

            _context.TeamMembers.Add(new TeamMember
            {
                TeamId = team.TeamId,
                UserId = user.Id,
                IsLeader = false,
                Role = TeamMemberRole.Member
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Member added successfully." });
        }

        [HttpPost("my-team/members")]
        [Authorize(Roles = "TeamLeader")]
        public async Task<IActionResult> AddMemberToMyTeam([FromBody] AddTeamMemberRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var studentCode = request.StudentCode?.Trim();

            if (string.IsNullOrWhiteSpace(studentCode))
                return BadRequest(new { message = "Student code is required." });

            var team = await _context.Teams
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .Include(t => t.Category)
                .Include(t => t.CurrentRound)
                .FirstOrDefaultAsync(t => t.LeaderId == currentUserId);

            if (team == null)
            {
                await AddTeamMemberDeniedAuditAsync(Guid.Empty, studentCode, "NoLedTeam");
                return NotFound(new { message = "Team not found." });
            }

            if (team.Status != TeamStatus.Pending)
            {
                await AddTeamMemberDeniedAuditAsync(team.TeamId, studentCode, "TeamLocked");
                return BadRequest(new { message = "Use the invite flow to add members after initial team creation." });
            }

            if (team.Members.Count >= 5)
            {
                await AddTeamMemberDeniedAuditAsync(team.TeamId, studentCode, "TeamFull");
                return BadRequest(new { message = "A team can have maximum 5 members." });
            }

            var user = await FindUserByStudentCodeAsync(studentCode);

            if (user == null)
            {
                await AddTeamMemberDeniedAuditAsync(team.TeamId, studentCode, "StudentCodeNotFound");
                return NotFound(new { message = $"User with Student Code '{studentCode}' was not found." });
            }

            if (!user.IsApproved)
            {
                await AddTeamMemberDeniedAuditAsync(team.TeamId, studentCode, "UnapprovedUser");
                return BadRequest(new { message = "This user has not been approved yet." });
            }

            if (user.Id == currentUserId)
            {
                await AddTeamMemberDeniedAuditAsync(team.TeamId, studentCode, "LeaderSelfAdd");
                return BadRequest(new { message = "Leader is already part of the team." });
            }

            var alreadyInTeam = team.Members.Any(m => m.UserId == user.Id);

            if (alreadyInTeam)
            {
                await AddTeamMemberDeniedAuditAsync(team.TeamId, studentCode, "DuplicateMember");
                return BadRequest(new { message = "User is already in this team." });
            }

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
            {
                await AddTeamMemberDeniedAuditAsync(team.TeamId, studentCode, "SameEventTeamConflict");
                return BadRequest(new { message = "User already joined another team in this event." });
            }

            var member = new TeamMember
            {
                TeamId = team.TeamId,
                UserId = user.Id,
                User = user,
                IsLeader = false,
                Role = TeamMemberRole.Member
            };

            _context.TeamMembers.Add(member);
            team.Members.Add(member);

            await AddAuditAsync(
                "TeamMemberAdded",
                "Team",
                team.TeamId,
                $"StudentCode={studentCode};Outcome=Success"
            );

            await _context.SaveChangesAsync();

            return Ok(new AddTeamMemberResponse
            {
                Message = "Member added successfully.",
                Team = MapTeamResponse(team),
                AddedMember = MapTeamMemberResponse(member)
            });
        }

        [HttpPost("{teamId}/invites")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> InviteMember(Guid teamId, [FromBody] InviteTeamMemberRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });
            if (team.LeaderId != currentUserId)
                return Forbid();
            if (team.Category.Event.RegistrationClosedAt != null || team.Category.Event.JudgingStartedAt != null)
                return BadRequest(new { message = "Cannot invite members after registration closes or judging starts." });
            if (team.Members.Count >= 5)
                return BadRequest(new { message = "A team can have maximum 5 members." });

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
                return BadRequest(new { message = "Student code or email is required." });
            }

            if (user == null)
                return NotFound(new { message = $"User with Student Code '{request.StudentCode}' was not found." });
            if (!user.IsApproved)
                return BadRequest(new { message = "This user has not been approved yet." });
            if (user.Id == currentUserId)
                return BadRequest(new { message = "Leader is already part of the team." });

            var categoryIdsInSameEvent = await _context.Categories
                .Where(c => c.EventId == team.Category.EventId)
                .Select(c => c.CategoryId)
                .ToListAsync();
            var alreadyJoinedEvent = await _context.TeamMembers
                .Include(tm => tm.Team)
                .AnyAsync(tm => tm.UserId == user.Id && categoryIdsInSameEvent.Contains(tm.Team!.CategoryId));
            if (alreadyJoinedEvent)
                return BadRequest(new { message = "User already joined a team in this event." });

            var hasPendingInvite = await _context.TeamInvites.AnyAsync(i =>
                i.TeamId == teamId &&
                i.InvitedUserId == user.Id &&
                i.Status == TeamInviteStatus.Pending &&
                i.ExpiresAt > DateTime.UtcNow);
            if (hasPendingInvite)
                return BadRequest(new { message = "User already has a pending invite for this team." });

            var invite = new TeamInvite
            {
                TeamId = teamId,
                InvitedUserId = user.Id,
                InvitedByUserId = currentUserId
            };

            _context.TeamInvites.Add(invite);
            AddNotification(user.Id, "InviteReceived", "Team invite received", $"You were invited to join {team.TeamName}.", "/my-team");
            await AddAuditAsync("TeamInviteCreated", "TeamInvite", invite.TeamInviteId, $"TeamId={teamId}");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Invite sent successfully.", invite.TeamInviteId });
        }

        [HttpGet("invites/pending")]
        public async Task<IActionResult> GetPendingInvites()
        {
            var currentUserId = GetCurrentUserId();
            var now = DateTime.UtcNow;
            var invites = await _context.TeamInvites
                .Include(i => i.Team)
                    .ThenInclude(t => t.Category)
                .Where(i => i.InvitedUserId == currentUserId && i.Status == TeamInviteStatus.Pending && i.ExpiresAt > now)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.TeamInviteId,
                    i.CreatedAt,
                    i.ExpiresAt,
                    status = i.Status.ToString(),
                    team = new
                    {
                        i.Team.TeamId,
                        i.Team.TeamName,
                        category = i.Team.Category.CategoryName
                    }
                })
                .ToListAsync();

            return Ok(invites);
        }

        [HttpPost("invites/{inviteId}/accept")]
        public async Task<IActionResult> AcceptInvite(Guid inviteId)
        {
            return await RespondToInvite(inviteId, true);
        }

        [HttpPost("invites/{inviteId}/reject")]
        public async Task<IActionResult> RejectInvite(Guid inviteId)
        {
            return await RespondToInvite(inviteId, false);
        }

        [HttpDelete("{teamId}/invites/{inviteId}")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> CancelInvite(Guid teamId, Guid inviteId)
        {
            var currentUserId = GetCurrentUserId();
            var invite = await _context.TeamInvites
                .Include(i => i.Team)
                .FirstOrDefaultAsync(i => i.TeamInviteId == inviteId && i.TeamId == teamId);
            if (invite == null)
                return NotFound(new { message = "Invite not found." });
            if (invite.Team.LeaderId != currentUserId)
                return Forbid();
            if (invite.Status != TeamInviteStatus.Pending)
                return BadRequest(new { message = "Only pending invites can be cancelled." });

            invite.Status = TeamInviteStatus.Cancelled;
            invite.RespondedAt = DateTime.UtcNow;
            AddNotification(invite.InvitedUserId, "InviteCancelled", "Team invite cancelled", $"The invite to join {invite.Team.TeamName} was cancelled.", "/my-team");
            await AddAuditAsync("TeamInviteCancelled", "TeamInvite", invite.TeamInviteId, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Invite cancelled successfully." });
        }

        [HttpDelete("{teamId}/members/{studentCode}")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> RemoveMember(Guid teamId, string studentCode)
        {
            var currentUserId = GetCurrentUserId();

            var user = await FindUserByStudentCodeAsync(studentCode);
            if (user == null)
                return NotFound(new { message = $"User with Student Code '{studentCode}' was not found." });

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return Forbid();

            if (team.Status != TeamStatus.Pending)
                return BadRequest(new { message = "Cannot modify members after team approval." });

            if (team.LeaderId == user.Id)
                return BadRequest(new { message = "Leader cannot be removed from the team." });

            var member = team.Members.FirstOrDefault(m => m.UserId == user.Id);
            if (member == null)
                return NotFound(new { message = "Member not found in this team." });

            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Member removed successfully." });
        }

        [HttpPost("{teamId}/leave")]
        public async Task<IActionResult> LeaveTeam(Guid teamId)
        {
            var currentUserId = GetCurrentUserId();
            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);
            if (team == null)
                return NotFound(new { message = "Team not found." });
            if (team.Category.Event.JudgingStartedAt != null)
                return BadRequest(new { message = "Team membership cannot be changed after judging starts." });
            if (team.LeaderId == currentUserId)
                return BadRequest(new { message = "Leader must transfer leadership or disband the team before leaving." });

            var member = team.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (member == null)
                return NotFound(new { message = "You are not a member of this team." });

            _context.TeamMembers.Remove(member);
            await AddAuditAsync("TeamLeft", "Team", teamId, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Left team successfully." });
        }

        [HttpPost("{teamId}/disband")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> DisbandTeam(Guid teamId)
        {
            var currentUserId = GetCurrentUserId();
            var team = await _context.Teams
                .Include(t => t.Submissions)
                .Include(t => t.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);
            if (team == null)
                return NotFound(new { message = "Team not found." });
            if (team.LeaderId != currentUserId)
                return Forbid();
            if (team.Category.Event.JudgingStartedAt != null)
                return BadRequest(new { message = "Team cannot be disbanded after judging starts." });
            if (team.Submissions.Any())
                return BadRequest(new { message = "Cannot disband a team with submissions. Withdraw submissions first." });

            team.Status = TeamStatus.Withdrawn;
            team.StatusReason = "Disbanded by leader";
            await AddAuditAsync("TeamDisbanded", "Team", teamId, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Team disbanded successfully." });
        }

        [HttpPost("{teamId}/transfer-leadership")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> TransferLeadership(Guid teamId, [FromBody] TransferLeadershipRequest request)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(request.NewLeaderStudentCode))
                return BadRequest(new { message = "New leader student code is required." });

            var normalizedStudentCode = request.NewLeaderStudentCode.Trim().ToUpper();

            var team = await _context.Teams
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return Forbid();

            var newLeader = team.Members.FirstOrDefault(m =>
                m.User != null &&
                m.User.StudentCode != null &&
                m.User.StudentCode.ToUpper() == normalizedStudentCode);

            if (newLeader == null)
                return BadRequest(new { message = "New leader must be a current team member." });

            if (newLeader.UserId == currentUserId)
                return BadRequest(new { message = "You are already the team leader." });

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
                $"NewLeaderStudentCode={normalizedStudentCode}"
            );

            await _context.SaveChangesAsync();

            return Ok(new { message = "Leadership transferred successfully." });
        }

        private async Task<IActionResult> RespondToInvite(Guid inviteId, bool accept)
        {
            var currentUserId = GetCurrentUserId();
            var invite = await _context.TeamInvites
                .Include(i => i.Team)
                    .ThenInclude(t => t.Members)
                .Include(i => i.Team)
                    .ThenInclude(t => t.Category)
                        .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(i => i.TeamInviteId == inviteId && i.InvitedUserId == currentUserId);

            if (invite == null)
                return NotFound(new { message = "Invite not found." });
            if (invite.Status != TeamInviteStatus.Pending || invite.ExpiresAt <= DateTime.UtcNow)
                return BadRequest(new { message = "Invite is no longer pending." });

            if (!accept)
            {
                invite.Status = TeamInviteStatus.Rejected;
                invite.RespondedAt = DateTime.UtcNow;
                AddNotification(invite.Team.LeaderId, "InviteRejected", "Team invite rejected", "A user rejected your team invite.", "/my-team");
                await AddAuditAsync("TeamInviteRejected", "TeamInvite", inviteId, null);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Invite rejected." });
            }

            if (invite.Team.Category.Event.RegistrationClosedAt != null || invite.Team.Category.Event.JudgingStartedAt != null)
                return BadRequest(new { message = "Cannot accept invites after registration closes or judging starts." });
            if (invite.Team.Members.Count >= 5)
                return BadRequest(new { message = "Team is already full." });

            var categoryIdsInSameEvent = await _context.Categories
                .Where(c => c.EventId == invite.Team.Category.EventId)
                .Select(c => c.CategoryId)
                .ToListAsync();
            var alreadyJoinedEvent = await _context.TeamMembers
                .Include(tm => tm.Team)
                .AnyAsync(tm => tm.UserId == currentUserId && categoryIdsInSameEvent.Contains(tm.Team!.CategoryId));
            if (alreadyJoinedEvent)
                return BadRequest(new { message = "You already joined a team in this event." });

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
            await AddAuditAsync("TeamInviteAccepted", "TeamInvite", inviteId, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Invite accepted." });
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

        private async Task AddAuditAsync(string action, string entityType, Guid entityId, string? details)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                ActorUserId = GetCurrentUserId()
            });
            await Task.CompletedTask;
        }
    }
}
