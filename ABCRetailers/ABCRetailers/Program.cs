using ABCRetailers.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IAzureStorageService, AzureStorageService>();

// Add Functions client
builder.Services.AddHttpClient<IFunctionsClient, FunctionsClient>();

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