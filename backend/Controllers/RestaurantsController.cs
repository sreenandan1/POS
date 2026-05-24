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
    public class RestaurantsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RestaurantsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/restaurants
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurants([FromQuery] int? ownerId)
        {
            var query = _context.Restaurants.AsQueryable();

            if (ownerId.HasValue)
            {
                query = query.Where(r => r.OwnerId == ownerId.Value);
            }

            return await query.OrderBy(r => r.Name).ToListAsync();
        }

        // GET: api/restaurants/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Restaurant>> GetRestaurant(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.Owner)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound("Restaurant branch not found.");
            }

            return restaurant;
        }

        // POST: api/restaurants
        [HttpPost]
        public async Task<ActionResult<Restaurant>> CreateRestaurant(Restaurant restaurant)
        {
            if (string.IsNullOrWhiteSpace(restaurant.Name))
            {
                return BadRequest("Restaurant name is required.");
            }

            // Verify owner exists in DB
            var owner = await _context.Users.FindAsync(restaurant.OwnerId);
            if (owner == null || owner.Role != "Owner")
            {
                return BadRequest("Invalid restaurant owner selection.");
            }

            restaurant.CreatedAt = DateTime.UtcNow;

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRestaurant), new { id = restaurant.Id }, restaurant);
        }
    }
}
