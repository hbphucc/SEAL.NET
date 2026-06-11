using SEAL.NET.Common;

namespace SEAL.NET.Services.Interfaces
{
    /// <summary>Round lifecycle transitions driven by admins (open/close/lock/reopen/publish/advance).</summary>
    public interface IAdminRoundService
    {
        Task<ServiceResult> OpenRoundAsync(Guid roundId, Guid actorUserId);
        Task<ServiceResult> CloseRoundAsync(Guid roundId, Guid actorUserId);
        Task<ServiceResult> LockSubmissionsAsync(Guid roundId, Guid actorUserId);
        Task<ServiceResult> ReopenRoundAsync(Guid roundId, Guid actorUserId);
        Task<ServiceResult> PublishResultAsync(Guid roundId, Guid actorUserId);
        Task<ServiceResult> AdvanceRoundAsync(Guid roundId, Guid actorUserId);
    }
}
