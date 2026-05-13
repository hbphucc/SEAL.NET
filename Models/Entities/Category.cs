using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEAL.NET.Models.Entities
{
    public class Category
    {
        [Key]
        public Guid CategoryId { get; set; } = Guid.NewGuid();
        public Guid EventId { get; set; }

        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }


        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<JudgeAssignment> JudgeAssignments { get; set; } = new List<JudgeAssignment>();
    }
}
