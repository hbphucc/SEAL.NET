using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEAL.NET.Models.Entities
{
    public class TeamMember
    {
        [Key]
        public Guid TeamMemberId { get; set; } = Guid.NewGuid();
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }

        [ForeignKey(nameof(TeamId))]
        public Team? Team { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }
    }
}
