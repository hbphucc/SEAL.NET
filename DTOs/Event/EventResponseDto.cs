using SEAL.NET.DTOs.Category;
using SEAL.NET.DTOs.Round;

namespace SEAL.NET.DTOs.Event
{
    public class EventResponseDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? RegistrationClosedAt { get; set; }
        public DateTime? JudgingStartedAt { get; set; }
        public DateTime? JudgingEndedAt { get; set; }
        public List<CategoryDto> Categories { get; set; } = [];
        public List<RoundDto> Rounds { get; set; } = [];
    }

}
