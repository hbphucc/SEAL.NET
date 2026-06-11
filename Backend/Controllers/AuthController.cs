using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Auth;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            IAuthService authService,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _authService = authService;
            _configuration = configuration;
            _environment = environment;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
            => (await _authService.RegisterAsync(model)).ToActionResult(this);

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var outcome = await _authService.LoginAsync(model);

            if (outcome.Token != null)
                Response.Cookies.Append("seal_token", outcome.Token, CreateAuthCookieOptions(outcome.Expires));

            return outcome.Result.ToActionResult(this);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("seal_token", CreateAuthCookieOptions());
            return Ok(new { message = "Logout successful." });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? userId = Guid.TryParse(value, out var parsed) ? parsed : null;
            return (await _authService.GetMeAsync(userId)).ToActionResult(this);
        }

        private CookieOptions CreateAuthCookieOptions(DateTimeOffset? expires = null)
        {
            var sameSite = GetCookieSameSiteMode();

            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !_environment.IsDevelopment() || Request.IsHttps || sameSite == SameSiteMode.None,
                SameSite = sameSite,
                Path = "/",
                Expires = expires
            };
        }

        private SameSiteMode GetCookieSameSiteMode()
        {
            var configured = _configuration["Auth:CookieSameSite"];
            if (Enum.TryParse<SameSiteMode>(configured, ignoreCase: true, out var sameSite))
                return sameSite;

            return SameSiteMode.Lax;
        }
    }
}
