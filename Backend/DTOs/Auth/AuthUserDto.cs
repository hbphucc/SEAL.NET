using SEAL.NET.Models.Enums;

namespace SEAL.NET.DTOs.Auth
{
    /// <summary>User profile returned by login and GET /api/auth/me.</summary>
    public class AuthUserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public StudentType? StudentType { get; set; }
        public string? StudentCode { get; set; }
        public string? SchoolName { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
