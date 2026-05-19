using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using SEAL.NET.Models.Enums;

namespace SEAL.NET.Models.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public StudentType? StudentType { get; set; }

        [MaxLength(50)]
        public string? StudentCode { get; set; }

        [MaxLength(150)]
        public string? SchoolName { get; set; }

        public ICollection<Team> LedTeams { get; set; } = new List<Team>();
        public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
        public ICollection<EventRegistration> EventRegistrations { get; set; } = new List<EventRegistration>();
        public ICollection<MentorAssignment> MentorAssignments { get; set; } = new List<MentorAssignment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<JudgeAssignment> JudgeAssignments { get; set; } = new List<JudgeAssignment>();
        public ICollection<Score> ScoresGiven { get; set; } = new List<Score>();
    }
}
