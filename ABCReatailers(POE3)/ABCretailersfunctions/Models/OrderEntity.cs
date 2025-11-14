using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailersfunctions.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty; // CustomerId
        public string RowKey { get; set; } = string.Empty; // OrderId
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public string ItemsJson { get; set; } = string.Empty;
    }
}

