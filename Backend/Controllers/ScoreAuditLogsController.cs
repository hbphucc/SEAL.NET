using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/score-audit-logs")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ScoreAuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public ScoreAuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet("submission/{submissionId}")]
        public async Task<IActionResult> GetSubmissionScoreAuditLogs(Guid submissionId)
            => Ok(await _auditLogService.GetScoreAuditLogsForSubmissionAsync(submissionId));
    }
}
