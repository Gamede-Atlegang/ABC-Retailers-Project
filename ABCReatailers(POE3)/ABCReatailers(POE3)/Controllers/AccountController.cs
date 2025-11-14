using System.Security.Claims;
using ABCRetailers_POE3_.Data;
using ABCRetailers_POE3_.Models.View_Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers_POE3_.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        ILogger<AccountController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? role = null, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        var normalizedRole = NormalizeRole(role);
        var model = new LoginViewModel { Role = normalizedRole };

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _dbContext.Users
            .Include(u => u.Customer)
            .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        if (!string.Equals(user.Role, model.Role, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Role), $"This account is not a {model.Role} account.");
            return View(model);
        }

        user.LastLoginDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await SignInAsync(user, model.RememberMe);
        _logger.LogInformation("User {Username} logged in successfully.", user.Username);

        return RedirectToLocal(returnUrl);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usernameExists = await _dbContext.Users.AnyAsync(u => u.Username == model.Username);
        if (usernameExists)
        {
            ModelState.AddModelError(nameof(model.Username), "This username is already taken.");
            return View(model);
        }

        var user = new User
        {
            Username = model.Username.Trim(),
            Email = model.Email.Trim(),
            Role = "Customer",
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        var customer = new Customer
        {
            CustomerId = $"CUS-{Guid.NewGuid():N}".Substring(0, 12),
            Username = user.Username,
            Name = model.Name.Trim(),
            Surname = model.Surname.Trim(),
            Email = model.Email.Trim(),
            Phone = model.Phone,
            Address = model.Address,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            User = user
        };

        await _dbContext.Customers.AddAsync(customer);
        await _dbContext.SaveChangesAsync();

        await SignInAsync(user, rememberMe: false);
        TempData["Message"] = "Registration successful. Welcome!";
        return RedirectToAction("Index", "Store");
    }

    private static string NormalizeRole(string? role)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Admin";
        }

        return "Customer";
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Store");
    }

    private async Task SignInAsync(User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(4)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Store");
    }
}

