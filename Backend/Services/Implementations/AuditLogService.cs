using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Audit;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AuditLogDto>> GetRecentAsync()
        {
            return await _context.AuditLogs
                .Include(log => log.ActorUser)
                .OrderByDescending(log => log.CreatedAt)
                .Take(200)
                .Select(log => new AuditLogDto
                {
                    AuditLogId = log.AuditLogId,
                    Action = log.Action,
                    EntityType = log.EntityType,
                    EntityId = log.EntityId,
                    Details = log.Details,
                    CreatedAt = log.CreatedAt,
                    Actor = log.ActorUser == null ? null : new ActorDto
                    {
                        Id = log.ActorUser.Id,
                        FullName = log.ActorUser.FullName,
                        Email = log.ActorUser.Email
                    }
                })
                .ToListAsync();
        }

        public async Task<List<ScoreAuditLogDto>> GetScoreAuditLogsForSubmissionAsync(Guid submissionId)
        {
            return await _context.ScoreAuditLogs
                .Include(log => log.Judge)
                .Include(log => log.Criteria)
                .Where(log => log.SubmissionId == submissionId)
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new ScoreAuditLogDto
                {
                    ScoreAuditLogId = log.ScoreAuditLogId,
                    ScoreId = log.ScoreId,
                    SubmissionId = log.SubmissionId,
                    CriteriaId = log.CriteriaId,
                    CriteriaName = log.Criteria == null ? null : log.Criteria.CriteriaName,
                    Judge = log.Judge == null ? null : new ActorDto
                    {
                        Id = log.Judge.Id,
                        FullName = log.Judge.FullName,
                        Email = log.Judge.Email
                    },
                    Action = log.Action,
                    OldScoreValue = log.OldScoreValue,
                    NewScoreValue = log.NewScoreValue,
                    OldComment = log.OldComment,
                    NewComment = log.NewComment,
                    CreatedAt = log.CreatedAt
                })
                .ToListAsync();
        }
    }
}
