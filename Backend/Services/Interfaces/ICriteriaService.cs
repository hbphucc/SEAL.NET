using SEAL.NET.Common;
using SEAL.NET.DTOs.Criteria;

namespace SEAL.NET.Services.Interfaces
{
    public interface ICriteriaService
    {
        Task<ServiceResult> GetCriteriaAsync(Guid roundId);
        Task<ServiceResult> CreateCriteriaAsync(Guid roundId, CreateCriteriaRequest request);
        Task<ServiceResult> UpdateCriteriaAsync(Guid roundId, Guid criteriaId, UpdateCriteriaRequest request);
        Task<ServiceResult> DeleteCriteriaAsync(Guid roundId, Guid criteriaId);
    }
}
