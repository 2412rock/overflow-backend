using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OverflowBackend.Controllers
{
    [ApiController]
    public class HealthCheck : ControllerBase
    {
        [HttpGet]
        [Route("api/health")]
        public IActionResult Health()
        {
            return Ok("Online");
        }
    }
}
