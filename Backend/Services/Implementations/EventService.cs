using SEAL.NET.DTOs.Category;
using SEAL.NET.DTOs.Event;
using SEAL.NET.DTOs.Round;
using SEAL.NET.Models.Entities;
using SEAL.NET.Repositories.Interfaces;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        private static DateTime ToUtc(DateTime value)

        {

            if (value.Kind == DateTimeKind.Unspecified)

            {

                throw new ArgumentException("DateTime must specify a timezone (e.g., append 'Z' for UTC).");

            }

            return value.ToUniversalTime();

        }

        public async Task<List<EventResponseDto>> GetAllEventsAsync()
        {
            var events = await _eventRepository.GetEventsWithDetailsAsync();
            return events.Select(MapToDto).ToList();
        }


        public async Task<EventResponseDto?> GetEventByIdAsync(Guid id)
        {
            var eventItem = await _eventRepository.GetEventDetailAsync(id);
            if (eventItem == null) return null;
            return MapToDto(eventItem);
        }

        public async Task<(bool Success, string Message, Guid? Id)> CreateEventAsync(CreateEventRequest request)
        {
            var startDate = ToUtc(request.StartDate);
            var endDate = ToUtc(request.EndDate);

            if (endDate <= startDate)
                return (false, "EndDate must be greater than StartDate.", null);

            var newEvent = new Event
            {
                EventName = request.EventName,
                Description = request.Description,
                StartDate = startDate,
                EndDate = endDate,
                Status = request.Status
            };

            await _eventRepository.AddAsync(newEvent);
            await _eventRepository.SaveChangesAsync();
            return (true, "Created successfully.", newEvent.EventId);
        }

        public async Task<(bool Success, string Message)> UpdateEventAsync(Guid id, UpdateEventRequest request)
        {
            var eventItem = await _eventRepository.GetEventDetailAsync(id);
            if (eventItem == null) return (false, "Event not found.");

            var startDate = ToUtc(request.StartDate);
            var endDate = ToUtc(request.EndDate);

            if (endDate <= startDate)
                return (false, "EndDate must be greater than StartDate.");

            eventItem.EventName = request.EventName;
            eventItem.Description = request.Description;
            eventItem.StartDate = startDate;
            eventItem.EndDate = endDate;
            eventItem.Status = request.Status;

            _eventRepository.Update(eventItem);
            await _eventRepository.SaveChangesAsync();
            return (true, "Updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteEventAsync(Guid id)
        {
            var eventItem = await _eventRepository.GetEventDetailAsync(id);
            if (eventItem == null) return (false, "Event not found.");

            if (eventItem.Categories.Any() || eventItem.Rounds.Any())
                return (false, "Cannot delete event that has categories or rounds.");

            _eventRepository.Delete(eventItem);
            await _eventRepository.SaveChangesAsync();
            return (true, "Deleted successfully.");
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
