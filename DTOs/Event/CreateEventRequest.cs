using System.ComponentModel.DataAnnotations;
using SEAL.NET.Models.Enums;

namespace SEAL.NET.DTOs.Event
{
    public class CreateEventRequest
    {
        [Required, MaxLength(150)]
        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public EventStatus Status { get; set; } = EventStatus.Upcoming;
    }
}