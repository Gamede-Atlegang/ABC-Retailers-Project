using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABCRetailers_POE3_.Models.View_Models;

public class OrderCreateViewModel
{
    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Shipping Address")]
    public string? ShippingAddress { get; set; }

    public IEnumerable<SelectListItem> Customers { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Products { get; set; } = Enumerable.Empty<SelectListItem>();
}
