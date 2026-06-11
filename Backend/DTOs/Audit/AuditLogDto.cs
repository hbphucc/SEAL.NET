namespace SEAL.NET.DTOs.Audit
{
    public class AuditLogDto
    {
        public Guid AuditLogId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public ActorDto? Actor { get; set; }
    }

    public class ActorDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
