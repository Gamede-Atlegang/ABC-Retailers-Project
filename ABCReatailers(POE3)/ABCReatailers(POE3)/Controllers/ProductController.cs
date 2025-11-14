using ABCRetailers_POE3_.Data;
using ABCRetailers_POE3_.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers_POE3_.Controllers;

[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAzureStorageService _storage;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ApplicationDbContext dbContext, IAzureStorageService storage, ILogger<ProductController> logger)
    {
        _dbContext = dbContext;
        _storage = storage;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _dbContext.Products
            .OrderBy(p => p.ProductName)
            .ToListAsync();

        return View(products);
    }

    public IActionResult Create() => View(new Product());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product model, IFormFile? imageFile)
    {
        if (string.IsNullOrWhiteSpace(model.ProductId))
        {
            ModelState.Remove(nameof(Product.ProductId));
            model.ProductId = $"SKU-{Guid.NewGuid():N}".Substring(0, 10).ToUpperInvariant();
        }

        if (await _dbContext.Products.AnyAsync(p => p.ProductId == model.ProductId))
        {
            ModelState.AddModelError(nameof(Product.ProductId), "Product SKU already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.CreatedDate = DateTime.UtcNow;
        model.UpdatedDate = DateTime.UtcNow;

        if (imageFile != null && imageFile.Length > 0)
        {
            model.ImageUrl = await _storage.UploadImageAsync(imageFile, "product-images");
        }

        _dbContext.Products.Add(model);
        await _dbContext.SaveChangesAsync();

        TempData["Message"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product model, IFormFile? imageFile)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (await _dbContext.Products.AnyAsync(p => p.ProductId == model.ProductId && p.Id != id))
        {
            ModelState.AddModelError(nameof(Product.ProductId), "Product SKU already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        product.ProductId = model.ProductId;
        product.ProductName = model.ProductName;
        product.Description = model.Description;
        product.Category = model.Category;
        product.Price = model.Price;
        product.StockAvailable = model.StockAvailable;
        product.UpdatedDate = DateTime.UtcNow;

        if (imageFile != null && imageFile.Length > 0)
        {
            product.ImageUrl = await _storage.UploadImageAsync(imageFile, "product-images");
        }

        await _dbContext.SaveChangesAsync();

        TempData["Message"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _dbContext.Products
            .Include(p => p.OrderItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (product.OrderItems.Any())
        {
            TempData["Error"] = "Cannot delete a product that is part of existing orders.";
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        TempData["Message"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }
}