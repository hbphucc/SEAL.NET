using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Mentor;
using SEAL.NET.Models.Entities;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class MentorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MentorsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("admin/teams/{teamId}/mentors")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignMentor(Guid teamId, [FromBody] MentorAssignmentRequest request)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound(new { message = "Team not found." });

            var mentor = await _userManager.FindByIdAsync(request.MentorId.ToString());
            if (mentor == null) return NotFound(new { message = "Mentor not found." });
            if (!await _userManager.IsInRoleAsync(mentor, "Mentor"))
                return BadRequest(new { message = "This user is not a Mentor." });

            var duplicate = await _context.MentorAssignments.AnyAsync(a => a.TeamId == teamId && a.MentorId == request.MentorId);
            if (duplicate) return BadRequest(new { message = "Mentor is already assigned to this team." });

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
                ActorUserId = GetCurrentUserId()
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Mentor assigned successfully." });
        }

        [HttpDelete("admin/teams/{teamId}/mentors/{mentorId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnassignMentor(Guid teamId, Guid mentorId)
        {
            var assignment = await _context.MentorAssignments.FirstOrDefaultAsync(a => a.TeamId == teamId && a.MentorId == mentorId);
            if (assignment == null) return NotFound(new { message = "Mentor assignment not found." });

            _context.MentorAssignments.Remove(assignment);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "MentorUnassigned",
                EntityType = "Team",
                EntityId = teamId,
                Details = $"Mentor={mentorId}",
                ActorUserId = GetCurrentUserId()
            });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Mentor unassigned successfully." });
        }

        [HttpGet("mentor/teams")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GetAssignedTeams()
        {
            var mentorId = GetCurrentUserId();
            var teams = await _context.MentorAssignments
                .Where(a => a.MentorId == mentorId)
                .Include(a => a.Team)
                    .ThenInclude(t => t.Category)
                .Include(a => a.Team)
                    .ThenInclude(t => t.Members)
                        .ThenInclude(m => m.User)
                .Select(a => new
                {
                    a.Team.TeamId,
                    a.Team.TeamName,
                    a.Team.Description,
                    status = a.Team.Status.ToString(),
                    category = new { a.Team.Category.CategoryId, a.Team.Category.CategoryName },
                    members = a.Team.Members.Select(m => new { m.UserId, m.User.FullName, m.User.Email, role = m.Role.ToString(), m.IsLeader })
                })
                .ToListAsync();

            return Ok(teams);
        }

        [HttpGet("mentor/teams/{teamId}/submissions")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GetTeamSubmissions(Guid teamId)
        {
            var mentorId = GetCurrentUserId();
            var assigned = await _context.MentorAssignments.AnyAsync(a => a.TeamId == teamId && a.MentorId == mentorId);
            if (!assigned) return Forbid();

            var submissions = await _context.Submissions
                .Include(s => s.Round)
                .Where(s => s.TeamId == teamId && !s.IsWithdrawn)
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new
                {
                    s.SubmissionId,
                    s.RepositoryUrl,
                    s.DemoUrl,
                    s.SlideUrl,
                    s.SubmittedAt,
                    s.UpdatedAt,
                    round = new { s.Round.RoundId, s.Round.RoundName }
                })
                .ToListAsync();

            return Ok(submissions);
        }

        [HttpPost("mentor/teams/{teamId}/notes")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> AddNote(Guid teamId, [FromBody] CreateMentorshipNoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Body))
                return BadRequest(new { message = "Note body is required." });

            var mentorId = GetCurrentUserId();
            var assigned = await _context.MentorAssignments.AnyAsync(a => a.TeamId == teamId && a.MentorId == mentorId);
            if (!assigned) return Forbid();

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
            return Ok(new { message = "Mentorship note added.", note.MentorshipNoteId });
        }

        [HttpGet("mentor/teams/{teamId}/notes")]
        [Authorize(Roles = "Admin,Mentor")]
        public async Task<IActionResult> GetNotes(Guid teamId)
        {
            var userId = GetCurrentUserId();
            if (!User.IsInRole("Admin"))
            {
                var assigned = await _context.MentorAssignments.AnyAsync(a => a.TeamId == teamId && a.MentorId == userId);
                if (!assigned) return Forbid();
            }

            var notes = await _context.MentorshipNotes
                .Include(n => n.Mentor)
                .Where(n => n.TeamId == teamId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.MentorshipNoteId,
                    n.Body,
                    n.CreatedAt,
                    mentor = new { n.MentorId, n.Mentor.FullName, n.Mentor.Email }
                })
                .ToListAsync();

            return Ok(notes);
        }
    }
}
