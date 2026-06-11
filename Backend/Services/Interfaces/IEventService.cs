using SEAL.NET.Common;
using SEAL.NET.DTOs.Event;

namespace SEAL.NET.Services.Interfaces
{
    public interface IEventService
    {
        Task<List<EventResponseDto>> GetAllEventsAsync();
        Task<List<PublicEventDto>> GetPublicEventsAsync();
        Task<EventResponseDto?> GetEventByIdAsync(Guid id);
        Task<List<MyEventDto>> GetMyEventsAsync(Guid userId);

        Task<ServiceResult> CreateEventAsync(CreateEventRequest request, Guid? actorUserId);
        Task<ServiceResult> UpdateEventAsync(Guid id, UpdateEventRequest request, Guid? actorUserId);
        Task<ServiceResult> DeleteEventAsync(Guid id, Guid? actorUserId);

        Task<ServiceResult> PublishEventAsync(Guid id, Guid? actorUserId);
        Task<ServiceResult> CloseRegistrationAsync(Guid id, Guid? actorUserId);
        Task<ServiceResult> StartJudgingAsync(Guid id, Guid? actorUserId);
        Task<ServiceResult> EndJudgingAsync(Guid id, Guid? actorUserId);
        Task<ServiceResult> ArchiveEventAsync(Guid id, Guid? actorUserId);

        Task<ServiceResult> JoinEventAsync(Guid id, Guid userId);
        Task<ServiceResult> LeaveEventAsync(Guid id, Guid userId);
    }
}
