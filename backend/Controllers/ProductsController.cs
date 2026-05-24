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
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] string? search, 
            [FromQuery] int? categoryId,
            [FromQuery] int? restaurantId)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (restaurantId.HasValue)
            {
                query = query.Where(p => p.RestaurantId == restaurantId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(p => 
                    p.Name.ToLower().Contains(searchLower) || 
                    p.Sku.ToLower().Contains(searchLower) || 
                    p.Barcode.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound("Product not found.");
            }

            return product;
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return BadRequest("Product name is required.");
            }

            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                return BadRequest("SKU is required.");
            }

            if (product.RestaurantId <= 0)
            {
                return BadRequest("Valid RestaurantId is required.");
            }

            // Check SKU uniqueness within the same restaurant!
            if (await _context.Products.AnyAsync(p => p.RestaurantId == product.RestaurantId && p.Sku.ToLower() == product.Sku.ToLower()))
            {
                return BadRequest($"SKU '{product.Sku}' is already assigned to another item in this restaurant.");
            }

            // Check Category exists
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);
            if (!categoryExists)
            {
                return BadRequest("Invalid category selected.");
            }

            product.CreatedAt = DateTime.UtcNow;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Reload to fetch the Category navigation property for response
            var createdProduct = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, createdProduct);
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest("ID mismatch.");
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return BadRequest("Product name is required.");
            }

            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                return BadRequest("SKU is required.");
            }

            // Check SKU uniqueness within the same restaurant!
            if (await _context.Products.AnyAsync(p => p.Id != id && p.RestaurantId == product.RestaurantId && p.Sku.ToLower() == product.Sku.ToLower()))
            {
                return BadRequest($"SKU '{product.Sku}' is already assigned to another item in this restaurant.");
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);
            if (!categoryExists)
            {
                return BadRequest("Invalid category selected.");
            }

            var dbProduct = await _context.Products.FindAsync(id);
            if (dbProduct == null)
            {
                return NotFound("Product not found.");
            }

            // Update fields
            dbProduct.Name = product.Name;
            dbProduct.Sku = product.Sku;
            dbProduct.Barcode = product.Barcode;
            dbProduct.Description = product.Description;
            dbProduct.Price = product.Price;
            dbProduct.CostPrice = product.CostPrice;
            dbProduct.StockQuantity = product.StockQuantity;
            dbProduct.MinStockLevel = product.MinStockLevel;
            dbProduct.CategoryId = product.CategoryId;
            dbProduct.ImageUrl = product.ImageUrl;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
