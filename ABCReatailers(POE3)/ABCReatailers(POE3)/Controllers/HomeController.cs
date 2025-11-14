using System.Diagnostics;
using ABCRetailers_POE3_.Data;
using ABCRetailers_POE3_.Models;
using ABCRetailers_POE3_.Models.View_Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers_POE3_.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public HomeController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var customersCount = await _dbContext.Customers.CountAsync();
        var products = await _dbContext.Products
            .OrderByDescending(p => p.UpdatedDate)
            .Take(5)
            .ToListAsync();
        var productCount = await _dbContext.Products.CountAsync();
        var orderCount = await _dbContext.Orders.CountAsync();

        var viewModel = new HomeViewModel
        {
            CustomerCount = customersCount,
            ProductCount = productCount,
            OrderCount = orderCount,
            FeaturedProducts = products
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