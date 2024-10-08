﻿using Microsoft.EntityFrameworkCore;
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

        public bool IsInQueue(string username)
        {
            lock (locker)
            {
                return queue.Any(e => e.Player1 == username || e.Player2 == username);
            }
        }

        public Maybe<int> GetQueueSize()
        {
            var maybe = new Maybe<int>();
            
            maybe.SetSuccess(queue.Count);

            return maybe;
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
                        if ( ( match.HearBeatPlayer1.HasValue && (DateTime.Now - match.HearBeatPlayer1.Value).Seconds > 12) || (match.HearBeatPlayer2.HasValue && (DateTime.Now - match.HearBeatPlayer2.Value).Seconds > 12 ) )
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

        private bool BotMatchedWithBot(string player1, string player2)
        {
            var usernames = new string[] { "random_bot", "Pixel", "Voyager", "Nebula", "Explorer", "Scribe" };

            bool player1Matched = usernames.Contains(player1);
            bool player2Matched = usernames.Contains(player2);

            // Return true if both usernames are in the list of valid usernames
            return player1Matched && player2Matched;
        }

        private Maybe<string> RandomMatch(string username)
        {
            var maybe = new Maybe<string>();
            if (queue.Count == 0)
            {
                var match = new Match() { Player1 = username, GameId = Guid.NewGuid().ToString() };
                if (GameCollection.List == null)
                {
                    GameCollection.List = new ConcurrentList<string>();
                }
                GameCollection.List.Add(match.GameId);
                queue.Add(match);
                maybe.SetSuccess("Added to queue");
                return maybe;
            }
            else
            {
                foreach (var match in queue)
                {
                    if (match.Player1 != null && match.Player2 == null && !BotMatchedWithBot(match.Player1, username)/*&& !(match.SeenByPlayer1 || match.SeenByPlayer2)*/)
                    {
                        match.Player2 = username;
                        maybe.SetSuccess("Matched");
                        return maybe;
                    }
                }
            }
            var newMatch = new Match() { Player1 = username, GameId = Guid.NewGuid().ToString() };
            if (GameCollection.List == null)
            {
                GameCollection.List = new ConcurrentList<string>();
            }
            GameCollection.List.Add(newMatch.GameId);
            queue.Add(newMatch);
            maybe.SetSuccess("Added to queue");
            return maybe;
        }


        private Maybe<string> Prematch(string username, string withUsername)
        {
            var maybe = new Maybe<string>();

            var anyMatches = queue.Any(e => e.Player1 == username || e.Player2 == username);

            if (!anyMatches)
            {
                var match = new Match() { Player1 = username, GameId = Guid.NewGuid().ToString(), Player2 = withUsername };
                if (GameCollection.List == null)
                {
                    GameCollection.List = new ConcurrentList<string>();
                }
                GameCollection.List.Add(match.GameId);
                queue.Add(match);
            }

            maybe.SetSuccess("Added to queue");
            return maybe;
        }


        public Maybe<string> AddOrMatchPlayer(string username, bool? prematch, string? withUsername)
        {
            lock (locker)
            {
                if (prematch.HasValue && withUsername != null)
                {
                    return Prematch(username, withUsername);
                }
                else
                {
                    return RandomMatch(username);
                }
            }
        }

        public Maybe<Match> FindMyMatch(string username, OverflowDbContext dbContext)
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

                        if(!String.IsNullOrEmpty(match.Player1) && !String.IsNullOrEmpty(match.Player2))
                        {
                            var player1 = dbContext.Users.First(e => e.Username == match.Player1);
                            var player2 = dbContext.Users.First(e => e.Username == match.Player2);
                            match.Player1Rank = player1.Rank;
                            match.Player2Rank = player2.Rank;
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
        public int Player1Rank { get; set; }
        public int Player2Rank { get; set; }
        public List<double> Board { get; set; }

        public DateTime? HearBeatPlayer1 = null;

        public DateTime? HearBeatPlayer2 = null;

        public string GameId { get; set; }
    }
}
