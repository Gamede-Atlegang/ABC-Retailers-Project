using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Functions.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string OrderId => RowKey;
        public string CustomerId { get; set; } = "";
        public string Username { get; set; } = "";
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Pending";
    }
}