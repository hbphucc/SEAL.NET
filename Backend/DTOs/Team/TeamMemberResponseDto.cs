namespace SEAL.NET.DTOs.Team
{
    public class TeamMemberResponseDto
    {
        public Guid UserId { get; set; }
        public string? StudentCode { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsLeader { get; set; }
    }
}
