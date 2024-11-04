using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WeatherImageGenerator.StartJob.Models;
using WeatherImageGenerator.StartJob.Models.DTOs;

namespace WeatherImageGenerator.StartJob
{
    public class StartJobFunction
    {
        private readonly ILogger _logger;
        private readonly string _queueConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _tableConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _queueName = "startjobqueue";
        private readonly string _tableName = "WeatherImageGeneratorJobs"; // Name of the table in Table Storage

        public StartJobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StartJobFunction>();
        }

        [Function("StartJob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Received request to start a new job.");

            // generate a unique jobId
            var jobId = Guid.NewGuid().ToString();

            // initialize Queue Client
            var queueClient = new QueueClient(_queueConnectionString, _queueName);
            await queueClient.CreateIfNotExistsAsync();

            // encode jobId in Base64
            var base64JobId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jobId));

            // add the Base64-encoded jobId as a message to the queue
            await queueClient.SendMessageAsync(base64JobId);

            _logger.LogInformation($"JobId {jobId} has been added to the queue.");

            // initialize Table Client
            var tableClient = new TableClient(_tableConnectionString, _tableName);
            await tableClient.CreateIfNotExistsAsync();

            // create a new JobEntry for Table Storage
            var jobEntry = new JobEntry
            {
                RowKey = jobId,
                Status = "In Progress",
                StartTime = DateTimeOffset.UtcNow,
                TotalImages = 0,
                ImagesCompleted = 0
            };

            // add the job entry to Table Storage
            await tableClient.AddEntityAsync(jobEntry);

            _logger.LogInformation($"JobId {jobId} has been added to Table Storage with status 'In Progress'.");

            // create a response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(
                new StartJobResponse
                {
                    JobId = jobId
                });

            return response;
        }
    }
}
