using ABCRetailers.Models;
using ABCRetailers.Services;
using Azure;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storage;

        public CustomerController(IAzureStorageService storage)
        {
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            // Lists all Customer entities
            var customers = await _storage.GetAllEntitiesAsync<Customer>();
            return View(customers
                .OrderBy(x => x.Surname)
                .ThenBy(x => x.Name));
        }

        public IActionResult Create() => View(new Customer());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            if (!ModelState.IsValid) return View(model);

            await _storage.AddEntityAsync(model);
            TempData["Message"] = "Customer created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var entity = await _storage.GetEntityAsync<Customer>("Customer", id);
            if (entity is null) return NotFound();

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer model)
        {
            if (!ModelState.IsValid) return View(model);

            model.PartitionKey = "Customer";
            await _storage.UpdateEntityAsync(model);
            TempData["Message"] = "Customer updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));

            await _storage.DeleteEntityAsync<Customer>("Customer", id);
            TempData["Message"] = "Customer deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}