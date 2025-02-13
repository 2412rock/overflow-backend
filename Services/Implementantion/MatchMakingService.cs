﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace OverflowBackend.Services.Implementantion
{
    public class MatchMakingService
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

        private bool BotMatchedWithBot(Match match, bool player2IsBot)
        {
            /*var usernames = new string[] { "random_bot", "Pixel", "Voyager", "Nebula", "Explorer", "Scribe" };

            bool player1Matched = usernames.Contains(player1);
            bool player2Matched = usernames.Contains(player2);*/

            // Return true if both usernames are in the list of valid usernames
            return match.Player1IsBot && player2IsBot;
        }

        private async Task<Maybe<string>> RandomMatch(string username, OverflowDbContext dbContext)
        {
            var maybe = new Maybe<string>();
            try
            {
                var isGuest = false;
                var isBot = false;

                var userGuest = await dbContext.GuestUsers.FirstOrDefaultAsync(e => e.Username == username);
                var user = await dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);

                if (userGuest != null)
                {
                    isGuest = true;
                    if (userGuest.IsBot)
                    {
                        isBot = true;
                    }
                }
                else if (user != null)
                {
                    if (user.IsBot)
                    {
                        isBot = true;
                    }
                }

                lock (locker)
                {
                    foreach (var match in queue)
                    {
                        if (match.Player1 != null && match.Player2 == null && !BotMatchedWithBot(match, isBot) && match.IsGuest == isGuest/*&& !(match.SeenByPlayer1 || match.SeenByPlayer2)*/)
                        {
                            match.Player2 = username;
                            maybe.SetSuccess("Matched");
                            return maybe;
                        }
                    }

                    // prevent player from entering the queue twice
                    var any = queue.Any(e => e.Player1 == username || e.Player2 == username);
                    if (any)
                    {
                        maybe.SetSuccess("Added to queue");
                        return maybe;
                    }
                    var newMatch = new Match() { Player1 = username, GameId = Guid.NewGuid().ToString(), IsGuest = isGuest, Player1IsBot = isBot };
                    if (GameCollection.List == null)
                    {
                        GameCollection.List = new ConcurrentList<string>();
                    }
                    GameCollection.List.Add(newMatch.GameId);
                    queue.Add(newMatch);
                    maybe.SetSuccess("Added to queue");
                }
            }
            catch(Exception e)
            {
                maybe.SetException("Exception occured gettin random match " + e.Message);
            }
            return maybe;
        }


        private Maybe<string> Prematch(string username, string withUsername)
        {
            var maybe = new Maybe<string>();

            lock (locker)
            {
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
            }
            return maybe;
        }


        public async Task<Maybe<string>> AddToQueue(string username, bool? prematch, string? withUsername, OverflowDbContext dbContext)
        {
            
                if (prematch.HasValue && withUsername != null)
                {
                    return Prematch(username, withUsername);
                }
                else
                {
                    return await RandomMatch(username, dbContext);
                }

        }

        public Maybe<Match> FindMyMatch(string username, OverflowDbContext dbContext)
        {
            var maybe = new Maybe<Match>();

            try
            {
                lock (locker)
                {
                    Match matchToRemove = null;
                    foreach (var match in queue)
                    {
                        if (match.Player1 == username || match.Player2 == username)
                        {
                            if (match.Player1 == username)
                            {
                                match.HearBeatPlayer1 = DateTime.Now;
                            }
                            if (match.Player2 == username)
                            {
                                match.HearBeatPlayer2 = DateTime.Now;
                            }

                            if (!String.IsNullOrEmpty(match.Player1) && !String.IsNullOrEmpty(match.Player2))
                            {
                                var player1User = dbContext.Users.FirstOrDefault(e => e.Username == match.Player1);
                                var player2User = dbContext.Users.FirstOrDefault(e => e.Username == match.Player2);

                                var player1Guest = dbContext.GuestUsers.FirstOrDefault(e => e.Username == match.Player1);
                                var player2Guest = dbContext.GuestUsers.FirstOrDefault(e => e.Username == match.Player2);

                                if (player1User != null && player2User != null)
                                {
                                    match.Player1Rank = player1User.Rank;
                                    match.Player2Rank = player2User.Rank;
                                }
                                else if (player1Guest == null || player1Guest == null) 
                                {
                                    matchToRemove = match;
                                    queue.Remove(matchToRemove);
                                    maybe.SetException("Invalid player");
                                    return maybe;
                                }                              
                            }

                            maybe.SetSuccess(match);
                        }
                    }
                }


            }
            catch(Exception e)
            {
                maybe.SetException(e.Message);
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
        public bool IsGuest { get; set; }
        public bool Player1IsBot { get; set; }
        public bool Player2IsBot { get; set; }
    }
}
