namespace SEAL.NET.DTOs.Submission
{
    public class SubmissionTeamInfo
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? Category { get; set; }
    }

    public class SubmissionRoundInfo
    {
        public Guid RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
    }

    /// <summary>Full submission read model returned by GET /api/submissions/{id}.</summary>
    public class SubmissionDetailDto
    {
        public Guid SubmissionId { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? DemoUrl { get; set; }
        public string? SlideUrl { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsWithdrawn { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public SubmissionTeamInfo Team { get; set; } = null!;
        public SubmissionRoundInfo Round { get; set; } = null!;
    }

    /// <summary>Submission list item scoped to one team.</summary>
    public class TeamSubmissionDto
    {
        public Guid SubmissionId { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? DemoUrl { get; set; }
        public string? SlideUrl { get; set; }
        public DateTime SubmittedAt { get; set; }
        public SubmissionRoundInfo Round { get; set; } = null!;
    }

    /// <summary>Submission list item scoped to one round (carries team + category).</summary>
    public class RoundSubmissionDto
    {
        public Guid SubmissionId { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? DemoUrl { get; set; }
        public string? SlideUrl { get; set; }
        public DateTime SubmittedAt { get; set; }
        public SubmissionTeamInfo Team { get; set; } = null!;
    }
}
