using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using WeatherImageGenerator.GenerateImage.Models;

namespace WeatherImageGenerator.GenerateImage.Services
{
    public class TableService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<TableService> _logger;

        public TableService(string tableConnectionString, string tableName, ILogger<TableService> logger)
        {
            _tableClient = new TableClient(tableConnectionString, tableName);
            _logger = logger;
        }

        public async Task UpdateJobWithRetry(string jobId)
        {
            int maxRetries = 10;
            int retryCount = 0;
            TimeSpan delay = TimeSpan.FromSeconds(1);

            while (retryCount < maxRetries)
            {
                try
                {
                    var jobEntry = await _tableClient.GetEntityAsync<JobEntry>("WeatherJob", jobId);
                    jobEntry.Value.ImagesCompleted++;
                    await _tableClient.UpdateEntityAsync(jobEntry.Value, jobEntry.Value.ETag);

                    _logger.LogInformation("Successfully updated job progress in Table Storage.");
                    return;
                }
                catch (RequestFailedException ex) when (ex.Status == 412)
                {
                    retryCount++;
                    _logger.LogWarning($"ETag mismatch detected. Retry {retryCount}/{maxRetries} in {delay.TotalSeconds} seconds.");
                    await Task.Delay(delay);
                    delay *= 2;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while updating Table Storage: {ex.Message}");
                    throw;
                }
            }

            _logger.LogError($"Failed to update job progress after {maxRetries} retries due to ETag conflicts.");
        }
    }
}