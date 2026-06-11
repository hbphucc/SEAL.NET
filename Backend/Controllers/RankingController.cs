using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [Route("api/ranking/public")]
    [ApiController]
    [AllowAnonymous]
    public class RankingController : ControllerBase
    {
        private readonly IRankingService _rankingService;

        public RankingController(IRankingService rankingService)
        {
            _rankingService = rankingService;
        }

        [HttpGet("round/{roundId}")]
        public async Task<IActionResult> GetRoundRanking(Guid roundId)
            => (await _rankingService.GetRoundRankingAsync(roundId, requirePublished: true)).ToActionResult(this);

        [HttpGet("category/{categoryId}/round/{roundId}")]
        public async Task<IActionResult> GetCategoryRoundRanking(Guid categoryId, Guid roundId)
            => (await _rankingService.GetCategoryRoundRankingAsync(categoryId, roundId, requirePublished: true)).ToActionResult(this);
    }
}
