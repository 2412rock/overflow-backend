using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OverflowBackend.Filters;
using OverflowBackend.Models.Requests;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Controllers
{
    [ApiController]
    public class MatchMakingController : ControllerBase
    {
        private readonly IMatchMakingService _matchMakingService;
        public MatchMakingController(IMatchMakingService matchMakingService)
        {
            _matchMakingService = matchMakingService;
        }

        [HttpPost]
        [AuthorizationFilter]
        [Route("api/addtoqueue")]
        public IActionResult AddToQueue()
        {
            var result = _matchMakingService.AddOrMatchPlayer((string)HttpContext.Items["username"]);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/getQueueSize")]
        public IActionResult GetQueueSize()
        {
            var result = _matchMakingService.GetQueueSize();
            return Ok(result);
        }

        [HttpGet]
        [AuthorizationFilter]
        [Route("api/getMyMatch")]
        public IActionResult GetMyMatch()
        {
            var result = _matchMakingService.FindMyMatch((string)HttpContext.Items["username"]);
            return Ok(result);
        }

        [HttpDelete]
        [AuthorizationFilter]
        [Route("api/removeMatch")]
        public IActionResult RemoveMatch()
        {
            var maybe = new Maybe<string>();
            _matchMakingService.RemoveMatch((string)HttpContext.Items["username"]);
            maybe.SetSuccess("Ok");
            return Ok(maybe);
        }

    }
}
