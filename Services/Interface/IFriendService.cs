using OverflowBackend.Models.DB;
using OverflowBackend.Models.Response;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;

namespace OverflowBackend.Services.Interface
{
    public interface IFriendService
    {
        public Task<Maybe<string>> SendFriendRequest(string username, string friendUsername);
        public Task<Maybe<string>> AcceptFriendRequest(string myUsername, string friendUsername);
        public Task<Maybe<string>> DeclineFriendRequest(string myUsername, string friendUsername);
        public Task<Maybe<List<Friend>>> GetFriends(string username);
        public Task<Maybe<List<Friend>>> GetFriendRequests(string username);
        public Task<Maybe<List<UserSearchResult>>> GetUsernames(string myUsername, string startsWith);
        public Task<Maybe<string>> Unfriend(string myUsername, string friendUsername);
    }
}
