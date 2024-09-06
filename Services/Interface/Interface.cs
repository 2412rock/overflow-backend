using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;

namespace OverflowBackend.Services.Interface
{
    public interface IVersionService
    {
        public Task<Maybe<bool>> IsGameVersionValid(string currentVersion);
        public Task<Maybe<bool>> UpdateGameVersion(string currentVersion);
    }
}
