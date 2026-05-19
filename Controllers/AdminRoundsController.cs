using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/rounds")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoundsController : ControllerBase
    {
        private readonly IRankingService _rankingService;
        private readonly ApplicationDbContext _context;

        public AdminRoundsController(IRankingService rankingService, ApplicationDbContext context)
        {
            _rankingService = rankingService;
            _context = context;
        }

        private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("{roundId}/open")]
        public async Task<IActionResult> OpenRound(Guid roundId)
        {
            var round = await _context.Rounds.Include(r => r.Event).FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return NotFound(new { message = "Round not found." });
            if (round.Event.IsArchived) return BadRequest(new { message = "Cannot open a round for an archived event." });

            round.Status = RoundStatus.Open;
            round.IsSubmissionLocked = false;
            await AddAuditAsync("RoundOpened", roundId);
            await NotifyTeamLeadersAsync(round.EventId, "RoundOpened", "Round opened", $"{round.RoundName} is open for submissions.");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Round opened successfully." });
        }

        [HttpPost("{roundId}/close")]
        public async Task<IActionResult> CloseRound(Guid roundId)
        {
            var round = await _context.Rounds.FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return NotFound(new { message = "Round not found." });
            if (round.Status != RoundStatus.Open) return BadRequest(new { message = "Only open rounds can be closed." });

            round.Status = RoundStatus.Closed;
            await AddAuditAsync("RoundClosed", roundId);
            await NotifyTeamLeadersAsync(round.EventId, "RoundClosed", "Round closed", $"{round.RoundName} is closed.");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Round closed successfully." });
        }

        [HttpPost("{roundId}/lock-submissions")]
        public async Task<IActionResult> LockSubmissions(Guid roundId)
        {
            var round = await _context.Rounds.FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return NotFound(new { message = "Round not found." });
            if (round.Status == RoundStatus.Draft || round.Status == RoundStatus.Open)
                return BadRequest(new { message = "Close the round before locking submissions." });

            round.IsSubmissionLocked = true;
            round.Status = RoundStatus.Locked;
            await AddAuditAsync("RoundSubmissionsLocked", roundId);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Round submissions locked successfully." });
        }

        [HttpPost("{roundId}/reopen")]
        public async Task<IActionResult> ReopenRound(Guid roundId)
        {
            var round = await _context.Rounds.FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return NotFound(new { message = "Round not found." });
            if (round.IsRankingPublished) return BadRequest(new { message = "Cannot reopen after ranking is published." });

            round.Status = RoundStatus.Open;
            round.IsSubmissionLocked = false;
            await AddAuditAsync("RoundReopened", roundId);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Round reopened successfully." });
        }

        [HttpPost("{roundId}/publish-result")]
        public async Task<IActionResult> PublishResult(Guid roundId)
        {
            var round = await _context.Rounds.Include(r => r.Event).FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return NotFound(new { message = "Round not found." });
            if (round.Event.JudgingEndedAt == null)
                return BadRequest(new { message = "Cannot publish ranking before judging ends." });
            if (round.Status != RoundStatus.Locked && round.Status != RoundStatus.Closed)
                return BadRequest(new { message = "Round must be closed or locked before publishing results." });

            round.IsRankingPublished = true;
            round.Status = RoundStatus.ResultsPublished;
            await AddAuditAsync("RankingPublished", roundId);
            await NotifyTeamLeadersAsync(round.EventId, "RankingPublished", "Ranking published", $"{round.RoundName} ranking is published.");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Round result published successfully." });
        }

        [HttpPost("{roundId}/advance")]
        public async Task<IActionResult> AdvanceRound(Guid roundId)
        {
            var round = await _context.Rounds.FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null)
                return NotFound(new { message = "Round not found." });
            if (!round.IsRankingPublished)
                return BadRequest(new { message = "Cannot advance before round results are published." });

            var result = await _rankingService.AdvanceRoundAsync(roundId);

            if (!result.Success)
            {
                if (result.Message == "Round not found.")
                    return NotFound(new { message = result.Message });

                if (result.Data != null)
                    return BadRequest(new { message = result.Message, details = result.Data });

                return BadRequest(new { message = result.Message });
            }

            await AddAuditAsync("RoundAdvanced", roundId);
            await _context.SaveChangesAsync();
            return Ok(new { message = result.Message, details = result.Data });
        }

        private async Task AddAuditAsync(string action, Guid roundId)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityType = "Round",
                EntityId = roundId,
                ActorUserId = GetCurrentUserId()
            });
            await Task.CompletedTask;
        }

        private async Task NotifyTeamLeadersAsync(Guid eventId, string type, string title, string message)
        {
            var leaderIds = await _context.Teams
                .Where(t => t.Category.EventId == eventId)
                .Select(t => t.LeaderId)
                .Distinct()
                .ToListAsync();

            foreach (var leaderId in leaderIds)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = leaderId,
                    Type = type,
                    Title = title,
                    Message = message,
                    Link = "/my-team"
                });
            }
        }
    }
}
