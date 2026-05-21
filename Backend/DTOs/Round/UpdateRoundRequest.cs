using System.ComponentModel.DataAnnotations;

namespace SEAL.NET.DTOs.Round
{
    public class UpdateRoundRequest
    {
        [Required, MaxLength(100)]
        public string RoundName { get; set; } = string.Empty;

        [Required]
        public DateTime SubmissionDeadline { get; set; }

        [Required]
        public int RoundOrder { get; set; }

        [Range(1, 100)]
        public int MaxTeamsAdvancing { get; set; } = 0;
    }
}
