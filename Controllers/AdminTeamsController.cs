using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.Models.Enums;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/teams")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminTeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminTeamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTeams()
        {
            var teams = await _context.Teams
                .Include(t => t.Category)
                .Include(t => t.CurrentRound)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.TeamId,
                    t.TeamName,
                    status = t.Status.ToString(),
                    category = new
                    {
                        t.Category!.CategoryId,
                        t.Category.CategoryName
                    },
                    currentRound = t.CurrentRound == null ? null : new
                    {
                        t.CurrentRound.RoundId,
                        t.CurrentRound.RoundName
                    },
                    members = t.Members.Select(m => new
                    {
                        m.UserId,
                        m.User!.FullName,
                        m.User.Email
                    })
                })
                .ToListAsync();

            return Ok(teams);
        }

        [HttpPut("{teamId}/approve")]
        public async Task<IActionResult> ApproveTeam(Guid teamId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            if (team.Members.Count < 3 || team.Members.Count > 5)
                return BadRequest(new { message = "Team must have 3 to 5 members before approval." });

            team.Status = TeamStatus.Approved;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Team approved successfully." });
        }

        [HttpPut("{teamId}/reject")]
        public async Task<IActionResult> RejectTeam(Guid teamId)
        {
            var team = await _context.Teams.FindAsync(teamId);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            team.Status = TeamStatus.Eliminated;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Team rejected successfully." });
        }
    }
}