using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;

namespace SEAL.NET.Controllers
{
    [ApiController]
    [Route("api/admin/audit-logs")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _context.AuditLogs
                .Include(log => log.ActorUser)
                .OrderByDescending(log => log.CreatedAt)
                .Take(200)
                .Select(log => new
                {
                    log.AuditLogId,
                    log.Action,
                    log.EntityType,
                    log.EntityId,
                    log.Details,
                    log.CreatedAt,
                    actor = log.ActorUser == null ? null : new
                    {
                        log.ActorUser.Id,
                        log.ActorUser.FullName,
                        log.ActorUser.Email
                    }
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}
