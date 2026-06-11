using SEAL.NET.Common;
using SEAL.NET.DTOs.Team;

namespace SEAL.NET.Services.Interfaces
{
    public interface IAdminTeamService
    {
        Task<List<AdminTeamDto>> GetTeamsAsync();
        Task<ServiceResult> ApproveTeamAsync(Guid teamId, Guid? actorUserId);
        Task<ServiceResult> RejectTeamAsync(Guid teamId, TeamDecisionRequest? request, Guid? actorUserId);
        Task<ServiceResult> EliminateTeamAsync(Guid teamId, EliminateTeamRequest request, Guid? actorUserId);
        Task<ServiceResult> DeleteTeamAsync(Guid teamId, Guid? actorUserId);
    }
}
