using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using OverflowBackend.Helpers;
using OverflowBackend.Services.Interface;
using OverflowBackend.Services.Implementantion;
using OverflowBackend.Services;

namespace OverflowBackend.Filters
{
    public class AuthorizationFilter : Attribute, IAuthorizationFilter
    {
        public string? Role { get; set; }

        private bool GameVersionValid(AuthorizationFilterContext context)
        {
            StringValues versionHeaderValues;
            if (context.HttpContext.Request.Headers.TryGetValue("GameVersion", out versionHeaderValues))
            {
                string version = versionHeaderValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(version)){
                    return VersionService.IsVersionValid(version, GameVersion.Value);
                }
            }
            return false;
        }
        public async void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!GameVersionValid(context))
            {
                context.Result = new StatusCodeResult(405);
                return;
            }

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

