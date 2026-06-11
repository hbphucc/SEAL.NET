using SEAL.NET.Common;
using SEAL.NET.DTOs.Notification;

namespace SEAL.NET.Services.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetMineAsync(Guid userId);
        Task<ServiceResult> MarkReadAsync(Guid userId, Guid notificationId);
    }
}
