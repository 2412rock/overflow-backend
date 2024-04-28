using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;

namespace OverflowBackend.Services.Interface
{
    public interface IAuthService
    {
        public Task<Maybe<bool>> UserNameExists(string username);

        public Task<Maybe<bool>> SignUp(string username, string password, string? email);
    }
}
