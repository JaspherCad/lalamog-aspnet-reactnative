using MyApi.Models;

namespace MyApi.Interfaces
{
    public interface IWeatherService
    {
        WeatherForecast[] GetWeatherForecast();
    }
}
