using Microsoft.EntityFrameworkCore;

namespace ABCRetailers_POE3_.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Cart> Cart { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Role).HasMaxLength(20);
            });

            // Customer configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasIndex(e => e.CustomerId).IsUnique();
                entity.HasIndex(e => e.Username);
                entity.HasIndex(e => e.UserId);

                entity.HasOne(c => c.User)
                    .WithOne(u => u.Customer)
                    .HasForeignKey<Customer>(c => c.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(e => e.ProductId).IsUnique();
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasIndex(e => e.OrderId).IsUnique();
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.OrderDate);
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(50);

                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // OrderItem configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasIndex(e => e.OrderId);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");

                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Cart configuration
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => new { e.CustomerId, e.ProductId }).IsUnique();

                entity.HasOne(c => c.Customer)
                    .WithMany(cust => cust.CartItems)
                    .HasForeignKey(c => c.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Product)
                    .WithMany(p => p.CartItems)
                    .HasForeignKey(c => c.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

