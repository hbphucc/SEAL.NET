using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.Models.Enums;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var userId = GetCurrentUserId();
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.Link,
                    status = n.Status.ToString(),
                    n.CreatedAt,
                    n.ReadAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPost("{notificationId}/read")]
        public async Task<IActionResult> MarkRead(Guid notificationId)
        {
            var userId = GetCurrentUserId();
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
            if (notification == null) return NotFound(new { message = "Notification not found." });

            notification.Status = NotificationStatus.Read;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Notification marked as read." });
        }
    }
}
