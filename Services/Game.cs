using System.Net.WebSockets;
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

        public Timer Player1TimerFirstMove { get; set; }
        public Timer Player2TimerFirstMove { get; set; }
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

        public readonly object LockeUpdateScores = new object();

        public Game()
        {
            Player1Timer = new Timer(120000); // 10 seconds
            Player1Timer.Elapsed += OnPlayer1Timeout;
            Player1Timer.AutoReset = false;

            Player2Timer = new Timer(10000); // 120 seconds in milliseconds
            Player2Timer.Elapsed += OnPlayer2Timeout;
            Player2Timer.AutoReset = false;

            Player1TimerFirstMove = new Timer(120000); // 120000 120 seconds in milliseconds
            Player1TimerFirstMove.Elapsed += OnPlayer1TimeoutFirstMove;
            Player1TimerFirstMove.AutoReset = false;

            Player2TimerFirstMove = new Timer(10000); // 10 seconds
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
}
