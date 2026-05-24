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
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions([FromQuery] int? restaurantId)
        {
            var query = _context.Transactions
                .Include(t => t.TransactionItems)
                .ThenInclude(ti => ti.Product)
                .ThenInclude(p => p != null ? p.Category : null)
                .AsQueryable();

            if (restaurantId.HasValue)
            {
                query = query.Where(t => t.RestaurantId == restaurantId.Value);
            }

            return await query.OrderByDescending(t => t.TransactionDate).ToListAsync();
        }

        // GET: api/transactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.TransactionItems)
                .ThenInclude(ti => ti.Product)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound("Transaction not found.");
            }

            return transaction;
        }

        // POST: api/transactions
        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        {
            if (transaction.TransactionItems == null || !transaction.TransactionItems.Any())
            {
                return BadRequest("A transaction must contain at least one item.");
            }

            // Start a SQL Transaction block for absolute stock consistency!
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                decimal calculatedSubtotal = 0;

                foreach (var item in transaction.TransactionItems)
                {
                    // Fetch product from SQL DB
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        return BadRequest($"Product with ID {item.ProductId} does not exist.");
                    }

                    // Check stock quantities
                    if (product.StockQuantity < item.Quantity)
                    {
                        return BadRequest($"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}.");
                    }

                    // Subtract stock levels
                    product.StockQuantity -= item.Quantity;

                    // Set audit-proof prices from database instead of client payload to prevent pricing hacks!
                    item.UnitPrice = product.Price;
                    item.CostPrice = product.CostPrice;
                    item.Subtotal = product.Price * item.Quantity;

                    calculatedSubtotal += item.Subtotal;
                }

                // Final calculations on server
                transaction.Subtotal = calculatedSubtotal;
                transaction.Tax = Math.Round(calculatedSubtotal * 0.05m, 2); // 5% flat sales tax rate
                transaction.Total = transaction.Subtotal + transaction.Tax - transaction.Discount;
                
                if (transaction.AmountPaid < transaction.Total)
                {
                    return BadRequest($"Payment amount paid (${transaction.AmountPaid}) is less than total due (${transaction.Total}).");
                }

                transaction.ChangeReturned = transaction.AmountPaid - transaction.Total;
                transaction.TransactionDate = DateTime.UtcNow;
                transaction.Status = "Completed";

                // Save to database
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Commit the database transaction
                await dbTransaction.CommitAsync();

                // Load complete transaction details to return to the front-end (including product objects)
                var response = await _context.Transactions
                    .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Product)
                    .FirstOrDefaultAsync(t => t.Id == transaction.Id);

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, response);
            }
            catch (Exception ex)
            {
                // Rollback any stock decrements on error!
                await dbTransaction.RollbackAsync();
                return StatusCode(500, $"An error occurred during checkout processing: {ex.Message}");
            }
        }
    }
}
