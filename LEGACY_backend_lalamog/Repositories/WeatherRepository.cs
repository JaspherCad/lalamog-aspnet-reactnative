using MyApi.Models;

namespace MyApi.Repositories
{
    public interface IWeatherRepository
    {
        WeatherForecast[] GetWeatherData();
    }

    public class WeatherRepository : IWeatherRepository
    {
        private readonly string[] _summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public WeatherForecast[] GetWeatherData()
        {
            // In a real application, this would fetch data from a database
            return Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    _summaries[Random.Shared.Next(_summaries.Length)]
                ))
                .ToArray();
        }
    }
}
