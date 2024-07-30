using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OverflowBackend.Models.Requests;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Controllers
{

    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [Route("api/signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            var result = await _authService.SignUp(request.Username, request.Password, request.Email);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/signin")]
        public async Task<IActionResult> SignIn([FromBody] SinInRequest request)
        {
            var result = await _authService.SignIn(request.UserName, request.Password);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/usernameExists")]
        public async Task<IActionResult> UsernameExists([FromQuery] string username)
        {
            var result = await _authService.UserNameExists(username);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/refreshToken")]
        public async Task<IActionResult> LoginGoogle([FromBody] RefreshRequest request)
        {
            var result = await _authService.RefreshToken(request.RefreshToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/signinGoogle")]
        public async Task<IActionResult> LoginGoogle(LoginGoogleRequest request)
        {
            var result = await _authService.LoginGoogle(request.Email, request.Username, request.IdToken);
            return Ok("plm");
        }
    }
}
