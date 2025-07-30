using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OverflowBackend.Filters;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services;

namespace OverflowBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoinsController : ControllerBase
    {
        private readonly OverflowDbContext _dbContext;

        public CoinsController(OverflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Route("coins")]
        [AuthorizationFilter]
        [HttpGet]
        public async Task<IActionResult> GetUserShopPoints()
        {
            var maybe = new Maybe<int>();
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == (string)HttpContext.Items["username"]);
                maybe.SetSuccess(user.ShopPoints);
            }
            catch (Exception e)
            {
                maybe.SetException(e.Message);
            }
            return Ok(maybe);
        }

        [Route("userCoins")]
        [AuthorizationFilter]
        [HttpGet]
        public async Task<IActionResult> GetUserShopPoints([FromQuery] string username)
        {
            var maybe = new Maybe<int>();
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
                maybe.SetSuccess(user.ShopPoints);
            }
            catch (Exception e)
            {
                maybe.SetException(e.Message);
            }
            return Ok(maybe);
        }
    }
}
