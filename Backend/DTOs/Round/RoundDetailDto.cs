namespace SEAL.NET.DTOs.Round
{
    /// <summary>Flat round read model used by the rounds endpoints.</summary>
    public class RoundDetailDto
    {
        public Guid RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
        public DateTime? SubmissionDeadline { get; set; }
        public int RoundOrder { get; set; }
        public int MaxTeamsAdvancing { get; set; }
        public Guid EventId { get; set; }
    }
}
