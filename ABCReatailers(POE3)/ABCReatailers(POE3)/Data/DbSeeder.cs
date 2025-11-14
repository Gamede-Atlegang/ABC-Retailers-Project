using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ABCRetailers_POE3_.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await context.Database.MigrateAsync(cancellationToken);

        await SeedUsersAsync(context, hasher, logger, cancellationToken);
        await SeedProductsAsync(context, logger, cancellationToken);
    }

    private static async Task SeedUsersAsync(
        ApplicationDbContext context,
        IPasswordHasher<User> hasher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var admin = new User
            {
                Username = "admin",
                Email = "admin@abcretailers.com",
                Role = "Admin",
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin@123!");

            var customerUser = new User
            {
                Username = "customer",
                Email = "customer@abcretailers.com",
                Role = "Customer",
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            customerUser.PasswordHash = hasher.HashPassword(customerUser, "Customer@123!");

            var customer = new Customer
            {
                CustomerId = "CUS-DEMO001",
                Username = customerUser.Username,
                Name = "Demo",
                Surname = "Customer",
                Email = customerUser.Email,
                Phone = "+27 11 123 4567",
                Address = "123 Azure Lane, Johannesburg",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                User = customerUser
            };

            await context.Users.AddAsync(admin, cancellationToken);
            await context.Customers.AddAsync(customer, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeded default admin and customer accounts.");
        }
    }

    private static async Task SeedProductsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = new[]
        {
            new Product
            {
                ProductId = "SKU-1001",
                ProductName = "Azure Wireless Headphones",
                Description = "Noise cancelling headphones with 30h battery life.",
                Category = "Electronics",
                Price = 1899.00m,
                StockAvailable = 25,
                ImageUrl = "https://via.placeholder.com/300x200?text=Headphones"
            },
            new Product
            {
                ProductId = "SKU-1002",
                ProductName = "Surface Laptop Backpack",
                Description = "Weather resistant backpack for 15\" laptops.",
                Category = "Accessories",
                Price = 1299.00m,
                StockAvailable = 40,
                ImageUrl = "https://via.placeholder.com/300x200?text=Backpack"
            },
            new Product
            {
                ProductId = "SKU-1003",
                ProductName = "Contoso Smart Speaker",
                Description = "Voice assistant smart speaker with premium sound.",
                Category = "Smart Home",
                Price = 2499.00m,
                StockAvailable = 15,
                ImageUrl = "https://via.placeholder.com/300x200?text=Speaker"
            }
        };

        await context.Products.AddRangeAsync(products, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded default catalog products.");
    }
}

