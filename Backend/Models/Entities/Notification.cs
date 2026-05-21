using SEAL.NET.Models.Enums;

namespace SEAL.NET.Models.Entities
{
    public class Notification
    {
        public Guid NotificationId { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Link { get; set; }
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}
