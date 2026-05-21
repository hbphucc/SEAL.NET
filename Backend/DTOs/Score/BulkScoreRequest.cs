using System.ComponentModel.DataAnnotations;

namespace SEAL.NET.DTOs.Score
{
    public class BulkScoreRequest
    {
        [Required]
        public Guid SubmissionId { get; set; }

        [Required]
        [MinLength(1)]
        public List<BulkScoreItemRequest> Scores { get; set; } = [];

        public bool SubmitFinal { get; set; } = false;
    }


    public class BulkScoreItemRequest
    {
        [Required]
        public Guid CriteriaId { get; set; }

        [Range(0, 100)]
        public decimal ScoreValue { get; set; }

        public string? Comment { get; set; }
    }
}
