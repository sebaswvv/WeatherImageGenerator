using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using WeatherImageGenerator.GenerateImage.Models;
using Azure.Data.Tables;

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
                    TextSize = 16, // smaller font size
                    IsAntialias = true
                };

                // Prepare the text lines
                var lines = new[]
                {
                    $"Station: {weatherStation.StationName}",
                    $"Region: {weatherStation.Region}",
                    $"Temp: {weatherStation.Temperature}°C",
                    $"Wind: {weatherStation.WindSpeed} km/h"
                };

                // Draw each line of text with a small vertical offset for each new line
                float x = 10;
                float y = 30; // Start a bit down from the top
                foreach (var line in lines)
                {
                    canvas.DrawText(line, x, y, paint);
                    y += paint.TextSize + 5; // Move down by text size plus a small padding
                }

                // save the modified image to a MemoryStream as JPEG
                using var outputStream = new MemoryStream();
                using var skImageEncoded = SKImage.FromBitmap(skImage);
                skImageEncoded.Encode(SKEncodedImageFormat.Jpeg, 100).SaveTo(outputStream);

                outputStream.Position = 0;

                // upload the image to Blob Storage
                var containerClient = _blobServiceClient.GetBlobContainerClient("weatherimages");
                await containerClient.CreateIfNotExistsAsync();
                
                var blobClient = containerClient.GetBlobClient($"{weatherStation.JobId}/{weatherStation.StationId}.jpg");
                await blobClient.UploadAsync(outputStream, overwrite: true);

                _logger.LogInformation($"Image successfully saved to Blob Storage as {weatherStation.JobId}/{weatherStation.StationId}.jpg");
                
                // initialize Table Client to update job progress
                var tableClient = new TableClient(_tableConnectionString, _tableName);
                
                // fetch the job entry from Table Storage
                var jobEntry = await tableClient.GetEntityAsync<JobEntry>("WeatherJob", weatherStation.JobId);
                jobEntry.Value.ImagesCompleted++;
       
                await tableClient.UpdateEntityAsync(jobEntry.Value, jobEntry.Value.ETag);
            }
            else
            {
                _logger.LogError($"Failed to get background image from external API. Status code: {response.StatusCode}");
            }
        }
    }
}
