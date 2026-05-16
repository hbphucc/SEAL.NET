using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/rounds")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoundsController : ControllerBase
    {
        private readonly IRankingService _rankingService;

        public AdminRoundsController(IRankingService rankingService)
        {
            _rankingService = rankingService;
        }

        [HttpPost("{roundId}/advance")]
        public async Task<IActionResult> AdvanceRound(Guid roundId)
        {
            var result = await _rankingService.AdvanceRoundAsync(roundId);

            if (!result.Success)
            {
                if (result.Message == "Round not found.")
                    return NotFound(new { message = result.Message });

                if (result.Data != null)
                    return BadRequest(new { message = result.Message, details = result.Data });

                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message, details = result.Data });
        }
    }
}
