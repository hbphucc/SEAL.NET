using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.DTOs.User;
using SEAL.NET.Models.Entities;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        private static readonly string[] AllowedRoles =
        {
            "Admin",
            "Member",
            "TeamLeader",
            "Judge",
            "Mentor"
        };

        public AdminUsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var result = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.IsApproved,
                    user.CreatedAt,
                    user.StudentType,
                    user.StudentCode,
                    user.SchoolName,
                    roles
                });
            }

            return Ok(result);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingUsers()
        {
            var users = await _userManager.Users
                .Where(u => !u.IsApproved)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var result = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.IsApproved,
                    user.CreatedAt,
                    user.StudentType,
                    user.StudentCode,
                    user.SchoolName,
                    roles
                });
            }

            return Ok(result);
        }

        [HttpPut("{userId}/approve")]
        public async Task<IActionResult> ApproveUser(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.IsApproved = true;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.UpdateSecurityStampAsync(user);

            return Ok(new { message = "User approved successfully." });
        }

        [HttpPut("{userId}/reject")]
        public async Task<IActionResult> RejectUser(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return NotFound(new { message = "User not found." });

            if (await IsLastApprovedAdminAsync(user))
                return BadRequest(new { message = "Cannot reject the last approved Admin account." });

            user.IsApproved = false;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.UpdateSecurityStampAsync(user);

            return Ok(new { message = "User rejected successfully." });
        }

        [HttpPut("{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
        {
            if (!AllowedRoles.Contains(request.Role))
                return BadRequest(new { message = "Invalid role." });

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return NotFound(new { message = "User not found." });

            if (!await _roleManager.RoleExistsAsync(request.Role))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(request.Role));

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains("Admin") &&
                request.Role != "Admin" &&
                await IsLastApprovedAdminAsync(user))
            {
                return BadRequest(new { message = "Cannot demote the last approved Admin account." });
            }

            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!removeResult.Succeeded)
                    return BadRequest(removeResult.Errors);
            }

            var addResult = await _userManager.AddToRoleAsync(user, request.Role);

            if (!addResult.Succeeded)
                return BadRequest(addResult.Errors);

            await _userManager.UpdateSecurityStampAsync(user);

            return Ok(new
            {
                message = "User role updated successfully.",
                userId = user.Id,
                role = request.Role
            });
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return NotFound(new { message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
                return BadRequest(new { message = "Cannot delete an Admin account." });

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "User deleted successfully." });
        }

        private async Task<bool> IsLastApprovedAdminAsync(ApplicationUser user)
        {
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
                return false;

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            return adminUsers.Count(admin => admin.IsApproved) <= 1;
        }
    }
}
