using System.ComponentModel.DataAnnotations;

namespace MyApi.Models
{
    public class WeatherForecast
    {
        [Key]
        public int Id { get; set; }

        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        public string? Summary { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public WeatherForecast() { }

        public WeatherForecast(DateOnly date, int temperatureC, string? summary)
        {
            Date = date;
            TemperatureC = temperatureC;
            Summary = summary;
        }
    }
}
