using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Mentor;
using SEAL.NET.Models.Entities;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class MentorService : IMentorService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MentorService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ServiceResult> AssignMentorAsync(Guid teamId, MentorAssignmentRequest request, Guid actorUserId)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return ServiceResult.NotFound(new { message = "Team not found." });

            var mentor = await _userManager.FindByIdAsync(request.MentorId.ToString());
            if (mentor == null) return ServiceResult.NotFound(new { message = "Mentor not found." });
            if (!await _userManager.IsInRoleAsync(mentor, "Mentor"))
                return ServiceResult.BadRequest(new { message = "This user is not a Mentor." });

            var duplicate = await _context.MentorAssignments.AnyAsync(a => a.TeamId == teamId && a.MentorId == request.MentorId);
            if (duplicate) return ServiceResult.BadRequest(new { message = "Mentor is already assigned to this team." });

            _context.MentorAssignments.Add(new MentorAssignment { TeamId = teamId, MentorId = request.MentorId });
            _context.Notifications.Add(new Notification
            {
                UserId = request.MentorId,
                Type = "MentorAssigned",
                Title = "Mentor assignment",
                Message = $"You were assigned to mentor {team.TeamName}.",
                Link = "/mentor/teams"
            });
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "MentorAssigned",
                EntityType = "Team",
                EntityId = teamId,
                Details = $"Mentor={request.MentorId}",
                ActorUserId = actorUserId
            });

            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Mentor assigned successfully." });
        }

        public async Task<ServiceResult> UnassignMentorAsync(Guid teamId, Guid mentorId, Guid actorUserId)
        {
            var assignment = await _context.MentorAssignments.FirstOrDefaultAsync(a => a.TeamId == teamId && a.MentorId == mentorId);
            if (assignment == null) return ServiceResult.NotFound(new { message = "Mentor assignment not found." });

            _context.MentorAssignments.Remove(assignment);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "MentorUnassigned",
                EntityType = "Team",
                EntityId = teamId,
                Details = $"Mentor={mentorId}",
                ActorUserId = actorUserId
            });
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Mentor unassigned successfully." });
        }

        public async Task<List<MentorTeamDto>> GetAssignedTeamsAsync(Guid mentorId)
        {
            return await _context.MentorAssignments
                .Where(a => a.MentorId == mentorId)
                .Include(a => a.Team)
                    .ThenInclude(t => t.Category)
                .Include(a => a.Team)
                    .ThenInclude(t => t.Members)
                        .ThenInclude(m => m.User)
                .Select(a => new MentorTeamDto
                {
                    TeamId = a.Team.TeamId,
                    TeamName = a.Team.TeamName,
                    Description = a.Team.Description,
                    Status = a.Team.Status.ToString(),
                    Category = new MentorTeamCategoryInfo
                    {
                        CategoryId = a.Team.Category.CategoryId,
                        CategoryName = a.Team.Category.CategoryName
                    },
                    Members = a.Team.Members.Select(m => new MentorTeamMemberInfo
                    {
                        UserId = m.UserId,
                        FullName = m.User.FullName,
                        Email = m.User.Email,
                        Role = m.Role.ToString(),
                        IsLeader = m.IsLeader
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> GetTeamSubmissionsAsync(Guid teamId, Guid mentorId)
        {
            var assigned = await _context.MentorAssignments.AnyAsync(a => a.TeamId == teamId && a.MentorId == mentorId);
            if (!assigned) return ServiceResult.Forbidden();

            var submissions = await _context.Submissions
                .Include(s => s.Round)
                .Where(s => s.TeamId == teamId && !s.IsWithdrawn)
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new MentorSubmissionDto
                {
                    SubmissionId = s.SubmissionId,
                    RepositoryUrl = s.RepositoryUrl,
                    DemoUrl = s.DemoUrl,
                    SlideUrl = s.SlideUrl,
                    SubmittedAt = s.SubmittedAt,
                    UpdatedAt = s.UpdatedAt,
                    Round = new MentorSubmissionRoundInfo { RoundId = s.Round.RoundId, RoundName = s.Round.RoundName }
                })
                .ToListAsync();

            return ServiceResult.Ok(submissions);
        }

        public async Task<ServiceResult> AddNoteAsync(Guid teamId, Guid mentorId, CreateMentorshipNoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Body))
                return ServiceResult.BadRequest(new { message = "Note body is required." });

            var assigned = await _context.MentorAssignments.AnyAsync(a => a.TeamId == teamId && a.MentorId == mentorId);
            if (!assigned) return ServiceResult.Forbidden();

            var note = new MentorshipNote
            {
                TeamId = teamId,
                MentorId = mentorId,
                Body = request.Body.Trim()
            };

            _context.MentorshipNotes.Add(note);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "MentorshipNoteCreated",
                EntityType = "Team",
                EntityId = teamId,
                ActorUserId = mentorId
            });
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Mentorship note added.", note.MentorshipNoteId });
        }

        public async Task<ServiceResult> GetNotesAsync(Guid teamId, Guid userId, bool isAdmin)
        {
            if (!isAdmin)
            {
                var assigned = await _context.MentorAssignments.AnyAsync(a => a.TeamId == teamId && a.MentorId == userId);
                if (!assigned) return ServiceResult.Forbidden();
            }

            var notes = await _context.MentorshipNotes
                .Include(n => n.Mentor)
                .Where(n => n.TeamId == teamId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new MentorshipNoteDto
                {
                    MentorshipNoteId = n.MentorshipNoteId,
                    Body = n.Body,
                    CreatedAt = n.CreatedAt,
                    Mentor = new MentorshipNoteMentorInfo
                    {
                        MentorId = n.MentorId,
                        FullName = n.Mentor.FullName,
                        Email = n.Mentor.Email
                    }
                })
                .ToListAsync();

            return ServiceResult.Ok(notes);
        }
    }
}
