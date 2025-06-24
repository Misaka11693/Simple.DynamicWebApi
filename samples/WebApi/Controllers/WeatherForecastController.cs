using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace WebApplication1.Controllers
{
    [ApiController]
    //[Route("api")]
    [Route("api/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }
        //[HttpGet(Name = "ABCGetWeatherForecast")]
        [Route("asd" + "[action]")]
        [HttpPost]

        public IEnumerable<WeatherForecast> GetASD(int a, int b)
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        //[HttpGet(template: "aaa/{id}")]
        //public int GetById(int id)
        //{
        //    return id;
        //}

        //public string GetHelloWorld112([FromRoute] int a)
        //{
        //    return "Hello World";
        //}
    }
}
