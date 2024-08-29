using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OverflowBackend.Filters;
using OverflowBackend.Models.Requests;
using OverflowBackend.Services.Implementantion;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Controllers
{
    [ApiController]
    public class FriendsController : ControllerBase
    {
        private readonly IFriendService _friendService;

        public FriendsController(IFriendService friendService)
        {
            _friendService = friendService;
        }

        [HttpGet]
        [AuthorizationFilter]
        [Route("api/getFriends")]
        public async Task<IActionResult> GetFriends()
        {
            var result = await _friendService.GetFriends((string)HttpContext.Items["username"]);
            return Ok(result);
        }

        [HttpGet]
        [AuthorizationFilter]
        [Route("api/getUsernames")]
        public async Task<IActionResult> GetUsernames([FromQuery] string startsWith)
        {
            var result = await _friendService.GetUsernames((string)HttpContext.Items["username"], startsWith);
            return Ok(result);
        }

        [HttpPost]
        [AuthorizationFilter]
        [Route("api/sendFriendRequest")]
        public async Task<IActionResult> GetFriends([FromBody] FriendRequest request)
        {
            var result = await _friendService.SendFriendRequest((string)HttpContext.Items["username"], request.FriendUsername);
            return Ok(result);
        }

        [HttpGet]
        [AuthorizationFilter]
        [Route("api/getFriendRequests")]
        public async Task<IActionResult> GetFriendRequests()
        {
            var result = await _friendService.GetFriendRequests((string)HttpContext.Items["username"]);
            return Ok(result);
        }
    }
}
