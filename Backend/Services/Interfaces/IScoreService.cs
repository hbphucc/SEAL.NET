using SEAL.NET.Common;
using SEAL.NET.DTOs.Score;

namespace SEAL.NET.Services.Interfaces
{
    public interface IScoreService
    {
        Task<ServiceResult> SubmitScoreAsync(Guid judgeId, CreateScoreRequest request);
        Task<ServiceResult> SubmitBulkScoresAsync(Guid judgeId, BulkScoreRequest request);
        Task<List<AssignedSubmissionDto>> GetMyAssignedSubmissionsAsync(Guid judgeId);
    }
}
