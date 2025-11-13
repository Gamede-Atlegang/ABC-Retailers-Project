using System.Net;
using System.Web;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace ABCRetailers.Functions.Functions
{
    public class WriteFileShareFunction
    {
        private readonly ShareClient _shareClient;

        public WriteFileShareFunction(IConfiguration config)
        {
            var storage = config["AzureWebJobsStorage"]!;
            var shareName = config["FileShareName"] ?? "exports";
            _shareClient = new ShareClient(storage, shareName);
            _shareClient.CreateIfNotExists();
        }

        [Function("WriteFileShare")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var path = query["path"] ?? "receipts";
                var name = query["name"] ?? $"receipt-{Guid.NewGuid():N}.txt";

                var directory = _shareClient.GetDirectoryClient(path);
                await directory.CreateIfNotExistsAsync();

                var file = directory.GetFileClient(name);
                using var ms = new MemoryStream();
                await req.Body.CopyToAsync(ms);
                ms.Position = 0;
                await file.CreateAsync(ms.Length);
                ms.Position = 0;
                await file.UploadAsync(ms);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteStringAsync($"{path}/{name}");
                return ok;
            }
            catch (Exception ex)
            {
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Failed to write file: {ex.Message}");
                return error;
            }
        }
    }
}


