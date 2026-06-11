using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Submission;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [Route("api/submissions")]
    [ApiController]
    [Authorize]
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionsController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> SubmitProject([FromBody] CreateSubmissionRequest request)
            => (await _submissionService.SubmitProjectAsync(CurrentUserId, request)).ToActionResult(this);

        [HttpGet("{submissionId}")]
        public async Task<IActionResult> GetSubmission(Guid submissionId)
            => (await _submissionService.GetSubmissionAsync(
                submissionId, CurrentUserId, User.IsInRole("Admin"), User.IsInRole("Judge"), User.IsInRole("Mentor")))
                .ToActionResult(this);

        [HttpPost("{submissionId}/withdraw")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> WithdrawSubmission(Guid submissionId)
            => (await _submissionService.WithdrawSubmissionAsync(submissionId, CurrentUserId)).ToActionResult(this);

        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetTeamSubmissions(Guid teamId)
            => (await _submissionService.GetTeamSubmissionsAsync(
                teamId, CurrentUserId, User.IsInRole("Admin"), User.IsInRole("Judge")))
                .ToActionResult(this);

        [HttpGet("round/{roundId}")]
        [Authorize(Roles = "Admin,Judge")]
        public async Task<IActionResult> GetRoundSubmissions(Guid roundId)
            => (await _submissionService.GetRoundSubmissionsAsync(
                roundId, CurrentUserId, User.IsInRole("Admin"), User.IsInRole("Judge")))
                .ToActionResult(this);
    }
}
