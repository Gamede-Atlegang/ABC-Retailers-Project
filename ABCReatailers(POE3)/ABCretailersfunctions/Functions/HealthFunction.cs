using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Functions
{
    public class HealthFunction
    {
        private readonly ILogger<HealthFunction> _logger;

        public HealthFunction(ILogger<HealthFunction> logger)
        {
            _logger = logger;
        }

        [Function("Health")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Health check invoked");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("OK");
            return response;
        }
    }
}
