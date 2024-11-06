using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OverflowBackend.Models.DB;
using OverflowBackend.Models.Response;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Services.Implementantion
{
    public class ScoreService : IScoreService
    {
        private readonly OverflowDbContext _dbContext;
        private readonly int ScoreConstant = 25;

        public ScoreService(OverflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Maybe<List<Score>>> GetHighScores(string myUsername)
        {
            var maybe = new Maybe<List<Score>>();
            try
            {
                var top10Scores = await _dbContext.Users
                .FromSqlRaw(
                  @"SELECT TOP 10 Username, Rank
                  FROM Users
                  WHERE Rank IS NOT NULL
                  ORDER BY Rank DESC, UserId ASC")
                .Select(e => new Score { Username = e.Username, Rank = e.Rank })
                .ToListAsync();
                var processedScores = new List<Score>();
                top10Scores.ForEach(score => {
                    var any = _dbContext.BlockedUsers.Any(e => e.Username == myUsername && e.BlockedUsername == score.Username);
                    if (any)
                    {
                        score.Username = "Hidden";
                    }
                });
                maybe.SetSuccess(top10Scores);
            }
            catch(Exception e)
            {
                maybe.SetException("Something went wrong");
            }
            
            return maybe;
        }


        public async Task<Maybe<Score>> GetPlayerScoreAsync(string username)
        {
            var maybe = new Maybe<Score>();
            var score = await _dbContext.Users.Select(e => new Score() { Username = e.Username, Rank = e.Rank }).FirstOrDefaultAsync(e => e.Username == username);
            if(score != null)
            {
                maybe.SetSuccess(score);
            }
            else
            {
                maybe.SetException("Cannot find user score");
            }
            return maybe;
        }

        public async Task<Maybe<int>> GetPlayerRank(string username)
        {
            var maybe = new Maybe<int>();

            // Fetch the user with the provided username
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user != null)
            {
                // Calculate the user's rank
                var rankQuery = await _dbContext.Users
                    .Where(u => u.Rank > user.Rank || (u.Rank == user.Rank && u.UserId < user.UserId))
                    .CountAsync();

                // The rank is the count + 1
                int userRank = rankQuery + 1;
                maybe.SetSuccess(userRank);
            }
            else
            {
                maybe.SetException("Cannot find user score.");
            }

            return maybe;
        }



        public int? GetPlayerScore(string username)
        {
            var score = _dbContext.Users.FirstOrDefault(e => e.Username == username);
            if (score != null)
            {
                return score.Rank;
            }

            return null;
        }

        private double GetExpectedScore(int rankA, int rankB)
        {
            double eA = 1 / (1 + Math.Pow(10, (rankB - rankA) / 400.0) );
            return eA;
        }

        private int GetUpdatedScore(int rank, int s, double expectedScore)
        {
            double newRank = rank + ScoreConstant * (s - expectedScore);
            int result = (int)Math.Round(newRank);
            return result;
        }
        public async Task UpdateScore(string username, string opponentUsername, bool win)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
            var opponent = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == opponentUsername);
            if(user != null && opponent != null)
            {
                var expectedScore = GetExpectedScore(user.Rank, opponent.Rank);
                int newRank = GetUpdatedScore(user.Rank, win ? 1 : 0, expectedScore);
                user.Rank = newRank;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
