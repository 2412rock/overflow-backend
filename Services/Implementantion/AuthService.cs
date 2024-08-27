﻿using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Server;
using OverflowBackend.Helpers;
using OverflowBackend.Models.DB;
using OverflowBackend.Models.Response;
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

        private bool IsValidEmail(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }

            // Check if all characters are either alphanumeric or dots and the length is within 14 characters
            bool isValid = input.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '@') && input.Length <= 30;
            var atChars = input.Count(c => c == '@');
            if (atChars > 1)
            {
                return false;
            }

            return isValid;
        }

        private bool IsValidUsername(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }

            // Check if all characters are either alphanumeric or dots and the length is within 14 characters
            bool isValid = input.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-') && input.Length <= 14;

            return isValid;
        }

        private bool IsValidPassword(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            // Define allowed characters (A-Z, a-z, 0-9, and specified special characters)
            const string allowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+[]{};:'\",<.>/?|\\~`+";

            // Check if all characters in the input are part of the allowed characters
            bool isValid = input.All(c => allowedCharacters.Contains(c)) && input.Length <= 20;

            return isValid;
        }

        public async Task<Maybe<Tokens>> SignIn(string username, string password)
        {
            var maybe = new Maybe<Tokens>();
            if (!IsValidUsername(username) || !IsValidPassword(password))
            {
                maybe.SetException("Username or password too long");
                return maybe;
            }
            var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);

            if (user != null)
            {
                var hashedPassword = user.Password;
                if(_passwordHashService.VerifyPassword(password, hashedPassword))
                {
                    maybe.SetSuccess(new Tokens() 
                    {
                         BearerToken =  TokenHelper.GenerateJwtToken(username, false), 
                         RefreshToken = TokenHelper.GenerateJwtToken(username, isAdmin: false) 
                    });
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
            if(!IsValidUsername(username) || !IsValidPassword(password))
            {
                maybe.SetException("Username or password too long");
                return maybe;
            }
            if(!email.IsNullOrEmpty() && !IsValidEmail(email))
            {
                maybe.SetException("Email invalid");
                return maybe;
            }
            var any = await _dbContext.Users.AnyAsync(element => element.Username == username);
            if (!any)
            {
                var user = new DBUser()
                {
                    Username = username,
                    Password = _passwordHashService.HashPassword(password),
                    Email = email,
                    Rank = 1000,
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
            if (username.Length > 14)
            {
                maybe.SetException("Username too long");
                return maybe;
            }
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

        public async Task<Maybe<Tokens>> LoginGoogle(string email, string? username, string idToken)
        {
            var response = new Maybe<Tokens>();
            if (username != null && username.Length > 14)
            {
                response.SetException("Username too long");
                return response;
            }
            var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Email == email);
            if (VerifyGoogleToken(idToken))
            {
                if (user == null)
                {
                    if(username != null)
                    {
                        await _dbContext.Users.AddAsync(new DBUser() { Email = email, Password = "", Username = username, Rank = 1000 });
                        _dbContext.SaveChanges();
                    }
                    else
                    {
                        response.SetException("usernotexists");
                        return response;
                    }
                }
                user = user == null ?  await _dbContext.Users.FirstOrDefaultAsync(e => e.Email == email): user;
                if(user == null)
                {
                    response.SetException("could not find user");
                    return response;
                }
                response.SetSuccess(new Tokens() 
                {
                    BearerToken =  TokenHelper.GenerateJwtToken(user.Username, true), 
                    RefreshToken =  TokenHelper.GenerateJwtToken(user.Username, isAdmin: false),
                    Username = user.Username
                });
            }
            else
            {
                response.SetException("Invalid google token");
            }
            return response;
        }

        private bool VerifyGoogleToken(string idToken)
        {
            try
            {
                var payload = GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings()).Result;
                if (payload != null)
                {
                    return true;
                }
            }
            catch (InvalidJwtException ex)
            {
                Console.WriteLine("Invalid google token");
            }
            return false;
        }
    }
}
