using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.EntityFrameworkCore;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Services.Implementantion
{
    public class StartupService: IStartupService
    {
        private readonly OverflowDbContext _context;

        public StartupService(OverflowDbContext context)
        {
            _context = context;
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
