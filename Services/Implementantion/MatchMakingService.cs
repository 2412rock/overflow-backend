using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;
using System.Collections.Concurrent;

namespace OverflowBackend.Services.Implementantion
{
    public class MatchMakingService : IMatchMakingService
    {

        ConcurrentQueue<Match> queue = new ConcurrentQueue<Match>();
        public MatchMakingService()
        {

        }

        public Maybe<string> AddOrMatchPlayer(string username)
        {
            var maybe = new Maybe<string>();
            if (queue.Count == 0)
            {
                var match = new Match() { Player1 = username };
                queue.Enqueue(match);
                maybe.SetSuccess("Added to queue");
                return maybe;
            }
            else
            {
                queue.TryDequeue(out Match result);
                if (result != null)
                {
                    result.Player2 = username;
                    queue.Enqueue(result);
                    maybe.SetSuccess("Matched");
                    return maybe;
                }
            }
            maybe.SetException("Something went wrong");
            return maybe;
        }

        public Maybe<Match> FindMyMatch(string username)
        {
            var maybe = new Maybe<Match>();
            var dequedElements = new List<Match>();
            while (queue.TryDequeue(out Match result))
            {
                if (result.Player1 == username || result.Player2 == username)
                {
                    maybe.SetSuccess(result);

                }
                dequedElements.Add(result);
            }
            foreach (var element in dequedElements)
            {
                queue.Enqueue(element);
            }
            if (!maybe.IsSuccess)
            {
                maybe.SetException("Could not find match");
            }

            return maybe;
        }

        public void RemoveMatch(string username)
        {
            var dequedElements = new List<Match>();
            while (queue.TryDequeue(out Match result))
            {
                if (!(result.Player1 == username || result.Player2 == username))
                {
                    dequedElements.Add(result);
                }
            }

            foreach (var element in dequedElements)
            {
                queue.Enqueue(element);
            }
        }
    }

    public class Match
    {
        public string Player1 { get; set; }
        public string Player2 { get; set; }
    }
}
