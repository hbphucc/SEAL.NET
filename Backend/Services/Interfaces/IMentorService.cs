using SEAL.NET.Common;
using SEAL.NET.DTOs.Mentor;

namespace SEAL.NET.Services.Interfaces
{
    public interface IMentorService
    {
        Task<ServiceResult> AssignMentorAsync(Guid teamId, MentorAssignmentRequest request, Guid actorUserId);
        Task<ServiceResult> UnassignMentorAsync(Guid teamId, Guid mentorId, Guid actorUserId);
        Task<List<MentorTeamDto>> GetAssignedTeamsAsync(Guid mentorId);
        Task<ServiceResult> GetTeamSubmissionsAsync(Guid teamId, Guid mentorId);
        Task<ServiceResult> AddNoteAsync(Guid teamId, Guid mentorId, CreateMentorshipNoteRequest request);
        Task<ServiceResult> GetNotesAsync(Guid teamId, Guid userId, bool isAdmin);
    }
}
