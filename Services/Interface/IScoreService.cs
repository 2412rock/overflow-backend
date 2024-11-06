using OverflowBackend.Models.DB;
using OverflowBackend.Models.Response;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;

namespace OverflowBackend.Services.Interface
{
    public interface IScoreService
    {
        public Task<Maybe<List<Score>>> GetHighScores(string myUsername);

        public Task<Maybe<Score>> GetPlayerScoreAsync(string username);

        public Task<Maybe<int>> GetPlayerRank(string username);

        public int? GetPlayerScore(string username);

        public Task UpdateScore(string username, string opponentUsername, bool win);
    }
}
