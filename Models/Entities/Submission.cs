using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEAL.NET.Models.Entities
{
    public class Submission
    {
        [Key]
        public Guid SubmissionId { get; set; } = Guid.NewGuid();
        public Guid TeamId { get; set; }
        public Guid RoundId { get; set; }

        [MaxLength(500)]
        public string? RepositoryUrl { get; set; }
        [MaxLength(500)]
        public string? DemoUrl { get; set; }
        [MaxLength(500)]
        public string? SlideUrl { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(TeamId))]
        public Team? Team { get; set; }

        [ForeignKey(nameof(RoundId))]
        public Round? Round { get; set; }

        public ICollection<Score> Scores { get; set; } = new List<Score>();
    }
}
