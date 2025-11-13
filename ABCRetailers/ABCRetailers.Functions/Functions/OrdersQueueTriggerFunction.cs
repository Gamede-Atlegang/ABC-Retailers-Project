using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ABCRetailers.Functions.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QueueTriggerAttribute = Microsoft.Azure.Functions.Worker.QueueTriggerAttribute;


namespace ABCRetailers.Functions.Functions
{
    internal class OrdersQueueTriggerFunction
    {
        private readonly ILogger<OrdersQueueTriggerFunction> _logger;
        private readonly TableClient _tableClient;

        public OrdersQueueTriggerFunction(ILogger<OrdersQueueTriggerFunction> logger, IConfiguration config)
        {
            _logger = logger;
            var storage = config["AzureWebJobsStorage"]!;
            var tableName = config["TableServiceTableName"] ?? "Orders";
            _tableClient = new TableClient(storage, tableName);
            _tableClient.CreateIfNotExists();
        }

        [Function("OrdersQueueTrigger")]
        public async Task RunAsync([QueueTrigger("%OrderQueueName%", Connection = "AzureWebJobsStorage")] string message)
        {
            try
            {
                _logger.LogInformation("Processing order message: {Message}", message);

                var order = JsonSerializer.Deserialize<OrderMessage>(message);
                if (order == null)
                {
                    _logger.LogWarning("Queue message could not be parsed: {Message}", message);
                    return;
                }

                _logger.LogInformation("Deserialized order: OrderId={OrderId}, CustomerId={CustomerId}, Total={Total}",
                    order.OrderId, order.CustomerId, order.Total);

                var entity = new Order
                {
                    PartitionKey = "Order",
                    RowKey = order.OrderId,
                    CustomerId = order.CustomerId,
                    Username = order.CustomerName,
                    ProductId = order.Items.FirstOrDefault()?.ProductId ?? "",
                    ProductName = order.ProductName,
                    OrderDate = order.CreatedUtc,
                    Quantity = order.Items.FirstOrDefault()?.Quantity ?? 0,
                    UnitPrice = (double)(order.Items.FirstOrDefault()?.UnitPrice ?? 0),
                    TotalPrice = (double)order.Total,
                    Status = "Processed"
                };

                await _tableClient.UpsertEntityAsync(entity);
                _logger.LogInformation("Order {OrderId} successfully upserted to table {TableName}",
                    order.OrderId, _tableClient.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue message: {Message}", message);
                throw; // Re-throw to trigger retry mechanism
            }
        }
    }
}

