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
            // Read a value from the database
            var gameVersion = _context.Versions.FirstOrDefault(e => e.VersionID == 1);
            if (gameVersion != null)
                GameVersion.Value = gameVersion.RequiredGameVerion;
            else
                throw new ApplicationException("Could not find version field");
        }
    }
}
