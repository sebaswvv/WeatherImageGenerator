using SkiaSharp;
using WeatherImageGenerator.GenerateImage.Models;
using Microsoft.Extensions.Logging;

namespace WeatherImageGenerator.GenerateImage.Services
{
    public class ImageGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImageGenerationService> _logger;

        public ImageGenerationService(HttpClient httpClient, ILogger<ImageGenerationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<MemoryStream> GenerateImageAsync(WeatherStation weatherStation)
        {
            var response = await _httpClient.GetAsync("https://picsum.photos/200");

            if (response.IsSuccessStatusCode)
            {
                using var imageStream = await response.Content.ReadAsStreamAsync();
                using var skImage = SKBitmap.Decode(imageStream);
                using var canvas = new SKCanvas(skImage);

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

                var outputStream = new MemoryStream();
                using var skImageEncoded = SKImage.FromBitmap(skImage);
                skImageEncoded.Encode(SKEncodedImageFormat.Jpeg, 100).SaveTo(outputStream);

                outputStream.Position = 0;
                return outputStream;
            }
            else
            {
                _logger.LogError($"Failed to get background image from external API. Status code: {response.StatusCode}");
                return null;
            }
        }
    }
}
