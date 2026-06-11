namespace SEAL.NET.DTOs.Ranking
{
    /// <summary>Round-level ranking row (includes the team's category name).</summary>
    public class RankingEntryDto
    {
        public int Rank { get; set; }
        public Guid SubmissionId { get; set; }
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    /// <summary>Category-scoped ranking row (category is implied by the request).</summary>
    public class CategoryRankingEntryDto
    {
        public int Rank { get; set; }
        public Guid SubmissionId { get; set; }
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
