using System.Text.Json;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WeatherImageGenerator.ProcessJob.Models;
using WeatherImageGenerator.ProcessJob.Services;
using System.Text;

namespace WeatherImageGenerator.ProcessJob
{
    public class ProcessJobFunction
    {
        private readonly ILogger _logger;
        private readonly string _queueConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _tableConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private const string TableName = "WeatherImageGeneratorJobs";
        private const string GenerateImageQueueName = "generateimagequeue";
        private readonly WeatherStationClient _weatherStationClient;

        public ProcessJobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessJobFunction>();
            _weatherStationClient = new WeatherStationClient(new HttpClient());
        }

        [Function("ProcessJob")]
        public async Task Run(
            [QueueTrigger("startjobqueue", Connection = "AzureWebJobsStorage")] string jobId)
        {
            _logger.LogInformation($"Processing job with JobId: {jobId}");

            // initialize Table Client to update job progress
            var tableClient = new TableClient(_tableConnectionString, TableName);
            
            // fetch the job entry from Table Storage
            var jobEntry = await tableClient.GetEntityAsync<JobEntry>("WeatherJob", jobId);
            jobEntry.Value.Status = "Stations Retrieved";
            
            // call the buienradar API to get the weather stations
            var weatherStations = await _weatherStationClient.GetWeatherStationsAsync();
            
            // add the jobId to each weather station
            foreach (var station in weatherStations)
            {
                station.JobId = jobId;
            }
            
            // add a message to the new queue called GenerateImageQueue foreach station
            var queueClient = new QueueClient(_queueConnectionString, GenerateImageQueueName);
            await queueClient.CreateIfNotExistsAsync();
            
            foreach (var station in weatherStations)
            {
                var message = JsonSerializer.Serialize(station);
                message = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
                await queueClient.SendMessageAsync(message);
            }
            
            // update the job entry with the number of stations retrieved
            jobEntry.Value.TotalImages = weatherStations.Count;
            
            // update the status of the job entry
            jobEntry.Value.Status = "Image generation started";
            
            // update the job entry in Table Storage
            await tableClient.UpdateEntityAsync(jobEntry.Value, jobEntry.Value.ETag);
        }
    }
}
