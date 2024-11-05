using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WeatherImageGenerator.FetchResults.Models;
using Azure.Storage.Sas;
using Azure.Storage;

namespace WeatherImageGenerator.FetchResults
{
    public class FetchResultsFunction
    {
        private readonly ILogger<FetchResultsFunction> _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _tableConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _tableName = "WeatherImageGeneratorJobs";

        public FetchResultsFunction(ILogger<FetchResultsFunction> logger)
        {
            _logger = logger;
            _blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        }

        [Function("FetchResults")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "fetchresults/{jobId}")] HttpRequestData req,
            string jobId)
        {
            _logger.LogInformation($"Fetching results for JobId: {jobId}");

            // attempt to retrieve the job entity from Table Storage
            try
            {
                // generate list of URLs for the generated images
                var containerClient = _blobServiceClient.GetBlobContainerClient("weatherimages");
                var urls = new List<string>();

                // first get all images from the folder with the job id
                var blobItems = containerClient.GetBlobs(prefix: jobId);
                
                // generate a SAS token for each image
                foreach (var blobItem in blobItems)
                {
                    // generate a SAS token for each image
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var sasBuilder = new BlobSasBuilder
                    {
                        BlobContainerName = blobClient.BlobContainerName,
                        BlobName = blobClient.Name,
                        Resource = "b",
                        StartsOn = DateTimeOffset.UtcNow,
                        ExpiresOn = DateTimeOffset.UtcNow.AddHours(2)
                    };
                    
                    sasBuilder.SetPermissions(BlobSasPermissions.Read);
                    
                    var sasToken = sasBuilder.ToSasQueryParameters(
                        new StorageSharedKeyCredential(
                            Environment.GetEnvironmentVariable("AzureAccountName"), 
                            Environment.GetEnvironmentVariable("AzureWebJobsStorageKey"))
                        );
                    
                    urls.Add($"{blobClient.Uri}?{sasToken}");
                }
                
                var numberOfImages = urls.Count;

                // create the response with the list of URLs
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { jobId, numberOfImages, urls });
                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                var response = req.CreateResponse(HttpStatusCode.NotFound);
                await response.WriteAsJsonAsync(new { jobId, error = "Job not found" });
                return response;
            }
        }
    }
}
