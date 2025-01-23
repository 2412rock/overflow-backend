using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json.Linq;
using OverflowBackend.Helpers;
using OverflowBackend.Models.DB;
using OverflowBackend.Models.Response;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;
using System.Diagnostics.CodeAnalysis;

namespace OverflowBackend.Services.Implementantion
{
    public class AuthService
    {
        private readonly OverflowDbContext _dbContext;
        private readonly IPasswordHashService _passwordHashService;
        private readonly IMailService _mailService;

        public AuthService(OverflowDbContext dbContext, IPasswordHashService passwordHashService, IMailService mailService)
        {
            _dbContext = dbContext;
            _passwordHashService = passwordHashService;
            _mailService = mailService;
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

        private async Task<string> HandleSession(string username)
        {
            var existingSession = await _dbContext.UserSessions.FirstOrDefaultAsync(s => s.Username == username && s.IsActive);
            if (existingSession != null)
            {
                existingSession.IsActive = false;
                await _dbContext.SaveChangesAsync();
            }

            // Generate new session token
            var sessionToken = Guid.NewGuid().ToString();
            var userSession = new DBUserSession
            {
                Username = username,
                SessionToken = sessionToken,
                LastActiveTime = DateTime.UtcNow,
                IsActive = true
            };
            var existingSessions = await _dbContext.UserSessions.Where(e => e.Username == username).ToListAsync();
            for(int i = 0; i < existingSessions.Count; i++)
            {
                _dbContext.Remove(existingSessions[i]);
            }
            _dbContext.UserSessions.Add(userSession);
            await _dbContext.SaveChangesAsync();

            return sessionToken;
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
                    var session = await HandleSession(username);
                    AppStatsLogger.LogSignIn(username, null);
                    maybe.SetSuccess(new Tokens() 
                    {
                         BearerToken =  TokenHelper.GenerateJwtToken(username, session, false), 
                         RefreshToken = TokenHelper.GenerateJwtToken(username, session, isRefreshToken: true, isAdmin: false) ,
                         Session = session
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

        public async Task<Maybe<bool>> SignUp(string username, string password, string email) 
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
                    Rank = 1200,
                    NumberOfGames = 0
                };
                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();
                AppStatsLogger.LogSignUp(username, null);
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
                var guestUser = await _dbContext.GuestUsers.FirstOrDefaultAsync(e => e.Username == username);
                if (user != null)
                {
                    var session = await HandleSession(username);
                    var refreshedToken = TokenHelper.GenerateJwtToken(username,session, isAdmin: false);
                    result.SetSuccess(refreshedToken);
                    return result;
                }
                else if(guestUser != null)
                {
                    var session = await HandleSession(username);
                    var refreshedToken = TokenHelper.GenerateJwtToken(username, session, isAdmin: false);
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

        private async Task<Tuple<bool, DBUser>> CanResetPassword(string username)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
            if(user != null)
            {
                if (String.IsNullOrEmpty(user.Password))
                {
                    // user is registered with google
                    return new Tuple<bool, DBUser>(false, user);
                }
                return new Tuple<bool, DBUser>(true, user);
            }
            throw new Exception("User does not exist");
        }

        public async Task<Maybe<bool>> CanResetPasswordResult(string username)
        {
            var maybe = new Maybe<bool>();
            try
            {
                var result = await CanResetPassword(username);
                maybe.SetSuccess(result.Item1);
            }
            catch(Exception e)
            {
                maybe.SetException(e.Message);
            }
            return maybe;
        }

        public async Task<Maybe<bool>> DeleteAccount(string username)
        {
            var maybe = new Maybe<bool>();
            var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
            if(user != null)
            {
                _dbContext.Users.Remove(user);
                var friends = await _dbContext.Friends.Where(e => e.UserId == user.UserId || e.FriendUserId == user.UserId).ToListAsync();
                foreach(var friend in friends)
                {
                    _dbContext.Friends.Remove(friend);
                }
                await _dbContext.SaveChangesAsync();
                maybe.SetSuccess(true);
            }
            else
            {
                maybe.SetException("User not found");
            }
            return maybe;
        }

        public async Task<Maybe<bool>> ResetPassword(string username, string oldPassword, string newPassword)
        {
            var maybe = new Maybe<bool>();

            try
            {
                var canReset = await CanResetPassword(username);
                if (canReset.Item1)
                {
                    var hashedPassword = canReset.Item2.Password;
                    if (!_passwordHashService.VerifyPassword(oldPassword, hashedPassword))
                    {
                        maybe.SetException("Invalid old password");
                    }
                    else if (oldPassword != newPassword)
                    {
                        canReset.Item2.Password = _passwordHashService.HashPassword(newPassword);
                        _dbContext.Update(canReset.Item2);
                        await _dbContext.SaveChangesAsync();
                        maybe.SetSuccess(true);
                    }
                    else
                    {
                        maybe.SetException("New password cant be the same as old one");
                    }
                }
            }
            catch(Exception e)
            {
                maybe.SetException(e.Message);
            }
            return maybe;
        }

        public async Task<Maybe<string>> SendVerificationCode(string username)
        {
            var maybe = new Maybe<string>();
            var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
            if(user != null)
            {
                if (String.IsNullOrEmpty(user.Password))
                {
                    maybe.SetException("User is google user");
                }
                else
                {
                    var verificationCode = new Random().Next(1000, 9999).ToString();
                    _mailService.SendMailToUser(verificationCode, user.Email);
                    if (VerificationCodeCollection.Values.ContainsKey(username))
                    {
                        string value;
                        VerificationCodeCollection.Values.Remove(username, out value);
                    }
                    if(VerificationCodeCollection.Values.TryAdd(username, verificationCode))
                    {
                        maybe.SetSuccess("Verification code sent");
                    }
                    else
                    {
                        maybe.SetException("Failed to generate code. Try again");
                    }
                }
            }
            else
            {
                maybe.SetException("No user found with that username");
            }
            return maybe;
        }

        public async Task<Maybe<string>> VerifyCodeAndChangePassword(string verificationCode, string username, string newPassword)
        {
            var maybe = new Maybe<string>();
            var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
            if (user != null)
            {
                if (String.IsNullOrEmpty(user.Password))
                {
                    maybe.SetException("User is google user");
                }
                else
                {
                    string code;
                    if(VerificationCodeCollection.Values.TryGetValue(username, out code))
                    {
                        if(code == verificationCode)
                        {
                            user.Password = _passwordHashService.HashPassword(newPassword);
                            _dbContext.Users.Update(user);
                            await _dbContext.SaveChangesAsync();
                            maybe.SetSuccess("Password changed");
                        }
                        else
                        {
                            maybe.SetException("Verification code invalid");
                        }
                    }
                    else
                    {
                        maybe.SetException("No verification code sent for this username");
                    }
                }
            }
            else
            {
                maybe.SetException("No user found with that email");
            }
            return maybe;
        }
        public async Task<Maybe<Tokens>> LoginApple(string email, string? username, string idToken) 
        {
            var response = new Maybe<Tokens>();
            if (username != null && username.Length > 14)
            {
                response.SetException("Username too long");
                return response;
            }
            var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Email == email);
            if (await AppleVerficationHelper.ValidateAppleIdToken(idToken))
            {
                if (user == null)
                {
                    if (username != null)
                    {
                        AppStatsLogger.LogSignUp(username, null);
                        await _dbContext.Users.AddAsync(new DBUser() { Email = email, Password = "", Username = username, Rank = 1200 });
                        _dbContext.SaveChanges();
                    }
                    else
                    {
                        response.SetException("usernotexists");
                        return response;
                    }
                }
                user = user == null ? await _dbContext.Users.FirstOrDefaultAsync(e => e.Email == email) : user;
                if (user == null)
                {
                    response.SetException("could not find user");
                    return response;
                }
                var session = await HandleSession(user.Username);
                AppStatsLogger.LogSignIn(user.Username, null);
                response.SetSuccess(new Tokens()
                {
                    BearerToken = TokenHelper.GenerateJwtToken(user.Username, session, true),
                    RefreshToken = TokenHelper.GenerateJwtToken(user.Username, session, isRefreshToken: true, isAdmin: false),
                    Username = user.Username
                });
            }
            else
            {
                response.SetException("Invalid google token");
            }
            return response;
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
                        AppStatsLogger.LogSignUp(username, null);
                        await _dbContext.Users.AddAsync(new DBUser() { Email = email, Password = "", Username = username, Rank = 1200 });
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
                var session = await HandleSession(user.Username);
                AppStatsLogger.LogSignIn(user.Username, null);
                response.SetSuccess(new Tokens() 
                {
                    BearerToken =  TokenHelper.GenerateJwtToken(user.Username, session, true), 
                    RefreshToken =  TokenHelper.GenerateJwtToken(user.Username,session,isRefreshToken: true, isAdmin: false),
                    Username = user.Username
                });
            }
            else
            {
                response.SetException("Invalid google token");
            }
            return response;
        }

        private async Task RemoveUnusedGuestUser()
        {
            DateTime currentDate = DateTime.Now;

            var cutoffDate = currentDate.AddDays(-30); // Calculate the date 30 days ago
            var expiredUsers = await _dbContext.UserSessions
                .Where(e => e.LastActiveTime <= cutoffDate)
                .Select(e => e.Username)
                .ToListAsync();
            for (int i=0; i < expiredUsers.Count; i++)
            {
                var user = await _dbContext.GuestUsers.FirstOrDefaultAsync(e => e.Username == expiredUsers[i]);
                if(user != null)
                {
                    _dbContext.GuestUsers.Remove(user);
                }
            }
            await _dbContext.SaveChangesAsync();
        }
        public async Task<Maybe<Tokens>> ContinueAsGuest()
        {
            var maybe = new Maybe<Tokens>();

            try
            {
                await RemoveUnusedGuestUser();
                string username;
                int tries = 0;
                while (true)
                {
                    var generatedUsername = GenerateRandomGuestUsername();
                    var any = await _dbContext.GuestUsers.AnyAsync(e => e.Username == generatedUsername);
                    tries++;
                    if(tries > 100)
                    {
                        maybe.SetException("Could not generate guest username");
                        return maybe;
                    }
                    if (!any)
                    {
                        username = generatedUsername;
                        await _dbContext.GuestUsers.AddAsync(new DBGuestUser()
                        {
                            Username = username,
                            NumberOfGames = 0
                        });
                        await _dbContext.SaveChangesAsync();
                        break;
                    }
                }
                
                var session = await HandleSession(username);
                var tokens = new Tokens()
                {
                    BearerToken = TokenHelper.GenerateJwtToken(username, session, true),
                    RefreshToken = TokenHelper.GenerateJwtToken(username, session, isRefreshToken: true, isAdmin: false),
                    Username = username
                };
                maybe.SetSuccess(tokens);
            }
            catch(Exception e)
            {
                maybe.SetException(e.Message);
            }

            return maybe;
        }

        private static string GenerateRandomGuestUsername()
        {
            Random random = new Random();
            int randomNumber = random.Next(100000, 999999); // Generates a random number between 100000 and 999999
            return "guest" + randomNumber.ToString();
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
