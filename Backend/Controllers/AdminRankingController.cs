using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [Route("api/admin/ranking")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRankingController : ControllerBase
    {
        private readonly IRankingService _rankingService;

        public AdminRankingController(IRankingService rankingService)
        {
            _rankingService = rankingService;
        }

        [HttpGet("round/{roundId}")]
        public async Task<IActionResult> GetRoundRanking(Guid roundId)
            => (await _rankingService.GetRoundRankingAsync(roundId, requirePublished: false)).ToActionResult(this);

        [HttpGet("category/{categoryId}/round/{roundId}")]
        public async Task<IActionResult> GetCategoryRoundRanking(Guid categoryId, Guid roundId)
            => (await _rankingService.GetCategoryRoundRankingAsync(categoryId, roundId, requirePublished: false)).ToActionResult(this);
    }
}
