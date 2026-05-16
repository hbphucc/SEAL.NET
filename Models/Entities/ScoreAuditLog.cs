using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEAL.NET.Models.Entities
{
    public class ScoreAuditLog
    {
        [Key]
        public Guid ScoreAuditLogId { get; set; } = Guid.NewGuid();

        public Guid ScoreId { get; set; }
        public Guid SubmissionId { get; set; }
        public Guid JudgeId { get; set; }
        public Guid CriteriaId { get; set; }

        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? OldScoreValue { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal NewScoreValue { get; set; }

        public string? OldComment { get; set; }
        public string? NewComment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ScoreId))]
        public Score? Score { get; set; }

        [ForeignKey(nameof(SubmissionId))]
        public Submission? Submission { get; set; }

        [ForeignKey(nameof(JudgeId))]
        public ApplicationUser? Judge { get; set; }

        [ForeignKey(nameof(CriteriaId))]
        public Criteria? Criteria { get; set; }
    }
}
