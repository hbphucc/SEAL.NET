using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Criteria;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [Route("api/rounds/{roundId}/criteria")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class CriteriaController : ControllerBase
    {
        private readonly ICriteriaService _criteriaService;

        public CriteriaController(ICriteriaService criteriaService)
        {
            _criteriaService = criteriaService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCriteria(Guid roundId)
            => (await _criteriaService.GetCriteriaAsync(roundId)).ToActionResult(this);

        [HttpPost]
        public async Task<IActionResult> CreateCriteria(Guid roundId, [FromBody] CreateCriteriaRequest request)
            => (await _criteriaService.CreateCriteriaAsync(roundId, request)).ToActionResult(this);

        [HttpPut("{criteriaId}")]
        public async Task<IActionResult> UpdateCriteria(Guid roundId, Guid criteriaId, [FromBody] UpdateCriteriaRequest request)
            => (await _criteriaService.UpdateCriteriaAsync(roundId, criteriaId, request)).ToActionResult(this);

        [HttpDelete("{criteriaId}")]
        public async Task<IActionResult> DeleteCriteria(Guid roundId, Guid criteriaId)
            => (await _criteriaService.DeleteCriteriaAsync(roundId, criteriaId)).ToActionResult(this);
    }
}
