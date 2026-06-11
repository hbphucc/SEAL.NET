using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Score;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class ScoreService : IScoreService
    {
        private readonly ApplicationDbContext _context;

        public ScoreService(ApplicationDbContext context)
        {
            _context = context;
        }

        private static ServiceResult? RejectIfRankingPublished(Submission submission)
        {
            return submission.Round?.IsRankingPublished == true
                ? ServiceResult.Conflict(new { message = "Scores cannot be changed after ranking is published for this round." })
                : null;
        }

        private void AddScoreAuditLog(
            Score score,
            string action,
            decimal? oldScoreValue,
            string? oldComment,
            decimal newScoreValue,
            string? newComment)
        {
            _context.ScoreAuditLogs.Add(new ScoreAuditLog
            {
                ScoreId = score.ScoreId,
                SubmissionId = score.SubmissionId,
                JudgeId = score.JudgeId,
                CriteriaId = score.CriteriaId,
                Action = action,
                OldScoreValue = oldScoreValue,
                NewScoreValue = newScoreValue,
                OldComment = oldComment,
                NewComment = newComment
            });
        }

        public async Task<ServiceResult> SubmitScoreAsync(Guid judgeId, CreateScoreRequest request)
        {
            var submission = await _context.Submissions
                .Include(s => s.Team)
                .Include(s => s.Round)
                .FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId);

            if (submission == null)
                return ServiceResult.NotFound(new { message = "Submission not found." });

            var publishedResult = RejectIfRankingPublished(submission);
            if (publishedResult != null)
                return publishedResult;

            if (submission.IsWithdrawn || submission.Team!.Status == TeamStatus.Eliminated)
                return ServiceResult.BadRequest(new { message = "Cannot score withdrawn or eliminated submissions." });

            if (submission.Round!.Status != RoundStatus.Locked && submission.Round.Status != RoundStatus.Closed)
                return ServiceResult.BadRequest(new { message = "Round is not ready for judging." });

            var criteria = await _context.Criteria
                .FirstOrDefaultAsync(c =>
                    c.CriteriaId == request.CriteriaId &&
                    c.RoundId == submission.RoundId);

            if (criteria == null)
                return ServiceResult.BadRequest(new { message = "Criteria does not belong to this submission round." });

            if (request.ScoreValue < 0 || request.ScoreValue > criteria.MaxScore)
                return ServiceResult.BadRequest(new { message = $"Score must be between 0 and {criteria.MaxScore}." });

            var isAssigned = await _context.JudgeAssignments.AnyAsync(a =>
                a.JudgeId == judgeId &&
                a.RoundId == submission.RoundId &&
                a.CategoryId == submission.Team!.CategoryId);

            if (!isAssigned)
                return ServiceResult.Forbidden();

            var existingScore = await _context.Scores.FirstOrDefaultAsync(s =>
                s.SubmissionId == request.SubmissionId &&
                s.JudgeId == judgeId &&
                s.CriteriaId == request.CriteriaId);

            if (existingScore != null)
            {
                if (existingScore.IsFinal)
                    return ServiceResult.Conflict(new { message = "Finalized scores are locked." });

                var oldScoreValue = existingScore.ScoreValue;
                var oldComment = existingScore.Comment;

                existingScore.ScoreValue = request.ScoreValue;
                existingScore.Comment = request.Comment;
                existingScore.CreatedAt = DateTime.UtcNow;
                existingScore.IsFinal = request.SubmitFinal;
                existingScore.FinalizedAt = request.SubmitFinal ? DateTime.UtcNow : null;

                AddScoreAuditLog(existingScore, "Updated", oldScoreValue, oldComment, request.ScoreValue, request.Comment);

                await _context.SaveChangesAsync();

                return ServiceResult.Ok(new { message = "Score updated successfully." });
            }

            var score = new Score
            {
                SubmissionId = request.SubmissionId,
                JudgeId = judgeId,
                CriteriaId = request.CriteriaId,
                ScoreValue = request.ScoreValue,
                Comment = request.Comment,
                IsFinal = request.SubmitFinal,
                FinalizedAt = request.SubmitFinal ? DateTime.UtcNow : null
            };

            _context.Scores.Add(score);
            AddScoreAuditLog(score, "Created", null, null, score.ScoreValue, score.Comment);

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Score submitted successfully.", score.ScoreId });
        }

        public async Task<ServiceResult> SubmitBulkScoresAsync(Guid judgeId, BulkScoreRequest request)
        {
            var submission = await _context.Submissions
                .Include(s => s.Team)
                .Include(s => s.Round)
                .FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId);

            if (submission == null)
                return ServiceResult.NotFound(new { message = "Submission not found." });

            var publishedResult = RejectIfRankingPublished(submission);
            if (publishedResult != null)
                return publishedResult;

            if (submission.IsWithdrawn || submission.Team!.Status == TeamStatus.Eliminated)
                return ServiceResult.BadRequest(new { message = "Cannot score withdrawn or eliminated submissions." });

            if (submission.Round!.Status != RoundStatus.Locked && submission.Round.Status != RoundStatus.Closed)
                return ServiceResult.BadRequest(new { message = "Round is not ready for judging." });

            var isAssigned = await _context.JudgeAssignments.AnyAsync(a =>
                a.JudgeId == judgeId &&
                a.RoundId == submission.RoundId &&
                a.CategoryId == submission.Team!.CategoryId);

            if (!isAssigned)
                return ServiceResult.Forbidden();

            var duplicateCriteriaIds = request.Scores
                .GroupBy(s => s.CriteriaId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateCriteriaIds.Any())
            {
                return ServiceResult.BadRequest(new
                {
                    message = "Duplicate criteria found in request.",
                    criteriaIds = duplicateCriteriaIds
                });
            }

            var requestedCriteriaIds = request.Scores
                .Select(s => s.CriteriaId)
                .ToList();

            var criteriaList = await _context.Criteria
                .Where(c =>
                    requestedCriteriaIds.Contains(c.CriteriaId) &&
                    c.RoundId == submission.RoundId)
                .ToListAsync();

            var missingCriteriaIds = requestedCriteriaIds
                .Except(criteriaList.Select(c => c.CriteriaId))
                .ToList();

            if (missingCriteriaIds.Any())
            {
                return ServiceResult.BadRequest(new
                {
                    message = "One or more criteria do not belong to this submission round.",
                    criteriaIds = missingCriteriaIds
                });
            }

            var criteriaById = criteriaList.ToDictionary(c => c.CriteriaId);
            var invalidScores = request.Scores
                .Where(s => s.ScoreValue < 0 || s.ScoreValue > criteriaById[s.CriteriaId].MaxScore)
                .Select(s => new
                {
                    s.CriteriaId,
                    s.ScoreValue,
                    maxScore = criteriaById[s.CriteriaId].MaxScore
                })
                .ToList();

            if (invalidScores.Any())
            {
                return ServiceResult.BadRequest(new
                {
                    message = "One or more scores are outside the allowed range.",
                    scores = invalidScores
                });
            }

            var existingScores = await _context.Scores
                .Where(s =>
                    s.SubmissionId == request.SubmissionId &&
                    s.JudgeId == judgeId &&
                    requestedCriteriaIds.Contains(s.CriteriaId))
                .ToListAsync();

            var existingByCriteriaId = existingScores.ToDictionary(s => s.CriteriaId);
            var createdScores = new List<object>();
            var updatedScores = new List<object>();

            foreach (var item in request.Scores)
            {
                if (existingByCriteriaId.TryGetValue(item.CriteriaId, out var existingScore))
                {
                    if (existingScore.IsFinal)
                        return ServiceResult.Conflict(new { message = "Finalized scores are locked.", existingScore.CriteriaId });

                    var oldScoreValue = existingScore.ScoreValue;
                    var oldComment = existingScore.Comment;

                    existingScore.ScoreValue = item.ScoreValue;
                    existingScore.Comment = item.Comment;
                    existingScore.CreatedAt = DateTime.UtcNow;
                    existingScore.IsFinal = request.SubmitFinal;
                    existingScore.FinalizedAt = request.SubmitFinal ? DateTime.UtcNow : null;

                    AddScoreAuditLog(existingScore, "Updated", oldScoreValue, oldComment, item.ScoreValue, item.Comment);

                    updatedScores.Add(new
                    {
                        existingScore.ScoreId,
                        existingScore.CriteriaId,
                        existingScore.ScoreValue
                    });

                    continue;
                }

                var score = new Score
                {
                    SubmissionId = request.SubmissionId,
                    JudgeId = judgeId,
                    CriteriaId = item.CriteriaId,
                    ScoreValue = item.ScoreValue,
                    Comment = item.Comment,
                    IsFinal = request.SubmitFinal,
                    FinalizedAt = request.SubmitFinal ? DateTime.UtcNow : null
                };

                _context.Scores.Add(score);
                AddScoreAuditLog(score, "Created", null, null, score.ScoreValue, score.Comment);

                createdScores.Add(new
                {
                    score.ScoreId,
                    score.CriteriaId,
                    score.ScoreValue
                });
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new
            {
                message = "Bulk scores submitted successfully.",
                createdCount = createdScores.Count,
                updatedCount = updatedScores.Count,
                createdScores,
                updatedScores
            });
        }

        public async Task<List<AssignedSubmissionDto>> GetMyAssignedSubmissionsAsync(Guid judgeId)
        {
            var assignments = await _context.JudgeAssignments
                .Where(a => a.JudgeId == judgeId)
                .ToListAsync();

            var roundIds = assignments.Select(a => a.RoundId).ToList();
            var categoryIds = assignments.Select(a => a.CategoryId).ToList();

            return await _context.Submissions
                .Include(s => s.Team)
                    .ThenInclude(t => t.Category)
                .Include(s => s.Round)
                .Where(s =>
                    roundIds.Contains(s.RoundId) &&
                    categoryIds.Contains(s.Team!.CategoryId) &&
                    !s.IsWithdrawn &&
                    s.Team.Status != TeamStatus.Eliminated)
                .Select(s => new AssignedSubmissionDto
                {
                    SubmissionId = s.SubmissionId,
                    RepositoryUrl = s.RepositoryUrl,
                    DemoUrl = s.DemoUrl,
                    SlideUrl = s.SlideUrl,
                    Team = new AssignedSubmissionTeamInfo
                    {
                        TeamId = s.Team!.TeamId,
                        TeamName = s.Team.TeamName,
                        Category = s.Team.Category!.CategoryName
                    },
                    Round = new AssignedSubmissionRoundInfo
                    {
                        RoundId = s.Round!.RoundId,
                        RoundName = s.Round.RoundName
                    }
                })
                .ToListAsync();
        }
    }
}
