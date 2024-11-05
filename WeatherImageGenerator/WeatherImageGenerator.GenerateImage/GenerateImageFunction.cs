using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using WeatherImageGenerator.GenerateImage.Models;

namespace WeatherImageGenerator.GenerateImage
{
    public class GenerateImageFunction
    {
        private readonly ILogger<GenerateImageFunction> _logger;
        private readonly HttpClient _httpClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _tableConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _tableName = "WeatherImageGeneratorJobs";

        public GenerateImageFunction(ILogger<GenerateImageFunction> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        }

        [Function("GenerateImageFunction")]
        public async Task Run([QueueTrigger("generateimagequeue", Connection = "AzureWebJobsStorage")] WeatherStation weatherStation)
        {
            _logger.LogInformation($"Processing image generation for JobId: {weatherStation.JobId}, StationId: {weatherStation.StationId}");

            // get background image from external API
            var response = await _httpClient.GetAsync("https://picsum.photos/200");

            if (response.IsSuccessStatusCode)
            {
                using var imageStream = await response.Content.ReadAsStreamAsync();
                using var skImage = SKBitmap.Decode(imageStream);
                using var canvas = new SKCanvas(skImage);

                // set up font and paint for text
                var paint = new SKPaint
                {
                    Color = SKColors.White,
                    TextSize = 16,
                    IsAntialias = true
                };

                var lines = new[]
                {
                    $"Region: {weatherStation.Region}",
                    $"Temp: {weatherStation.Temperature}°C",
                    $"Wind: {weatherStation.WindSpeed} km/h"
                };

                float x = 10;
                float y = 30;
                foreach (var line in lines)
                {
                    canvas.DrawText(line, x, y, paint);
                    y += paint.TextSize + 5;
                }

                using var outputStream = new MemoryStream();
                using var skImageEncoded = SKImage.FromBitmap(skImage);
                skImageEncoded.Encode(SKEncodedImageFormat.Jpeg, 100).SaveTo(outputStream);

                outputStream.Position = 0;

                var containerClient = _blobServiceClient.GetBlobContainerClient("weatherimages");
                await containerClient.CreateIfNotExistsAsync();
                
                var blobClient = containerClient.GetBlobClient($"{weatherStation.JobId}/{weatherStation.StationId}.jpg");
                await blobClient.UploadAsync(outputStream, overwrite: true);

                _logger.LogInformation($"Image successfully saved to Blob Storage as {weatherStation.JobId}/{weatherStation.StationId}.jpg");

                // update the job in Table Storage with retries
                await UpdateJobWithRetry(weatherStation.JobId);
            }
            else
            {
                _logger.LogError($"Failed to get background image from external API. Status code: {response.StatusCode}");
            }
        }

        private async Task UpdateJobWithRetry(string jobId)
        {
            var tableClient = new TableClient(_tableConnectionString, _tableName);
            int maxRetries = 5;
            int retryCount = 0;
            TimeSpan delay = TimeSpan.FromSeconds(1);

            while (retryCount < maxRetries)
            {
                try
                {
                    // retrieve the latest entity version
                    var jobEntry = await tableClient.GetEntityAsync<JobEntry>("WeatherJob", jobId);

                    // increment images completed
                    jobEntry.Value.ImagesCompleted++;

                    // update entity with current ETag
                    await tableClient.UpdateEntityAsync(jobEntry.Value, jobEntry.Value.ETag);

                    _logger.LogInformation("Successfully updated job progress in Table Storage.");
                    return; // exit on success
                }
                catch (RequestFailedException ex) when (ex.Status == 412)
                {
                    retryCount++;
                    _logger.LogWarning($"ETag mismatch detected. Retry {retryCount}/{maxRetries} in {delay.TotalSeconds} seconds.");

                    // wait with exponential backoff
                    await Task.Delay(delay);
                    delay = delay * 2;
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
