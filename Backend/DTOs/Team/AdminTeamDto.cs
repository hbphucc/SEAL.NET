namespace SEAL.NET.DTOs.Team
{
    /// <summary>Admin-facing team list row (GET /api/admin/teams).</summary>
    public class AdminTeamDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public Guid LeaderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? EliminationReason { get; set; }
        public DateTime? EliminatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public AdminTeamCategoryInfo Category { get; set; } = null!;
        public AdminTeamRoundInfo? CurrentRound { get; set; }
        public List<AdminTeamMemberInfo> Members { get; set; } = new();
    }

    public class AdminTeamCategoryInfo
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class AdminTeamRoundInfo
    {
        public Guid RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
    }

    public class AdminTeamMemberInfo
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
