using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Services
{
    public static class WebSocketHandler
    {
        private static List<Game> games = new List<Game>();
        static readonly object locker = new object();
        private static IScoreService _scoreService;
        private const string FIRST_MOVE_MSG = "start first move:";
        private const string PLAYER_1_RAN_OUT_TIME = "Player 1 ran out of time";
        private const string PLAYER_2_RAN_OUT_TIME = "Player 2 ran out of time";
        private const string PLAYER_1_NO_FIRST_MOVE = "Player 1 did not make first move";
        private const string PLAYER_2_NO_FIRST_MOVE = "Player 2 did not make first move";
        private const string YOU_WON = "You won";
        private const string YOU_LOST = "You lost";
        private const string OPPONENT_CONNECT = "opponent";
        private const string START_TIMER = "start:";
        private const string OPPONENT_LEFT = "Opponent left";

        public static async Task HandleWebSocketRequest(WebSocket webSocket, HttpContext httpConext, IScoreService scoreService)
        {
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
                byte[] msg = Encoding.UTF8.GetBytes(PLAYER_1_RAN_OUT_TIME);
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
                    game.GameOver = true;
                    await UpdatePlayerScore(1, game, false);
                    await UpdatePlayerScore(2, game, true);
                    games.Remove(game);
                }).Wait();
            }
        }

        private static void OnPlayer1TimeoutFirstMove(object sender, EventArgs e)
        {
            if (sender is Game game)
            {
                byte[] msg = Encoding.UTF8.GetBytes(PLAYER_1_NO_FIRST_MOVE);
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
                    game.GameOver = true;
                    game.UpdatedPlayer1Score = true;
                    game.UpdatedPlayer2Score = true;

                    games.Remove(game);
                }).Wait();
            }
        }

        private static void OnPlayer2TimeoutFirstMove(object sender, EventArgs e)
        {
            if (sender is Game game)
            {
                byte[] msg = Encoding.UTF8.GetBytes(PLAYER_2_NO_FIRST_MOVE);
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
                    game.GameOver = true;
                    game.UpdatedPlayer1Score = true;
                    game.UpdatedPlayer2Score = true;
                    games.Remove(game);
                }).Wait();
            }
        }

        private static void OnPlayer2Timeout(object sender, EventArgs e)
        {
            if (sender is Game game)
            {
                byte[] msg = Encoding.UTF8.GetBytes(PLAYER_2_RAN_OUT_TIME);
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
                    game.GameOver = true;
                    await UpdatePlayerScore(1, game, true);
                    await UpdatePlayerScore(2, game, false);
                    games.Remove(game);
                }).Wait();
            }
        }

        public static async Task HandleWebSocketRequest(WebSocket webSocket, string gameId, string players, string playerName)
        {
            if (!IsValidGameRequest(gameId)) return;

            Game game;
            bool isPlayer1;

            lock (locker)
            {
                game = FindOrCreateGame(gameId, players, playerName, webSocket, out isPlayer1);
            }

            if (game == null) return;

            await StartListeningOnSocket(game, isPlayer1);
        }

        private static bool IsValidGameRequest(string gameId)
        {
            return GameCollection.List != null && GameCollection.List.Contains(gameId);
        }

        private static Game FindOrCreateGame(string gameId, string players, string playerName, WebSocket webSocket, out bool isPlayer1)
        {
            var game = games.FirstOrDefault(g => g.GameId == gameId && (g.Player1 != null || g.Player2 != null));
            if (game != null)
            {
                return AssignPlayerToGame(game, players, playerName, webSocket, out isPlayer1);
            }
            else
            {
                return CreateNewGame(gameId, players, playerName, webSocket, out isPlayer1);
            }
        }

        private static Game AssignPlayerToGame(Game game, string players, string playerName, WebSocket webSocket, out bool isPlayer1)
        {
            var playersSplit = players.Split("-");
            if (playersSplit[0] == playerName)
            {
                isPlayer1 = true;
                game.Player1 = playerName;
                game.Player1Socket = webSocket;
            }
            else
            {
                isPlayer1 = false;
                game.Player2 = playerName;
                game.Player2Socket = webSocket;
            }

            return game;
        }

        private static Game CreateNewGame(string gameId, string players, string playerName, WebSocket webSocket, out bool isPlayer1)
        {
            var newGame = new Game
            {
                GameId = gameId,
                
                BoardLogic = new BoardLogic()
            };

            newGame.Player1TimeoutEvent += OnPlayer1Timeout;
            newGame.Player2TimeoutEvent += OnPlayer2Timeout;
            newGame.Player1TimeoutEventFirstMove += OnPlayer1TimeoutFirstMove;
            newGame.Player2TimeoutEventFirstMove += OnPlayer2TimeoutFirstMove;

            var playersSplit = players.Split("-");
            if (playersSplit[0] == playerName)
            {
                isPlayer1 = true;
                newGame.Player1 = playerName;
                newGame.Player1Socket = webSocket;
                SetPlayerScore(newGame, playerName, isPlayer1);
            }
            else
            {
                isPlayer1 = false;
                newGame.Player2 = playerName;
                newGame.Player2Socket = webSocket;
                SetPlayerScore(newGame, playerName, isPlayer1);
            }

            games.Add(newGame);
            return newGame;
        }

        private static void SetPlayerScore(Game game, string playerName, bool isPlayer1)
        {
            var playerScore = _scoreService.GetPlayerScore(playerName);
            if (!playerScore.HasValue)
            {
                throw new ApplicationException($"Could not get player {(isPlayer1 ? "1" : "2")} rank");
            }

            if (isPlayer1)
            {
                game.Player1Score = playerScore.Value;
            }
            else
            {
                game.Player2Score = playerScore.Value;
            }
        }

        private static async Task StartListeningOnSocket(Game game, bool isPlayer1)
        {
            if (isPlayer1)
            {
                await ListenOnSocketPlayer1(game);
            }
            else
            {
                await ListenOnSocketPlayer2(game);
            }
        }


        private static async Task UpdatePlayerScore(int player, Game game, bool win)
        {
            lock (game.LockeUpdateScores)
            {
                Task.Run(async () =>
                {
                    if (player == 1)
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
                }).Wait();
            }
            
        }
        private static async Task HandleGameOver(Game game, bool player1Won)
        {
            byte[] wonMsg = Encoding.UTF8.GetBytes(YOU_WON);
            byte[] lostMsg = Encoding.UTF8.GetBytes(YOU_LOST);
            if (player1Won)
            {
                await UpdatePlayerScore(1, game, true);
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(wonMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                await UpdatePlayerScore(2, game, false);
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(lostMsg), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                await UpdatePlayerScore(2, game, true);
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(wonMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                await UpdatePlayerScore(1, game, false);
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(lostMsg), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            
            game.Player1Timer.Close();
            game.Player2Timer.Close();
        }

        private static async Task HandleOpponentMessagePlayer1(Game game, byte[] receivedMessageBuffer, WebSocketReceiveResult result)
        {
            // Received message opponent from player 1, need to send it back to player 2 and start the first move timer
            if (!game.Player1TimerFirstMove.Enabled)
            {
                game.Player1Connected = true;
                // send the opponent message
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(receivedMessageBuffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);

                var bothPlayerConnected = game.Player1Connected && game.Player2Connected;

                if (bothPlayerConnected)
                {
                    game.Player1TimerFirstMove.Start();
                    game.Player1TimerStartFirstMove = DateTime.UtcNow;
                    //Notify both players that the first move timer for player 1 player has started
                    var timeStartByteArray = Encoding.UTF8.GetBytes(FIRST_MOVE_MSG + game.Player1TimerStartFirstMove.ToString("o"));
                    await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                    await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                }
            }
            else
            {
                // send the opponent message
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(receivedMessageBuffer, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
            }
        }

        private static async Task HandleOpponentMessagePlayer2(Game game, byte[] receivedMessageBuffer, WebSocketReceiveResult websocketReceivedResult)
        {
            // Received message opponent from player 2, need to send it back to player 1 and start the first move timer
            if (!game.Player1TimerFirstMove.Enabled)
            {
                game.Player2Connected = true;
                // send the opponent message
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(receivedMessageBuffer, 0, websocketReceivedResult.Count), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);

                var bothPlayersConnected = game.Player1Connected && game.Player2Connected;

                if (bothPlayersConnected)
                {
                    game.Player1TimerFirstMove.Start();
                    game.Player1TimerStartFirstMove = DateTime.UtcNow;
                    //Notify both players that the first move timer for player 1 player has started
                    var timeStartByteArray = Encoding.UTF8.GetBytes(FIRST_MOVE_MSG + game.Player1TimerStartFirstMove.ToString("o"));
                    await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
                    await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
                }
            }
            else
            {
                // send the opponent message
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(receivedMessageBuffer, 0, websocketReceivedResult.Count), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
            }
        }

        private static async Task HandleMovePlayer1(Game game, byte[] receivedMessageBuffer, WebSocketReceiveResult websocketReceivedResult)
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
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(receivedMessageBuffer, 0, websocketReceivedResult.Count), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
                // Notify the players that the first move timer for player 2 has started
                var timeStartByteArray = Encoding.UTF8.GetBytes(FIRST_MOVE_MSG + game.Player2TimerStartFirstMove.ToString("o"));
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
            }
            else
            {
                game.Player1Timer.Stop();
                game.Player2Timer.Start();
                game.Player2TimerStart = DateTime.UtcNow;
                // send move
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(receivedMessageBuffer, 0, websocketReceivedResult.Count), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
                // send timer start
                var timeStartByteArray = Encoding.UTF8.GetBytes(START_TIMER + game.Player2TimerStart.ToString("o"));
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
            }
        }

        private static async Task HandleMovePlayer2(Game game, byte[] receivedMessageBuffer, WebSocketReceiveResult websocketReceivedResult)
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
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(receivedMessageBuffer, 0, websocketReceivedResult.Count), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
                // Notify the players that the standard timer has started for player 1
                var timeStartByteArray = Encoding.UTF8.GetBytes(START_TIMER + game.Player1TimerStart.ToString("o"));
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);

            }
            else
            {
                game.Player2Timer.Stop();
                game.Player1Timer.Start();
                game.Player1TimerStart = DateTime.UtcNow;

                // send move
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(receivedMessageBuffer, 0, websocketReceivedResult.Count), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);

                // send timer start
                var timeStartByteArray = Encoding.UTF8.GetBytes(START_TIMER + game.Player1TimerStart.ToString("o"));
                await game.Player1Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
                await game.Player2Socket.SendAsync(new ArraySegment<byte>(timeStartByteArray, 0, timeStartByteArray.Length), WebSocketMessageType.Text, websocketReceivedResult.EndOfMessage, System.Threading.CancellationToken.None);
            }
        }

        private static async Task ListenOnSocketPlayer1(Game game)
        {
            byte[] msg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(game.BoardLogic.BoardData));

            await game.Player1Socket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);

            byte[] receivedMessageBuffer = new byte[1024];

            WebSocketReceiveResult websocketReceivedResult = await game.Player1Socket.ReceiveAsync(new ArraySegment<byte>(receivedMessageBuffer), System.Threading.CancellationToken.None);
            
            while (!websocketReceivedResult.CloseStatus.HasValue)
            {
                string receivedDataString = Encoding.UTF8.GetString(receivedMessageBuffer, 0, websocketReceivedResult.Count);
                string[] parts = null;

                if (receivedDataString != OPPONENT_CONNECT)
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
                        if (receivedDataString == OPPONENT_CONNECT || allowedMove)
                        {
                            if(game.BoardLogic.playerTwoAvailableMoves.Count == 0 && receivedDataString != OPPONENT_CONNECT)
                            {
                                await HandleGameOver(game, player1Won: true);
                            }
                            else
                            {
                                if(receivedDataString == OPPONENT_CONNECT)
                                {
                                    await HandleOpponentMessagePlayer1(game, receivedMessageBuffer, websocketReceivedResult);
                                }
                                else
                                {
                                    await HandleMovePlayer1(game, receivedMessageBuffer, websocketReceivedResult);
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
                        //Player not yet connected, wait 
                        await Task.Delay(2000);
                    }
                }
                // receive move from player 1
                websocketReceivedResult = await game.Player1Socket.ReceiveAsync(new ArraySegment<byte>(receivedMessageBuffer), System.Threading.CancellationToken.None);
            }
            await game.Player1Socket.CloseAsync(websocketReceivedResult.CloseStatus.Value, websocketReceivedResult.CloseStatusDescription, System.Threading.CancellationToken.None);

            game.Player1Timer.Close();
            game.Player2Timer.Close();

            GameCollection.List.Remove(game.GameId);

            TryDeleteGame(game);

            await HandleOpponentLeft(game, websocketReceivedResult, player1Won: false);
        }

        private static async Task HandleOpponentLeft(Game game, WebSocketReceiveResult result, bool player1Won)
        {
            try
            {
                byte[] msgBuffer = Encoding.UTF8.GetBytes(OPPONENT_LEFT);
                await UpdatePlayerScore(1, game, player1Won);
                await UpdatePlayerScore(2, game, !player1Won);
                if (!player1Won)
                {
                    await game.Player2Socket.SendAsync(new ArraySegment<byte>(msgBuffer), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                }
                else
                {
                    await game.Player1Socket.SendAsync(new ArraySegment<byte>(msgBuffer), WebSocketMessageType.Text, result.EndOfMessage, System.Threading.CancellationToken.None);
                }
            }
            catch { }
        }

        private static void TryDeleteGame(Game game)
        {
            try
            {
                games.Remove(game);
            }
            catch { }
        }

        private static async Task ListenOnSocketPlayer2(Game game)
        {
            byte[] helloMsg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(game.BoardLogic.BoardData));
            await game.Player2Socket.SendAsync(new ArraySegment<byte>(helloMsg), WebSocketMessageType.Text, true, CancellationToken.None);

            byte[] receivedMessageBuffer = new byte[1024];

            WebSocketReceiveResult websocketReceivedResult = await game.Player2Socket.ReceiveAsync(new ArraySegment<byte>(receivedMessageBuffer), System.Threading.CancellationToken.None);
            while (!websocketReceivedResult.CloseStatus.HasValue)
            {
                string receivedDataString = Encoding.UTF8.GetString(receivedMessageBuffer, 0, websocketReceivedResult.Count);
                string[] parts = null;

                if (receivedDataString != OPPONENT_CONNECT)
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
                            //game.Player2Timer.Start();
                            allowedMove = game.BoardLogic.MovePlayer(int.Parse(parts[0]), int.Parse(parts[1]));
                        }
                        if(receivedDataString == OPPONENT_CONNECT || allowedMove)
                        {
                            if (game.BoardLogic.playerOneAvailableMoves.Count == 0 && receivedDataString != OPPONENT_CONNECT)
                            {
                                await HandleGameOver(game, false);
                            }
                            else
                            {
                                if (receivedDataString == OPPONENT_CONNECT)
                                {
                                    await HandleOpponentMessagePlayer2(game, receivedMessageBuffer, websocketReceivedResult);
                                }
                                else
                                {
                                    await HandleMovePlayer2(game, receivedMessageBuffer, websocketReceivedResult);

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
                websocketReceivedResult = await game.Player2Socket.ReceiveAsync(new ArraySegment<byte>(receivedMessageBuffer), System.Threading.CancellationToken.None);
            }
            await game.Player2Socket.CloseAsync(websocketReceivedResult.CloseStatus.Value, websocketReceivedResult.CloseStatusDescription, System.Threading.CancellationToken.None);
            
            game.Player1Timer.Close();
            game.Player2Timer.Close();

            GameCollection.List.Remove(game.GameId);

            TryDeleteGame(game);

            await HandleOpponentLeft(game, websocketReceivedResult, player1Won: true);
        }
    }
}
