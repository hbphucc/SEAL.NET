using SEAL.NET.Common;
using SEAL.NET.DTOs.User;

namespace SEAL.NET.Services.Interfaces
{
    public interface IAdminUserService
    {
        Task<List<AdminUserDto>> GetUsersAsync();
        Task<List<AdminUserDto>> GetPendingUsersAsync();
        Task<ServiceResult> ApproveUserAsync(Guid userId);
        Task<ServiceResult> RejectUserAsync(Guid userId);
        Task<ServiceResult> UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request);
        Task<ServiceResult> DeleteUserAsync(Guid userId);
    }
}
