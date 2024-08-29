using Microsoft.EntityFrameworkCore;
using OverflowBackend.Models.DB;

namespace OverflowBackend.Services
{
    public class OverflowDbContext : DbContext
    {
        public OverflowDbContext(DbContextOptions<OverflowDbContext> options) : base(options)
        { }
            public DbSet<DBUser> Users { get; set; }
            public DbSet<DBFriend> Friends { get; set; }
            public DbSet<DBGameInvitation> GameInvitations { get; set; }
    }
}
