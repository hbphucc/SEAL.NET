using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Mentor;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class MentorsController : ControllerBase
    {
        private readonly IMentorService _mentorService;

        public MentorsController(IMentorService mentorService)
        {
            _mentorService = mentorService;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("admin/teams/{teamId}/mentors")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignMentor(Guid teamId, [FromBody] MentorAssignmentRequest request)
            => (await _mentorService.AssignMentorAsync(teamId, request, CurrentUserId)).ToActionResult(this);

        [HttpDelete("admin/teams/{teamId}/mentors/{mentorId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnassignMentor(Guid teamId, Guid mentorId)
            => (await _mentorService.UnassignMentorAsync(teamId, mentorId, CurrentUserId)).ToActionResult(this);

        [HttpGet("mentor/teams")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GetAssignedTeams()
            => Ok(await _mentorService.GetAssignedTeamsAsync(CurrentUserId));

        [HttpGet("mentor/teams/{teamId}/submissions")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GetTeamSubmissions(Guid teamId)
            => (await _mentorService.GetTeamSubmissionsAsync(teamId, CurrentUserId)).ToActionResult(this);

        [HttpPost("mentor/teams/{teamId}/notes")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> AddNote(Guid teamId, [FromBody] CreateMentorshipNoteRequest request)
            => (await _mentorService.AddNoteAsync(teamId, CurrentUserId, request)).ToActionResult(this);

        [HttpGet("mentor/teams/{teamId}/notes")]
        [Authorize(Roles = "Admin,Mentor")]
        public async Task<IActionResult> GetNotes(Guid teamId)
            => (await _mentorService.GetNotesAsync(teamId, CurrentUserId, User.IsInRole("Admin"))).ToActionResult(this);
    }
}
