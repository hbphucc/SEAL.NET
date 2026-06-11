using SEAL.NET.Common;
using SEAL.NET.DTOs.Judge;

namespace SEAL.NET.Services.Interfaces
{
    public interface IJudgeAssignmentService
    {
        Task<List<JudgeAssignmentDto>> GetAssignmentsAsync();
        Task<ServiceResult> CreateAssignmentAsync(CreateJudgeAssignmentRequest request);
        Task<ServiceResult> DeleteAssignmentAsync(Guid assignmentId);
    }
}
