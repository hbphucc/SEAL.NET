using Microsoft.EntityFrameworkCore;
using SEAL.NET.Common;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Category;
using SEAL.NET.Models.Entities;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult> GetCategoriesAsync(Guid eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
                return ServiceResult.NotFound(new { message = "Event not found." });

            var categories = await _context.Categories
                .Where(c => c.EventId == eventId)
                .Select(c => new CategoryDetailDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    EventId = c.EventId
                })
                .ToListAsync();

            return ServiceResult.Ok(categories);
        }

        public async Task<CategoryDetailDto?> GetCategoryByIdAsync(Guid eventId, Guid categoryId)
        {
            return await _context.Categories
                .Where(c => c.EventId == eventId && c.CategoryId == categoryId)
                .Select(c => new CategoryDetailDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    EventId = c.EventId
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ServiceResult> CreateCategoryAsync(Guid eventId, CreateCategoryRequest request)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
                return ServiceResult.NotFound(new { message = "Event not found." });

            var duplicate = await _context.Categories.AnyAsync(c =>
                c.EventId == eventId &&
                c.CategoryName.ToLower() == request.CategoryName.ToLower());

            if (duplicate)
                return ServiceResult.BadRequest(new { message = "Category name already exists in this event." });

            var category = new Category
            {
                EventId = eventId,
                CategoryName = request.CategoryName,
                Description = request.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return ServiceResult.Created(new
            {
                message = "Category created successfully.",
                category.CategoryId
            });
        }

        public async Task<ServiceResult> UpdateCategoryAsync(Guid eventId, Guid categoryId, UpdateCategoryRequest request)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.EventId == eventId && c.CategoryId == categoryId);

            if (category == null)
                return ServiceResult.NotFound(new { message = "Category not found." });

            var duplicate = await _context.Categories.AnyAsync(c =>
                c.EventId == eventId &&
                c.CategoryId != categoryId &&
                c.CategoryName.ToLower() == request.CategoryName.ToLower());

            if (duplicate)
                return ServiceResult.BadRequest(new { message = "Category name already exists in this event." });

            category.CategoryName = request.CategoryName;
            category.Description = request.Description;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Category updated successfully." });
        }

        public async Task<ServiceResult> DeleteCategoryAsync(Guid eventId, Guid categoryId)
        {
            var category = await _context.Categories
                .Include(c => c.Teams)
                .Include(c => c.JudgeAssignments)
                .FirstOrDefaultAsync(c => c.EventId == eventId && c.CategoryId == categoryId);

            if (category == null)
                return ServiceResult.NotFound(new { message = "Category not found." });

            if (category.Teams.Any() || category.JudgeAssignments.Any())
                return ServiceResult.BadRequest(new { message = "Cannot delete category because it already has teams or judge assignments." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new { message = "Category deleted successfully." });
        }
    }
}
