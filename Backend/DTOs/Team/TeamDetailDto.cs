namespace SEAL.NET.DTOs.Team
{
    /// <summary>Read model returned by GET /api/teams/my-team.</summary>
    public class TeamDetailDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid LeaderId { get; set; }
        public TeamCategoryInfo Category { get; set; } = null!;
        public TeamRoundInfo? CurrentRound { get; set; }
        public List<TeamMemberInfo> Members { get; set; } = new();
    }

    public class TeamCategoryInfo
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class TeamRoundInfo
    {
        public Guid RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
    }

    public class TeamMemberInfo
    {
        public Guid UserId { get; set; }
        public string? StudentCode { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsLeader { get; set; }
    }
}
