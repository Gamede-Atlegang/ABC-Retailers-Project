using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ABCRetailers.Functions.Models;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Functions
{
    public class EnqueueOrderFunction
    {
        private readonly ILogger<EnqueueOrderFunction> _logger;
        private readonly QueueClient _queueClient;

        public EnqueueOrderFunction(ILogger<EnqueueOrderFunction> logger, IConfiguration config)
        {
            _logger = logger;
            var storage = config["AzureWebJobsStorage"]!;
            var queueName = config["OrderQueueName"] ?? "orders-to-persist";
            _queueClient = new QueueClient(storage, queueName);
            _queueClient.CreateIfNotExists();
        }

        [Function("EnqueueOrder")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var message = JsonSerializer.Deserialize<OrderMessage>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (message == null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid order payload");
                    return bad;
                }

                var payload = JsonSerializer.Serialize(message);
                await _queueClient.SendMessageAsync(payload);

                var resp = req.CreateResponse(HttpStatusCode.Accepted);
                await resp.WriteStringAsync(message.OrderId);
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue order");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Failed to enqueue order: {ex.Message}");
                return error;
            }
        }
    }
}


