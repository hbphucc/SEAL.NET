using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/score-audit-logs")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ScoreAuditLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ScoreAuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("submission/{submissionId}")]
        public async Task<IActionResult> GetSubmissionScoreAuditLogs(Guid submissionId)
        {
            var logs = await _context.ScoreAuditLogs
                .Include(log => log.Judge)
                .Include(log => log.Criteria)
                .Where(log => log.SubmissionId == submissionId)
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new
                {
                    log.ScoreAuditLogId,
                    log.ScoreId,
                    log.SubmissionId,
                    log.CriteriaId,
                    criteriaName = log.Criteria == null ? null : log.Criteria.CriteriaName,
                    judge = log.Judge == null ? null : new
                    {
                        log.Judge.Id,
                        log.Judge.FullName,
                        log.Judge.Email
                    },
                    log.Action,
                    log.OldScoreValue,
                    log.NewScoreValue,
                    log.OldComment,
                    log.NewComment,
                    log.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}
