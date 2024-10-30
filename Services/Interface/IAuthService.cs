using OverflowBackend.Models.Response;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;

namespace OverflowBackend.Services.Interface
{
    public interface IAuthService
    {
        public Task<Maybe<bool>> UserNameExists(string username);
        public Task<Maybe<bool>> SignUp(string username, string password, string email);
        public Task<Maybe<Tokens>> SignIn(string username, string password);
        public Task<Maybe<string>> RefreshToken(string token);
        public Task<Maybe<Tokens>> LoginGoogle(string email, string? username, string idToken);
        public Task<Maybe<Tokens>> LoginApple(string email, string? username, string idToken);
        public Task<Maybe<bool>> ResetPassword(string username, string oldPassword, string newPassword);
        public Task<Maybe<bool>> CanResetPasswordResult(string username);
        public Task<Maybe<bool>> DeleteAccount(string username);
        public Task<Maybe<string>> SendVerificationCode(string username);
        public Task<Maybe<string>> VerifyCodeAndChangePassword(string verificationCode, string username, string newPassword);
    }
}
