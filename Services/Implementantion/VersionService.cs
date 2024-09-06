using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Services.Implementantion
{
    public class VersionService: IVersionService
    {
        private readonly OverflowDbContext _dbContext;
        public VersionService(OverflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public static bool IsVersionValid(string currentVersionString, string requiredVersionString)
        {
            // Parse the version strings
            Version currentVersion = new Version(currentVersionString);
            Version requiredVersion = new Version(requiredVersionString);

            // Compare versions
            return currentVersion >= requiredVersion;
        }

        public async Task<Maybe<bool>> IsGameVersionValid(string currentVersion)
        {
            var maybe = new Maybe<bool>();
            if (GameVersion.Value.IsNullOrEmpty())
            {
                var version = await _dbContext.Versions.FirstOrDefaultAsync(e => e.VersionID == 1);
                if (version != null)
                {
                    var requiredVersion = version.RequiredGameVerion;
                    maybe.SetSuccess(IsVersionValid(currentVersion, requiredVersion));
                    return maybe;
                }

                maybe.SetException("No version field found");
            }
            else
            {
                maybe.SetSuccess(IsVersionValid(currentVersion, GameVersion.Value));
            }
            
            return maybe;
        }

        public async Task<Maybe<bool>> UpdateGameVersion(string currentVersion)
        {
            var maybe = new Maybe<bool>();
            var version = await _dbContext.Versions.FirstOrDefaultAsync(e => e.VersionID == 1);
            if(version != null)
            {
                version.RequiredGameVerion = currentVersion;
                _dbContext.Update(version);
                await _dbContext.SaveChangesAsync();
                GameVersion.Value = currentVersion;
                maybe.SetSuccess(true);
            }
            else
            {
                maybe.SetException("No version field found");
            }
            return maybe;
        }
    }
}
