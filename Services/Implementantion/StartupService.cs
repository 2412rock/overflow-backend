using Microsoft.EntityFrameworkCore;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Services.Implementantion
{
    public class StartupService: IStartupService
    {
        private readonly OverflowDbContext _context;
        private readonly ILogger _logger;

        public StartupService(OverflowDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public void Initialize()
        {
            _logger.LogInformation("Server started");
            // Read a value from the database
            var gameVersion = _context.Versions.FirstOrDefault(e => e.VersionID == 1);
            if (gameVersion != null)
                GameVersion.Value = gameVersion.RequiredGameVerion;
            else
                throw new ApplicationException("Could not find version field");
        }
    }
}
