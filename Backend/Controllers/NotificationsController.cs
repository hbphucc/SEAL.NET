using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetMine()
            => Ok(await _notificationService.GetMineAsync(CurrentUserId));

        [HttpPost("{notificationId}/read")]
        public async Task<IActionResult> MarkRead(Guid notificationId)
            => (await _notificationService.MarkReadAsync(CurrentUserId, notificationId)).ToActionResult(this);
    }
}
