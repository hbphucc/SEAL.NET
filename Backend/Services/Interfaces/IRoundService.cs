using SEAL.NET.Common;
using SEAL.NET.DTOs.Round;

namespace SEAL.NET.Services.Interfaces
{
    /// <summary>Round CRUD owned by event admins (create/read/update/delete).</summary>
    public interface IRoundService
    {
        Task<ServiceResult> GetRoundsAsync(Guid eventId);
        Task<RoundDetailDto?> GetRoundByIdAsync(Guid eventId, Guid roundId);
        Task<ServiceResult> CreateRoundAsync(Guid eventId, CreateRoundRequest request);
        Task<ServiceResult> UpdateRoundAsync(Guid eventId, Guid roundId, UpdateRoundRequest request);
        Task<ServiceResult> DeleteRoundAsync(Guid eventId, Guid roundId);
    }
}
