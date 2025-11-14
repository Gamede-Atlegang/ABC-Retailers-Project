using ABCRetailers_POE3_.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers_POE3_.Controllers;

[Authorize(Roles = "Admin")]
public class CustomerController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(ApplicationDbContext dbContext, ILogger<CustomerController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var customers = await _dbContext.Customers
            .OrderBy(c => c.Surname)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return View(customers);
    }

    public IActionResult Create() => View(new Customer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer model)
    {
        if (string.IsNullOrWhiteSpace(model.CustomerId))
        {
            ModelState.Remove(nameof(Customer.CustomerId));
            model.CustomerId = $"CUS-{Guid.NewGuid():N}".Substring(0, 12).ToUpperInvariant();
        }

        if (await _dbContext.Customers.AnyAsync(c => c.CustomerId == model.CustomerId))
        {
            ModelState.AddModelError(nameof(Customer.CustomerId), "Customer code already exists.");
        }

        if (await _dbContext.Customers.AnyAsync(c => c.Username == model.Username))
        {
            ModelState.AddModelError(nameof(Customer.Username), "Username already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.CreatedDate = DateTime.UtcNow;
        model.UpdatedDate = DateTime.UtcNow;

        _dbContext.Customers.Add(model);
        await _dbContext.SaveChangesAsync();

        TempData["Message"] = "Customer created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (await _dbContext.Customers.AnyAsync(c => c.CustomerId == model.CustomerId && c.Id != id))
        {
            ModelState.AddModelError(nameof(Customer.CustomerId), "Customer code already exists.");
        }

        if (await _dbContext.Customers.AnyAsync(c => c.Username == model.Username && c.Id != id))
        {
            ModelState.AddModelError(nameof(Customer.Username), "Username already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        customer.CustomerId = model.CustomerId;
        customer.Username = model.Username;
        customer.Name = model.Name;
        customer.Surname = model.Surname;
        customer.Email = model.Email;
        customer.Phone = model.Phone;
        customer.Address = model.Address;
        customer.UpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        TempData["Message"] = "Customer updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _dbContext.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (customer.Orders.Any())
        {
            TempData["Error"] = "Cannot delete customer with existing orders.";
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync();

        TempData["Message"] = "Customer deleted.";
        return RedirectToAction(nameof(Index));
    }
}
