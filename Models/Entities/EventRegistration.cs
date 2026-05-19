namespace SEAL.NET.Models.Entities
{
    public class EventRegistration
    {
        public Guid EventRegistrationId { get; set; } = Guid.NewGuid();
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}
