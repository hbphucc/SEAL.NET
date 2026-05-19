using System.ComponentModel.DataAnnotations;

namespace SEAL.NET.DTOs.Team
{
    public class CreateTeamRequest
    {
        [Required, MaxLength(100)]
        public string TeamName { get; set; } = string.Empty;

        [Required]
        public Guid CategoryId { get; set; }

        public List<string> MemberStudentCodes { get; set; } = new();
    }
}