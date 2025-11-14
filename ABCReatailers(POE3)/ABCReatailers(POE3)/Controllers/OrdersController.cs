using ABCRetailers_POE3_.Data;
using ABCRetailers_POE3_.Extensions;
using ABCRetailers_POE3_.Models.View_Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers_POE3_.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public OrdersController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyOrders()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null)
        {
            TempData["Error"] = "We could not find your customer profile.";
            return RedirectToAction("Index", "Store");
        }

        var orders = await _dbContext.Orders
            .Where(o => o.CustomerId == customer.Id)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin(string? status = null)
    {
        var query = _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.Statuses = new[] { "Pending", "Processing", "Processed", "Shipped", "Delivered", "Cancelled" };

        return View(orders);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkProcessed(int id)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction(nameof(Admin));
        }

        order.Status = "Processed";
        order.UpdatedDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        TempData["Message"] = $"Order {order.OrderId} marked as PROCESSED.";
        return RedirectToAction(nameof(Admin));
    }
}

