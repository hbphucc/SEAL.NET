using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEAL.NET.Models.Entities
{
    public class JudgeAssignment
    {
        [Key]
        public Guid AssignmentId { get; set; } = Guid.NewGuid();
        public Guid JudgeId { get; set; }
        public Guid RoundId { get; set; }
        public Guid CategoryId { get; set; }

        [ForeignKey(nameof(JudgeId))]
        public ApplicationUser? Judge { get; set; }

        [ForeignKey(nameof(RoundId))]
        public Round? Round { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }
    }
}
