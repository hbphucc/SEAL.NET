namespace SEAL.NET.Models.Entities
{
    public class MentorshipNote
    {
        public Guid MentorshipNoteId { get; set; } = Guid.NewGuid();
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public Guid MentorId { get; set; }
        public ApplicationUser Mentor { get; set; } = null!;
    }
}
