namespace SEAL.NET.DTOs.Audit
{
    public class ScoreAuditLogDto
    {
        public Guid ScoreAuditLogId { get; set; }
        public Guid ScoreId { get; set; }
        public Guid SubmissionId { get; set; }
        public Guid CriteriaId { get; set; }
        public string? CriteriaName { get; set; }
        public ActorDto? Judge { get; set; }
        public string Action { get; set; } = string.Empty;
        public decimal? OldScoreValue { get; set; }
        public decimal NewScoreValue { get; set; }
        public string? OldComment { get; set; }
        public string? NewComment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
