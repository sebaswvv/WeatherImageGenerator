namespace WeatherImageGenerator.Models
{
    public class WeatherStation
    {
        public string JobId { get; set; }
        public int StationId { get; set; }
        public string StationName { get; set; }
        public string Region { get; set; }
        public double Temperature { get; set; }
        public double WindSpeed { get; set; }
        public double WindDirectionDegrees { get; set; }
    }
}