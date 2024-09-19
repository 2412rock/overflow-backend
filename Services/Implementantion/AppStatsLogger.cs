using System;

namespace OverflowBackend.Services.Implementantion
{
    public static class AppStatsLogger
    {
        private static readonly object _lock = new object();
        public static void LogSignIn(string username, string ip)
        {
            DateTime now = DateTime.UtcNow;
            string isoString = now.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var message = $"{isoString},{username},{ip}" + Environment.NewLine;

            lock (_lock)
            {
                var logFilePath = $"/app/logs/{DateTime.Now:yyyy-MM}_SignIn.csv";
                // EnsureDirectoryExists($"C:/Users/{Environment.UserName}/OverflowLogs");
                try
                {
                    if (!File.Exists(logFilePath))
                    {
                        File.Create(logFilePath).Dispose(); // Dispose to release the file handle
                    }
                    File.AppendAllText(logFilePath, message + Environment.NewLine);
                }
                catch { }
            }
        }

        public static void LogSignUp(string username, string ip)
        {
            DateTime now = DateTime.UtcNow;
            string isoString = now.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var message = $"{isoString},{username},{ip}" + Environment.NewLine;

            lock (_lock)
            {
                var logFilePath = $"/app/logs/{DateTime.Now:yyyy-MM}_SignUp.csv";
                // EnsureDirectoryExists($"C:/Users/{Environment.UserName}/OverflowLogs");
                try
                {
                    if (!File.Exists(logFilePath))
                    {
                        File.Create(logFilePath).Dispose(); // Dispose to release the file handle
                    }
                    File.AppendAllText(logFilePath, message + Environment.NewLine);
                }
                catch { }
            }
        }

        public static void LogNumberOfGames(int count)
        {
            DateTime now = DateTime.UtcNow;
            string isoString = now.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var message = $"{isoString},{count}" + Environment.NewLine;

            lock (_lock)
            {
                var logFilePath = $"/app/logs/{DateTime.Now:yyyy-MM}_NrGames.csv";
                // EnsureDirectoryExists($"C:/Users/{Environment.UserName}/OverflowLogs");
                try
                {
                    if (!File.Exists(logFilePath))
                    {
                        File.Create(logFilePath).Dispose(); // Dispose to release the file handle
                    }
                    File.AppendAllText(logFilePath, message + Environment.NewLine);
                }
                catch { }
            }
        }

        public static void LogOnlinePlayers(int count)
        {
            DateTime now = DateTime.UtcNow;
            string isoString = now.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var message = $"{isoString},{count}" + Environment.NewLine;

            lock (_lock)
            {
                var logFilePath = $"/app/logs/{DateTime.Now:yyyy-MM}_NrOfOnlinePlayers.csv";
                // EnsureDirectoryExists($"C:/Users/{Environment.UserName}/OverflowLogs");
                try
                {
                    if (!File.Exists(logFilePath))
                    {
                        File.Create(logFilePath).Dispose(); // Dispose to release the file handle
                    }
                    File.AppendAllText(logFilePath, message + Environment.NewLine);
                }
                catch { }
            }
        }
    }
}
