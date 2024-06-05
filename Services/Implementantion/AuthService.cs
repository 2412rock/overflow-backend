using Microsoft.EntityFrameworkCore;
using OverflowBackend.Helpers;
using OverflowBackend.Models.DB;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;
using System.Diagnostics.CodeAnalysis;

namespace OverflowBackend.Services.Implementantion
{
    public class AuthService: IAuthService
    {
        private readonly OverflowDbContext _dbContext;
        private readonly IPasswordHashService _passwordHashService;

        public AuthService(OverflowDbContext dbContext, IPasswordHashService passwordHashService)
        {
            _dbContext = dbContext;
            _passwordHashService = passwordHashService;
        }

        public async Task<Maybe<string[]>> SignIn(string username, string password)
        {
            Console.WriteLine($"Loggin in with username {username} {password}");
            var maybe = new Maybe<string[]>();
            var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
            Console.WriteLine($"Got user {user}");
            if (user != null)
            {
                var hashedPassword = user.Password;
                if(_passwordHashService.VerifyPassword(password, hashedPassword))
                {
                    maybe.SetSuccess(new string[] { TokenHelper.GenerateJwtToken(username, false), TokenHelper.GenerateJwtToken(username, isAdmin: false), user.UserId.ToString() });
                }
                else
                {
                    maybe.SetException("Invalid username or password");
                }
                
            }
            else
            {
                maybe.SetException("Invalid username or password");
            }
            return maybe;
        }

        public async Task<Maybe<bool>> SignUp(string username, string password, string? email) 
        {
            var maybe = new Maybe<bool>();
            var any = await _dbContext.Users.AnyAsync(element => element.Username == username);
            if (!any)
            {
                var user = new DBUser()
                {
                    Username = username,
                    Password = _passwordHashService.HashPassword(password),
                    Email = "",
                    Rank = 1,
                    NumberOfGames = 0
                };
                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();
                maybe.SetSuccess(true);
            }
            else
            {
                maybe.SetException("User already exists");
            }
            return maybe;
        }

        public async Task<Maybe<bool>> UserNameExists(string username)
        {
            var maybe = new Maybe<bool>();
            var value =  await _dbContext.Users.AnyAsync(element => element.Username == username);
            maybe.SetSuccess(value);
            return maybe;
        }

        public async Task<Maybe<string>> RefreshToken(string token)
        {
            var username = TokenHelper.GetUsernameFromToken(token);
            var result = new Maybe<string>();
            if (!TokenHelper.IsTokenExpired(token) && username != null)
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
                if (user != null)
                {
                    var refreshedToken = TokenHelper.GenerateJwtToken(username, isAdmin: false);
                    result.SetSuccess(refreshedToken);
                    return result;
                }
                else
                {
                    result.SetException("User does not exist");
                    return result;
                }

            }
            result.SetException("Token expired");
            return result;
        }
    }
}
