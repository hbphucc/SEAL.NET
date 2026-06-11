using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.DTOs.User;
using SEAL.NET.Models.Entities;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class AdminUserService : IAdminUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ApplicationDbContext _context;

        private static readonly string[] AllowedRoles =
        {
            "Admin",
            "Member",
            "TeamLeader",
            "Judge",
            "Mentor"
        };

        public AdminUserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<List<AdminUserDto>> GetUsersAsync()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return await MapUsersAsync(users);
        }

        public async Task<List<AdminUserDto>> GetPendingUsersAsync()
        {
            var users = await _userManager.Users
                .Where(u => !u.IsApproved)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return await MapUsersAsync(users);
        }

        public async Task<ServiceResult> ApproveUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ServiceResult.NotFound(new { message = "User not found." });

            user.IsApproved = true;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return ServiceResult.BadRequest(result.Errors);

            await _userManager.UpdateSecurityStampAsync(user);

            return ServiceResult.Ok(new { message = "User approved successfully." });
        }

        public async Task<ServiceResult> RejectUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ServiceResult.NotFound(new { message = "User not found." });

            if (await IsLastApprovedAdminAsync(user))
                return ServiceResult.BadRequest(new { message = "Cannot reject the last approved Admin account." });

            user.IsApproved = false;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return ServiceResult.BadRequest(result.Errors);

            await _userManager.UpdateSecurityStampAsync(user);

            return ServiceResult.Ok(new { message = "User rejected successfully." });
        }

        public async Task<ServiceResult> UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request)
        {
            if (!AllowedRoles.Contains(request.Role))
                return ServiceResult.BadRequest(new { message = "Invalid role." });

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ServiceResult.NotFound(new { message = "User not found." });

            if (!await _roleManager.RoleExistsAsync(request.Role))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(request.Role));

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains("Admin") &&
                request.Role != "Admin" &&
                await IsLastApprovedAdminAsync(user))
            {
                return ServiceResult.BadRequest(new { message = "Cannot demote the last approved Admin account." });
            }

            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!removeResult.Succeeded)
                    return ServiceResult.BadRequest(removeResult.Errors);
            }

            var addResult = await _userManager.AddToRoleAsync(user, request.Role);

            if (!addResult.Succeeded)
                return ServiceResult.BadRequest(addResult.Errors);

            await _userManager.UpdateSecurityStampAsync(user);

            return ServiceResult.Ok(new
            {
                message = "User role updated successfully.",
                userId = user.Id,
                role = request.Role
            });
        }

        public async Task<ServiceResult> DeleteUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ServiceResult.NotFound(new { message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
                return ServiceResult.BadRequest(new { message = "Cannot delete an Admin account." });

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
                return ServiceResult.BadRequest(result.Errors);

            return ServiceResult.Ok(new { message = "User deleted successfully." });
        }

        private async Task<List<AdminUserDto>> MapUsersAsync(List<ApplicationUser> users)
        {
            var userIds = users.Select(u => u.Id).ToList();

            // Bulk-load every user's roles in a single query (avoids an N+1 GetRolesAsync per user).
            var rolesByUser = await (
                from userRole in _context.UserRoles
                join role in _context.Roles on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new { userRole.UserId, role.Name })
                .ToListAsync();

            var roleLookup = rolesByUser
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name!).ToList());

            return users.Select(user => new AdminUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsApproved = user.IsApproved,
                CreatedAt = user.CreatedAt,
                StudentType = user.StudentType,
                StudentCode = user.StudentCode,
                SchoolName = user.SchoolName,
                Roles = roleLookup.TryGetValue(user.Id, out var roles) ? roles : new List<string>()
            }).ToList();
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
