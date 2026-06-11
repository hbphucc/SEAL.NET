using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Auth;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SEAL.NET.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private const string SecurityStampClaimType = "seal_security_stamp";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<ServiceResult> RegisterAsync(RegisterRequest model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return ServiceResult.BadRequest(new { message = "Email is already used." });

            if (model.StudentType == StudentType.External &&
                string.IsNullOrWhiteSpace(model.SchoolName))
            {
                return ServiceResult.BadRequest(new { message = "School name is required for external students." });
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
                return ServiceResult.BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Member"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Member"));

            await _userManager.AddToRoleAsync(user, "Member");

            return ServiceResult.Ok(new { message = "Created account successfully!" });
        }

        public async Task<LoginOutcome> LoginAsync(LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return Fail("Email or password is incorrect.");

            var signInResult = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
                return Fail("Too many failed login attempts. Please try again later.");

            if (!signInResult.Succeeded)
                return Fail("Email or password is incorrect.");

            if (!user.IsApproved)
                return Fail("Your account is waiting for approval.");

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

            var body = new
            {
                message = "Login successful.",
                user = MapUser(user, userRoles)
            };

            return new LoginOutcome
            {
                Result = ServiceResult.Ok(body),
                Token = tokenString,
                Expires = token.ValidTo
            };
        }

        public async Task<ServiceResult> GetMeAsync(Guid? userId)
        {
            if (userId == null)
                return ServiceResult.Unauthorized(new { message = "User not found or not approved." });

            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null || !user.IsApproved)
                return ServiceResult.Unauthorized(new { message = "User not found or not approved." });

            var userRoles = await _userManager.GetRolesAsync(user);
            return ServiceResult.Ok(MapUser(user, userRoles));
        }

        private static LoginOutcome Fail(string message) => new()
        {
            Result = ServiceResult.Unauthorized(new { message })
        };

        private static AuthUserDto MapUser(ApplicationUser user, IList<string> roles) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            StudentType = user.StudentType,
            StudentCode = user.StudentCode,
            SchoolName = user.SchoolName,
            IsApproved = user.IsApproved,
            CreatedAt = user.CreatedAt,
            Roles = roles
        };

        private JwtSecurityToken GenerateNewJsonWebToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            return new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDays"])),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
        }
    }
}
