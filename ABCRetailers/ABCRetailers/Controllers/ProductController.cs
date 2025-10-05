using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storage;

        public ProductController(IAzureStorageService storage)
        {
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storage.GetAllEntitiesAsync<Product>();
            return View(products.OrderBy(p => p.ProductName));
        }

        public IActionResult Create() => View(new Product());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(model);

            if (imageFile != null && imageFile.Length > 0)
            {
                model.ImageUrl = await _storage.UploadImageAsync(imageFile, "product-images");
            }

            await _storage.AddEntityAsync(model);
            TempData["Message"] = "Product created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _storage.GetEntityAsync<Product>("Product", id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(model);

            model.PartitionKey = "Product";

            if (imageFile == null || imageFile.Length == 0)
            {
                // Keep current image
                var existing = await _storage.GetEntityAsync<Product>("Product", model.RowKey);
                if (existing != null) model.ImageUrl = existing.ImageUrl;
            }
            else
            {
                model.ImageUrl = await _storage.UploadImageAsync(imageFile, "product-images");
            }

            await _storage.UpdateEntityAsync(model);
            TempData["Message"] = "Product updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));
            await _storage.DeleteEntityAsync<Product>("Product", id);
            TempData["Message"] = "Product deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}