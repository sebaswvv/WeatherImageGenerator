using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WeatherImageGenerator.GenerateImage.Models;
using WeatherImageGenerator.GenerateImage.Services;
using Azure.Storage.Blobs;

namespace WeatherImageGenerator.GenerateImage
{
    public class GenerateImageFunction
    {
        private readonly ILogger<GenerateImageFunction> _logger;
        private readonly ImageGenerationService _imageGenerationService;
        private readonly BlobService _blobService;
        private readonly TableService _tableService;

        public GenerateImageFunction(
            ILogger<GenerateImageFunction> logger, 
            ILogger<ImageGenerationService> imageLogger, 
            ILogger<BlobService> blobLogger, 
            ILogger<TableService> tableLogger
        )
        {
            _logger = logger;

            var httpClient = new HttpClient();
            _imageGenerationService = new ImageGenerationService(httpClient, imageLogger);

            var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            _blobService = new BlobService(blobServiceClient, blobLogger);

            var tableConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableName = "WeatherImageGeneratorJobs";
            _tableService = new TableService(tableConnectionString, tableName, tableLogger);
        }

        [Function("GenerateImageFunction")]
        public async Task Run([QueueTrigger("generateimagequeue", Connection = "AzureWebJobsStorage")] WeatherStation weatherStation)
        {
            _logger.LogInformation($"Processing image generation for JobId: {weatherStation.JobId}, StationId: {weatherStation.StationId}");

            var imageStream = await _imageGenerationService.GenerateImageAsync(weatherStation);

            if (imageStream != null)
            {
                await _blobService.UploadImageAsync(weatherStation.JobId, weatherStation.StationId.ToString(), imageStream);
                await _tableService.UpdateJobWithRetry(weatherStation.JobId);
            }
            else
            {
                _logger.LogError("Image generation failed.");
            }
        }
    }
}
