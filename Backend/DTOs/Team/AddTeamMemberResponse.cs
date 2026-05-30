namespace SEAL.NET.DTOs.Team
{
    public class AddTeamMemberResponse
    {
        public string Message { get; set; } = string.Empty;
        public TeamResponseDto Team { get; set; } = new();
        public TeamMemberResponseDto AddedMember { get; set; } = new();
    }
}
