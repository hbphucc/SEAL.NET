using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Judge;
using SEAL.NET.Models.Entities;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class JudgeAssignmentService : IJudgeAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JudgeAssignmentService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<JudgeAssignmentDto>> GetAssignmentsAsync()
        {
            return await _context.JudgeAssignments
                .Include(a => a.Judge)
                .Include(a => a.Round)
                .Include(a => a.Category)
                .Select(a => new JudgeAssignmentDto
                {
                    AssignmentId = a.AssignmentId,
                    Judge = new JudgeAssignmentJudgeInfo
                    {
                        JudgeId = a.JudgeId,
                        FullName = a.Judge!.FullName,
                        Email = a.Judge.Email
                    },
                    Round = new JudgeAssignmentRoundInfo
                    {
                        RoundId = a.RoundId,
                        RoundName = a.Round!.RoundName
                    },
                    Category = new JudgeAssignmentCategoryInfo
                    {
                        CategoryId = a.CategoryId,
                        CategoryName = a.Category!.CategoryName
                    }
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> CreateAssignmentAsync(CreateJudgeAssignmentRequest request)
        {
            var judge = await _userManager.FindByIdAsync(request.JudgeId.ToString());
            if (judge == null)
                return ServiceResult.NotFound(new { message = "Judge not found." });

            var isJudge = await _userManager.IsInRoleAsync(judge, "Judge");
            if (!isJudge)
                return ServiceResult.BadRequest(new { message = "This user is not a Judge." });

            var round = await _context.Rounds.FindAsync(request.RoundId);
            if (round == null)
                return ServiceResult.NotFound(new { message = "Round not found." });

            var category = await _context.Categories.FindAsync(request.CategoryId);
            if (category == null)
                return ServiceResult.NotFound(new { message = "Category not found." });

            if (round.EventId != category.EventId)
                return ServiceResult.BadRequest(new { message = "Round and category must belong to the same event." });

            var duplicate = await _context.JudgeAssignments.AnyAsync(a =>
                a.JudgeId == request.JudgeId &&
                a.RoundId == request.RoundId &&
                a.CategoryId == request.CategoryId);

            if (duplicate)
                return ServiceResult.BadRequest(new { message = "Judge assignment already exists." });

            var assignment = new JudgeAssignment
            {
                JudgeId = request.JudgeId,
                RoundId = request.RoundId,
                CategoryId = request.CategoryId
            };

            _context.JudgeAssignments.Add(assignment);
            _context.Notifications.Add(new Notification
            {
                UserId = request.JudgeId,
                Type = "JudgeAssigned",
                Title = "Judge assignment",
                Message = "You have been assigned submissions to judge.",
                Link = "/judge/dashboard"
            });
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "JudgeAssigned",
                EntityType = "JudgeAssignment",
                EntityId = assignment.AssignmentId,
                Details = $"Round={request.RoundId};Category={request.CategoryId};Judge={request.JudgeId}"
            });
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new
            {
                message = "Judge assigned successfully.",
                assignment.AssignmentId
            });
        }

        public async Task<ServiceResult> DeleteAssignmentAsync(Guid assignmentId)
        {
            var assignment = await _context.JudgeAssignments.FindAsync(assignmentId);

            if (assignment == null)
                return ServiceResult.NotFound(new { message = "Assignment not found." });

            _context.JudgeAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Judge assignment removed successfully." });
        }
    }
}
