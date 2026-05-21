using SEAL.NET.Models.Enums;

namespace SEAL.NET.Models.Entities
{
    public class Team
    {
        public Guid TeamId { get; set; } = Guid.NewGuid();
        public string TeamName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TeamStatus Status { get; set; } = TeamStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? EliminationReason { get; set; }
        public string? StatusReason { get; set; }
        public DateTime? EliminatedAt { get; set; }
        public bool IsArchived { get; set; } = false;

        public Guid LeaderId { get; set; }
        public ApplicationUser Leader { get; set; } = null!;

        public Guid? CurrentRoundId { get; set; }
        public Round? CurrentRound { get; set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public List<TeamMember> Members { get; set; } = [];
        public List<Submission> Submissions { get; set; } = [];
        public List<TeamInvite> Invites { get; set; } = [];
        public List<MentorAssignment> MentorAssignments { get; set; } = [];
        public List<MentorshipNote> MentorshipNotes { get; set; } = [];
    }
}
