using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Submission;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [Route("api/submissions")]
    [ApiController]
    [Authorize]
    public class SubmissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubmissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetCurrentUserId()
        {
            return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        private static DateTime PersistedUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }

        [HttpPost]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> SubmitProject([FromBody] CreateSubmissionRequest request)
        {
            var currentUserId = GetCurrentUserId();

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == request.TeamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return Forbid();

            if (team.Status != TeamStatus.Approved)
                return BadRequest(new { message = "Only approved teams can submit." });
            if (!string.IsNullOrWhiteSpace(team.EliminationReason) || team.Status == TeamStatus.Eliminated)
                return BadRequest(new { message = "Eliminated teams cannot submit." });

            if (team.CurrentRoundId != request.RoundId)
                return BadRequest(new { message = "Team can only submit for its current round." });

            var round = await _context.Rounds.FindAsync(request.RoundId);
            if (round == null)
                return NotFound(new { message = "Round not found." });

            if (round.Status != RoundStatus.Open || round.IsSubmissionLocked)
                return BadRequest(new { message = "Round is not open for submissions." });

            if (round.SubmissionDeadline == null)
                return BadRequest(new { message = "This round has no submission deadline configured." });

            var submissionDeadline = PersistedUtc(round.SubmissionDeadline.Value);

            if (DateTime.UtcNow > submissionDeadline)
                return BadRequest(new { message = "Submission deadline has passed." });

            var existingSubmission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.TeamId == request.TeamId && s.RoundId == request.RoundId);

            if (existingSubmission != null)
            {
                existingSubmission.RepositoryUrl = request.RepositoryUrl;
                existingSubmission.DemoUrl = request.DemoUrl;
                existingSubmission.SlideUrl = request.SlideUrl;
                existingSubmission.SubmittedAt = DateTime.UtcNow;
                existingSubmission.UpdatedAt = DateTime.UtcNow;
                existingSubmission.IsWithdrawn = false;
                existingSubmission.WithdrawnAt = null;

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "SubmissionUpdated",
                    EntityType = "Submission",
                    EntityId = existingSubmission.SubmissionId,
                    ActorUserId = currentUserId
                });
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Submission updated successfully.",
                    existingSubmission.SubmissionId
                });
            }

            var submission = new Submission
            {
                TeamId = request.TeamId,
                RoundId = request.RoundId,
                RepositoryUrl = request.RepositoryUrl,
                DemoUrl = request.DemoUrl,
                SlideUrl = request.SlideUrl
            };

            _context.Submissions.Add(submission);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "SubmissionCreated",
                EntityType = "Submission",
                EntityId = submission.SubmissionId,
                ActorUserId = currentUserId
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Submission created successfully.",
                submission.SubmissionId
            });
        }

        [HttpGet("{submissionId}")]
        public async Task<IActionResult> GetSubmission(Guid submissionId)
        {
            var currentUserId = GetCurrentUserId();
            var submission = await _context.Submissions
                .Include(s => s.Team)
                    .ThenInclude(t => t.Members)
                .Include(s => s.Team)
                    .ThenInclude(t => t.Category)
                .Include(s => s.Round)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
                return NotFound(new { message = "Submission not found." });

            var isMember = submission.Team.Members.Any(m => m.UserId == currentUserId);
            var isAdmin = User.IsInRole("Admin");
            var isJudge = User.IsInRole("Judge");
            var isMentor = User.IsInRole("Mentor") && await _context.MentorAssignments.AnyAsync(a => a.TeamId == submission.TeamId && a.MentorId == currentUserId);

            if (!isMember && !isAdmin && !isMentor)
            {
                if (!isJudge)
                    return Forbid();

                var assigned = await _context.JudgeAssignments.AnyAsync(a =>
                    a.JudgeId == currentUserId &&
                    a.RoundId == submission.RoundId &&
                    a.CategoryId == submission.Team.CategoryId);
                if (!assigned)
                    return Forbid();
            }

            return Ok(new
            {
                submission.SubmissionId,
                submission.RepositoryUrl,
                submission.DemoUrl,
                submission.SlideUrl,
                submission.SubmittedAt,
                submission.UpdatedAt,
                submission.IsWithdrawn,
                submission.WithdrawnAt,
                team = new { submission.Team.TeamId, submission.Team.TeamName },
                round = new { submission.Round.RoundId, submission.Round.RoundName }
            });
        }

        [HttpPost("{submissionId}/withdraw")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> WithdrawSubmission(Guid submissionId)
        {
            var currentUserId = GetCurrentUserId();
            var submission = await _context.Submissions
                .Include(s => s.Team)
                .Include(s => s.Round)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
                return NotFound(new { message = "Submission not found." });
            if (submission.Team.LeaderId != currentUserId)
                return Forbid();
            if (submission.Round.Status != RoundStatus.Open || submission.Round.IsSubmissionLocked)
                return BadRequest(new { message = "Cannot withdraw after the round closes or locks." });

            submission.IsWithdrawn = true;
            submission.WithdrawnAt = DateTime.UtcNow;
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "SubmissionWithdrawn",
                EntityType = "Submission",
                EntityId = submission.SubmissionId,
                ActorUserId = currentUserId
            });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Submission withdrawn successfully." });
        }

        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetTeamSubmissions(Guid teamId)
        {
            var currentUserId = GetCurrentUserId();

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            var isMember = team.Members.Any(m => m.UserId == currentUserId);
            var isAdmin = User.IsInRole("Admin");
            var isJudge = User.IsInRole("Judge");

            if (!isMember && !isAdmin && !isJudge)
                return Forbid();

            var submissionsQuery = _context.Submissions
                .Include(s => s.Round)
                .Where(s => s.TeamId == teamId && !s.IsWithdrawn);

            if (isJudge && !isAdmin && !isMember)
            {
                var assignedRoundIds = await _context.JudgeAssignments
                    .Where(a =>
                        a.JudgeId == currentUserId &&
                        a.CategoryId == team.CategoryId)
                    .Select(a => a.RoundId)
                    .ToListAsync();

                if (!assignedRoundIds.Any())
                    return Forbid();

                submissionsQuery = submissionsQuery.Where(s => assignedRoundIds.Contains(s.RoundId));
            }

            var submissions = await submissionsQuery
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new
                {
                    s.SubmissionId,
                    s.RepositoryUrl,
                    s.DemoUrl,
                    s.SlideUrl,
                    s.SubmittedAt,
                    round = new
                    {
                        s.Round!.RoundId,
                        s.Round.RoundName
                    }
                })
                .ToListAsync();

            return Ok(submissions);
        }

        [HttpGet("round/{roundId}")]
        [Authorize(Roles = "Admin,Judge")]
        public async Task<IActionResult> GetRoundSubmissions(Guid roundId)
        {
            var submissionsQuery = _context.Submissions
                .Include(s => s.Team)
                    .ThenInclude(t => t.Category)
                .Where(s => s.RoundId == roundId && !s.IsWithdrawn && s.Team!.Status != TeamStatus.Eliminated);

            if (User.IsInRole("Judge") && !User.IsInRole("Admin"))
            {
                var judgeId = GetCurrentUserId();
                var assignedCategoryIds = await _context.JudgeAssignments
                    .Where(a =>
                        a.JudgeId == judgeId &&
                        a.RoundId == roundId)
                    .Select(a => a.CategoryId)
                    .ToListAsync();

                if (!assignedCategoryIds.Any())
                    return Forbid();

                submissionsQuery = submissionsQuery
                    .Where(s => assignedCategoryIds.Contains(s.Team!.CategoryId));
            }

            var submissions = await submissionsQuery
                .Select(s => new
                {
                    s.SubmissionId,
                    s.RepositoryUrl,
                    s.DemoUrl,
                    s.SlideUrl,
                    s.SubmittedAt,
                    team = new
                    {
                        s.Team!.TeamId,
                        s.Team.TeamName,
                        category = s.Team.Category!.CategoryName
                    }
                })
                .ToListAsync();

            return Ok(submissions);
        }
    }
}
