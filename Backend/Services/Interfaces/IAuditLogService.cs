using SEAL.NET.DTOs.Audit;

namespace SEAL.NET.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task<List<AuditLogDto>> GetRecentAsync();
        Task<List<ScoreAuditLogDto>> GetScoreAuditLogsForSubmissionAsync(Guid submissionId);
    }
}
