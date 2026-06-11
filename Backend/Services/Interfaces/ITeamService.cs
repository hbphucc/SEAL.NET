using SEAL.NET.Common;
using SEAL.NET.DTOs.Team;

namespace SEAL.NET.Services.Interfaces
{
    /// <summary>
    /// Application/Service layer for the Team aggregate. Owns all team business rules,
    /// validation and persistence. Identity is passed in as primitives (userId, isAdmin)
    /// so the service has no dependency on HttpContext / ClaimsPrincipal.
    /// </summary>
    public interface ITeamService
    {
        Task<ServiceResult> CreateTeamAsync(Guid leaderId, CreateTeamRequest request);
        Task<TeamDetailDto?> GetMyTeamAsync(Guid userId);
        Task<ServiceResult> UpdateTeamAsync(Guid teamId, Guid currentUserId, bool isAdmin, UpdateTeamRequest request);
        Task<ServiceResult> AddMemberAsync(Guid teamId, Guid currentUserId, AddTeamMemberRequest request);
        Task<ServiceResult> InviteMemberAsync(Guid teamId, Guid currentUserId, InviteTeamMemberRequest request);
        Task<List<PendingInviteDto>> GetPendingInvitesAsync(Guid userId);
        Task<ServiceResult> RespondToInviteAsync(Guid inviteId, Guid currentUserId, bool accept);
        Task<ServiceResult> CancelInviteAsync(Guid teamId, Guid inviteId, Guid currentUserId);
        Task<ServiceResult> RemoveMemberAsync(Guid teamId, string studentCode, Guid currentUserId);
        Task<ServiceResult> LeaveTeamAsync(Guid teamId, Guid currentUserId);
        Task<ServiceResult> DisbandTeamAsync(Guid teamId, Guid currentUserId);
        Task<ServiceResult> TransferLeadershipAsync(Guid teamId, Guid currentUserId, TransferLeadershipRequest request);
    }
}
