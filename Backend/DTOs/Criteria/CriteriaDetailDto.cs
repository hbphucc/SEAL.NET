namespace SEAL.NET.DTOs.Criteria
{
    public class CriteriaDetailDto
    {
        public Guid CriteriaId { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; }
        public Guid RoundId { get; set; }
    }
}
