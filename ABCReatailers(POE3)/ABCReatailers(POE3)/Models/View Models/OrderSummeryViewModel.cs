using ABCRetailers_POE3_.Data;

namespace ABCRetailers_POE3_.Models.View_Models;

public class OrderSummaryViewModel
{
    public Order Order { get; set; } = null!;
    public IReadOnlyCollection<OrderItem> Items { get; set; } = Array.Empty<OrderItem>();
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
}

