using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OverflowBackend.Filters;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services;
using OverflowBackend.Services.Implementantion;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Controllers
{
    [ApiController]
    public class GameInviteController : ControllerBase
    {
        private readonly MatchMakingService _matchMakingService;
        private readonly IConnectionManager _connectionManager;
        private readonly IVersionService _versionService;
        public GameInviteController(MatchMakingService matchMakingService, IConnectionManager connectionManager, IVersionService versionService)
        {
            _matchMakingService = matchMakingService;
            _connectionManager = connectionManager;
            _versionService = versionService;
        }

        [HttpGet]
        [Route("api/canInvite")]
        [AuthorizationFilter]
        public IActionResult CanInvite([FromQuery] string username)
        {
            var maybe = new Maybe<bool>();
            var inQueue = _matchMakingService.IsInQueue(username);
            if (inQueue)
            {
                maybe.SetSuccess(false);
                return Ok(maybe);
            }
            var anyInGame = WebSocketHandler.Games.Any(e => e.Player1 == username || e.Player2 == username);
            if (anyInGame)
            {
                maybe.SetSuccess(false);
                return Ok(maybe);
            }
            var isOnline = _connectionManager.UserConnections.ContainsKey(username);
            if (!isOnline)
            {
                maybe.SetSuccess(false);
                return Ok(maybe);
            }
            maybe.SetSuccess(true);
            return Ok(maybe);
        }
    }
}
