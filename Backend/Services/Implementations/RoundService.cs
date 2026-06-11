using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Round;
using SEAL.NET.Models.Entities;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class RoundService : IRoundService
    {
        private readonly ApplicationDbContext _context;

        public RoundService(ApplicationDbContext context)
        {
            _context = context;
        }

        private static DateTime RequireInputUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("DateTime must specify a timezone (e.g., append 'Z' for UTC).");

            return value.ToUniversalTime();
        }

        private static DateTime PersistedUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }

        public async Task<ServiceResult> GetRoundsAsync(Guid eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
                return ServiceResult.NotFound(new { message = "Event not found." });

            var rounds = await _context.Rounds
                .Where(r => r.EventId == eventId)
                .OrderBy(r => r.RoundOrder)
                .Select(r => new RoundDetailDto
                {
                    RoundId = r.RoundId,
                    RoundName = r.RoundName,
                    SubmissionDeadline = r.SubmissionDeadline,
                    RoundOrder = r.RoundOrder,
                    MaxTeamsAdvancing = r.MaxTeamsAdvancing,
                    EventId = r.EventId
                })
                .ToListAsync();

            return ServiceResult.Ok(rounds);
        }

        public async Task<RoundDetailDto?> GetRoundByIdAsync(Guid eventId, Guid roundId)
        {
            return await _context.Rounds
                .Where(r => r.EventId == eventId && r.RoundId == roundId)
                .Select(r => new RoundDetailDto
                {
                    RoundId = r.RoundId,
                    RoundName = r.RoundName,
                    SubmissionDeadline = r.SubmissionDeadline,
                    RoundOrder = r.RoundOrder,
                    MaxTeamsAdvancing = r.MaxTeamsAdvancing,
                    EventId = r.EventId
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ServiceResult> CreateRoundAsync(Guid eventId, CreateRoundRequest request)
        {
            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
                return ServiceResult.NotFound(new { message = "Event not found." });

            DateTime submissionDeadline;
            try
            {
                submissionDeadline = RequireInputUtc(request.SubmissionDeadline);
            }
            catch (ArgumentException)
            {
                return ServiceResult.BadRequest(new { message = "Datetime must include UTC or timezone offset." });
            }

            var eventStartDate = PersistedUtc(eventItem.StartDate);
            var eventEndDate = PersistedUtc(eventItem.EndDate);

            if (submissionDeadline < eventStartDate || submissionDeadline > eventEndDate)
                return ServiceResult.BadRequest(new { message = "SubmissionDeadline must be within event date range." });

            var duplicateOrder = await _context.Rounds.AnyAsync(r =>
                r.EventId == eventId &&
                r.RoundOrder == request.RoundOrder);

            if (duplicateOrder)
                return ServiceResult.BadRequest(new { message = "RoundOrder already exists in this event." });

            var round = new Round
            {
                EventId = eventId,
                RoundName = request.RoundName,
                SubmissionDeadline = submissionDeadline,
                RoundOrder = request.RoundOrder,
                MaxTeamsAdvancing = request.MaxTeamsAdvancing
            };

            _context.Rounds.Add(round);
            await _context.SaveChangesAsync();

            return ServiceResult.Created(new
            {
                message = "Round created successfully.",
                round.RoundId
            });
        }

        public async Task<ServiceResult> UpdateRoundAsync(Guid eventId, Guid roundId, UpdateRoundRequest request)
        {
            var round = await _context.Rounds
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.RoundId == roundId);

            if (round == null)
                return ServiceResult.NotFound(new { message = "Round not found." });

            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
                return ServiceResult.NotFound(new { message = "Event not found." });

            DateTime submissionDeadline;
            try
            {
                submissionDeadline = RequireInputUtc(request.SubmissionDeadline);
            }
            catch (ArgumentException)
            {
                return ServiceResult.BadRequest(new { message = "Datetime must include UTC or timezone offset." });
            }

            var eventStartDate = PersistedUtc(eventItem.StartDate);
            var eventEndDate = PersistedUtc(eventItem.EndDate);

            if (submissionDeadline < eventStartDate || submissionDeadline > eventEndDate)
                return ServiceResult.BadRequest(new { message = "SubmissionDeadline must be within event date range." });

            var duplicateOrder = await _context.Rounds.AnyAsync(r =>
                r.EventId == eventId &&
                r.RoundId != roundId &&
                r.RoundOrder == request.RoundOrder);

            if (duplicateOrder)
                return ServiceResult.BadRequest(new { message = "RoundOrder already exists in this event." });

            round.RoundName = request.RoundName;
            round.SubmissionDeadline = submissionDeadline;
            round.RoundOrder = request.RoundOrder;
            round.MaxTeamsAdvancing = request.MaxTeamsAdvancing;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Round updated successfully." });
        }

        public async Task<ServiceResult> DeleteRoundAsync(Guid eventId, Guid roundId)
        {
            var round = await _context.Rounds
                .Include(r => r.CriteriaList)
                .Include(r => r.Submissions)
                .Include(r => r.JudgeAssignments)
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.RoundId == roundId);

            if (round == null)
                return ServiceResult.NotFound(new { message = "Round not found." });

            if (round.CriteriaList.Any() || round.Submissions.Any() || round.JudgeAssignments.Any())
                return ServiceResult.BadRequest(new { message = "Cannot delete round because it already has criteria, submissions, or judge assignments." });

            _context.Rounds.Remove(round);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Round deleted successfully." });
        }
    }
}
