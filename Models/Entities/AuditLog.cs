namespace SEAL.NET.Models.Entities
{
    public class AuditLog
    {
        public Guid AuditLogId { get; set; } = Guid.NewGuid();
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid? ActorUserId { get; set; }
        public ApplicationUser? ActorUser { get; set; }
    }
}
