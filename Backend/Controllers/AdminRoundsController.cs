using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/rounds")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoundsController : ControllerBase
    {
        private readonly IAdminRoundService _adminRoundService;

        public AdminRoundsController(IAdminRoundService adminRoundService)
        {
            _adminRoundService = adminRoundService;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("{roundId}/open")]
        public async Task<IActionResult> OpenRound(Guid roundId)
            => (await _adminRoundService.OpenRoundAsync(roundId, CurrentUserId)).ToActionResult(this);

        [HttpPost("{roundId}/close")]
        public async Task<IActionResult> CloseRound(Guid roundId)
            => (await _adminRoundService.CloseRoundAsync(roundId, CurrentUserId)).ToActionResult(this);

        [HttpPost("{roundId}/lock-submissions")]
        public async Task<IActionResult> LockSubmissions(Guid roundId)
            => (await _adminRoundService.LockSubmissionsAsync(roundId, CurrentUserId)).ToActionResult(this);

        [HttpPost("{roundId}/reopen")]
        public async Task<IActionResult> ReopenRound(Guid roundId)
            => (await _adminRoundService.ReopenRoundAsync(roundId, CurrentUserId)).ToActionResult(this);

        [HttpPost("{roundId}/publish-result")]
        public async Task<IActionResult> PublishResult(Guid roundId)
            => (await _adminRoundService.PublishResultAsync(roundId, CurrentUserId)).ToActionResult(this);

        [HttpPost("{roundId}/advance")]
        public async Task<IActionResult> AdvanceRound(Guid roundId)
            => (await _adminRoundService.AdvanceRoundAsync(roundId, CurrentUserId)).ToActionResult(this);
    }
}
