using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/ranking")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRankingController : ControllerBase 
    {
        private readonly ApplicationDbContext _context;
        private readonly IRankingService _rankingService;

        public AdminRankingController(ApplicationDbContext context, IRankingService rankingService)
        {
            _context = context;
            _rankingService = rankingService;
        }

        private async Task<IActionResult?> EnsureRoundExistsAsync(Guid roundId)
        {
            var exists = await _context.Rounds.AnyAsync(r => r.RoundId == roundId);
            return exists ? null : NotFound(new { message = "Round not found." });
        }

        [HttpGet("round/{roundId}")]
        public async Task<IActionResult> GetRoundRanking(Guid roundId)
        {
            var missingRoundResult = await EnsureRoundExistsAsync(roundId);
            if (missingRoundResult != null)
                return missingRoundResult;

            var submissions = await _context.Submissions
                .Include(s => s.Team)
                    .ThenInclude(t => t.Category)
                .Include(s => s.Scores)
                    .ThenInclude(sc => sc.Criteria)
                .Where(s => s.RoundId == roundId)
                .ToListAsync();

            var ranking = submissions
                .Select(s => new
                {
                    s.SubmissionId,
                    teamId = s.Team!.TeamId,
                    teamName = s.Team.TeamName,
                    categoryName = s.Team.Category!.CategoryName,
                    totalScore = _rankingService.CalculateWeightedScore(s.Scores),
                    submittedAt = s.SubmittedAt
                })
                .OrderByDescending(x => x.totalScore)
                .ThenBy(x => x.submittedAt)
                .ToList();

            var result = ranking.Select((r, index) => new
            {
                rank = index + 1,
                r.SubmissionId,
                r.teamId,
                r.teamName,
                r.categoryName,
                r.totalScore,
                r.submittedAt
            });

            return Ok(result);
        }

        [HttpGet("category/{categoryId}/round/{roundId}")]
        public async Task<IActionResult> GetCategoryRoundRanking(Guid categoryId, Guid roundId)
        {
            var missingRoundResult = await EnsureRoundExistsAsync(roundId);
            if (missingRoundResult != null)
                return missingRoundResult;

            var submissions = await _context.Submissions
                .Include(s => s.Team)
                .Include(s => s.Scores)
                    .ThenInclude(sc => sc.Criteria)
                .Where(s =>
                    s.RoundId == roundId &&
                    s.Team!.CategoryId == categoryId)
                .ToListAsync();

            var ranking = submissions
                .Select(s => new
                {
                    s.SubmissionId,
                    teamId = s.Team!.TeamId,
                    teamName = s.Team.TeamName,
                    totalScore = _rankingService.CalculateWeightedScore(s.Scores),
                    submittedAt = s.SubmittedAt
                })
                .OrderByDescending(x => x.totalScore)
                .ThenBy(x => x.submittedAt)
                .ToList();

            var result = ranking.Select((r, index) => new
            {
                rank = index + 1,
                r.SubmissionId,
                r.teamId,
                r.teamName,
                r.totalScore,
                r.submittedAt
            });

            return Ok(result);
        }
    }
}
