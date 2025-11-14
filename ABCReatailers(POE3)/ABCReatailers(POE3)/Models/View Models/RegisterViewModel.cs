using System.ComponentModel.DataAnnotations;

namespace ABCRetailers_POE3_.Models.View_Models;

public class RegisterViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Surname { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [StringLength(500)]
    [Display(Name = "Shipping Address")]
    public string? Address { get; set; }

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

