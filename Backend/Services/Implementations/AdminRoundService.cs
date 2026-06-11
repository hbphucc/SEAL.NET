using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class AdminRoundService : IAdminRoundService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRankingService _rankingService;

        public AdminRoundService(ApplicationDbContext context, IRankingService rankingService)
        {
            _context = context;
            _rankingService = rankingService;
        }

        public async Task<ServiceResult> OpenRoundAsync(Guid roundId, Guid actorUserId)
        {
            var round = await _context.Rounds.Include(r => r.Event).FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return ServiceResult.NotFound(new { message = "Round not found." });

            if (round.Event.IsArchived) return ServiceResult.BadRequest(new { message = "Cannot open a round for an archived event." });

            round.Status = RoundStatus.Open;
            round.IsSubmissionLocked = false;
            AddAudit("RoundOpened", roundId, actorUserId);
            await NotifyTeamLeadersAsync(round.EventId, "RoundOpened", "Round opened", $"{round.RoundName} is open for submissions.");
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Round opened successfully." });
        }

        public async Task<ServiceResult> CloseRoundAsync(Guid roundId, Guid actorUserId)
        {
            var round = await _context.Rounds.FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return ServiceResult.NotFound(new { message = "Round not found." });

            if (round.Status != RoundStatus.Open) return ServiceResult.BadRequest(new { message = "Only open rounds can be closed." });

            round.Status = RoundStatus.Closed;
            AddAudit("RoundClosed", roundId, actorUserId);
            await NotifyTeamLeadersAsync(round.EventId, "RoundClosed", "Round closed", $"{round.RoundName} is closed.");
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Round closed successfully." });
        }

        public async Task<ServiceResult> LockSubmissionsAsync(Guid roundId, Guid actorUserId)
        {
            var round = await _context.Rounds.FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return ServiceResult.NotFound(new { message = "Round not found." });

            if (round.Status == RoundStatus.Draft || round.Status == RoundStatus.Open)
                return ServiceResult.BadRequest(new { message = "Close the round before locking submissions." });

            round.IsSubmissionLocked = true;
            round.Status = RoundStatus.Locked;
            AddAudit("RoundSubmissionsLocked", roundId, actorUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Round submissions locked successfully." });
        }

        public async Task<ServiceResult> ReopenRoundAsync(Guid roundId, Guid actorUserId)
        {
            var round = await _context.Rounds.FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return ServiceResult.NotFound(new { message = "Round not found." });
            if (round.IsRankingPublished) return ServiceResult.BadRequest(new { message = "Cannot reopen after ranking is published." });

            round.Status = RoundStatus.Open;
            round.IsSubmissionLocked = false;
            AddAudit("RoundReopened", roundId, actorUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Round reopened successfully." });
        }

        public async Task<ServiceResult> PublishResultAsync(Guid roundId, Guid actorUserId)
        {
            var round = await _context.Rounds.Include(r => r.Event).FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null) return ServiceResult.NotFound(new { message = "Round not found." });
            if (round.Event.JudgingEndedAt == null)
                return ServiceResult.BadRequest(new { message = "Cannot publish ranking before judging ends." });
            if (round.Status != RoundStatus.Locked && round.Status != RoundStatus.Closed)
                return ServiceResult.BadRequest(new { message = "Round must be closed or locked before publishing results." });

            round.IsRankingPublished = true;
            round.Status = RoundStatus.ResultsPublished;
            AddAudit("RankingPublished", roundId, actorUserId);
            await NotifyTeamLeadersAsync(round.EventId, "RankingPublished", "Ranking published", $"{round.RoundName} ranking is published.");
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Round result published successfully." });
        }

        public async Task<ServiceResult> AdvanceRoundAsync(Guid roundId, Guid actorUserId)
        {
            var round = await _context.Rounds.FirstOrDefaultAsync(r => r.RoundId == roundId);
            if (round == null)
                return ServiceResult.NotFound(new { message = "Round not found." });
            if (!round.IsRankingPublished)
                return ServiceResult.BadRequest(new { message = "Cannot advance before round results are published." });

            var result = await _rankingService.AdvanceRoundAsync(roundId);

            if (!result.Success)
            {
                if (result.Message == "Round not found.")
                    return ServiceResult.NotFound(new { message = result.Message });

                if (result.Data != null)
                    return ServiceResult.BadRequest(new { message = result.Message, details = result.Data });

                return ServiceResult.BadRequest(new { message = result.Message });
            }

            AddAudit("RoundAdvanced", roundId, actorUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = result.Message, details = result.Data });
        }

        private void AddAudit(string action, Guid roundId, Guid actorUserId)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityType = "Round",
                EntityId = roundId,
                ActorUserId = actorUserId
            });
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
