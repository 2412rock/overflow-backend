using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OverflowBackend.Models.Requests;
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
        [Route("api/addtoqueue")]
        public IActionResult AddToQueue([FromBody] AddToQueueRequest req)
        {
            var result = _matchMakingService.AddOrMatchPlayer(req.Username);
            return Ok(result);
        }


        [HttpGet]
        [Route("api/getMyMatch")]
        public IActionResult GetMyMatch([FromQuery] string username)
        {
            var result = _matchMakingService.FindMyMatch(username);
            return Ok(result);
        }

        [HttpDelete]
        [Route("api/removeMatch")]
        public IActionResult RemoveMatch([FromQuery] string username)
        {
            _matchMakingService.RemoveMatch(username);
            return Ok("ok");
        }

    }
}
