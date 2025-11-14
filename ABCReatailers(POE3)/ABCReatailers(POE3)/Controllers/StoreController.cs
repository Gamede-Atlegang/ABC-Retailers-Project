using ABCRetailers_POE3_.Data;
using ABCRetailers_POE3_.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers_POE3_.Controllers;


[Authorize(Roles = "Customer")]
public class StoreController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public StoreController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var products = await _dbContext.Products
            .OrderBy(p => p.ProductName)
            .ToListAsync();

        return View(products);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            quantity = 1;
        }

        var userId = User.GetUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index") });
        }

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null)
        {
            TempData["Error"] = "We could not find your customer profile.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
        {
            TempData["Error"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        if (product.StockAvailable < quantity)
        {
            TempData["Error"] = $"Only {product.StockAvailable} units available.";
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        var cartItem = await _dbContext.Cart
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.ProductId == productId);

        if (cartItem == null)
        {
            cartItem = new Cart
            {
                CustomerId = customer.Id,
                ProductId = productId,
                Quantity = quantity,
                AddedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            await _dbContext.Cart.AddAsync(cartItem);
        }
        else
        {
            cartItem.Quantity += quantity;
            cartItem.UpdatedDate = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        TempData["Message"] = $"{product.ProductName} added to your cart.";
        return RedirectToAction(nameof(Index));
    }
}

