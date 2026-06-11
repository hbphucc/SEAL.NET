using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.User;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
            => Ok(await _adminUserService.GetUsersAsync());

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingUsers()
            => Ok(await _adminUserService.GetPendingUsersAsync());

        [HttpPut("{userId}/approve")]
        public async Task<IActionResult> ApproveUser(Guid userId)
            => (await _adminUserService.ApproveUserAsync(userId)).ToActionResult(this);

        [HttpPut("{userId}/reject")]
        public async Task<IActionResult> RejectUser(Guid userId)
            => (await _adminUserService.RejectUserAsync(userId)).ToActionResult(this);

        [HttpPut("{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
            => (await _adminUserService.UpdateUserRoleAsync(userId, request)).ToActionResult(this);

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
            => (await _adminUserService.DeleteUserAsync(userId)).ToActionResult(this);
    }
}
