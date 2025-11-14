using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCRetailers.Functions.Models
{
    public class OrderItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderMessage
    {
        public string OrderId { get; set; } = Guid.NewGuid().ToString();
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
        public string ProductName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}

