using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Score;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [Route("api/judge/scores")]
    [ApiController]
    [Authorize(Roles = "Judge")]
    public class ScoresController : ControllerBase
    {
        private readonly IScoreService _scoreService;

        public ScoresController(IScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> SubmitScore([FromBody] CreateScoreRequest request)
            => (await _scoreService.SubmitScoreAsync(CurrentUserId, request)).ToActionResult(this);

        [HttpPost("bulk")]
        public async Task<IActionResult> SubmitBulkScores([FromBody] BulkScoreRequest request)
            => (await _scoreService.SubmitBulkScoresAsync(CurrentUserId, request)).ToActionResult(this);

        [HttpGet("my-assigned-submissions")]
        public async Task<IActionResult> GetMyAssignedSubmissions()
            => Ok(await _scoreService.GetMyAssignedSubmissionsAsync(CurrentUserId));
    }
}
