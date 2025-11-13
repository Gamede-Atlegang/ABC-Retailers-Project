using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public interface IFunctionsClient
    {
        Task<string> EnqueueOrderAsync(Order order);
        Task<string> UploadBlobAsync(Stream fileStream, string fileName);
        Task<string> WriteFileAsync(string content, string path, string fileName);
        string GetTestUrl();
    }
}
