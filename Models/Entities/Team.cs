using SEAL.NET.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEAL.NET.Models.Entities
{
    public class Team
    {
        [Key]
        public Guid TeamId { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string TeamName { get; set; } = string.Empty;

        public Guid LeaderId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? CurrentRoundId { get; set; } 

        public TeamStatus Status { get; set; } = TeamStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [ForeignKey(nameof(LeaderId))]
        public ApplicationUser? Leader { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [ForeignKey(nameof(CurrentRoundId))]
        public Round? CurrentRound { get; set; }

        public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}
