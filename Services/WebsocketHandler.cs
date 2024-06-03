using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using static Azure.Core.HttpHeader;
using System.Timers;
using Timer = System.Timers.Timer;

namespace OverflowBackend.Services
{
    public class Game
    {
        public string GameId { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }

        public WebSocket Player1Socket { get; set; }

        public WebSocket Player2Socket { get; set; }

        public BoardLogic BoardLogic { get; set; }
        public Timer Player1Timer { get; set; }
        public Timer Player2Timer { get; set; }
        // Add additional game state properties as needed

        public event EventHandler Player1TimeoutEvent;
        public event EventHandler Player2TimeoutEvent;

        public DateTime Player1TimerStart { get; set; }
        public DateTime Player2TimerStart { get; set; }

        public Game()
        {
            Player1Timer = new Timer(120000); // 120000 120 seconds in milliseconds
            Player1Timer.Elapsed += OnPlayer1Timeout;
            Player1Timer.AutoReset = false;

            Player2Timer = new Timer(120000); // 120 seconds in milliseconds
            Player2Timer.Elapsed += OnPlayer2Timeout;
            Player2Timer.AutoReset = false;
        }

        private void OnPlayer1Timeout(object sender, ElapsedEventArgs e)
        {
            Player1TimeoutEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlayer2Timeout(object sender, ElapsedEventArgs e)
        {
            Player2TimeoutEvent?.Invoke(this, EventArgs.Empty);
        }
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

        private static void OnPlayer1Timeout(object sender, EventArgs e)
        {
            if (sender is Game game)
            {
                byte[] msg = Encoding.UTF8.GetBytes("Player 1 ran out of time");
                Task.Run(async () =>
                {
                    if (game.Player1Socket != null && game.Player1Socket.State == WebSocketState.Open)
                    {
                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    if (game.Player2Socket != null && game.Player2Socket.State == WebSocketState.Open)
                    {
                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    games.Remove(game);
                }).Wait();
            }
        }

        private static void OnPlayer2Timeout(object sender, EventArgs e)
        {
            if (sender is Game game)
            {
                byte[] msg = Encoding.UTF8.GetBytes("Player 2 ran out of time");
                Task.Run(async () =>
                {
                    if (game.Player1Socket != null && game.Player1Socket.State == WebSocketState.Open)
                    {
                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    if (game.Player2Socket != null && game.Player2Socket.State == WebSocketState.Open)
                    {
                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    games.Remove(game);
                }).Wait();
            }
        }

        public static async Task HandleWebSocketRequest(WebSocket webSocket, string gameId, string playerName)
        {
            var player1 = false;
            Game game;
            lock (locker)
            {
                game = games.Where(game => game.GameId == gameId && (game.Player1 != null || game.Player2 != null) ).FirstOrDefault();
                if (game != null)
                {
                    
                    var gameIdSplit = gameId.Split("-");
                    if (gameIdSplit[0] == playerName)
                    {
                        player1 = true;
                        game.Player1 = playerName;
                        game.Player1Socket = webSocket;
                    }
                    else
                    {
                        game.Player2 = playerName;
                        game.Player2Socket = webSocket;
                    }
                    
                }
                else
                {
                    var newGame = new Game();
                    newGame.GameId = gameId;
                    var gameIdSplit = gameId.Split("-");
                    if (gameIdSplit[0] == playerName)
                    {
                        player1 = true;
                        newGame.Player1 = playerName;
                        newGame.Player1Socket = webSocket;
                        newGame.Player2Socket = null;
                        newGame.Player1TimeoutEvent += OnPlayer1Timeout;
                        newGame.Player2TimeoutEvent += OnPlayer2Timeout;

                        newGame.BoardLogic = new BoardLogic();
                        games.Add(newGame);
                        game = newGame;
                    }
                    else
                    {
                        newGame.Player2 = playerName;
                        newGame.Player2Socket = webSocket;
                        newGame.Player1Socket = null;
                        newGame.Player1TimeoutEvent += OnPlayer1Timeout;
                        newGame.Player2TimeoutEvent += OnPlayer2Timeout;
                        newGame.BoardLogic = new BoardLogic();
                        games.Add(newGame);
                        game = newGame;
                    }
                    

                }
            }

            if (!player1)
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

        private static async Task ListenOnSocketPlayer1(Game game)
        {
            byte[] msg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(game.BoardLogic.BoardData));
            await game.Player1Socket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);

            byte[] buffer = new byte[1024];
            //receive move from player 1
            WebSocketReceiveResult result = await game.Player1Socket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                //byte[] msgBuffer = Encoding.UTF8.GetBytes("Some message from player 1");
                //send move to player 2
                string receivedDataString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                string[] parts = null;
                if (receivedDataString != "opponent")
                {
                    parts = receivedDataString.Split(':');

                }
                while (true)
                {
                    if(game.Player2Socket != null)
                    {
                        var allowedMove = false;
                        if (parts != null && parts.Length == 2)
                        {
                            allowedMove = game.BoardLogic.MovePlayer(int.Parse(parts[0]), int.Parse(parts[1]));
                        }
                        if (receivedDataString == "opponent" || allowedMove)
                        {
                            if(game.BoardLogic.playerTwoAvailableMoves.Count == 0 && receivedDataString != "opponent")
                            {
                                byte[] wonMsg = Encoding.UTF8.GetBytes("You won");
                                await game.Player1Socket.SendAsync(new ArraySegment<byte>(wonMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                            game.Player1Timer.Stop();
                            game.Player2Timer.Start();
                            game.Player2TimerStart = DateTime.Now;
                            // send move
                            await game.Player2Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                            // send timer start
                            var timeStartByteArray = Encoding.UTF8.GetBytes("start:" + game.Player2TimerStart.ToString());
                            await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                            await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                        }
                        else
                        {
                            byte[] availableMoves = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(game.BoardLogic.playerOneAvailableMoves));
                            await game.Player1Socket.SendAsync(new ArraySegment<byte>(availableMoves), WebSocketMessageType.Text, true, CancellationToken.None);
                        }

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
            { }
            try
            {
                byte[] msgBuffer = Encoding.UTF8.GetBytes("Opponent left");
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(msgBuffer), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
            }
            catch { }

        }

        private static async Task ListenOnSocketPlayer2(Game game)
        {
            byte[] msg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(game.BoardLogic.BoardData));
            await game.Player2Socket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);


            byte[] buffer = new byte[1024];
            //receive move from player 1
            WebSocketReceiveResult result = await game.Player2Socket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                //byte[] msgBuffer = Encoding.UTF8.GetBytes("Some message from player 2");
                //send move to player 2
                string receivedDataString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                string[] parts = null;
                if (receivedDataString != "opponent")
                {
                   parts = receivedDataString.Split(':');
                   
                }
                

                while (true)
                {
                    if(game.Player1Socket != null)
                    {
                        var allowedMove = false;
                        if(parts != null && parts.Length == 2)
                        {
                            game.Player2Timer.Start();
                            allowedMove = game.BoardLogic.MovePlayer(int.Parse(parts[0]), int.Parse(parts[1]));
                        }
                        if(receivedDataString == "opponent" || allowedMove)
                        {
                            if (game.BoardLogic.playerOneAvailableMoves.Count == 0 && receivedDataString != "opponent")
                            {
                                byte[] wonMsg = Encoding.UTF8.GetBytes("You won");
                                await game.Player2Socket.SendAsync(new ArraySegment<byte>(wonMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                            game.Player2Timer.Stop();
                            game.Player1Timer.Start();
                            game.Player1TimerStart = DateTime.Now;
                            
                            // send move
                            await game.Player1Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                            // send timer start
                            var timeStartByteArray = Encoding.UTF8.GetBytes("start:" + game.Player1TimerStart.ToString());
                            await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                            await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                            
                        }
                        else
                        {
                            byte[] availableMoves = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(game.BoardLogic.playerTwoAvailableMoves));
                            await game.Player2Socket.SendAsync(new ArraySegment<byte>(availableMoves), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        
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
            catch {}
            try
            {
                byte[] msgBuffer = Encoding.UTF8.GetBytes("Opponent left");
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(msgBuffer), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
            }
            catch { }
        }
    }
}
