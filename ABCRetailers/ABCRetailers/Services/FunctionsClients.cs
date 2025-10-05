using System.Text;
using System.Text.Json;
using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public class FunctionsClient : IFunctionsClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FunctionsClient> _logger;
        private readonly string _baseUrl;

        public FunctionsClient(HttpClient httpClient, IConfiguration configuration, ILogger<FunctionsClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["Functions:BaseUrl"] ?? "http://localhost:7081";
        }

        public async Task<string> EnqueueOrderAsync(Order order)
        {
            try
            {
                var orderMessage = new
                {
                    orderId = order.OrderId,
                    customerId = order.CustomerId,
                    items = new[] { new
                    {
                        productId = order.ProductId,
                        quantity = order.Quantity,
                        unitPrice = order.UnitPrice
                    }},
                    total = order.TotalPrice,
                    status = "Pending",
                    createdUtc = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(orderMessage);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/EnqueueOrder", content);

                if (response.IsSuccessStatusCode)
                {
                    var orderId = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Order {OrderId} enqueued successfully", orderId);
                    return orderId;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to enqueue order: {Error}", error);
                    throw new Exception($"Failed to enqueue order: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing order");
                throw;
            }
        }

        public async Task<string> UploadBlobAsync(Stream fileStream, string fileName)
        {
            try
            {
                var content = new StreamContent(fileStream);
                content.Headers.Add("x-filename", fileName);

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/WriteBlob", content);

                if (response.IsSuccessStatusCode)
                {
                    var blobUrl = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("File {FileName} uploaded to blob storage: {BlobUrl}", fileName, blobUrl);
                    return blobUrl;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to upload blob: {Error}", error);
                    throw new Exception($"Failed to upload blob: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob");
                throw;
            }
        }

        public async Task<string> WriteFileAsync(string content, string path, string fileName)
        {
            try
            {
                var stringContent = new StringContent(content, Encoding.UTF8, "text/plain");
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/WriteFileShare?path={Uri.EscapeDataString(path)}&name={Uri.EscapeDataString(fileName)}", stringContent);

                if (response.IsSuccessStatusCode)
                {
                    var filePath = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("File written to Azure Files: {FilePath}", filePath);
                    return filePath;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to write file: {Error}", error);
                    throw new Exception($"Failed to write file: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file");
                throw;
            }
        }
    }
}