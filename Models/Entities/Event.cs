using SEAL.NET.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SEAL.NET.Models.Entities
{
    public class Event
    {
        [Key]
        public Guid EventId { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Upcoming;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Round> Rounds { get; set; } = new List<Round>();
    }
}
