using Microsoft.EntityFrameworkCore;
using OrderManagement.API.Models;

namespace OrderManagement.API.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InventoryRecord> InventoryRecords => Set<InventoryRecord>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<ShipmentRecord> ShipmentRecords => Set<ShipmentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Status).HasConversion<string>();
            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
            e.HasOne(o => o.InventoryRecord).WithOne().HasForeignKey<InventoryRecord>(r => r.OrderId);
            e.HasOne(o => o.PaymentRecord).WithOne().HasForeignKey<PaymentRecord>(r => r.OrderId);
            e.HasOne(o => o.ShipmentRecord).WithOne().HasForeignKey<ShipmentRecord>(r => r.OrderId);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasMany(c => c.Orders).WithOne().HasForeignKey(o => o.CustomerId).IsRequired(false);
        });

        modelBuilder.Entity<Product>(e => e.HasKey(p => p.ProductId));

        // Seed products from SportsStore
        modelBuilder.Entity<Product>().HasData(
            new Product { ProductId = 1, Name = "Kayak", Description = "A boat for one person", Price = 275m, Category = "Watersports", StockQuantity = 50 },
            new Product { ProductId = 2, Name = "Lifejacket", Description = "Protective and fashionable", Price = 48.95m, Category = "Watersports", StockQuantity = 100 },
            new Product { ProductId = 3, Name = "Soccer Ball", Description = "FIFA-approved size and weight", Price = 19.50m, Category = "Soccer", StockQuantity = 200 },
            new Product { ProductId = 4, Name = "Corner Flags", Description = "Give your playing field a professional touch", Price = 34.95m, Category = "Soccer", StockQuantity = 75 },
            new Product { ProductId = 5, Name = "Stadium", Description = "Flat-packed 35,000-seat stadium", Price = 79500m, Category = "Soccer", StockQuantity = 5 },
            new Product { ProductId = 6, Name = "Thinking Cap", Description = "Improve brain efficiency by 75%", Price = 16m, Category = "Chess", StockQuantity = 150 },
            new Product { ProductId = 7, Name = "Unsteady Chair", Description = "Secretly give your opponent a disadvantage", Price = 29.95m, Category = "Chess", StockQuantity = 80 },
            new Product { ProductId = 8, Name = "Human Chess Board", Description = "A fun game for the family", Price = 75m, Category = "Chess", StockQuantity = 30 },
            new Product { ProductId = 9, Name = "Bling-Bling King", Description = "Gold-plated, diamond-studded King", Price = 1200m, Category = "Chess", StockQuantity = 10 }
        );

        // Seed demo customer
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = "customer-1", Name = "Demo User", Email = "demo@sportsstore.com", Address = "123 Main St, Dublin, Ireland" }
        );
    }
}
