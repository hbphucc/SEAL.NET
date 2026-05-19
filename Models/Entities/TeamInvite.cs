using SEAL.NET.Models.Enums;

namespace SEAL.NET.Models.Entities
{
    public class TeamInvite
    {
        public Guid TeamInviteId { get; set; } = Guid.NewGuid();
        public TeamInviteStatus Status { get; set; } = TeamInviteStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
        public DateTime? RespondedAt { get; set; }

        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public Guid InvitedUserId { get; set; }
        public ApplicationUser InvitedUser { get; set; } = null!;

        public Guid InvitedByUserId { get; set; }
        public ApplicationUser InvitedByUser { get; set; } = null!;
    }
}
