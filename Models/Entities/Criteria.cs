using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEAL.NET.Models.Entities
{
    public class Criteria
    {
        [Key]
        public Guid CriteriaId { get; set; } = Guid.NewGuid();
        public Guid RoundId { get; set; }

        [Required, MaxLength(100)]
        public string CriteriaName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Weight { get; set; }

        [ForeignKey(nameof(RoundId))]
        public Round? Round { get; set; }
        public ICollection<Score> Scores { get; set; } = new List<Score>();
    }
}
