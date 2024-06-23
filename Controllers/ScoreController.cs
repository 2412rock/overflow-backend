using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OverflowBackend.Filters;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Controllers
{
    [ApiController]
    public class ScoreController : ControllerBase
    {
        private readonly IScoreService _scoreService;
        public ScoreController(IScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        [Route("api/highScores")]
        public async Task<IActionResult> GetHighScores()
        {
            var result = await _scoreService.GetHighScores();
            return Ok(result);
        }

        [Route("api/getMyScore")]
        [AuthorizationFilter]
        public async Task<IActionResult> GetMyScore()
        {
            var result = await _scoreService.GetPlayerScoreAsync((string)HttpContext.Items["username"]);
            return Ok(result);
        }
    }
}
