using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models;
using MyApi.Services;
using MyApi.Interfaces;

namespace MyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("forecast")]
        [Authorize]
        public ActionResult<WeatherForecast[]> GetWeatherForecast()
        {
            var forecast = _weatherService.GetWeatherForecast();
            return Ok(forecast);
        }
    }
}
