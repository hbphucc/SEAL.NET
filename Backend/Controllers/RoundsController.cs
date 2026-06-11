using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Round;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [Route("api/events/{eventId}/rounds")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RoundsController : ControllerBase
    {
        private readonly IRoundService _roundService;

        public RoundsController(IRoundService roundService)
        {
            _roundService = roundService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRounds(Guid eventId)
            => (await _roundService.GetRoundsAsync(eventId)).ToActionResult(this);

        [HttpGet("{roundId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoundById(Guid eventId, Guid roundId)
        {
            var round = await _roundService.GetRoundByIdAsync(eventId, roundId);
            if (round == null)
                return NotFound(new { message = "Round not found." });
            return Ok(round);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRound(Guid eventId, [FromBody] CreateRoundRequest request)
            => (await _roundService.CreateRoundAsync(eventId, request)).ToActionResult(this);

        [HttpPut("{roundId}")]
        public async Task<IActionResult> UpdateRound(Guid eventId, Guid roundId, [FromBody] UpdateRoundRequest request)
            => (await _roundService.UpdateRoundAsync(eventId, roundId, request)).ToActionResult(this);

        [HttpDelete("{roundId}")]
        public async Task<IActionResult> DeleteRound(Guid eventId, Guid roundId)
            => (await _roundService.DeleteRoundAsync(eventId, roundId)).ToActionResult(this);
    }
}
