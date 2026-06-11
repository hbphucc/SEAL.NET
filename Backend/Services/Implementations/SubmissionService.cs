using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Submission;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class SubmissionService : ISubmissionService
    {
        private readonly ApplicationDbContext _context;

        public SubmissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        private static DateTime PersistedUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }

        public async Task<ServiceResult> SubmitProjectAsync(Guid currentUserId, CreateSubmissionRequest request)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == request.TeamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            if (team.LeaderId != currentUserId)
                return ServiceResult.Forbidden();

            if (team.Status != TeamStatus.Approved)
                return ServiceResult.BadRequest(new { message = "Only approved teams can submit." });
            if (!string.IsNullOrWhiteSpace(team.EliminationReason) || team.Status == TeamStatus.Eliminated)
                return ServiceResult.BadRequest(new { message = "Eliminated teams cannot submit." });

            if (team.CurrentRoundId != request.RoundId)
                return ServiceResult.BadRequest(new { message = "Team can only submit for its current round." });

            var round = await _context.Rounds.FindAsync(request.RoundId);
            if (round == null)
                return ServiceResult.NotFound(new { message = "Round not found." });

            if (round.Status != RoundStatus.Open || round.IsSubmissionLocked)
                return ServiceResult.BadRequest(new { message = "Round is not open for submissions." });

            if (round.SubmissionDeadline == null)
                return ServiceResult.BadRequest(new { message = "This round has no submission deadline configured." });

            var submissionDeadline = PersistedUtc(round.SubmissionDeadline.Value);

            if (DateTime.UtcNow > submissionDeadline)
                return ServiceResult.BadRequest(new { message = "Submission deadline has passed." });

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

                return ServiceResult.Ok(new
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

            return ServiceResult.Ok(new
            {
                message = "Submission created successfully.",
                submission.SubmissionId
            });
        }

        public async Task<ServiceResult> GetSubmissionAsync(Guid submissionId, Guid currentUserId, bool isAdmin, bool isJudge, bool isMentorRole)
        {
            var submission = await _context.Submissions
                .Include(s => s.Team)
                    .ThenInclude(t => t.Members)
                .Include(s => s.Team)
                    .ThenInclude(t => t.Category)
                .Include(s => s.Round)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
                return ServiceResult.NotFound(new { message = "Submission not found." });

            var isMember = submission.Team.Members.Any(m => m.UserId == currentUserId);
            var isMentor = isMentorRole && await _context.MentorAssignments.AnyAsync(a => a.TeamId == submission.TeamId && a.MentorId == currentUserId);

            if (!isMember && !isAdmin && !isMentor)
            {
                if (!isJudge)
                    return ServiceResult.Forbidden();

                var assigned = await _context.JudgeAssignments.AnyAsync(a =>
                    a.JudgeId == currentUserId &&
                    a.RoundId == submission.RoundId &&
                    a.CategoryId == submission.Team.CategoryId);
                if (!assigned)
                    return ServiceResult.Forbidden();
            }

            return ServiceResult.Ok(new SubmissionDetailDto
            {
                SubmissionId = submission.SubmissionId,
                RepositoryUrl = submission.RepositoryUrl,
                DemoUrl = submission.DemoUrl,
                SlideUrl = submission.SlideUrl,
                SubmittedAt = submission.SubmittedAt,
                UpdatedAt = submission.UpdatedAt,
                IsWithdrawn = submission.IsWithdrawn,
                WithdrawnAt = submission.WithdrawnAt,
                Team = new SubmissionTeamInfo { TeamId = submission.Team.TeamId, TeamName = submission.Team.TeamName },
                Round = new SubmissionRoundInfo { RoundId = submission.Round.RoundId, RoundName = submission.Round.RoundName }
            });
        }

        public async Task<ServiceResult> WithdrawSubmissionAsync(Guid submissionId, Guid currentUserId)
        {
            var submission = await _context.Submissions
                .Include(s => s.Team)
                .Include(s => s.Round)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
                return ServiceResult.NotFound(new { message = "Submission not found." });
            if (submission.Team.LeaderId != currentUserId)
                return ServiceResult.Forbidden();
            if (submission.Round.Status != RoundStatus.Open || submission.Round.IsSubmissionLocked)
                return ServiceResult.BadRequest(new { message = "Cannot withdraw after the round closes or locks." });

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
            return ServiceResult.Ok(new { message = "Submission withdrawn successfully." });
        }

        public async Task<ServiceResult> GetTeamSubmissionsAsync(Guid teamId, Guid currentUserId, bool isAdmin, bool isJudge)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return ServiceResult.NotFound(new { message = "Team not found." });

            var isMember = team.Members.Any(m => m.UserId == currentUserId);

            if (!isMember && !isAdmin && !isJudge)
                return ServiceResult.Forbidden();

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
                    return ServiceResult.Forbidden();

                submissionsQuery = submissionsQuery.Where(s => assignedRoundIds.Contains(s.RoundId));
            }

            var submissions = await submissionsQuery
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new TeamSubmissionDto
                {
                    SubmissionId = s.SubmissionId,
                    RepositoryUrl = s.RepositoryUrl,
                    DemoUrl = s.DemoUrl,
                    SlideUrl = s.SlideUrl,
                    SubmittedAt = s.SubmittedAt,
                    Round = new SubmissionRoundInfo
                    {
                        RoundId = s.Round!.RoundId,
                        RoundName = s.Round.RoundName
                    }
                })
                .ToListAsync();

            return ServiceResult.Ok(submissions);
        }

        public async Task<ServiceResult> GetRoundSubmissionsAsync(Guid roundId, Guid currentUserId, bool isAdmin, bool isJudge)
        {
            var submissionsQuery = _context.Submissions
                .Include(s => s.Team)
                    .ThenInclude(t => t.Category)
                .Where(s => s.RoundId == roundId && !s.IsWithdrawn && s.Team!.Status != TeamStatus.Eliminated);

            if (isJudge && !isAdmin)
            {
                var assignedCategoryIds = await _context.JudgeAssignments
                    .Where(a =>
                        a.JudgeId == currentUserId &&
                        a.RoundId == roundId)
                    .Select(a => a.CategoryId)
                    .ToListAsync();

                if (!assignedCategoryIds.Any())
                    return ServiceResult.Forbidden();

                submissionsQuery = submissionsQuery
                    .Where(s => assignedCategoryIds.Contains(s.Team!.CategoryId));
            }

            var submissions = await submissionsQuery
                .Select(s => new RoundSubmissionDto
                {
                    SubmissionId = s.SubmissionId,
                    RepositoryUrl = s.RepositoryUrl,
                    DemoUrl = s.DemoUrl,
                    SlideUrl = s.SlideUrl,
                    SubmittedAt = s.SubmittedAt,
                    Team = new SubmissionTeamInfo
                    {
                        TeamId = s.Team!.TeamId,
                        TeamName = s.Team.TeamName,
                        Category = s.Team.Category!.CategoryName
                    }
                })
                .ToListAsync();

            return ServiceResult.Ok(submissions);
        }
    }
}
