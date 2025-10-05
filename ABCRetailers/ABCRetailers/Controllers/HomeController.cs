using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureStorageService _storage;

        public HomeController(IAzureStorageService storage)
        {
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _storage.GetAllEntitiesAsync<Customer>();
            var products = await _storage.GetAllEntitiesAsync<Product>();
            var orders = await _storage.GetAllEntitiesAsync<Order>();

            var viewModel = new HomeViewModel
            {
                CustomerCount = customers.Count,
                ProductCount = products.Count,
                OrderCount = orders.Count,
                FeaturedProducts = products.Take(5).ToList()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}