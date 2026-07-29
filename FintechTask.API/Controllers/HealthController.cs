using Microsoft.AspNetCore.Mvc;

namespace FintechTask.API.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        
        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Проверка контроллера Health");
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}
