﻿using Microsoft.EntityFrameworkCore;
using OverflowBackend.Models.DB;

namespace OverflowBackend.Services
{
    public class OverflowDbContext : DbContext
    {
        public OverflowDbContext(DbContextOptions<OverflowDbContext> options) : base(options)
        { }
            public DbSet<DBUser> Users { get; set; }
            public DbSet<DBGuestUser> GuestUsers { get; set; }
            public DbSet<DBFriend> Friends { get; set; }
            public DbSet<DBVersion> Versions { get; set; }
            public DbSet<DBUserSession> UserSessions { get; set; }
            public DbSet<DbBlocked> BlockedUsers { get; set; }
            public DbSet<DbReport> UserReports { get; set; }
            public DbSet<DBSkin> Skins { get; set; }
            public DbSet<DBOwnedSkins> OwnedSkins { get; set; }
    }
}
