using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.EntityFrameworkCore;
using OverflowBackend.Services.Interface;
using IConnectionManager = OverflowBackend.Services.Interface.IConnectionManager;

namespace OverflowBackend.Services.Implementantion
{
    public class StartupService: IStartupService
    {
        private readonly OverflowDbContext _context;
        private readonly IMatchMakingService _matchMakingService;
        private readonly IConnectionManager _connectionManager;

        public StartupService(OverflowDbContext context, IMatchMakingService matchMakingService, IConnectionManager connectionManager)
        {
            _context = context;
            _matchMakingService = matchMakingService;
            _connectionManager = connectionManager;

            Thread thread = new Thread(new ThreadStart(StatsCheck));
            thread.Start();
        }

        public void StatsCheck()
        {
            while (true)
            {
                var gamesCount = WebSocketHandler.Games.Count;
                var onlinePlayers = _connectionManager.UserConnections.Count;
                AppStatsLogger.LogNumberOfGames(gamesCount);
                AppStatsLogger.LogOnlinePlayers(onlinePlayers);
                Thread.Sleep(300000); // sleep for 5 minutes
            }
        }

        public void Initialize()
        {
            try
            {
                var logFilePath = $"/app/logs/{DateTime.Now:yyyy-MM-dd}.txt";
                // EnsureDirectoryExists($"C:/Users/{Environment.UserName}/OverflowLogs");

                if (!File.Exists(logFilePath))
                {
                    File.Create(logFilePath).Dispose(); // Dispose to release the file handle
                }
                File.AppendAllText(logFilePath, "Server started" + Environment.NewLine);
            }
            catch(DirectoryNotFoundException e) { }
            
            // Read a value from the database
            var gameVersion = _context.Versions.FirstOrDefault(e => e.VersionID == 1);
            if (gameVersion != null)
                GameVersion.Value = gameVersion.RequiredGameVerion;
            else
                throw new ApplicationException("Could not find version field");
        }
    }
}
