using Microsoft.EntityFrameworkCore;
using OverflowBackend.Models.DB;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;
using System.Diagnostics.CodeAnalysis;

namespace OverflowBackend.Services.Implementantion
{
    public class AuthService: IAuthService
    {
        private readonly OverflowDbContext _dbContext;

        public AuthService(OverflowDbContext dbContext)
        {
            _dbContext = dbContext;
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
                    Password = password,
                    Email = email,
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
    }
}
