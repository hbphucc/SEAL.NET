using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Event;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ApplicationDbContext _context;

        public EventsController(IEventService eventService, ApplicationDbContext context)
        {
            _eventService = eventService;
            _context = context;
        }

        private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var result = await _eventService.GetAllEventsAsync();
            return Ok(result);
        }


        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicEvents()
        {
            var events = await _context.Events
                .Where(e => e.IsPublished && !e.IsArchived)
                .Include(e => e.Categories)
                    .ThenInclude(c => c.Teams)
                .Include(e => e.Rounds)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.Description,
                    e.StartDate,
                    e.EndDate,
                    status = e.Status.ToString(),
                    e.IsPublished,
                    totalTeams = e.Categories.SelectMany(c => c.Teams).Count(),
                    categories = e.Categories.Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.Description,
                        teamCount = c.Teams.Count
                    }),
                    rounds = e.Rounds.OrderBy(r => r.RoundOrder).Select(r => new
                    {
                        r.RoundId,
                        r.RoundName,
                        r.RoundOrder,
                        r.SubmissionDeadline,
                        status = r.Status.ToString(),
                        r.IsRankingPublished
                    })
                })
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            var result = await _eventService.GetEventByIdAsync(id);
            if (result == null) return NotFound(new { message = "Event not found." });
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            (bool Success, string Message, Guid? Id) result;
            try
            {
                result = await _eventService.CreateEventAsync(request);
            }
            catch (ArgumentException)
            {
                return BadRequest(new { message = "Datetime must include UTC or timezone offset." });
            }

            if (!result.Success) return BadRequest(new { message = result.Message });
            await AddAuditAsync("EventCreated", "Event", result.Id, null);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEventById), new { id = result.Id }, new { id = result.Id });
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
        {
            (bool Success, string Message) result;
            try
            {
                result = await _eventService.UpdateEventAsync(id, request);
            }
            catch (ArgumentException)
            {
                return BadRequest(new { message = "Datetime must include UTC or timezone offset." });
            }

            if (!result.Success && result.Message == "Event not found.")
                return NotFound(new { message = result.Message });

            if (!result.Success) return BadRequest(new { message = result.Message });
            await AddAuditAsync("EventUpdated", "Event", id, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = result.Message });
        }


        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var result = await _eventService.DeleteEventAsync(id);
            if (!result.Success && result.Message == "Event not found.")
                return NotFound(new { message = result.Message });

            if (!result.Success) return BadRequest(new { message = result.Message });
            await AddAuditAsync("EventDeleted", "Event", id, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = result.Message });
        }


        [HttpPost("{id:guid}/publish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PublishEvent(Guid id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound(new { message = "Event not found." });
            if (eventItem.IsArchived) return BadRequest(new { message = "Archived events cannot be published." });

            eventItem.IsPublished = true;
            if (eventItem.Status == EventStatus.Draft)
                eventItem.Status = EventStatus.Upcoming;

            await AddAuditAsync("EventPublished", "Event", id, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Event published successfully." });
        }


        [HttpPost("{id:guid}/close-registration")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CloseRegistration(Guid id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound(new { message = "Event not found." });
            if (!eventItem.IsPublished) return BadRequest(new { message = "Cannot close registration before publishing the event." });
            if (eventItem.IsArchived) return BadRequest(new { message = "Archived events cannot be changed." });
            if (eventItem.RegistrationClosedAt != null) return BadRequest(new { message = "Registration is already closed." });

            eventItem.RegistrationClosedAt = DateTime.UtcNow;
            eventItem.Status = EventStatus.RegistrationClosed;
            await AddAuditAsync("RegistrationClosed", "Event", id, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Registration closed successfully." });
        }


        [HttpPost("{id:guid}/start-judging")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StartJudging(Guid id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound(new { message = "Event not found." });
            if (eventItem.RegistrationClosedAt == null) return BadRequest(new { message = "Cannot start judging before registration is closed." });
            if (eventItem.JudgingStartedAt != null) return BadRequest(new { message = "Judging already started." });

            eventItem.JudgingStartedAt = DateTime.UtcNow;
            eventItem.Status = EventStatus.Judging;
            await AddAuditAsync("JudgingStarted", "Event", id, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Judging started successfully." });
        }

        [HttpPost("{id:guid}/end-judging")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EndJudging(Guid id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound(new { message = "Event not found." });
            if (eventItem.JudgingStartedAt == null) return BadRequest(new { message = "Cannot end judging before it starts." });
            if (eventItem.JudgingEndedAt != null) return BadRequest(new { message = "Judging already ended." });

            eventItem.JudgingEndedAt = DateTime.UtcNow;
            eventItem.Status = EventStatus.Completed;
            await AddAuditAsync("JudgingEnded", "Event", id, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Judging ended successfully." });
        }


        [HttpPost("{id:guid}/archive")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveEvent(Guid id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound(new { message = "Event not found." });

            eventItem.IsArchived = true;
            eventItem.Status = EventStatus.Archived;
            await AddAuditAsync("EventArchived", "Event", id, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Event archived successfully." });
        }


        [HttpPost("{id:guid}/join")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> JoinEvent(Guid id)
        {
            var userId = GetCurrentUserId();
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound(new { message = "Event not found." });
            if (!eventItem.IsPublished || eventItem.IsArchived) return BadRequest(new { message = "Cannot join an unpublished or archived event." });
            if (eventItem.RegistrationClosedAt != null) return BadRequest(new { message = "Registration is closed for this event." });

            var exists = await _context.EventRegistrations.AnyAsync(r => r.EventId == id && r.UserId == userId);
            if (exists) return BadRequest(new { message = "You already joined this event." });

            _context.EventRegistrations.Add(new EventRegistration { EventId = id, UserId = userId });
            await AddAuditAsync("EventJoined", "Event", id, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Joined event successfully." });
        }


        [HttpDelete("{id:guid}/leave")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> LeaveEvent(Guid id)
        {
            var userId = GetCurrentUserId();
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound(new { message = "Event not found." });
            if (eventItem.RegistrationClosedAt != null || eventItem.JudgingStartedAt != null)
                return BadRequest(new { message = "Cannot leave after registration closes or judging starts." });

            var categoryIds = await _context.Categories.Where(c => c.EventId == id).Select(c => c.CategoryId).ToListAsync();
            var hasTeam = await _context.TeamMembers.AnyAsync(tm => tm.UserId == userId && categoryIds.Contains(tm.Team.CategoryId));
            if (hasTeam) return BadRequest(new { message = "Leave or disband your team before leaving the event." });

            var registration = await _context.EventRegistrations.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);
            if (registration == null) return NotFound(new { message = "Event registration not found." });

            _context.EventRegistrations.Remove(registration);
            await AddAuditAsync("EventLeft", "Event", id, null);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Left event successfully." });
        }


        [HttpGet("mine")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = GetCurrentUserId();
            var events = await _context.EventRegistrations
                .Where(r => r.UserId == userId)
                .Include(r => r.Event)
                .OrderByDescending(r => r.RegisteredAt)
                .Select(r => new
                {
                    r.Event.EventId,
                    r.Event.EventName,
                    r.Event.Description,
                    r.Event.StartDate,
                    r.Event.EndDate,
                    status = r.Event.Status.ToString(),
                    r.Event.IsPublished,
                    r.Event.IsArchived,
                    r.RegisteredAt
                })
                .ToListAsync();

            return Ok(events);
        }


        private async Task AddAuditAsync(string action, string entityType, Guid? entityId, string? details)
        {
            Guid? actorId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : null;
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                ActorUserId = actorId
            });
            await Task.CompletedTask;
        }
    }
}
