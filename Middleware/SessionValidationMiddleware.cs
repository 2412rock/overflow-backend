using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using OverflowBackend.Helpers;
using OverflowBackend.Services;
using StackExchange.Redis;
using System.Net;

namespace OverflowBackend.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, OverflowDbContext dbContext)
        {
            StringValues authorizationHeaderValues;
            if (context.Request.Headers.TryGetValue("Authorization", out authorizationHeaderValues))
            {
                string authorizationHeader = authorizationHeaderValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(authorizationHeader) && authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    string token = authorizationHeader.Substring("Bearer ".Length).Trim();
                    if(token != "null")
                    {
                        var sessionId = TokenHelper.GetSessionIdFromToken(token);
                        var username = TokenHelper.GetUsernameFromToken(token);
                        var user = await dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
                        var guest = await dbContext.GuestUsers.FirstOrDefaultAsync(e => e.Username == username);
                        if ( (sessionId == null) || (user == null && guest == null))
                        {
                            context.Response.StatusCode = 406;
                            await context.Response.WriteAsync("Session is invalid.");
                            return;
                        }
                        var session = await dbContext.UserSessions.FirstOrDefaultAsync(s => s.SessionToken == sessionId && s.IsActive);
                        
                        if (session == null)
                        {
                            context.Response.StatusCode = 406;
                            await context.Response.WriteAsync("Session is invalid.");
                            return;
                        }
                        // Update last active time
                        session.LastActiveTime = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                    }
                    
                }
            }
            await _next(context);
        }
    }

}
