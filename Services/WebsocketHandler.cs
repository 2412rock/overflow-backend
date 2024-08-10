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
using OverflowBackend.Services.Implementantion;
using OverflowBackend.Services.Interface;

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

        public Timer Player1TimerFirstMove { get; set; }
        public Timer Player2TimerFirstMove { get; set; }
        // Add additional game state properties as needed

        public event EventHandler Player1TimeoutEvent;
        public event EventHandler Player2TimeoutEvent;

        public event EventHandler Player1TimeoutEventFirstMove;
        public event EventHandler Player2TimeoutEventFirstMove;

        public DateTime Player1TimerStart { get; set; }
        public DateTime Player2TimerStart { get; set; }
        public DateTime Player1TimerStartFirstMove { get; set; }
        public DateTime Player2TimerStartFirstMove { get; set; }

        public bool GameOver { get; set; }

        public int Player1Score { get; set; }

        public int Player2Score { get; set; }

        public bool UpdatedPlayer1Score { get; set; }

        public bool UpdatedPlayer2Score { get; set; }

        public bool Player1Connected { get; set; }
        public bool Player2Connected { get; set; }


        public Game()
        {
            Player1Timer = new Timer(120000); // 120000 120 seconds in milliseconds
            Player1Timer.Elapsed += OnPlayer1Timeout;
            Player1Timer.AutoReset = false;

            Player2Timer = new Timer(120000); // 120 seconds in milliseconds
            Player2Timer.Elapsed += OnPlayer2Timeout;
            Player2Timer.AutoReset = false;

            Player1TimerFirstMove = new Timer(10000); // 120000 120 seconds in milliseconds
            Player1TimerFirstMove.Elapsed += OnPlayer1TimeoutFirstMove;
            Player1TimerFirstMove.AutoReset = false;

            Player2TimerFirstMove = new Timer(10000); // 120 seconds in milliseconds
            Player2TimerFirstMove.Elapsed += OnPlayer2TimeoutFirstMove;
            Player2TimerFirstMove.AutoReset = false;

            Player1Connected = false;
            Player2Connected = false;
        }

        private void OnPlayer1Timeout(object sender, ElapsedEventArgs e)
        {
            Player1TimeoutEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlayer2Timeout(object sender, ElapsedEventArgs e)
        {
            Player2TimeoutEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlayer1TimeoutFirstMove(object sender, ElapsedEventArgs e)
        {
            Player1TimeoutEventFirstMove?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlayer2TimeoutFirstMove(object sender, ElapsedEventArgs e)
        {
            Player2TimeoutEventFirstMove?.Invoke(this, EventArgs.Empty);
        }
    }
    public static class WebSocketHandler
    {
        private static List<Game> games = new List<Game>();
        static readonly object locker = new object();
        static readonly object lockerConnected = new object();
        private static IScoreService _scoreService;

        public static async Task HandleWebSocketRequest(WebSocket webSocket, HttpContext httpConext, IScoreService scoreService)
        {
            /* */
            
            string gameId = httpConext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries)[1];
            string players = httpConext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries)[2];
            string playerName = httpConext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries)[3];
            _scoreService = scoreService;
     
            await HandleWebSocketRequest(webSocket, gameId, players, playerName);
        
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
                    game.Player1TimerFirstMove.Close();
                    game.Player2TimerFirstMove.Close();
                    game.Player1Timer.Close();
                    game.Player2Timer.Close();
                    games.Remove(game);
                }).Wait();
            }
        }

        private static void OnPlayer1TimeoutFirstMove(object sender, EventArgs e)
        {
            if (sender is Game game)
            {
                byte[] msg = Encoding.UTF8.GetBytes("Player 1 did not make first move");
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
                    game.Player1TimerFirstMove.Close();
                    game.Player2TimerFirstMove.Close();
                    game.Player1Timer.Close();
                    game.Player2Timer.Close();
                    games.Remove(game);
                }).Wait();
            }
        }

        private static void OnPlayer2TimeoutFirstMove(object sender, EventArgs e)
        {
            if (sender is Game game)
            {
                byte[] msg = Encoding.UTF8.GetBytes("Player 2 did not make first move");
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
                    game.Player1TimerFirstMove.Close();
                    game.Player2TimerFirstMove.Close();
                    game.Player1Timer.Close();
                    game.Player2Timer.Close();
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
                    game.Player1TimerFirstMove.Close();
                    game.Player2TimerFirstMove.Close();
                    game.Player1Timer.Close();
                    game.Player2Timer.Close();
                    games.Remove(game);
                }).Wait();
            }
        }

        public static async Task HandleWebSocketRequest(WebSocket webSocket, string gameId, string players, string playerName)
        {
            var gameFound = false;
            if(GameCollection.List != null)
            {
                foreach(var element in GameCollection.List)
                {
                    if(element == gameId)
                    {
                        gameFound = true;
                        break;
                    }
                }
            }
            if (!gameFound)
            {
                // Invalid request
                return;
            }
            var player1 = false;
            Game game;
            lock (locker)
            {
                game = games.Where(game => game.GameId == gameId && (game.Player1 != null || game.Player2 != null) ).FirstOrDefault();
                if (game != null)
                {
                    
                    var playersSplit = players.Split("-");
                    if (playersSplit[0] == playerName)
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
                    var playersSplit = players.Split("-");
                    if (playersSplit[0] == playerName)
                    {
                        player1 = true;
                        newGame.Player1 = playerName;
                        newGame.Player1Socket = webSocket;
                        newGame.Player2Socket = null;
                        newGame.Player1TimeoutEvent += OnPlayer1Timeout;
                        newGame.Player2TimeoutEvent += OnPlayer2Timeout;
                        newGame.Player1TimeoutEventFirstMove += OnPlayer1TimeoutFirstMove;
                        newGame.Player2TimeoutEventFirstMove += OnPlayer2TimeoutFirstMove;

                        var player1Score = _scoreService.GetPlayerScore(playerName);
                        if (player1Score.HasValue)
                        {
                            newGame.Player1Score = player1Score.Value;
                        }
                        else
                        {
                            throw new ApplicationException("Could not get player 1 rank");
                        }
                        

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
                        newGame.Player1TimeoutEventFirstMove += OnPlayer1TimeoutFirstMove;
                        newGame.Player2TimeoutEventFirstMove += OnPlayer2TimeoutFirstMove;

                        var player2Score = _scoreService.GetPlayerScore(playerName);
                        if (player2Score.HasValue)
                        {
                            newGame.Player2Score = player2Score.Value;
                        }
                        else
                        {
                            throw new ApplicationException("Could not get player 2 rank");
                        }

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

        private static async Task UpdatePlayerScore(int player, Game game, bool win)
        {
            if(player == 1)
            {
                if (!game.UpdatedPlayer1Score)
                {
                    game.UpdatedPlayer1Score = true;
                    await _scoreService.UpdateScore(game.Player1, game.Player2, win);
                }
            }
            else if (player == 2)
            {
                if (!game.UpdatedPlayer2Score)
                {
                    game.UpdatedPlayer2Score = true;
                    await _scoreService.UpdateScore(game.Player2, game.Player1, win);
                }
            }
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
                                await UpdatePlayerScore(1, game, true);
                                await game.Player1Socket.SendAsync(new ArraySegment<byte>(wonMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                                byte[] lostMsg = Encoding.UTF8.GetBytes("You lost");
                                await UpdatePlayerScore(2, game, false);
                                await game.Player2Socket.SendAsync(new ArraySegment<byte>(lostMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                                game.Player1Timer.Close();
                                game.Player2Timer.Close();
                            }
                            else
                            {
                                if(receivedDataString == "opponent")
                                {
                                    // Received message opponent from player 1, need to send it back to player 2 and start the first move timer
                                    if (!game.Player1TimerFirstMove.Enabled)
                                    {
                                        game.Player1Connected = true;
                                        // send the opponent message
                                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                            
                                        var bothPlayerConnected = game.Player1Connected && game.Player2Connected;

                                        if (bothPlayerConnected)
                                        {
                                            game.Player1TimerFirstMove.Start();
                                            game.Player1TimerStartFirstMove = DateTime.UtcNow;
                                            //Notify both players that the first move timer for player 1 player has started
                                            var timeStartByteArray = Encoding.UTF8.GetBytes("start first move:" + game.Player1TimerStartFirstMove.ToString("o"));
                                            await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                            await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                                        }

                                    }
                                    else
                                    {
                                        // send the opponent message
                                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                                    }

                                }
                                else
                                {
                                    // Received an actual move from player 1, need to send it back to player 2 and start the timer for player 2
                                    if (game.Player1TimerFirstMove.Enabled)
                                    {
                                        // In case the first move timer is running for player 1, stop it
                                        game.Player1TimerFirstMove.Stop();
                                        game.Player1TimerFirstMove.Close();
                                        // If the first move timer is running, it means player 2 has to make its first move too, so start that timer
                                        game.Player2TimerFirstMove.Start();
                                        game.Player2TimerStartFirstMove = DateTime.UtcNow;
                                        //Send player 1 move to player 2
                                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                        // Notify the players that the first move timer for player 2 has started
                                        var timeStartByteArray = Encoding.UTF8.GetBytes("start first move:" + game.Player2TimerStartFirstMove.ToString("o"));
                                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                                    }
                                    else
                                    {
                                        game.Player1Timer.Stop();
                                        game.Player2Timer.Start();
                                        game.Player2TimerStart = DateTime.UtcNow;
                                        // send move
                                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                        // send timer start
                                        var timeStartByteArray = Encoding.UTF8.GetBytes("start:" + game.Player2TimerStart.ToString("o"));
                                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                                    }

                                }

                            }
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
            game.Player1Timer.Close();
            game.Player2Timer.Close();
            GameCollection.List.Remove(game.GameId);
            try
            {
                games.Remove(game);
            }
            catch 
            { }
            try
            {
                byte[] msgBuffer = Encoding.UTF8.GetBytes("Opponent left");
                await UpdatePlayerScore(1, game, false);
                await UpdatePlayerScore(2, game, true);
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
                                await UpdatePlayerScore(2, game, true);
                                await game.Player2Socket.SendAsync(new ArraySegment<byte>(wonMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                                byte[] lostMsg = Encoding.UTF8.GetBytes("You lost");
                                await UpdatePlayerScore(1, game, false);
                                await game.Player1Socket.SendAsync(new ArraySegment<byte>(lostMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                                game.Player1Timer.Close();
                                game.Player2Timer.Close();
                            }
                            else
                            {
                                if (receivedDataString == "opponent")
                                {
                                    // Received message opponent from player 2, need to send it back to player 1 and start the first move timer
                                    if (!game.Player1TimerFirstMove.Enabled)
                                    {
                                        game.Player2Connected = true;
                                        // send the opponent message
                                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                            
                                        var bothPlayersConnected = game.Player1Connected && game.Player2Connected;
                                        
                                        if (bothPlayersConnected)
                                        {
                                            game.Player1TimerFirstMove.Start();
                                            game.Player1TimerStartFirstMove = DateTime.UtcNow;
                                            //Notify both players that the first move timer for player 1 player has started
                                            var timeStartByteArray = Encoding.UTF8.GetBytes("start first move:" + game.Player1TimerStartFirstMove.ToString("o"));
                                            await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                            await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                                        }

                                    }
                                    else
                                    {
                                        // send the opponent message
                                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                                    }

                                }
                                else
                                {
                                    // Received an actual move from player 2, need to send it back to player 1 and start the timer for player 1
                                    if (game.Player2TimerFirstMove.Enabled)
                                    {
                                        // In case the first move timer is running for player 2, stop it
                                        game.Player2TimerFirstMove.Stop();
                                        game.Player2TimerFirstMove.Close();
                                        game.Player1Timer.Start();
                                        game.Player1TimerStart = DateTime.UtcNow;
                                        //Send player 2 move to player 1
                                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                        // Notify the players that the standard timer has started for player 1
                                        var timeStartByteArray = Encoding.UTF8.GetBytes("start:" + game.Player1TimerStart.ToString("o"));
                                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                                    }
                                    else
                                    {
                                        game.Player2Timer.Stop();
                                        game.Player1Timer.Start();
                                        game.Player1TimerStart = DateTime.UtcNow;

                                        // send move
                                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                                        // send timer start
                                        var timeStartByteArray = Encoding.UTF8.GetBytes("start:" + game.Player1TimerStart.ToString("o"));
                                        await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                                        await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                                    }

                                }

                            }

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
            game.Player1Timer.Close();
            game.Player2Timer.Close();
            GameCollection.List.Remove(game.GameId);
            try
            {
                games.Remove(game);
            }
            catch {}
            try
            {
                byte[] msgBuffer = Encoding.UTF8.GetBytes("Opponent left");
                await UpdatePlayerScore(1, game, true);
                await UpdatePlayerScore(2, game, false);
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(msgBuffer), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
            }
            catch { }
        }
    }
}
