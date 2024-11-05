using System.Text.Json;
using WeatherImageGenerator.ProcessJob.Models;

namespace WeatherImageGenerator.ProcessJob.Services
{
    public class WeatherStationClient
    {
        private readonly HttpClient _httpClient;

        public WeatherStationClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<WeatherStation>> GetWeatherStationsAsync()
        {
            var response = await _httpClient.GetStringAsync("https://api.buienradar.nl/data/public/2.0/jsonfeed");
            
            using JsonDocument doc = JsonDocument.Parse(response);
            var stationMeasurements = doc.RootElement
                .GetProperty("actual")
                .GetProperty("stationmeasurements");

            var weatherStations = new List<WeatherStation>();

            foreach (var station in stationMeasurements.EnumerateArray())
            {
                var weatherStation = new WeatherStation
                {
                    StationId = station.TryGetProperty("stationid", out var stationId) ? stationId.GetInt32() : 0,
                    StationName = station.TryGetProperty("stationname", out var stationName) ? stationName.GetString() : "Unknown",
                    Region = station.TryGetProperty("regio", out var region) ? region.GetString() : "Unknown",
                    Temperature = station.TryGetProperty("temperature", out var temperature) ? temperature.GetDouble() : 0.0,
                    WindSpeed = station.TryGetProperty("windspeed", out var windSpeed) ? windSpeed.GetDouble() : 0.0,
                    WindDirectionDegrees = station.TryGetProperty("winddirectiondegrees", out var windDirection) ? windDirection.GetDouble() : 0.0
                };

                weatherStations.Add(weatherStation);
            }

            return weatherStations;
        }
    }
}