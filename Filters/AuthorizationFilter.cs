using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using OverflowBackend.Helpers;

namespace OverflowBackend.Filters
{
    public class AuthorizationFilter : Attribute, IAuthorizationFilter
    {
        public string? Role { get; set; }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            StringValues authorizationHeaderValues;
            if (context.HttpContext.Request.Headers.TryGetValue("Authorization", out authorizationHeaderValues))
            {
                string authorizationHeader = authorizationHeaderValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(authorizationHeader) && authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    string token = authorizationHeader.Substring("Bearer ".Length).Trim();
                    if (Role == "admin")
                    {
                        if (!TokenHelper.IsAdmin(token))
                        {
                            context.Result = new StatusCodeResult(401);
                            return;
                        }
                    }
                    var username = TokenHelper.GetUsernameFromToken(token);
                    if (TokenHelper.IsTokenExpired(token))
                    {
                        context.Result = new StatusCodeResult(403);
                        return;
                    }
                    if (!String.IsNullOrEmpty(username))
                    {
                        context.HttpContext.Items["username"] = username;
                        return;
                    }

                }
            }
            context.Result = new StatusCodeResult(401); ;
            return;
        }
    }
}

