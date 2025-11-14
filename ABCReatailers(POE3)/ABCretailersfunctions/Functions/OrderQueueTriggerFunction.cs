using System.Text.Json;
using ABCRetailersfunctions.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailersfunctions.Functions
{
    public class OrdersQueueTriggerFunction
    {
        private readonly ILogger<OrdersQueueTriggerFunction> _logger;
        private readonly TableClient _tableClient;

        public OrdersQueueTriggerFunction(ILogger<OrdersQueueTriggerFunction> logger, IConfiguration config)
        {
            _logger = logger;
            var storage = config["AzureWebJobsStorage"]!;
            var tableName = "Order"; // Write to the same table MVC reads from
            _tableClient = new TableClient(storage, tableName);
            _tableClient.CreateIfNotExists();
        }

        [Function("OrdersQueueTrigger")]
        public async Task RunAsync([Microsoft.Azure.Functions.Worker.QueueTrigger("%OrderQueueName%", Connection = "AzureWebJobsStorage")] string message)
        {
            try
            {
                _logger.LogInformation("Processing order message. Raw length: {Length}", message?.Length ?? 0);

                OrderMessage? order = null;

                try
                {
                    order = JsonSerializer.Deserialize<OrderMessage>(message);
                }
                catch
                {
                    // Some SDKs/base bindings deliver base64 – attempt to decode
                    try
                    {
                        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message));
                        order = JsonSerializer.Deserialize<OrderMessage>(decoded);
                        _logger.LogInformation("Message was base64 – successfully decoded.");
                    }
                    catch (Exception inner)
                    {
                        _logger.LogError(inner, "Failed to parse queue message as JSON (raw or base64). Message sample: {Sample}", message?.Substring(0, Math.Min(128, message?.Length ?? 0)));
                        throw;
                    }
                }

                if (order == null)
                {
                    _logger.LogWarning("Queue message could not be parsed into OrderMessage.");
                    return;
                }

                // Write to the original Order table structure that MVC expects
                var orderEntity = new ABCRetailersfunctions.Functions.Models.Order
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

                await _tableClient.UpsertEntityAsync(orderEntity);
                _logger.LogInformation("Order {OrderId} upserted to table", order.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrdersQueueTrigger failed processing message.");
                throw; // rethrow to allow retries/poison handling
            }
        }
    }
}


