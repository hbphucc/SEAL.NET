namespace SEAL.NET.DTOs.Team
{
    /// <summary>Read model returned by GET /api/teams/invites/pending.</summary>
    public class PendingInviteDto
    {
        public Guid TeamInviteId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public PendingInviteTeamInfo Team { get; set; } = null!;
    }

    public class PendingInviteTeamInfo
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
