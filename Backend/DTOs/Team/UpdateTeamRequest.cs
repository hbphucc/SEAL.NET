namespace SEAL.NET.DTOs.Team
{
    public class UpdateTeamRequest
    {
        public string TeamName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
    }
}
