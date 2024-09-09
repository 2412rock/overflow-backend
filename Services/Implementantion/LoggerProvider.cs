namespace OverflowBackend.Services.Implementantion
{
    public class LoggerProvider: ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new LoggerService();
        }

        public void Dispose()
        {
        }
    }
}
