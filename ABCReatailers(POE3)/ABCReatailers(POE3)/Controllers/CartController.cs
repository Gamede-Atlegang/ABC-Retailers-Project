using ABCRetailers_POE3_.Data;
using ABCRetailers_POE3_.Extensions;
using ABCRetailers_POE3_.Models.View_Models;
using ABCRetailers_POE3_.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TableOrder = ABCRetailers_POE3_.Models.Order;

namespace ABCRetailers_POE3_.Controllers;

[Authorize(Roles = "Customer")]
public class CartController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFunctionsClient _functionsClient;
    private readonly ILogger<CartController> _logger;

    public CartController(
        ApplicationDbContext dbContext,
        IFunctionsClient functionsClient,
        ILogger<CartController> logger)
    {
        _dbContext = dbContext;
        _functionsClient = functionsClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            TempData["Error"] = "Unable to load your customer profile.";
            return RedirectToAction("Index", "Store");
        }

        var cartItems = await _dbContext.Cart
            .Where(c => c.CustomerId == customer.Id)
            .Include(c => c.Product)
            .OrderByDescending(c => c.UpdatedDate)
            .ToListAsync();

        var model = cartItems.Select(item => new CartItemViewModel
        {
            CartId = item.Id,
            ProductId = item.ProductId,
            ProductName = item.Product.ProductName,
            ImageUrl = item.Product.ImageUrl,
            Quantity = item.Quantity,
            UnitPrice = item.Product.Price
        }).ToList();

        ViewBag.CartTotal = model.Sum(i => i.LineTotal);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int cartId, int quantity)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return RedirectToAction("Index", "Store");
        }

        var cartItem = await _dbContext.Cart
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cartItem == null)
        {
            TempData["Error"] = "Cart item not found.";
            return RedirectToAction(nameof(Index));
        }

        if (cartItem.CustomerId != customer.Id)
        {
            return Forbid();
        }

        if (quantity <= 0)
        {
            _dbContext.Cart.Remove(cartItem);
        }
        else
        {
            if (cartItem.Product.StockAvailable < quantity)
            {
                TempData["Error"] = $"Only {cartItem.Product.StockAvailable} units available for {cartItem.Product.ProductName}.";
                return RedirectToAction(nameof(Index));
            }

            cartItem.Quantity = quantity;
            cartItem.UpdatedDate = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartId)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return RedirectToAction("Index", "Store");
        }

        var cartItem = await _dbContext.Cart.FirstOrDefaultAsync(c => c.Id == cartId);
        if (cartItem != null && cartItem.CustomerId == customer.Id)
        {
            _dbContext.Cart.Remove(cartItem);
            await _dbContext.SaveChangesAsync();
        }
        else if (cartItem != null)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            TempData["Error"] = "Unable to load your customer profile.";
            return RedirectToAction(nameof(Index));
        }

        var cartItems = await _dbContext.Cart
            .Where(c => c.CustomerId == customer.Id)
            .Include(c => c.Product)
            .ToListAsync();

        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction(nameof(Index));
        }

        var order = new Data.Order
        {
            OrderId = $"ORD-{Guid.NewGuid():N}".Substring(0, 14),
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = customer.Address,
            Status = "Pending",
            Notes = "Online purchase",
            OrderItems = new List<OrderItem>()
        };

        foreach (var item in cartItems)
        {
            if (item.Product.StockAvailable < item.Quantity)
            {
                TempData["Error"] = $"Not enough stock for {item.Product.ProductName}.";
                return RedirectToAction(nameof(Index));
            }

            item.Product.StockAvailable -= item.Quantity;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Product = item.Product,
                Quantity = item.Quantity,
                UnitPrice = item.Product.Price,
                TotalPrice = item.Product.Price * item.Quantity
            });
        }

        order.TotalPrice = order.OrderItems.Sum(i => i.TotalPrice);

        await _dbContext.Orders.AddAsync(order);
        _dbContext.Cart.RemoveRange(cartItems);
        await _dbContext.SaveChangesAsync();

        try
        {
            await EnqueueOrderForProcessingAsync(order, customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger Azure Functions for order {OrderId}", order.OrderId);
        }

        TempData["Message"] = $"Order {order.OrderId} submitted successfully.";
        return RedirectToAction("MyOrders", "Orders");
    }

    private async Task<Customer?> GetCurrentCustomerAsync()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return null;
        }

        return await _dbContext.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
    }

    private async Task EnqueueOrderForProcessingAsync(Data.Order order, Customer customer)
    {
        var firstItem = order.OrderItems.First();
        var tableOrder = new TableOrder
        {
            PartitionKey = "Order",
            RowKey = order.OrderId,
            CustomerId = customer.CustomerId,
            Username = customer.Username,
            ProductId = firstItem.Product.ProductId,
            ProductName = firstItem.Product.ProductName,
            OrderDate = order.OrderDate,
            Quantity = firstItem.Quantity,
            UnitPrice = (double)firstItem.UnitPrice,
            TotalPrice = (double)order.TotalPrice,
            Status = order.Status
        };

        await _functionsClient.EnqueueOrderAsync(tableOrder);

        var receipt = $"Order Receipt\n" +
                      $"Order ID: {order.OrderId}\n" +
                      $"Customer: {customer.Name} {customer.Surname}\n" +
                      $"Items: {string.Join(", ", order.OrderItems.Select(i => $"{i.Quantity} x {(i.Product?.ProductName ?? i.ProductId.ToString())}"))}\n" +
                      $"Total: {order.TotalPrice:C}\n" +
                      $"Date: {order.OrderDate:O}\n" +
                      $"Status: {order.Status}";

        await _functionsClient.WriteFileAsync(receipt, "receipts", $"receipt-{order.OrderId}.txt");
    }
}

