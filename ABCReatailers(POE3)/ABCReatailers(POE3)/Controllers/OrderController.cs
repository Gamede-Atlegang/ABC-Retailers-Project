using ABCRetailers_POE3_.Data;
using ABCRetailers_POE3_.Models;
using ABCRetailers_POE3_.Models.View_Models;
using ABCRetailers_POE3_.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers_POE3_.Controllers;

[Authorize(Roles = "Admin")]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFunctionsClient _functionsClient;
    private readonly ILogger<OrderController> _logger;

    public OrderController(ApplicationDbContext dbContext, IFunctionsClient functionsClient, ILogger<OrderController> logger)
    {
        _dbContext = dbContext;
        _functionsClient = functionsClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string searchTerm = "")
    {
        var query = _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowered = searchTerm.ToLowerInvariant();
            query = query.Where(o =>
                o.OrderId.ToLower().Contains(lowered) ||
                o.Status.ToLower().Contains(lowered) ||
                (o.Customer != null && (o.Customer.Name + " " + o.Customer.Surname + " " + o.Customer.Username).ToLower().Contains(lowered)) ||
                o.OrderItems.Any(i => i.Product != null && i.Product.ProductName.ToLower().Contains(lowered)));
        }

        var orders = await query.ToListAsync();
        ViewBag.SearchTerm = searchTerm;
        return View(orders);
    }

    public async Task<IActionResult> Create()
    {
        var viewModel = await BuildOrderCreateViewModelAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildOrderCreateViewModelAsync(model));
        }

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == model.CustomerId);
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == model.ProductId);

        if (customer == null || product == null)
        {
            ModelState.AddModelError("", "Invalid customer or product selected.");
            return View(await BuildOrderCreateViewModelAsync(model));
        }

        if (product.StockAvailable < model.Quantity)
        {
            ModelState.AddModelError(nameof(model.Quantity), $"Only {product.StockAvailable} unit(s) available.");
            return View(await BuildOrderCreateViewModelAsync(model));
        }

        var order = new Data.Order
        {
            OrderId = $"ORD-{Guid.NewGuid():N}".Substring(0, 14).ToUpperInvariant(),
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            ShippingAddress = string.IsNullOrWhiteSpace(model.ShippingAddress) ? customer.Address : model.ShippingAddress,
            Notes = "Created via backoffice portal",
            OrderItems = new List<OrderItem>()
        };

        var orderItem = new OrderItem
        {
            ProductId = product.Id,
            Quantity = model.Quantity,
            UnitPrice = product.Price,
            TotalPrice = product.Price * model.Quantity
        };

        order.OrderItems.Add(orderItem);
        order.TotalPrice = orderItem.TotalPrice;

        product.StockAvailable -= model.Quantity;
        _dbContext.Orders.Add(order);

        await _dbContext.SaveChangesAsync();

        try
        {
            await TriggerServerlessPipelineAsync(order, customer, product, model.Quantity);
            TempData["Message"] = "Order submitted and queued for processing.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Order {OrderId} saved but failed to enqueue Azure Function workflow.", order.OrderId);
            TempData["Error"] = "Order saved locally but Azure processing failed. Please retry queue submission.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Data.Order model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var order = await _dbContext.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = model.Status;
        order.Notes = model.Notes;
        order.UpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        TempData["Message"] = "Order updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Orders.Remove(order);
        await _dbContext.SaveChangesAsync();

        TempData["Message"] = "Order deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetProductPrice(int productId)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
        {
            return Json(new { price = 0m, stock = 0 });
        }

        return Json(new { price = product.Price, stock = product.StockAvailable });
    }

    private async Task<OrderCreateViewModel> BuildOrderCreateViewModelAsync(OrderCreateViewModel? existing = null)
    {
        var customers = await _dbContext.Customers
            .OrderBy(c => c.Surname).ThenBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Name} {c.Surname} ({c.CustomerId})"
            })
            .ToListAsync();

        customers.Insert(0, new SelectListItem { Value = "", Text = "Select Customer" });

        var products = await _dbContext.Products
            .OrderBy(p => p.ProductName)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.ProductName} - {p.Price:C2} (Stock: {p.StockAvailable})",
                Disabled = p.StockAvailable <= 0
            })
            .ToListAsync();

        products.Insert(0, new SelectListItem { Value = "", Text = "Select Product" });

        var viewModel = existing ?? new OrderCreateViewModel();
        viewModel.Customers = customers;
        viewModel.Products = products;
        return viewModel;
    }

    private async Task TriggerServerlessPipelineAsync(Data.Order order, Data.Customer customer, Data.Product product, int quantity)
    {
        var tableOrder = new Models.Order
        {
            PartitionKey = "Order",
            RowKey = order.OrderId,
            CustomerId = customer.CustomerId,
            Username = customer.Username,
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Quantity = quantity,
            UnitPrice = (double)product.Price,
            TotalPrice = (double)order.TotalPrice,
            Status = order.Status,
            OrderDate = order.OrderDate
        };

        await _functionsClient.EnqueueOrderAsync(tableOrder);

        var receiptContent = $"Order Receipt\n" +
                             $"Order ID: {order.OrderId}\n" +
                             $"Customer: {customer.Name} {customer.Surname}\n" +
                             $"Product: {product.ProductName}\n" +
                             $"Quantity: {quantity}\n" +
                             $"Total: {order.TotalPrice:C}\n" +
                             $"Date: {order.OrderDate:yyyy-MM-dd HH:mm:ss}\n" +
                             $"Status: {order.Status}";

        await _functionsClient.WriteFileAsync(receiptContent, "receipts", $"receipt-{order.OrderId}.txt");
    }
}