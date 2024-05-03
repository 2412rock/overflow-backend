using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace OverflowBackend.Services.Implementantion
{
    public class MatchMakingService : IMatchMakingService
    {

        List<Match> queue = new List<Match>();
        static readonly object locker = new object();
        public MatchMakingService()
        {
            Thread thread = new Thread(new ThreadStart(MatchCheck));
            thread.Start();
        }

        private void MatchCheck()
        {
            while (true)
            {
                lock (locker)
                {
                    var matchesToRemove = new List<Match>();
                    foreach (var match in queue)
                    {
                        if ((DateTime.Now - match.HearBeatPlayer1).Seconds > 60 || (DateTime.Now - match.HearBeatPlayer2).Seconds > 20)
                        {
                            matchesToRemove.Add(match);
                        }
                    }
                    foreach (var match in matchesToRemove)
                    {
                        Console.WriteLine($"Removed match with {match.Player1} and {match.Player2}");
                        queue.Remove(match);
                    }
                }
                Thread.Sleep(5000);
            }
            
        }
        private static int GetRandomNumber(int minValue, int maxValue)
        {
            Random random = new Random();

            // Generate a random number within the range
            int randomNumber = random.Next(minValue, maxValue + 1);
            return randomNumber;
        }

        static void Shuffle<T>(List<T> list)
        {
            Random random = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private void GenerateBoard(Match match)
        {
            var grid = new List<int>();
            var numberOf2s = GetRandomNumber(3, 7);

            for (var i = 0; i < numberOf2s; i++)
            {
                grid.Add(2);
            }
            for (var i = 0; i < 21 - numberOf2s; i++)
            {
                grid.Add(1);
            }
            grid.Add(3);
            grid.Add(4);
            Shuffle(grid);
            var gridWithPlayers = new List<int>();
            gridWithPlayers.Add(1);
            for (var gridIndex = 0; gridIndex < grid.Count; gridIndex++)
            {
                gridWithPlayers.Add(grid[gridIndex]);
            }
            gridWithPlayers.Add(1);
            match.Board = gridWithPlayers;
        }

        public Maybe<string> AddOrMatchPlayer(string username)
        {
            lock (locker)
            {
                var maybe = new Maybe<string>();
                if (queue.Count == 0)
                {
                    var match = new Match() { Player1 = username };
                    GenerateBoard(match);
                    queue.Add(match);
                    maybe.SetSuccess("Added to queue");
                    return maybe;
                }
                else
                {
                    foreach(var match in queue)
                    {
                        if(match.Player1 != null && match.Player2 == null /*&& !(match.SeenByPlayer1 || match.SeenByPlayer2)*/)
                        {
                            match.Player2 = username;
                            maybe.SetSuccess("Matched");
                            return maybe;
                        }
                    }
                }
                maybe.SetException("Something went wrong");
                return maybe;
            }
        }

        public Maybe<Match> FindMyMatch(string username)
        {
            var maybe = new Maybe<Match>();

            lock (locker)
            {
                Match matchToRemove = null;
                foreach(var match in queue)
                {
                    if(match.Player1 == username || match.Player2 == username)
                    {
                        if(match.Player1 == username)
                        {
                            match.HearBeatPlayer1 = DateTime.Now;
                        }
                        if (match.Player2 == username)
                        {
                            match.HearBeatPlayer2 = DateTime.Now;
                        }
                        
                        maybe.SetSuccess(match);
                    }
                }
                if(matchToRemove != null)
                {
                    queue.Remove(matchToRemove);
                }
            }
            
            if (!maybe.IsSuccess)
            {
                maybe.SetException("Could not find match");
            }

            return maybe;
        }

        public void RemoveMatch(string username)
        {
            lock (locker)
            {
                Match matchToRemove = null;
                foreach(var match in queue)
                {
                    if(match.Player1 == username || match.Player2 == username)
                    {
                        matchToRemove = match;
                    }
                }
                if(matchToRemove != null)
                {
                    queue.Remove(matchToRemove);
                }
            }
        }
    }

    public class Match
    {
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public List<int> Board { get; set; }

        public bool SeenByPlayer1 = false;
        public bool SeenByPlayer2 = false;

        public DateTime HearBeatPlayer1 = DateTime.Now;

        public DateTime HearBeatPlayer2 = DateTime.Now;
    }
}
