using System.Text;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using WeatherImageGenerator.StartJob.Models;

namespace WeatherImageGenerator.StartJob.Services
{
    public class JobService
    {
        private readonly QueueClient _queueClient;
        private readonly TableClient _tableClient;
        private readonly ILogger<JobService> _logger;

        public JobService(string queueConnectionString, string tableConnectionString, string queueName, string tableName, ILogger<JobService> logger)
        {
            _queueClient = new QueueClient(queueConnectionString, queueName);
            _tableClient = new TableClient(tableConnectionString, tableName);
            _logger = logger;
        }

        public async Task<string> StartNewJobAsync()
        {
            // generate a unique jobId
            var jobId = Guid.NewGuid().ToString();

            // create the queue if it doesn't exist and add the jobId
            await _queueClient.CreateIfNotExistsAsync();
            var base64JobId = Convert.ToBase64String(Encoding.UTF8.GetBytes(jobId));
            await _queueClient.SendMessageAsync(base64JobId);
            _logger.LogInformation($"JobId {jobId} has been added to the queue.");

            // create the table if it doesn't exist and add a new job entry
            await _tableClient.CreateIfNotExistsAsync();
            var jobEntry = new JobEntry
            {
                RowKey = jobId,
                Status = "In Progress",
                StartTime = DateTimeOffset.UtcNow,
                TotalImages = 0,
                ImagesCompleted = 0
            };
            await _tableClient.AddEntityAsync(jobEntry);
            _logger.LogInformation($"JobId {jobId} has been added to Table Storage with status 'In Progress'.");

            return jobId;
        }
    }
}