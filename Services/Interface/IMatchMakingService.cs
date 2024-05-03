using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Implementantion;
using static OverflowBackend.Services.Implementantion.MatchMakingService;

namespace OverflowBackend.Services.Interface
{
    public interface IMatchMakingService
    {
        public Maybe<string> AddOrMatchPlayer(string username);

        public Maybe<Match> FindMyMatch(string username);

        public void RemoveMatch(string username);

    }
}
