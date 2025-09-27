using Microsoft.EntityFrameworkCore;
using AdeniumBot.Models;

namespace AdeniumBot.Data
{
    public class BotDbContext(string connectionString) : DbContext
    {
        public DbSet<RoleExpRule> RoleExpRules { get; set; } = null!;

        protected BotDbContext() : this("")
        {
        }

        public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
        public DbSet<FavoriteLink> FavoriteLinks => Set<FavoriteLink>();
        public DbSet<BlacklistLink> BlacklistLinks => Set<BlacklistLink>();
        public DbSet<Quest> Quests => Set<Quest>();
        public DbSet<PlayerQuest> PlayerQuests => Set<PlayerQuest>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseNpgsql(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // =========================
            // PlayerProfile
            // =========================
            var pp = mb.Entity<PlayerProfile>();
            pp.ToTable("player_profiles");
            pp.HasKey(p => p.Id);
            pp.HasIndex(p => p.DiscordUserId).IsUnique();

            pp.Property(p => p.Id).HasColumnName("id");
            pp.Property(p => p.DiscordUserId).HasColumnName("discord_user_id").HasColumnType("bigint");
            pp.Property(p => p.Username).HasColumnName("username").HasMaxLength(64);
            pp.Property(p => p.Exp).HasColumnName("exp");
            pp.Property(p => p.CreatedAt).HasColumnName("created_at");

            // CHECKS
            pp.ToTable(t => { t.HasCheckConstraint("ck_player_profiles_exp_nonneg", "exp >= 0"); });

            // =========================
            // FavoriteLink
            // =========================
            var fav = mb.Entity<FavoriteLink>();
            fav.ToTable("favorite_links");
            fav.HasKey(x => new { x.OwnerId, x.TargetId });
            fav.Property(x => x.OwnerId).HasColumnName("owner_id");
            fav.Property(x => x.TargetId).HasColumnName("target_id");

            fav.HasOne(x => x.Owner)
                .WithMany(p => p.Favorites)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            fav.HasOne(x => x.Target)
                .WithMany(p => p.FavoritedBy)
                .HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Restrict);

            fav.HasIndex(x => x.TargetId);

            // =========================
            // BlacklistLink
            // =========================
            var bl = mb.Entity<BlacklistLink>();
            bl.ToTable("blacklist_links");
            bl.HasKey(x => new { x.OwnerId, x.TargetId });
            bl.Property(x => x.OwnerId).HasColumnName("owner_id");
            bl.Property(x => x.TargetId).HasColumnName("target_id");

            bl.HasOne(x => x.Owner)
                .WithMany(p => p.Blacklist)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            bl.HasOne(x => x.Target)
                .WithMany(p => p.BlacklistedBy)
                .HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Restrict);

            bl.HasIndex(x => x.TargetId);

            // =========================
            // Quest
            // =========================
            var q = mb.Entity<Quest>();
            q.ToTable("quests");
            q.HasKey(x => x.Id);
            q.HasIndex(x => x.Number).IsUnique();

            q.Property(x => x.Id).HasColumnName("id");
            q.Property(x => x.Number).HasColumnName("number");
            q.Property(x => x.Description).HasColumnName("description");
            q.Property(x => x.ExpReward).HasColumnName("exp_reward");
            q.Property(x => x.MaxCompletionsPerPlayer).HasColumnName("max_completions_per_player");
            q.Property(x => x.IsActive).HasColumnName("is_active");

            q.ToTable(t =>
            {
                t.HasCheckConstraint("ck_quests_exp_reward_nonneg", "exp_reward >= 0");
                t.HasCheckConstraint(
                    "ck_quests_max_comp_valid",
                    "max_completions_per_player IS NULL OR max_completions_per_player > 0"
                );
            });

            // =========================
            // PlayerQuest
            // =========================
            var pq = mb.Entity<PlayerQuest>();
            pq.ToTable("player_quests");
            pq.HasKey(x => new { x.PlayerId, x.QuestId });

            pq.Property(x => x.PlayerId).HasColumnName("player_id");
            pq.Property(x => x.QuestId).HasColumnName("quest_id");
            pq.Property(x => x.CompletedCount).HasColumnName("completed_count");
            pq.Property(x => x.LastCompletedAt).HasColumnName("last_completed_at");

            pq.HasOne(x => x.Player)
                .WithMany(p => p.Quests)
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            pq.HasOne(x => x.Quest)
                .WithMany(q => q.PlayerQuests)
                .HasForeignKey(x => x.QuestId)
                .OnDelete(DeleteBehavior.Restrict);

            pq.ToTable(t => { t.HasCheckConstraint("ck_player_quests_completed_nonneg", "completed_count >= 0"); });

            // =========================
            // RoleExpRule
            // =========================
            var r = mb.Entity<RoleExpRule>();
            r.ToTable("role_exp_rules");
            r.HasKey(e => e.Id);
            r.HasIndex(e => new { e.GuildId, e.RoleId }).IsUnique();

            r.Property(e => e.Id).HasColumnName("id");
            r.Property(e => e.GuildId).HasColumnName("guild_id").HasColumnType("bigint");
            r.Property(e => e.RoleId).HasColumnName("role_id").HasColumnType("bigint");
            r.Property(e => e.ExpAmount).HasColumnName("exp_amount");
            r.Property(e => e.RoleName).HasColumnName("role_name").HasMaxLength(200);

            r.ToTable(t => { t.HasCheckConstraint("ck_role_exp_rules_amount_nonneg", "exp_amount >= 0"); });
        }
    }
}