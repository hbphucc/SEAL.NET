using System.ComponentModel.DataAnnotations;

namespace SEAL.NET.DTOs.Team
{
    public class TransferLeadershipRequest
    {
        [Required]
        public string NewLeaderStudentCode { get; set; } = string.Empty;
    }
}