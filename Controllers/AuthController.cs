using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SEAL.NET.DTOs.Auth;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SEAL.NET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const string SecurityStampClaimType = "seal_security_stamp";
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _environment = environment;
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
            {
                return sameSite;
            }

            return SameSiteMode.Lax;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new { message = "Email is already used." });

            if (model.StudentType == StudentType.External &&
            string.IsNullOrWhiteSpace(model.SchoolName))
            {
                return BadRequest(new
                {
                    message = "School name is required for external students."
                });
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                StudentType = model.StudentType,
                StudentCode = model.StudentCode,
                SchoolName = model.SchoolName,
                IsApproved = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Member"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Member"));

            await _userManager.AddToRoleAsync(user, "Member");

            return Ok(new { message = "Created account successfully!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return Unauthorized(new { message = "Email or password is incorrect." });

            var signInResult = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
                return Unauthorized(new { message = "Too many failed login attempts. Please try again later." });

            if (!signInResult.Succeeded)
                return Unauthorized(new { message = "Email or password is incorrect." });

            if (!user.IsApproved)
                return Unauthorized(new { message = "Your account is waiting for approval." });

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("FullName", user.FullName),
                new Claim(SecurityStampClaimType, user.SecurityStamp ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = GenerateNewJsonWebToken(authClaims);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            Response.Cookies.Append("seal_token", tokenString, CreateAuthCookieOptions(token.ValidTo));

            return Ok(new
            {
                message = "Login successful.",
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    studentType = user.StudentType,
                    studentCode = user.StudentCode,
                    schoolName = user.SchoolName,
                    isApproved = user.IsApproved,
                    createdAt = user.CreatedAt,
                    roles = userRoles
                }
            });
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsApproved)
                return Unauthorized(new { message = "User not found or not approved." });

            var userRoles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                studentType = user.StudentType,
                studentCode = user.StudentCode,
                schoolName = user.SchoolName,
                isApproved = user.IsApproved,
                createdAt = user.CreatedAt,
                roles = userRoles
            });
        }

        private JwtSecurityToken GenerateNewJsonWebToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDays"])),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }
    }
}
