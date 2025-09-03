using Adenium.Models;
using Microsoft.EntityFrameworkCore;

namespace Adenium.Data
{
    public class BotDbContext : DbContext
    {
        private readonly string _conn;
        public BotDbContext(string connectionString) => _conn = connectionString;

        public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
        public DbSet<FavoriteLink> FavoriteLinks => Set<FavoriteLink>();
        public DbSet<BlacklistLink> BlacklistLinks => Set<BlacklistLink>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(_conn);

        protected override void OnModelCreating(ModelBuilder mb)
        {
            var pp = mb.Entity<PlayerProfile>();
            pp.HasIndex(p => p.DiscordUserId).IsUnique();
            
            var fav = mb.Entity<FavoriteLink>();
            fav.HasKey(x => new { x.OwnerId, x.TargetId });
            fav.HasOne(x => x.Owner)
                .WithMany(p => p.Favorites)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            fav.HasOne(x => x.Target)
                .WithMany(p => p.FavoritedBy)
                .HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Restrict);
            
            var bl = mb.Entity<BlacklistLink>();
            bl.HasKey(x => new { x.OwnerId, x.TargetId });
            bl.HasOne(x => x.Owner)
                .WithMany(p => p.Blacklist)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            bl.HasOne(x => x.Target)
                .WithMany(p => p.BlacklistedBy)
                .HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}