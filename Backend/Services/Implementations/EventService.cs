using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Category;
using SEAL.NET.DTOs.Event;
using SEAL.NET.DTOs.Round;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Repositories.Interfaces;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly ApplicationDbContext _context;

        public EventService(IEventRepository eventRepository, ApplicationDbContext context)
        {
            _eventRepository = eventRepository;
            _context = context;
        }

        private static DateTime ToUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("DateTime must specify a timezone (e.g., append 'Z' for UTC).");

            return value.ToUniversalTime();
        }

        public async Task<List<EventResponseDto>> GetAllEventsAsync()
        {
            var events = await _eventRepository.GetEventsWithDetailsAsync();
            return events.Select(MapToDto).ToList();
        }

        public async Task<List<PublicEventDto>> GetPublicEventsAsync()
        {
            return await _context.Events
                .Where(e => e.IsPublished && !e.IsArchived)
                .Include(e => e.Categories)
                    .ThenInclude(c => c.Teams)
                .Include(e => e.Rounds)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new PublicEventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Status = e.Status.ToString(),
                    IsPublished = e.IsPublished,
                    TotalTeams = e.Categories.SelectMany(c => c.Teams).Count(),
                    Categories = e.Categories.Select(c => new PublicEventCategoryInfo
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        Description = c.Description,
                        TeamCount = c.Teams.Count
                    }).ToList(),
                    Rounds = e.Rounds.OrderBy(r => r.RoundOrder).Select(r => new PublicEventRoundInfo
                    {
                        RoundId = r.RoundId,
                        RoundName = r.RoundName,
                        RoundOrder = r.RoundOrder,
                        SubmissionDeadline = r.SubmissionDeadline,
                        Status = r.Status.ToString(),
                        IsRankingPublished = r.IsRankingPublished
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<EventResponseDto?> GetEventByIdAsync(Guid id)
        {
            var eventItem = await _eventRepository.GetEventDetailAsync(id);
            if (eventItem == null) return null;
            return MapToDto(eventItem);
        }

        public async Task<List<MyEventDto>> GetMyEventsAsync(Guid userId)
        {
            return await _context.EventRegistrations
                .Where(r => r.UserId == userId)
                .Include(r => r.Event)
                .OrderByDescending(r => r.RegisteredAt)
                .Select(r => new MyEventDto
                {
                    EventId = r.Event.EventId,
                    EventName = r.Event.EventName,
                    Description = r.Event.Description,
                    StartDate = r.Event.StartDate,
                    EndDate = r.Event.EndDate,
                    Status = r.Event.Status.ToString(),
                    IsPublished = r.Event.IsPublished,
                    IsArchived = r.Event.IsArchived,
                    RegisteredAt = r.RegisteredAt
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> CreateEventAsync(CreateEventRequest request, Guid? actorUserId)
        {
            DateTime startDate, endDate;
            try
            {
                startDate = ToUtc(request.StartDate);
                endDate = ToUtc(request.EndDate);
            }
            catch (ArgumentException)
            {
                return ServiceResult.BadRequest(new { message = "Datetime must include UTC or timezone offset." });
            }

            if (endDate <= startDate)
                return ServiceResult.BadRequest(new { message = "EndDate must be greater than StartDate." });

            var newEvent = new Event
            {
                EventName = request.EventName,
                Description = request.Description,
                StartDate = startDate,
                EndDate = endDate,
                Status = request.Status
            };

            await _eventRepository.AddAsync(newEvent);
            AddAudit("EventCreated", "Event", newEvent.EventId, null, actorUserId);
            await _eventRepository.SaveChangesAsync();

            return ServiceResult.Created(new { id = newEvent.EventId });
        }

        public async Task<ServiceResult> UpdateEventAsync(Guid id, UpdateEventRequest request, Guid? actorUserId)
        {
            var eventItem = await _eventRepository.GetEventDetailAsync(id);
            if (eventItem == null) return ServiceResult.NotFound(new { message = "Event not found." });

            DateTime startDate, endDate;
            try
            {
                startDate = ToUtc(request.StartDate);
                endDate = ToUtc(request.EndDate);
            }
            catch (ArgumentException)
            {
                return ServiceResult.BadRequest(new { message = "Datetime must include UTC or timezone offset." });
            }

            if (endDate <= startDate)
                return ServiceResult.BadRequest(new { message = "EndDate must be greater than StartDate." });

            eventItem.EventName = request.EventName;
            eventItem.Description = request.Description;
            eventItem.StartDate = startDate;
            eventItem.EndDate = endDate;
            eventItem.Status = request.Status;

            _eventRepository.Update(eventItem);
            AddAudit("EventUpdated", "Event", id, null, actorUserId);
            await _eventRepository.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Updated successfully." });
        }

        public async Task<ServiceResult> DeleteEventAsync(Guid id, Guid? actorUserId)
        {
            var eventItem = await _eventRepository.GetEventDetailAsync(id);
            if (eventItem == null) return ServiceResult.NotFound(new { message = "Event not found." });

            if (eventItem.Categories.Any() || eventItem.Rounds.Any())
                return ServiceResult.BadRequest(new { message = "Cannot delete event that has categories or rounds." });

            _eventRepository.Delete(eventItem);
            AddAudit("EventDeleted", "Event", id, null, actorUserId);
            await _eventRepository.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Deleted successfully." });
        }

        public async Task<ServiceResult> PublishEventAsync(Guid id, Guid? actorUserId)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return ServiceResult.NotFound(new { message = "Event not found." });
            if (eventItem.IsArchived) return ServiceResult.BadRequest(new { message = "Archived events cannot be published." });

            eventItem.IsPublished = true;
            if (eventItem.Status == EventStatus.Draft)
                eventItem.Status = EventStatus.Upcoming;

            AddAudit("EventPublished", "Event", id, null, actorUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Event published successfully." });
        }

        public async Task<ServiceResult> CloseRegistrationAsync(Guid id, Guid? actorUserId)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return ServiceResult.NotFound(new { message = "Event not found." });
            if (!eventItem.IsPublished) return ServiceResult.BadRequest(new { message = "Cannot close registration before publishing the event." });
            if (eventItem.IsArchived) return ServiceResult.BadRequest(new { message = "Archived events cannot be changed." });
            if (eventItem.RegistrationClosedAt != null) return ServiceResult.BadRequest(new { message = "Registration is already closed." });

            eventItem.RegistrationClosedAt = DateTime.UtcNow;
            eventItem.Status = EventStatus.RegistrationClosed;
            AddAudit("RegistrationClosed", "Event", id, null, actorUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Registration closed successfully." });
        }

        public async Task<ServiceResult> StartJudgingAsync(Guid id, Guid? actorUserId)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return ServiceResult.NotFound(new { message = "Event not found." });
            if (eventItem.RegistrationClosedAt == null) return ServiceResult.BadRequest(new { message = "Cannot start judging before registration is closed." });
            if (eventItem.JudgingStartedAt != null) return ServiceResult.BadRequest(new { message = "Judging already started." });

            eventItem.JudgingStartedAt = DateTime.UtcNow;
            eventItem.Status = EventStatus.Judging;
            AddAudit("JudgingStarted", "Event", id, null, actorUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Judging started successfully." });
        }

        public async Task<ServiceResult> EndJudgingAsync(Guid id, Guid? actorUserId)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return ServiceResult.NotFound(new { message = "Event not found." });
            if (eventItem.JudgingStartedAt == null) return ServiceResult.BadRequest(new { message = "Cannot end judging before it starts." });
            if (eventItem.JudgingEndedAt != null) return ServiceResult.BadRequest(new { message = "Judging already ended." });

            eventItem.JudgingEndedAt = DateTime.UtcNow;
            eventItem.Status = EventStatus.Completed;
            AddAudit("JudgingEnded", "Event", id, null, actorUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Judging ended successfully." });
        }

        public async Task<ServiceResult> ArchiveEventAsync(Guid id, Guid? actorUserId)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return ServiceResult.NotFound(new { message = "Event not found." });

            eventItem.IsArchived = true;
            eventItem.Status = EventStatus.Archived;
            AddAudit("EventArchived", "Event", id, null, actorUserId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Event archived successfully." });
        }

        public async Task<ServiceResult> JoinEventAsync(Guid id, Guid userId)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return ServiceResult.NotFound(new { message = "Event not found." });
            if (!eventItem.IsPublished || eventItem.IsArchived) return ServiceResult.BadRequest(new { message = "Cannot join an unpublished or archived event." });
            if (eventItem.RegistrationClosedAt != null) return ServiceResult.BadRequest(new { message = "Registration is closed for this event." });

            var exists = await _context.EventRegistrations.AnyAsync(r => r.EventId == id && r.UserId == userId);
            if (exists) return ServiceResult.BadRequest(new { message = "You already joined this event." });

            _context.EventRegistrations.Add(new EventRegistration { EventId = id, UserId = userId });
            AddAudit("EventJoined", "Event", id, null, userId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Joined event successfully." });
        }

        public async Task<ServiceResult> LeaveEventAsync(Guid id, Guid userId)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return ServiceResult.NotFound(new { message = "Event not found." });
            if (eventItem.RegistrationClosedAt != null || eventItem.JudgingStartedAt != null)
                return ServiceResult.BadRequest(new { message = "Cannot leave after registration closes or judging starts." });

            var categoryIds = await _context.Categories.Where(c => c.EventId == id).Select(c => c.CategoryId).ToListAsync();
            var hasTeam = await _context.TeamMembers.AnyAsync(tm => tm.UserId == userId && categoryIds.Contains(tm.Team.CategoryId));
            if (hasTeam) return ServiceResult.BadRequest(new { message = "Leave or disband your team before leaving the event." });

            var registration = await _context.EventRegistrations.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);
            if (registration == null) return ServiceResult.NotFound(new { message = "Event registration not found." });

            _context.EventRegistrations.Remove(registration);
            AddAudit("EventLeft", "Event", id, null, userId);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(new { message = "Left event successfully." });
        }

        private void AddAudit(string action, string entityType, Guid? entityId, string? details, Guid? actorUserId)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                ActorUserId = actorUserId
            });
        }

        private static EventResponseDto MapToDto(Event e) => new()
        {
            EventId = e.EventId,
            EventName = e.EventName,
            Description = e.Description,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            Status = e.Status.ToString(),
            IsPublished = e.IsPublished,
            IsArchived = e.IsArchived,
            RegistrationClosedAt = e.RegistrationClosedAt,
            JudgingStartedAt = e.JudgingStartedAt,
            JudgingEndedAt = e.JudgingEndedAt,
            Categories = e.Categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description,
                TeamCount = c.Teams.Count
            }).ToList(),
            Rounds = e.Rounds
                .OrderBy(r => r.RoundOrder)
                .Select(r => new RoundDto
                {
                    RoundId = r.RoundId,
                    RoundName = r.RoundName,
                    RoundOrder = r.RoundOrder,
                    MaxTeamsAdvancing = r.MaxTeamsAdvancing,
                    SubmissionDeadline = r.SubmissionDeadline
                }).ToList()
        };
    }
}
