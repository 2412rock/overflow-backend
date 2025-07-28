using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OverflowBackend.Services;
using System.Text;

namespace OverflowBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssignCoinsController: ControllerBase
    {
        private readonly string _customAuthToken;
        private readonly OverflowDbContext _dbContext;
        private readonly string _password;

        // In a real app, you'd inject your database context or service here
        // private readonly ApplicationDbContext _dbContext;
        // private readonly IUserService _userService;
        // private readonly ITransactionService _transactionService; // For deduplication

        public AssignCoinsController(IConfiguration configuration, OverflowDbContext dbContext)
        {
            _customAuthToken = configuration["RevenueCat:CustomAuthorizationToken"] ?? throw new ArgumentNullException("RevenueCat:CustomAuthorizationToken not configured.");
            _password = configuration["OverflowService:OverflowPassword"] ?? throw new ArgumentNullException("OverflowService:Password");
            Console.WriteLine($"DEBUG (Controller): Initialized Overflow Password: '{_password}'");
            _dbContext = dbContext;
            // _userService = userService;
            // _transactionService = transactionService;
        }
        [HttpPost]
        public async Task<IActionResult> BuyCoins([FromBody] AssignCoinsRequest request)
        {
            Console.WriteLine("Received RevenueCat webhook request.");


            // 1. Get raw request body (still useful for logging/debugging if needed, though not for signature verification now)
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            // 2. Verify Custom Authorization Header (Primary Security Check)
            if (request.Password != _password)
            {
                Console.WriteLine("Unauthorized: Missing or invalid custom Authorization header.");
                return Unauthorized("Unauthorized: Invalid or missing Authorization header.");
            }
            else
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == request.Username);
                if (user == null) 
                {
                    return BadRequest("User does not exist");
                }
                else
                {
                    var order = await _dbContext.Orders.FirstOrDefaultAsync(e => e.TransactionId == request.TransactionId);
                    if (order == null || !order.Finalized)
                    {
                        
                        if(order == null)
                        {
                            _dbContext.Orders.Add(new Models.DB.DBOrder()
                            {
                                TransactionId = request.TransactionId,
                                Finalized = true
                            });
                        }
                        else
                        {
                            order.Finalized = true;
                            _dbContext.Update(order);
                        }

                        user.ShopPoints += request.Amount;
                        _dbContext.Update(user);
                        
                        await _dbContext.SaveChangesAsync();
                        Console.WriteLine("Order finalized");
                        return Ok("Order finezlied");
                    }
                    else
                    {
                        Console.WriteLine("Order already finalized");
                        return Ok("Order already finalized");
                    }
                    
                }
            }
        }

    }

    public class AssignCoinsRequest()
    {
        public string Password { get; set; }
        public string Username { get; set; }
        public int Amount { get; set; }
        public string TransactionId { get; set; }
    }
}
