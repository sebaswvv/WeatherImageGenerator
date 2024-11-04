using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WeatherImageGenerator.ProcessJob.Models;
using WeatherImageGenerator.ProcessJob.Services;
using System.Text;
using Azure;

namespace WeatherImageGenerator.ProcessJob
{
    public class ProcessJobFunction
    {
        private readonly ILogger _logger;
        private readonly string _queueConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _tableConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _tableName = "WeatherImageGeneratorJobs";
        private readonly string _generateImageQueueName = "generateimagequeue";
        private readonly WeatherStationService _weatherStationService;

        public ProcessJobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessJobFunction>();
            _weatherStationService = new WeatherStationService(new HttpClient());
        }

        [Function("ProcessJob")]
        public async Task Run(
            [QueueTrigger("startjobqueue", Connection = "AzureWebJobsStorage")] string jobId)
        {
            _logger.LogInformation($"Processing job with JobId: {jobId}");

            // initialize Table Client to update job progress
            var tableClient = new TableClient(_tableConnectionString, _tableName);
            
            // fetch the job entry from Table Storage
            var jobEntry = await tableClient.GetEntityAsync<JobEntry>("WeatherJob", jobId);
            jobEntry.Value.Status = "Stations Retrieved";
            
            // call the buienradar API to get the weather stations
            var weatherStations = await _weatherStationService.GetWeatherStationsAsync();
            
            // add the jobId to each weather station
            foreach (var station in weatherStations)
            {
                station.JobId = jobId;
            }
            
            // add a message to the new queue called GenerateImageQueue foreach station
            var queueClient = new QueueClient(_queueConnectionString, _generateImageQueueName);
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
