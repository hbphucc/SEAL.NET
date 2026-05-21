
namespace SEAL.NET.Models.Entities
{
    using SEAL.NET.Models.Enums;

    public class Round
    {
        public Guid RoundId { get; set; } = Guid.NewGuid();
        public string RoundName { get; set; } = string.Empty;
        public int RoundOrder { get; set; }
        public int MaxTeamsAdvancing { get; set; }
        public bool IsRankingPublished { get; set; } = false;
        public RoundStatus Status { get; set; } = RoundStatus.Draft;
        public bool IsSubmissionLocked { get; set; } = false;
        public DateTime? SubmissionDeadline { get; set; }


        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;

        public List<Criteria> CriteriaList { get; set; } = new List<Criteria>();
        public List<JudgeAssignment> JudgeAssignments { get; set; } = new List<JudgeAssignment>();
        public List<Submission> Submissions { get; set; } = new List<Submission>();
    }
}
