using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WeatherImageGenerator.StartJob.Models.DTOs;
using WeatherImageGenerator.StartJob.Services;

namespace WeatherImageGenerator.StartJob
{
    public class StartJobFunction
    {
        private readonly ILogger<StartJobFunction> _logger;
        private readonly JobService _jobService;

        public StartJobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StartJobFunction>();

            var queueConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var queueName = "startjobqueue";
            var tableName = "WeatherImageGeneratorJobs";

            _jobService = new JobService(queueConnectionString, tableConnectionString, queueName, tableName, loggerFactory.CreateLogger<JobService>());
        }

        [Function("StartJob")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Received request to start a new job.");

            // start a new job and get the generated jobId
            var jobId = await _jobService.StartNewJobAsync();

            // create a response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new StartJobResponse { JobId = jobId });

            return response;
        }
    }
}