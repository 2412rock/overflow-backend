using OverflowBackend.Models.DB;
using OverflowBackend.Models.Response;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;

namespace OverflowBackend.Services.Interface
{
    public interface IScoreService
    {
        public Task<Maybe<List<Score>>> GetHighScores();

        public Task<Maybe<Score>> GetPlayerScoreAsync(string username);

        public int? GetPlayerScore(string username);

        public Task UpdateScore(string username, string opponentUsername, bool win);
    }
}
