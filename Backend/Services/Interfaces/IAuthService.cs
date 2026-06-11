using SEAL.NET.Common;
using SEAL.NET.DTOs.Auth;

namespace SEAL.NET.Services.Interfaces
{
    /// <summary>
    /// Identity/authentication logic. The service issues the JWT but does not touch cookies or
    /// the HTTP response — the controller owns that, since cookie configuration depends on the
    /// request (HTTPS, SameSite, environment).
    /// </summary>
    public interface IAuthService
    {
        Task<ServiceResult> RegisterAsync(RegisterRequest model);
        Task<LoginOutcome> LoginAsync(LoginRequest model);
        Task<ServiceResult> GetMeAsync(Guid? userId);
    }

    /// <summary>
    /// Result of a login attempt. On success <see cref="Token"/>/<see cref="Expires"/> are set so
    /// the controller can append the auth cookie; <see cref="Result"/> always carries the HTTP outcome.
    /// </summary>
    public sealed class LoginOutcome
    {
        public required ServiceResult Result { get; init; }
        public string? Token { get; init; }
        public DateTimeOffset? Expires { get; init; }
    }
}
