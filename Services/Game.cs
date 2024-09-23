using System.Net.WebSockets;
using System.Timers;
using Timer = System.Timers.Timer;

namespace OverflowBackend.Services
{
    public class Game : IDisposable
    {
        public string GameId { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }

        public WebSocket Player1Socket { get; set; }

        public WebSocket Player2Socket { get; set; }

        public BoardLogic BoardLogic { get; set; }
        public Timer Player1Timer { get; set; }
        public Timer Player2Timer { get; set; }

        public Timer PlayerConnectTimer { get; set; }

        public Timer Player1TimerFirstMove { get; set; }
        public Timer Player2TimerFirstMove { get; set; }
        public event EventHandler Player1TimeoutEvent;
        public event EventHandler Player2TimeoutEvent;
        public event EventHandler Player1TimeoutEventFirstMove;
        public event EventHandler Player2TimeoutEventFirstMove;
        public event EventHandler PlayerConnectTimeoutEvent;

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

        public readonly object LockeUpdateScores = new object();

        public Game()
        {
            PlayerConnectTimer = new Timer(12000); // 15 seconds
            PlayerConnectTimer.Elapsed += OnPlayerConnectTimeout;
            PlayerConnectTimer.AutoReset = false;
            PlayerConnectTimer.Start();

            Player1Timer = new Timer(120000); // 120 seconds in milliseconds
            Player1Timer.Elapsed += OnPlayer1Timeout;
            Player1Timer.AutoReset = false;

            Player2Timer = new Timer(120000); // 120 seconds in milliseconds
            Player2Timer.Elapsed += OnPlayer2Timeout;
            Player2Timer.AutoReset = false;

            Player1TimerFirstMove = new Timer(15000); // 120000 120 seconds in milliseconds
            Player1TimerFirstMove.Elapsed += OnPlayer1TimeoutFirstMove;
            Player1TimerFirstMove.AutoReset = false;

            Player2TimerFirstMove = new Timer(15000); // 10 seconds
            Player2TimerFirstMove.Elapsed += OnPlayer2TimeoutFirstMove;
            Player2TimerFirstMove.AutoReset = false;

            Player1Connected = false;
            Player2Connected = false;
        }

        private void OnPlayerConnectTimeout(object sender, ElapsedEventArgs e)
        {
            PlayerConnectTimeoutEvent?.Invoke(this, EventArgs.Empty);
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

        public void Dispose()
        {
            Player1TimeoutEvent = null;
            Player2TimeoutEvent = null;
            Player1TimeoutEventFirstMove = null;
            Player2TimeoutEventFirstMove = null;
            PlayerConnectTimeoutEvent = null;
            // Unsubscribe from events before disposing of timers
            PlayerConnectTimer.Elapsed -= OnPlayerConnectTimeout;
            Player1Timer.Elapsed -= OnPlayer1Timeout;
            Player2Timer.Elapsed -= OnPlayer2Timeout;
            Player1TimerFirstMove.Elapsed -= OnPlayer1TimeoutFirstMove;
            Player2TimerFirstMove.Elapsed -= OnPlayer2TimeoutFirstMove;

            // Dispose of timers
            PlayerConnectTimer?.Dispose();
            Player1Timer?.Dispose();
            Player2Timer?.Dispose();
            Player1TimerFirstMove?.Dispose();
            Player2TimerFirstMove?.Dispose();

            // Dispose of WebSockets if necessary
            if (Player1Socket != null && Player1Socket.State == WebSocketState.Open)
            {
                Player1Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Game over", CancellationToken.None).Wait();
                Player1Socket.Dispose();
            }

            if (Player2Socket != null && Player2Socket.State == WebSocketState.Open)
            {
                Player2Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Game over", CancellationToken.None).Wait();
                Player2Socket.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}
