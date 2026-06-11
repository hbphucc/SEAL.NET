namespace SEAL.NET.DTOs.Score
{
    public class AssignedSubmissionDto
    {
        public Guid SubmissionId { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? DemoUrl { get; set; }
        public string? SlideUrl { get; set; }
        public AssignedSubmissionTeamInfo Team { get; set; } = null!;
        public AssignedSubmissionRoundInfo Round { get; set; } = null!;
    }

    public class AssignedSubmissionTeamInfo
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class AssignedSubmissionRoundInfo
    {
        public Guid RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
    }
}
