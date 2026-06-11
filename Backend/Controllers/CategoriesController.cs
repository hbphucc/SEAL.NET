using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Category;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [Route("api/events/{eventId}/categories")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories(Guid eventId)
            => (await _categoryService.GetCategoriesAsync(eventId)).ToActionResult(this);

        [HttpGet("{categoryId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(Guid eventId, Guid categoryId)
        {
            var category = await _categoryService.GetCategoryByIdAsync(eventId, categoryId);
            if (category == null)
                return NotFound(new { message = "Category not found." });
            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(Guid eventId, [FromBody] CreateCategoryRequest request)
            => (await _categoryService.CreateCategoryAsync(eventId, request)).ToActionResult(this);

        [HttpPut("{categoryId}")]
        public async Task<IActionResult> UpdateCategory(Guid eventId, Guid categoryId, [FromBody] UpdateCategoryRequest request)
            => (await _categoryService.UpdateCategoryAsync(eventId, categoryId, request)).ToActionResult(this);

        [HttpDelete("{categoryId}")]
        public async Task<IActionResult> DeleteCategory(Guid eventId, Guid categoryId)
            => (await _categoryService.DeleteCategoryAsync(eventId, categoryId)).ToActionResult(this);
    }
}
