using Microsoft.EntityFrameworkCore;
using Adenium.Models;

namespace Adenium.Data
{
    public class BotDbContext : DbContext
    {
        private readonly string _conn;
        public DbSet<RoleExpRule> RoleExpRules { get; set; } = null!;

        public BotDbContext(string connectionString)
        {
            _conn = connectionString;
        }

        protected BotDbContext()
        {
            _conn = "";
        }

        public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
        public DbSet<FavoriteLink> FavoriteLinks => Set<FavoriteLink>();
        public DbSet<BlacklistLink> BlacklistLinks => Set<BlacklistLink>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseNpgsql(_conn);
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
            mb.Entity<RoleExpRule>(entity =>
            {
                entity.ToTable("role_exp_rules");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.GuildId, e.RoleId })
                    .IsUnique();

                entity.Property(e => e.GuildId).HasColumnName("guild_id").HasColumnType("bigint");
                entity.Property(e => e.RoleId).HasColumnName("role_id").HasColumnType("bigint");
                entity.Property(e => e.ExpAmount).HasColumnName("exp_amount");
            });
            var pp = mb.Entity<PlayerProfile>();
            pp.HasIndex(p => p.DiscordUserId).IsUnique();

            var fav = mb.Entity<FavoriteLink>();
            fav.HasKey(x => new { x.OwnerId, x.TargetId });
            fav.HasOne(x => x.Owner).WithMany(p => p.Favorites).HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            fav.HasOne(x => x.Target).WithMany(p => p.FavoritedBy).HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Restrict);

            var bl = mb.Entity<BlacklistLink>();
            bl.HasKey(x => new { x.OwnerId, x.TargetId });
            bl.HasOne(x => x.Owner).WithMany(p => p.Blacklist).HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            bl.HasOne(x => x.Target).WithMany(p => p.BlacklistedBy).HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}