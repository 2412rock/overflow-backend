using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services;

namespace OverflowBackend.Controllers
{
    [ApiController]
    public class HealthCheck : ControllerBase
    {
        private readonly OverflowDbContext _dbContext;
        public HealthCheck(OverflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpGet]
        [Route("api/health")]
        public async Task<IActionResult> Health()
        {
            var maybe = new Maybe<string>();
            try
            {
                // test data base connection is working
                var version = await _dbContext.Versions.FirstOrDefaultAsync(e => e.VersionID == 1);
                if(version != null)
                {
                    maybe.SetSuccess("Healthy");
                }
                else
                {
                    return StatusCode(500, "Version is null");
                }
            }
            catch(Exception e)
            {
                return StatusCode(500, "Failed to connect to databse");
            }
            
            return Ok(maybe);
        }
    }
}
