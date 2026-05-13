using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEAL.NET.Models.Entities
{
    public class Round
    {
        [Key]
        public Guid RoundId { get; set; } = Guid.NewGuid();
        public Guid EventId { get; set; }

        [Required, MaxLength(100)]
        public string RoundName { get; set; } = string.Empty;
        public DateTime SubmissionDeadline { get; set; }
        public int RoundOrder { get; set; }
        public int MaxTeamsAdvancing { get; set; } = 0;


        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }
        public ICollection<Criteria> CriteriaList { get; set; } = new List<Criteria>();
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public ICollection<JudgeAssignment> JudgeAssignments { get; set; } = new List<JudgeAssignment>();
    }
}
