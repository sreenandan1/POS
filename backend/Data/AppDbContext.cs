using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Users table
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

            // Configure Categories table: unique category name per restaurant
            modelBuilder.Entity<Category>().HasIndex(c => new { c.Name, c.RestaurantId }).IsUnique();

            // Configure Products table: unique SKU per restaurant
            modelBuilder.Entity<Product>().HasIndex(p => new { p.Sku, p.RestaurantId }).IsUnique();
        }
    }
}

