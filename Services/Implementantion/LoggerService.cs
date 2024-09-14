namespace OverflowBackend.Services.Implementantion
{
    public class LoggerService : ILogger
    {
        private readonly object _lock = new object();

        public LoggerService()
        {
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null; // No need to implement scoping
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Error; // You can change the level as needed
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
            if (exception != null)
            {
                message += Environment.NewLine + exception;
            }

            lock (_lock)
            {
                var logFilePath = $"/app/logs/{DateTime.Now:yyyy-MM-dd}.txt";
               // EnsureDirectoryExists($"C:/Users/{Environment.UserName}/OverflowLogs");

                if (!File.Exists(logFilePath))
                {
                    File.Create(logFilePath).Dispose(); // Dispose to release the file handle
                }
                File.AppendAllText(logFilePath, message + Environment.NewLine);
            }
        }

        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}
