using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storage;
        private readonly IFunctionsClient _functionsClient;

        public OrderController(IAzureStorageService storage, IFunctionsClient functionsClient)
        {
            _storage = storage;
            _functionsClient = functionsClient;
        }

        public async Task<IActionResult> Index(string searchTerm = "")
        {
            var orders = await _storage.GetAllEntitiesAsync<Order>();
            var orderedOrders = orders.OrderByDescending(o => o.OrderDate);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                orderedOrders = orderedOrders.Where(o =>
                    o.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    o.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    o.Status.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).OrderByDescending(o => o.OrderDate);
            }

            ViewBag.SearchTerm = searchTerm;
            return View(orderedOrders);
        }

        public async Task<IActionResult> Create()
        {
            var customers = await _storage.GetAllEntitiesAsync<Customer>();
            var products = await _storage.GetAllEntitiesAsync<Product>();

            ViewBag.Customers = customers.OrderBy(c => c.Surname).ThenBy(c => c.Name);
            ViewBag.Products = products.OrderBy(p => p.ProductName);

            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order model)
        {

            // These are computed server-side; remove from model validation
            ModelState.Remove(nameof(Order.Username));
            ModelState.Remove(nameof(Order.ProductName));

            if (!ModelState.IsValid)
            {
                var customers = await _storage.GetAllEntitiesAsync<Customer>();
                var products = await _storage.GetAllEntitiesAsync<Product>();
                ViewBag.Customers = customers.OrderBy(c => c.Surname).ThenBy(c => c.Name);
                ViewBag.Products = products.OrderBy(p => p.ProductName);
                return View(model);
            }

            // Fetch referenced entities
            var customer = await _storage.GetEntityAsync<Customer>("Customer", model.CustomerId);
            var product = await _storage.GetEntityAsync<Product>("Product", model.ProductId);

            if (customer is null || product is null)
            {
                ModelState.AddModelError("", "Invalid customer or product selected.");
                var customers = await _storage.GetAllEntitiesAsync<Customer>();
                var products = await _storage.GetAllEntitiesAsync<Product>();
                ViewBag.Customers = customers.OrderBy(c => c.Surname).ThenBy(c => c.Name);
                ViewBag.Products = products.OrderBy(p => p.ProductName);
                return View(model);
            }

            // Stock check
            if (product.StockAvailable < model.Quantity)
            {
                ModelState.AddModelError("Quantity", $"Only {product.StockAvailable} item(s) available.");
                var customers = await _storage.GetAllEntitiesAsync<Customer>();
                var products = await _storage.GetAllEntitiesAsync<Product>();
                ViewBag.Customers = customers.OrderBy(c => c.Surname).ThenBy(c => c.Name);
                ViewBag.Products = products.OrderBy(p => p.ProductName);
                return View(model);
            }

            // Compose order
            model.PartitionKey = "Order";
            model.OrderDate = DateTime.UtcNow;
            model.Username = customer.Username;
            model.ProductName = product.ProductName;
            model.UnitPrice = product.Price;
            model.TotalPrice = product.Price * model.Quantity;
            model.Status = "Pending";

            // Use Functions client to enqueue order (this will trigger the queue function to write to table)
            try
            {
                await _functionsClient.EnqueueOrderAsync(model);

                // Decrease stock and save product
                product.StockAvailable -= model.Quantity;
                await _storage.UpdateEntityAsync(product);

                // Generate receipt and save to Azure Files
                var receiptContent = $"Order Receipt\n" +
                    $"Order ID: {model.OrderId}\n" +
                    $"Customer: {customer.Username}\n" +
                    $"Product: {product.ProductName}\n" +
                    $"Quantity: {model.Quantity}\n" +
                    $"Unit Price: ${model.UnitPrice:F2}\n" +
                    $"Total: ${model.TotalPrice:F2}\n" +
                    $"Date: {model.OrderDate:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Status: {model.Status}";

                await _functionsClient.WriteFileAsync(receiptContent, "receipts", $"receipt-{model.OrderId}.txt");

                TempData["Message"] = "Order submitted successfully and is being processed.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to submit order: {ex.Message}";
                var customers = await _storage.GetAllEntitiesAsync<Customer>();
                var products = await _storage.GetAllEntitiesAsync<Product>();
                ViewBag.Customers = customers.OrderBy(c => c.Surname).ThenBy(c => c.Name);
                ViewBag.Products = products.OrderBy(p => p.ProductName);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _storage.GetEntityAsync<Order>("Order", id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order model)
        {
            // Remove validation for computed fields
            ModelState.Remove(nameof(Order.Username));
            ModelState.Remove(nameof(Order.ProductName));
            ModelState.Remove(nameof(Order.CustomerId));
            ModelState.Remove(nameof(Order.ProductId));
            ModelState.Remove(nameof(Order.Quantity));
            ModelState.Remove(nameof(Order.UnitPrice));
            ModelState.Remove(nameof(Order.TotalPrice));
            ModelState.Remove(nameof(Order.OrderDate));

            if (!ModelState.IsValid) return View(model);

            model.PartitionKey = "Order";

            // Preserve the original values that shouldn't change
            var existingOrder = await _storage.GetEntityAsync<Order>("Order", model.RowKey);
            if (existingOrder != null)
            {
                model.Username = existingOrder.Username;
                model.ProductName = existingOrder.ProductName;
                model.CustomerId = existingOrder.CustomerId;
                model.ProductId = existingOrder.ProductId;
                model.Quantity = existingOrder.Quantity;
                model.UnitPrice = existingOrder.UnitPrice;
                model.TotalPrice = existingOrder.TotalPrice;
                model.OrderDate = existingOrder.OrderDate;
            }

            await _storage.UpdateEntityAsync(model);
            TempData["Message"] = "Order updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _storage.GetEntityAsync<Order>("Order", id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));
            await _storage.DeleteEntityAsync<Order>("Order", id);
            TempData["Message"] = "Order deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetProductPrice(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return Json(new { price = 0.0, stock = 0 });

            var product = await _storage.GetEntityAsync<Product>("Product", productId);
            if (product is null)
                return Json(new { price = 0.0, stock = 0 });

            return Json(new { price = product.Price, stock = product.StockAvailable });
        }
    }
}