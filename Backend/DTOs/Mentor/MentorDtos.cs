namespace SEAL.NET.DTOs.Mentor
{
    public class MentorTeamDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public MentorTeamCategoryInfo Category { get; set; } = null!;
        public List<MentorTeamMemberInfo> Members { get; set; } = new();
    }

    public class MentorTeamCategoryInfo
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class MentorTeamMemberInfo
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsLeader { get; set; }
    }

    public class MentorSubmissionDto
    {
        public Guid SubmissionId { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? DemoUrl { get; set; }
        public string? SlideUrl { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public MentorSubmissionRoundInfo Round { get; set; } = null!;
    }

    public class MentorSubmissionRoundInfo
    {
        public Guid RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
    }

    public class MentorshipNoteDto
    {
        public Guid MentorshipNoteId { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public MentorshipNoteMentorInfo Mentor { get; set; } = null!;
    }

    public class MentorshipNoteMentorInfo
    {
        public Guid MentorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
