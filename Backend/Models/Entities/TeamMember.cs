

namespace SEAL.NET.Models.Entities
{
    using SEAL.NET.Models.Enums;

    public class TeamMember
    {
        public Guid TeamMemberId { get; set; } = Guid.NewGuid();
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
        public bool IsLeader { get; set; } = false;

        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}
