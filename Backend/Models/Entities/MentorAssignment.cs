namespace SEAL.NET.Models.Entities
{
    public class MentorAssignment
    {
        public Guid MentorAssignmentId { get; set; } = Guid.NewGuid();
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public Guid MentorId { get; set; }
        public ApplicationUser Mentor { get; set; } = null!;
    }
}
