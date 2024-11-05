using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace WeatherImageGenerator.GenerateImage.Services
{
    public class BlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobService> _logger;

        public BlobService(BlobServiceClient blobServiceClient, ILogger<BlobService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        public async Task UploadImageAsync(string jobId, string stationId, MemoryStream imageStream)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("weatherimages");
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient($"{jobId}/{stationId}.jpg");
            await blobClient.UploadAsync(imageStream, overwrite: true);

            _logger.LogInformation($"Image successfully saved to Blob Storage as {jobId}/{stationId}.jpg");
        }
    }
}