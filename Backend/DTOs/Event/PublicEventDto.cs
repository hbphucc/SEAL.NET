namespace SEAL.NET.DTOs.Event
{
    /// <summary>Public catalogue view of a published event (GET /api/events/public).</summary>
    public class PublicEventDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int TotalTeams { get; set; }
        public List<PublicEventCategoryInfo> Categories { get; set; } = new();
        public List<PublicEventRoundInfo> Rounds { get; set; } = new();
    }

    public class PublicEventCategoryInfo
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TeamCount { get; set; }
    }

    public class PublicEventRoundInfo
    {
        public Guid RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
        public int RoundOrder { get; set; }
        public DateTime? SubmissionDeadline { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsRankingPublished { get; set; }
    }
}
