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
        public async Task RunAsync([Microsoft.Azure.Functions.Worker.QueueTrigger("%OrderQueueName%", Connection = "AzureWebJobsStorage")] string message)
        {
            _logger.LogInformation("Processing order message");
            var order = JsonSerializer.Deserialize<OrderMessage>(message);
            if (order == null)
            {
                _logger.LogWarning("Queue message could not be parsed");
                return;
            }

            var entity = new OrderEntity
            {
                PartitionKey = order.CustomerId,
                RowKey = order.OrderId,
                Total = order.Total,
                Status = "Processed",
                CreatedUtc = order.CreatedUtc,
                ItemsJson = JsonSerializer.Serialize(order.Items)
            };

            await _tableClient.UpsertEntityAsync(entity);
            _logger.LogInformation("Order {OrderId} upserted to table", order.OrderId);
        }
    }
}

