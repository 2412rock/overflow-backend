using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [Controller]
    public class AuthController : ControllerBase
    {
        [HttpPost]
        [Route("api/login")]
        public IActionResult Login()
        {
            return Ok("login success");
        }
    }
}
