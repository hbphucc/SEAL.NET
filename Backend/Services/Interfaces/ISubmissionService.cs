using SEAL.NET.Common;
using SEAL.NET.DTOs.Submission;

namespace SEAL.NET.Services.Interfaces
{
    public interface ISubmissionService
    {
        Task<ServiceResult> SubmitProjectAsync(Guid currentUserId, CreateSubmissionRequest request);
        Task<ServiceResult> GetSubmissionAsync(Guid submissionId, Guid currentUserId, bool isAdmin, bool isJudge, bool isMentorRole);
        Task<ServiceResult> WithdrawSubmissionAsync(Guid submissionId, Guid currentUserId);
        Task<ServiceResult> GetTeamSubmissionsAsync(Guid teamId, Guid currentUserId, bool isAdmin, bool isJudge);
        Task<ServiceResult> GetRoundSubmissionsAsync(Guid roundId, Guid currentUserId, bool isAdmin, bool isJudge);
    }
}
