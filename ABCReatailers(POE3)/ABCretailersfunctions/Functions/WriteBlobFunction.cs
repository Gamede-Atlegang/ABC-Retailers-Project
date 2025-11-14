using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace ABCretailersfunctions.Functions
{
    public class WriteBlobFunction
    {
        private readonly BlobContainerClient _container;

        public WriteBlobFunction(IConfiguration config)
        {
            var storage = config["AzureWebJobsStorage"]!;
            var containerName = config["BlobContainerName"] ?? "product-images";
            _container = new BlobContainerClient(storage, containerName);
            _container.CreateIfNotExists();
        }

        [Function("WriteBlob")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req)
        {
            try
            {
                if (!req.Headers.TryGetValues("x-filename", out var names))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("x-filename header required");
                    return bad;
                }

                var fileName = names.First();
                var blob = _container.GetBlobClient(fileName);

                // Copy the request body to a memory stream since req.Body might be disposed
                using var ms = new MemoryStream();
                await req.Body.CopyToAsync(ms);
                ms.Position = 0;

                await blob.UploadAsync(ms, overwrite: true);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteStringAsync(blob.Uri.ToString());
                return ok;
            }
            catch (Exception ex)
            {
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Failed to upload blob: {ex.Message}");
                return error;
            }
        }
    }
}


