namespace SEAL.NET.DTOs.Judge
{
    public class JudgeAssignmentDto
    {
        public Guid AssignmentId { get; set; }
        public JudgeAssignmentJudgeInfo Judge { get; set; } = null!;
        public JudgeAssignmentRoundInfo Round { get; set; } = null!;
        public JudgeAssignmentCategoryInfo Category { get; set; } = null!;
    }

    public class JudgeAssignmentJudgeInfo
    {
        public Guid JudgeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class JudgeAssignmentRoundInfo
    {
        public Guid RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
    }

    public class JudgeAssignmentCategoryInfo
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
