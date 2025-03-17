using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mono.TextTemplating;
using SWD392.Service;

namespace SWD392.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UtilitiesController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<UtilitiesController> _logger;
        private readonly Cloudinary _cloudinary;

        public UtilitiesController(ILogger<UtilitiesController> logger, Cloudinary cloudinary)
        {
            _logger = logger;
            _cloudinary = cloudinary;
        }

        // POST: WeatherForecast/UploadImage
        /// <summary>
        /// Upload an image to Cloudinary (check action)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No file uploaded
        /// - Upload failed
        /// </remarks>
        /// <response code="200">Image uploaded successfully</response>
        /// <response code="400">No file uploaded</response>
        /// <response code="500">Upload failed</response>
        [Authorize]
        [HttpPost("UploadImage")]
        public async Task<ActionResult<ApiResponse<object>>> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Error("No file uploaded."));
            }

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return Ok(ApiResponse<object>.Success(new { uploadResult.Url }, message: "Image uploaded successfully"));
            }
            else
            {
                return StatusCode((int)uploadResult.StatusCode, ApiResponse<object>.Error(uploadResult.Error.Message));
            }
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [Authorize]
        [HttpGet("GetWeatherForecastAuth")]
        public IEnumerable<WeatherForecast> GetAuth()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [Authorize(Roles = "admin")]
        [HttpGet("GetWeatherForecastAdmin")]
        public IEnumerable<WeatherForecast> GetAdmin()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [Authorize(Roles = "member")]
        [HttpGet("GetWeatherForecastMember")]
        public IEnumerable<WeatherForecast> GetMember()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [Authorize(Roles = "doctor, admin")]
        [HttpGet("GetWeatherForecastDoctorAdmin")]
        public IEnumerable<WeatherForecast> GetDoctorAdmin()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}

