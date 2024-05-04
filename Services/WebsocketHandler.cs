using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using static Azure.Core.HttpHeader;

namespace OverflowBackend.Services
{
    public class Game
    {
        public string GameId { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }

        public WebSocket Player1Socket { get; set; }

        public WebSocket Player2Socket { get; set; }
        // Add additional game state properties as needed
    }
    public static class WebSocketHandler
    {
        private static List<Game> games = new List<Game>();
        static readonly object locker = new object();

        public static async Task HandleWebSocketRequest(WebSocket webSocket, HttpContext httpConext)
        {
            /* */
            
            string gameId = httpConext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries)[1];
            string playerName = httpConext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries)[2];

     
            await HandleWebSocketRequest(webSocket, gameId, playerName);
        
        }

        public static async Task HandleWebSocketRequest(WebSocket webSocket, string gameId, string playerName)
        {
            var gameFound = false;
            Game game;
            lock (locker)
            {
                game = games.Where(game => game.GameId == gameId && game.Player1 != null && game.Player2 == null).FirstOrDefault();
                if (game != null)
                {
                    gameFound = true;
                    game.Player2 = playerName;
                    game.Player2Socket = webSocket;
                }
                else
                {
                    var newGame = new Game();
                    newGame.GameId = gameId;
                    newGame.Player1 = playerName;
                    newGame.Player1Socket = webSocket;
                    newGame.Player2Socket = null;
                    games.Add(newGame);
                    game = newGame;
                }
            }

            if (gameFound)
            {
                await ListenOnSocketPlayer2(game);
            }
            else
            {
                await ListenOnSocketPlayer1(game);
            }
            
            /*foreach(var game in games)
            {
                if(game.GameId == gameId)
                {
                    gameFound = true;
                    if(game.Player1 != null && game.Player2 == null)
                    {
                        game.Player2 = playerName;
                        game.Player2Socket = webSocket;
                        await ListenOnSocketPlayer2(game);
                    }

                }
            }
            if (!gameFound)
            {
                var game = new Game();
                game.GameId = gameId;
                game.Player1 = playerName;
                game.Player1Socket = webSocket;
                game.Player2Socket = null;
                games.Add(game);
                await ListenOnSocketPlayer1(game);
            }*/
            
        }


        private static async Task GameLogic(string gameId, Game game)
        {
            // Implement game logic here
            // You can send messages to players using the WebSocket connections stored in _gameConnections
            // For example:
            // await _gameConnections[gameId].Item1.SendAsync(messageData, WebSocketMessageType.Text, true, CancellationToken.None);
            // await _gameConnections[gameId].Item2.SendAsync(messageData, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task ListenOnSocketPlayer1(Game game)
        {
            byte[] msg = Encoding.UTF8.GetBytes("Welcome player 1");
            await game.Player1Socket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);

            byte[] buffer = new byte[1024];
            //receive move from player 1
            WebSocketReceiveResult result = await game.Player1Socket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                //byte[] msgBuffer = Encoding.UTF8.GetBytes("Some message from player 1");
                //send move to player 2
                while (true)
                {
                    if(game.Player2Socket != null)
                    {
                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                        break;
                    }
                    else
                    {
                        await Task.Delay(2000);
                        //player not yet connected
                    }
                }
                // receive move from player 1
                result = await game.Player1Socket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
            }
            await game.Player1Socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, System.Threading.CancellationToken.None);
            try
            {
                games.Remove(game);
            }
            catch 
            {

            }
            
        }

        private static async Task ListenOnSocketPlayer2(Game game)
        {
            byte[] msg = Encoding.UTF8.GetBytes("Welcome player 2");
            await game.Player2Socket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);


            byte[] buffer = new byte[1024];
            //receive move from player 1
            WebSocketReceiveResult result = await game.Player2Socket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                //byte[] msgBuffer = Encoding.UTF8.GetBytes("Some message from player 2");
                //send move to player 2
                while (true)
                {
                    if(game.Player1Socket != null)
                    {
                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                        break;
                    }
                    else
                    {
                        //Player not yer connected
                        await Task.Delay(2000);

                    }
                }
                // receive move from player 1
                result = await game.Player2Socket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
            }
            await game.Player2Socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, System.Threading.CancellationToken.None);
            try
            {
                games.Remove(game);
            }
            catch 
            {

            }
        }
    }
}
