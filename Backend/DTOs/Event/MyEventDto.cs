namespace SEAL.NET.DTOs.Event
{
    /// <summary>An event the current user has registered for (GET /api/events/mine).</summary>
    public class MyEventDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public bool IsArchived { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
