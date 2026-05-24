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
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/dashboard/superadmin
        [HttpGet("superadmin")]
        public async Task<IActionResult> GetSuperAdminStats()
        {
            var totalOwners = await _context.Users.CountAsync(u => u.Role == "Owner");
            var totalRestaurants = await _context.Restaurants.CountAsync();
            var totalRevenue = await _context.Transactions.SumAsync(t => t.Total);
            var totalSalesCount = await _context.Transactions.CountAsync();

            // Fetch today's sales breakdown per restaurant
            var today = DateTime.UtcNow.Date;
            var restaurantSales = await _context.Restaurants
                .Select(r => new
                {
                    RestaurantId = r.Id,
                    RestaurantName = r.Name,
                    TodaySales = _context.Transactions
                        .Where(t => t.RestaurantId == r.Id && t.TransactionDate >= today)
                        .Sum(t => (decimal?)t.Total) ?? 0,
                    TotalSales = _context.Transactions
                        .Where(t => t.RestaurantId == r.Id)
                        .Sum(t => (decimal?)t.Total) ?? 0
                })
                .OrderByDescending(r => r.TotalSales)
                .ToListAsync();

            return Ok(new
            {
                TotalOwners = totalOwners,
                TotalRestaurants = totalRestaurants,
                TotalRevenue = totalRevenue,
                TotalSalesCount = totalSalesCount,
                RestaurantSalesBreakdown = restaurantSales
            });
        }

        // GET: api/dashboard/owner/5
        [HttpGet("owner/{ownerId}")]
        public async Task<IActionResult> GetOwnerStats(int ownerId)
        {
            // Find restaurants owned by this user
            var restaurantIds = await _context.Restaurants
                .Where(r => r.OwnerId == ownerId)
                .Select(r => r.Id)
                .ToListAsync();

            if (!restaurantIds.Any())
            {
                return Ok(new
                {
                    TotalRestaurants = 0,
                    TotalRevenue = 0,
                    TotalSalesCount = 0,
                    LowStockCount = 0,
                    RestaurantSalesBreakdown = new List<object>()
                });
            }

            var totalRestaurants = restaurantIds.Count;
            var totalRevenue = await _context.Transactions
                .Where(t => restaurantIds.Contains(t.RestaurantId))
                .SumAsync(t => t.Total);
            var totalSalesCount = await _context.Transactions
                .Where(t => restaurantIds.Contains(t.RestaurantId))
                .CountAsync();
            
            var lowStockCount = await _context.Products
                .Where(p => restaurantIds.Contains(p.RestaurantId) && p.StockQuantity <= p.MinStockLevel)
                .CountAsync();

            var today = DateTime.UtcNow.Date;
            var restaurantSales = await _context.Restaurants
                .Where(r => r.OwnerId == ownerId)
                .Select(r => new
                {
                    RestaurantId = r.Id,
                    RestaurantName = r.Name,
                    TodaySales = _context.Transactions
                        .Where(t => t.RestaurantId == r.Id && t.TransactionDate >= today)
                        .Sum(t => (decimal?)t.Total) ?? 0,
                    TotalSales = _context.Transactions
                        .Where(t => t.RestaurantId == r.Id)
                        .Sum(t => (decimal?)t.Total) ?? 0
                })
                .OrderByDescending(r => r.TotalSales)
                .ToListAsync();

            return Ok(new
            {
                TotalRestaurants = totalRestaurants,
                TotalRevenue = totalRevenue,
                TotalSalesCount = totalSalesCount,
                LowStockCount = lowStockCount,
                RestaurantSalesBreakdown = restaurantSales
            });
        }

        // GET: api/dashboard/restaurant/5
        [HttpGet("restaurant/{restaurantId}")]
        public async Task<IActionResult> GetRestaurantStats(int restaurantId)
        {
            var restaurant = await _context.Restaurants.FindAsync(restaurantId);
            if (restaurant == null)
            {
                return NotFound("Restaurant not found.");
            }

            var today = DateTime.UtcNow.Date;
            var todaySales = await _context.Transactions
                .Where(t => t.RestaurantId == restaurantId && t.TransactionDate >= today)
                .SumAsync(t => t.Total);

            var todayTransactionsCount = await _context.Transactions
                .Where(t => t.RestaurantId == restaurantId && t.TransactionDate >= today)
                .CountAsync();

            var totalSales = await _context.Transactions
                .Where(t => t.RestaurantId == restaurantId)
                .SumAsync(t => t.Total);

            var lowStockProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.RestaurantId == restaurantId && p.StockQuantity <= p.MinStockLevel)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Sku,
                    p.StockQuantity,
                    p.MinStockLevel,
                    CategoryName = p.Category != null ? p.Category.Name : "Unassigned"
                })
                .ToListAsync();

            var recentTransactions = await _context.Transactions
                .Include(t => t.Cashier)
                .Where(t => t.RestaurantId == restaurantId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(5)
                .Select(t => new
                {
                    t.Id,
                    t.TransactionDate,
                    t.Total,
                    t.PaymentMethod,
                    CashierName = t.Cashier != null ? t.Cashier.FullName : "System"
                })
                .ToListAsync();

            return Ok(new
            {
                RestaurantName = restaurant.Name,
                TodaySales = todaySales,
                TodayTransactionsCount = todayTransactionsCount,
                TotalSales = totalSales,
                LowStockProducts = lowStockProducts,
                LowStockCount = lowStockProducts.Count,
                RecentTransactions = recentTransactions
            });
        }
    }
}
