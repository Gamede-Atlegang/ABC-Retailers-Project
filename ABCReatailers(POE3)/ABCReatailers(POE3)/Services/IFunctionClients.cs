using ABCRetailers_POE3_.Models;

namespace ABCRetailers_POE3_.Services
{
    public interface IFunctionsClient
    {
        Task<string> EnqueueOrderAsync(Order order);
        Task<string> UploadBlobAsync(Stream fileStream, string fileName);
        Task<string> WriteFileAsync(string content, string path, string fileName);
    }
}
