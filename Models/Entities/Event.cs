using SEAL.NET.Models.Enums;

namespace SEAL.NET.Models.Entities
{
    public class Event
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Upcoming;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPublished { get; set; } = false;
        public bool IsArchived { get; set; } = false;
        public DateTime? RegistrationClosedAt { get; set; }
        public DateTime? JudgingStartedAt { get; set; }
        public DateTime? JudgingEndedAt { get; set; }


        public List<Category> Categories { get; set; } = new();
        public List<Round> Rounds { get; set; } = new();
        public List<EventRegistration> Registrations { get; set; } = new();
    }
}
