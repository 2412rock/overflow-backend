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

        [HttpPut]
        [AuthorizationFilter]
        [Route("api/acceptFriendRequest")]
        public async Task<IActionResult> AcceptFriendRequest([FromBody] FriendRequest request)
        {
            var result = await _friendService.AcceptFriendRequest((string)HttpContext.Items["username"], request.FriendUsername);
            return Ok(result);
        }

        [HttpPut]
        [AuthorizationFilter]
        [Route("api/declineFriendRequest")]
        public async Task<IActionResult> DeclineFriendRequest([FromBody] FriendRequest request)
        {
            var result = await _friendService.DeclineFriendRequest((string)HttpContext.Items["username"], request.FriendUsername);
            return Ok(result);
        }

        [HttpPost]
        [AuthorizationFilter]
        [Route("api/unfriend")]
        public async Task<IActionResult> Unfriend([FromBody] FriendRequest request)
        {
            var result = await _friendService.Unfriend((string)HttpContext.Items["username"], request.FriendUsername);
            return Ok(result);
        }

        [HttpPost]
        [AuthorizationFilter]
        [Route("api/blockUser")]
        public async Task<IActionResult> BlockUser([FromBody] FriendRequest request)
        {
            var result = await _friendService.BlockUser((string)HttpContext.Items["username"], request.FriendUsername);
            return Ok(result);
        }

        [HttpGet]
        [AuthorizationFilter]
        [Route("api/getBlockedUsers")]
        public async Task<IActionResult> GetBlockedUsers()
        {
            var result = await _friendService.GetBlockedUsers((string)HttpContext.Items["username"]);
            return Ok(result);
        }

        [HttpPost]
        [AuthorizationFilter]
        [Route("api/unblockUser")]
        public async Task<IActionResult> UnblockUser(FriendRequest request)
        {
            var result = await _friendService.UnblockUser((string)HttpContext.Items["username"], request.FriendUsername);
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

        [HttpPost]
        [AuthorizationFilter]
        [Route("api/reportUser")]
        public async Task<IActionResult> ReportUser(ReportRequest request)
        {
            var result = await _friendService.ReportUser((string)HttpContext.Items["username"], request.ReportedUsername, request.Description);
            return Ok(result);
        }
    }
}
