using MyApi.Models;
using MyApi.Repositories;
using MyApi.Interfaces;

namespace MyApi.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly IWeatherRepository _weatherRepository;

        public WeatherService(IWeatherRepository weatherRepository)
        {
            _weatherRepository = weatherRepository;
        }

        public WeatherForecast[] GetWeatherForecast()
        {
            return _weatherRepository.GetWeatherData();
        }
    }
}
