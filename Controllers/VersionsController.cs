using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services;
using OverflowBackend.Helpers;
using OverflowBackend.Services.Interface;
using OverflowBackend.Models.Requests;

namespace OverflowBackend.Controllers
{
    [ApiController]
    public class VersionsController : ControllerBase
    {
        private readonly IVersionService _versionService;
        public VersionsController(IVersionService versionService)
        {
            _versionService = versionService;
        }

        [HttpGet]
        [Route("api/isAllowedVersion")]
        public async Task<IActionResult> IsAllowedVersion([FromQuery] string version)
        {
            var result = await _versionService.IsGameVersionValid(version);
            return Ok(result);
        }

        [HttpPut]
        [Route("api/updateAllowedVersion")]
        public async Task<IActionResult> UpdateAllowedVersion([FromBody] UpdateGameVersionRequest request)
        {
            var saPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");
            if(request.Password == saPassword)
            {
                var result = await _versionService.UpdateGameVersion(request.Version);
                
                return Ok(result);
            }
            return BadRequest("Invalid pass");
        }
    }
}
