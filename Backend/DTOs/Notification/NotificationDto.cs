namespace SEAL.NET.DTOs.Notification
{
    public class NotificationDto
    {
        public Guid NotificationId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Link { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
