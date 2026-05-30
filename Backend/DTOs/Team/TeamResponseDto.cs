namespace SEAL.NET.DTOs.Team
{
    public class TeamResponseDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid LeaderId { get; set; }
        public TeamCategoryResponseDto Category { get; set; } = new();
        public TeamRoundResponseDto? CurrentRound { get; set; }
        public List<TeamMemberResponseDto> Members { get; set; } = [];
    }
}
