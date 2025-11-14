using ABCRetailers_POE3_.Data;
using ABCRetailers_POE3_.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IAzureStorageService, AzureStorageService>();

// Add Functions client
builder.Services.AddHttpClient<IFunctionsClient, FunctionsClient>();

// Add Entity Framework Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Initialize Azure Storage - FIXED VERSION
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<AzureStorageService>>();
    try
    {
        // Change this line - resolve IAzureStorageService instead of AzureStorageService
        await scope.ServiceProvider.GetRequiredService<IAzureStorageService>().InitializeAsync();
        logger.LogInformation("Azure Storage initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize Azure Storage");
        throw;
    }
}

// Ensure SQL Database is created (for development)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
    try
    {
        // This will create the database if it doesn't exist
        // In production, use migrations: dotnet ef database update
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("SQL Database initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize SQL Database. Make sure connection string is correct.");
        // Don't throw - allow app to start even if SQL DB is not available yet
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();