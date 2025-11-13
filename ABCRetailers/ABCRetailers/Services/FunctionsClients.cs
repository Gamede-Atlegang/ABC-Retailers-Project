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

            // Try multiple configuration paths and log all attempts
            var configValue1 = configuration["Functions:BaseUrl"];
            var configValue2 = configuration["Functions:BaseURL"];

            _logger.LogInformation("Configuration 'Functions:BaseUrl': '{ConfigValue1}'", configValue1);
            _logger.LogInformation("Configuration 'Functions:BaseURL': '{ConfigValue2}'", configValue2);

            // Log environment info for debugging
            var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown";
            _logger.LogInformation("Environment: '{Environment}'", environment);

            _baseUrl = configValue1 ?? configValue2 ?? "http://localhost:7081";

            _logger.LogInformation("FunctionsClient initialized with BaseUrl: '{BaseUrl}'", _baseUrl);
        }

        private string GetValidBaseUrl()
        {
            var validBaseUrl = string.IsNullOrEmpty(_baseUrl) ? "http://localhost:7081" : _baseUrl.TrimEnd('/');

            // Fix common typos in the URL
            if (validBaseUrl.StartsWith("htpp//"))
            {
                validBaseUrl = validBaseUrl.Replace("htpp//", "http://");
            }
            else if (validBaseUrl.StartsWith("htpp/"))
            {
                validBaseUrl = validBaseUrl.Replace("htpp/", "http://");
            }
            else if (!validBaseUrl.StartsWith("http://") && !validBaseUrl.StartsWith("https://"))
            {
                validBaseUrl = $"http://{validBaseUrl}";
            }

            return validBaseUrl;
        }

        public string GetTestUrl()
        {
            return $"{GetValidBaseUrl()}/api/Health";
        }

        public async Task<string> EnqueueOrderAsync(Order order)
        {
            try
            {
                // Debug logging
                _logger.LogInformation("EnqueueOrderAsync called with BaseUrl: '{BaseUrl}'", _baseUrl);

                var orderMessage = new
                {
                    orderId = order.OrderId,
                    customerId = order.CustomerId,
                    customerName = order.Username,
                    items = new[] { new
                    {
                        productId = order.ProductId,
                        quantity = order.Quantity,
                        unitPrice = order.UnitPrice
                    }},
                    productName = order.ProductName,
                    total = order.TotalPrice,
                    status = "Pending",
                    createdUtc = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(orderMessage);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Get valid base URL with typo correction
                var validBaseUrl = GetValidBaseUrl();
                var fullUrl = $"{validBaseUrl}/api/EnqueueOrder";

                _logger.LogInformation("Making request to: {FullUrl} (BaseUrl was: '{BaseUrl}')", fullUrl, _baseUrl);
                _logger.LogInformation("ValidBaseUrl: '{ValidBaseUrl}'", validBaseUrl);

                // Validate the URL before making the request
                if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri))
                {
                    _logger.LogError("Invalid URL constructed: '{FullUrl}'", fullUrl);
                    throw new InvalidOperationException($"Invalid URL constructed: {fullUrl}");
                }

                _logger.LogInformation("Validated URI: '{Uri}'", uri.ToString());

                // Create a new HttpClient for this request to avoid any BaseAddress issues
                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync(uri, content);

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

                // Use absolute URL to avoid BaseAddress issues
                var validBaseUrl = GetValidBaseUrl();
                var response = await _httpClient.PostAsync($"{validBaseUrl}/api/WriteBlob", content);

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
                // Use absolute URL to avoid BaseAddress issues
                var validBaseUrl = GetValidBaseUrl();
                var queryString = $"?path={Uri.EscapeDataString(path)}&name={Uri.EscapeDataString(fileName)}";
                var response = await _httpClient.PostAsync($"{validBaseUrl}/api/WriteFileShare{queryString}", stringContent);

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