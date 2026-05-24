using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using backend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// EF Core SQLite setup
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=pos.db"));

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "superSecretKey12345678901234567890";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "pos-api",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "pos-client",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-create database & Seed initial admin and cashier
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {
        // 1. Super Admin / Software Owner
        var superAdmin = new backend.Models.User
        {
            Username = "sreenandan",
            FullName = "SaaS Platform Owner",
            PasswordHash = backend.Utilities.PasswordHasher.HashPassword("sreenandan@@123"),
            Role = "SuperAdmin"
        };
        db.Users.Add(superAdmin);
        db.SaveChanges(); // ID 1

        // 2. Restaurant Owners (Business Owners)
        var owner1 = new backend.Models.User
        {
            Username = "owner1",
            FullName = "Gourmet Cafe Owner",
            PasswordHash = backend.Utilities.PasswordHasher.HashPassword("owner123"),
            Role = "Owner",
            ParentOwnerId = superAdmin.Id
        };
        var owner2 = new backend.Models.User
        {
            Username = "owner2",
            FullName = "Seaside Grill Owner",
            PasswordHash = backend.Utilities.PasswordHasher.HashPassword("owner223"),
            Role = "Owner",
            ParentOwnerId = superAdmin.Id
        };
        db.Users.Add(owner1);
        db.Users.Add(owner2);
        db.SaveChanges(); // owner1 ID 2, owner2 ID 3

        // 3. Restaurants
        var shop1 = new backend.Models.Restaurant
        {
            Name = "Gourmet Bistro",
            Address = "123 Gourmet St, City Center",
            Phone = "555-0199",
            OwnerId = owner1.Id
        };
        var shop2 = new backend.Models.Restaurant
        {
            Name = "Seaside Grill",
            Address = "456 Beach Blvd, Coastal Area",
            Phone = "555-0299",
            OwnerId = owner2.Id
        };
        db.Restaurants.Add(shop1);
        db.Restaurants.Add(shop2);
        db.SaveChanges(); // shop1 ID 1, shop2 ID 2

        // 4. Managers
        var manager1 = new backend.Models.User
        {
            Username = "manager1",
            FullName = "Alice Manager (Gourmet)",
            PasswordHash = backend.Utilities.PasswordHasher.HashPassword("manager123"),
            Role = "Manager",
            RestaurantId = shop1.Id,
            ParentOwnerId = owner1.Id
        };
        var manager2 = new backend.Models.User
        {
            Username = "manager2",
            FullName = "Bob Manager (Seaside)",
            PasswordHash = backend.Utilities.PasswordHasher.HashPassword("manager223"),
            Role = "Manager",
            RestaurantId = shop2.Id,
            ParentOwnerId = owner2.Id
        };
        db.Users.Add(manager1);
        db.Users.Add(manager2);
        db.SaveChanges(); // manager1 ID 4, manager2 ID 5

        // 5. Staff (Waiters / Cashiers)
        var waiter1 = new backend.Models.User
        {
            Username = "waiter1",
            FullName = "John Waiter (Gourmet)",
            PasswordHash = backend.Utilities.PasswordHasher.HashPassword("waiter123"),
            Role = "Waiter",
            RestaurantId = shop1.Id,
            ParentOwnerId = manager1.Id
        };
        var cashier2 = new backend.Models.User
        {
            Username = "cashier2",
            FullName = "Emily Cashier (Seaside)",
            PasswordHash = backend.Utilities.PasswordHasher.HashPassword("cashier223"),
            Role = "Cashier",
            RestaurantId = shop2.Id,
            ParentOwnerId = manager2.Id
        };
        db.Users.Add(waiter1);
        db.Users.Add(cashier2);
        db.SaveChanges();
    }

    if (!db.Categories.Any())
    {
        // Gourmet Cafe Categories (RestaurantId = 1)
        db.Categories.Add(new backend.Models.Category { Name = "Beverages", Color = "#0d6efd", RestaurantId = 1 });
        db.Categories.Add(new backend.Models.Category { Name = "Food & Snacks", Color = "#198754", RestaurantId = 1 });
        db.Categories.Add(new backend.Models.Category { Name = "Desserts", Color = "#dc3545", RestaurantId = 1 });
        db.Categories.Add(new backend.Models.Category { Name = "Merchandise", Color = "#ffc107", RestaurantId = 1 });

        // Seaside Grill Categories (RestaurantId = 2)
        db.Categories.Add(new backend.Models.Category { Name = "Seafood Specials", Color = "#0dcaf0", RestaurantId = 2 });
        db.Categories.Add(new backend.Models.Category { Name = "Drinks & Cocktails", Color = "#6f42c1", RestaurantId = 2 });
        db.Categories.Add(new backend.Models.Category { Name = "Platter Deals", Color = "#d63384", RestaurantId = 2 });
        
        db.SaveChanges();
    }

    if (!db.Products.Any())
    {
        // Gourmet Bistro Products (RestaurantId = 1, CategoryIds 1, 2, 3, 4)
        db.Products.Add(new backend.Models.Product
        {
            Name = "Espresso",
            Sku = "BEV-ESP-001",
            Barcode = "880102030405",
            Description = "Rich, dark espresso shot brewed from premium organic beans.",
            Price = 3.50m,
            CostPrice = 1.00m,
            StockQuantity = 50,
            MinStockLevel = 10,
            CategoryId = 1,
            RestaurantId = 1,
            ImageUrl = "https://images.unsplash.com/photo-151097252790b-a481d6d7e9f9?w=300"
        });
        db.Products.Add(new backend.Models.Product
        {
            Name = "Iced Latte",
            Sku = "BEV-LAT-002",
            Barcode = "880102030406",
            Description = "Chilled espresso with creamy cold milk over ice.",
            Price = 4.50m,
            CostPrice = 1.20m,
            StockQuantity = 40,
            MinStockLevel = 10,
            CategoryId = 1,
            RestaurantId = 1,
            ImageUrl = "https://images.unsplash.com/photo-1517701604599-bb29b565090c?w=300"
        });
        db.Products.Add(new backend.Models.Product
        {
            Name = "Chicken Club Sandwich",
            Sku = "FOD-CHS-001",
            Barcode = "880202030401",
            Description = "Classic double-decker toast with grilled chicken, bacon, lettuce, and mayo.",
            Price = 8.50m,
            CostPrice = 3.00m,
            StockQuantity = 25,
            MinStockLevel = 5,
            CategoryId = 2,
            RestaurantId = 1,
            ImageUrl = "https://images.unsplash.com/photo-1521390188846-e2a3a97453a0?w=300"
        });
        db.Products.Add(new backend.Models.Product
        {
            Name = "French Fries",
            Sku = "FOD-FRF-002",
            Barcode = "880202030402",
            Description = "Crispy, golden-salted potato fries served with tomato ketchup.",
            Price = 4.00m,
            CostPrice = 1.20m,
            StockQuantity = 30,
            MinStockLevel = 8,
            CategoryId = 2,
            RestaurantId = 1,
            ImageUrl = "https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=300"
        });
        db.Products.Add(new backend.Models.Product
        {
            Name = "Chocolate Brownie",
            Sku = "DES-BRW-001",
            Barcode = "880302030401",
            Description = "Warm, chewy fudge brownie loaded with chocolate chips.",
            Price = 4.50m,
            CostPrice = 1.50m,
            StockQuantity = 15,
            MinStockLevel = 5,
            CategoryId = 3,
            RestaurantId = 1,
            ImageUrl = "https://images.unsplash.com/photo-1606313564200-e75d5e30476c?w=300"
        });
        db.Products.Add(new backend.Models.Product
        {
            Name = "Strawberry Cheesecake",
            Sku = "DES-CSC-002",
            Barcode = "880302030402",
            Description = "Creamy, smooth New York style cheesecake topped with strawberry compote.",
            Price = 5.50m,
            CostPrice = 2.00m,
            StockQuantity = 4,
            MinStockLevel = 5,
            CategoryId = 3,
            RestaurantId = 1,
            ImageUrl = "https://images.unsplash.com/photo-1524351199679-46cddf530c04?w=300"
        });
        db.Products.Add(new backend.Models.Product
        {
            Name = "Branded Coffee Mug",
            Sku = "MER-MUG-001",
            Barcode = "880402030401",
            Description = "Ceramic 12oz mug with custom store branding.",
            Price = 12.00m,
            CostPrice = 4.00m,
            StockQuantity = 20,
            MinStockLevel = 3,
            CategoryId = 4,
            RestaurantId = 1,
            ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=300"
        });

        // Seaside Grill Products (RestaurantId = 2, CategoryIds 5, 6, 7)
        db.Products.Add(new backend.Models.Product
        {
            Name = "Garlic Butter Lobster",
            Sku = "SEA-LOB-001",
            Barcode = "880502030401",
            Description = "Premium whole lobster tail broiled with melted garlic butter.",
            Price = 24.50m,
            CostPrice = 9.00m,
            StockQuantity = 15,
            MinStockLevel = 3,
            CategoryId = 5,
            RestaurantId = 2,
            ImageUrl = "https://images.unsplash.com/photo-1553618551-fba689030290?w=300"
        });
        db.Products.Add(new backend.Models.Product
        {
            Name = "Golden Fish & Chips",
            Sku = "SEA-FIC-002",
            Barcode = "880502030402",
            Description = "Crispy beer-battered cod fish fillet served with fresh tartar sauce.",
            Price = 14.99m,
            CostPrice = 4.50m,
            StockQuantity = 25,
            MinStockLevel = 5,
            CategoryId = 5,
            RestaurantId = 2,
            ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=300"
        });
        db.Products.Add(new backend.Models.Product
        {
            Name = "Blue Ocean Curacao",
            Sku = "SEA-DRK-001",
            Barcode = "880602030401",
            Description = "Refreshing blue curacao cocktail with mint leaves and club soda.",
            Price = 7.50m,
            CostPrice = 1.80m,
            StockQuantity = 60,
            MinStockLevel = 10,
            CategoryId = 6,
            RestaurantId = 2,
            ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=300"
        });
        db.Products.Add(new backend.Models.Product
        {
            Name = "Grilled Seafood Platter",
            Sku = "SEA-PLT-001",
            Barcode = "880702030401",
            Description = "Assorted platter of grilled prawns, calamari, scallops, and salmon.",
            Price = 34.99m,
            CostPrice = 12.00m,
            StockQuantity = 10,
            MinStockLevel = 2,
            CategoryId = 7,
            RestaurantId = 2,
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=300"
        });

        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

