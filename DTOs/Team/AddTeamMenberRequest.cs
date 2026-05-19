using System.ComponentModel.DataAnnotations;

namespace SEAL.NET.DTOs.Team
{
    public class AddTeamMemberRequest
    {
        [Required]
        public string StudentCode { get; set; } = string.Empty;
    }
}