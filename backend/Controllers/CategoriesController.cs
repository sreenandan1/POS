using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories([FromQuery] int? restaurantId)
        {
            var query = _context.Categories.AsQueryable();

            if (restaurantId.HasValue)
            {
                query = query.Where(c => c.RestaurantId == restaurantId.Value);
            }

            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        // GET: api/categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound("Category not found.");
            }

            return category;
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return BadRequest("Category name is required.");
            }

            if (category.RestaurantId <= 0)
            {
                return BadRequest("Valid RestaurantId is required.");
            }

            // Check uniqueness within the same restaurant!
            if (await _context.Categories.AnyAsync(c => c.RestaurantId == category.RestaurantId && c.Name.ToLower() == category.Name.ToLower()))
            {
                return BadRequest("A category with this name already exists in this restaurant.");
            }

            category.CreatedAt = DateTime.UtcNow;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        // PUT: api/categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, Category category)
        {
            if (id != category.Id)
            {
                return BadRequest("ID mismatch.");
            }

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return BadRequest("Category name is required.");
            }

            // Check if name is taken by another record in the same restaurant!
            if (await _context.Categories.AnyAsync(c => c.Id != id && c.RestaurantId == category.RestaurantId && c.Name.ToLower() == category.Name.ToLower()))
            {
                return BadRequest("Another category already has this name in this restaurant.");
            }

            var dbCategory = await _context.Categories.FindAsync(id);
            if (dbCategory == null)
            {
                return NotFound("Category not found.");
            }

            dbCategory.Name = category.Name;
            dbCategory.Color = category.Color;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Category not found.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
