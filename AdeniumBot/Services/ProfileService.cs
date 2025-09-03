using Adenium.Data;
using Adenium.Models;
using Microsoft.EntityFrameworkCore;

namespace Adenium.Services
{
    public interface IProfileService
    {
        Task<PlayerProfile> GetOrCreateAsync(ulong userId, string username);
        Task<PlayerProfile?> FindByDiscordIdAsync(ulong userId);

        Task AddFavoriteAsync(ulong ownerDiscordId, ulong targetDiscordId);
        Task RemoveFavoriteAsync(ulong ownerDiscordId, ulong targetDiscordId);

        Task AddBlacklistAsync(ulong ownerDiscordId, ulong targetDiscordId);
        Task RemoveBlacklistAsync(ulong ownerDiscordId, ulong targetDiscordId);

        Task<PlayerProfile?> GetWithRelationsAsync(ulong userId);
    }

    public class ProfileService : IProfileService
    {
        private readonly string _conn;
        public ProfileService(string connectionString) => _conn = connectionString;

        private static long ToLong(ulong x) => unchecked((long)x);

        public async Task<PlayerProfile> GetOrCreateAsync(ulong userId, string username)
        {
            await using var db = new BotDbContext(_conn);
            await db.Database.MigrateAsync();

            var lid = ToLong(userId);

            var existing = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == lid);
            if (existing != null)
            {
                if (!string.Equals(existing.Username, username, StringComparison.Ordinal))
                {
                    existing.Username = username;
                    await db.SaveChangesAsync();
                }
                return existing;
            }

            var profile = new PlayerProfile
            {
                DiscordUserId = lid,
                Username = username
            };
            db.PlayerProfiles.Add(profile);
            await db.SaveChangesAsync();
            return profile;
        }

        public async Task<PlayerProfile?> FindByDiscordIdAsync(ulong userId)
        {
            await using var db = new BotDbContext(_conn);
            var lid = ToLong(userId);
            return await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == lid);
        }

        public async Task<PlayerProfile?> GetWithRelationsAsync(ulong userId)
        {
            await using var db = new BotDbContext(_conn);
            var lid = ToLong(userId);
            return await db.PlayerProfiles
                .Include(p => p.Favorites).ThenInclude(l => l.Target)
                .Include(p => p.Blacklist).ThenInclude(l => l.Target)
                .FirstOrDefaultAsync(p => p.DiscordUserId == lid);
        }

        public async Task AddFavoriteAsync(ulong ownerDiscordId, ulong targetDiscordId)
        {
            if (ownerDiscordId == targetDiscordId) return;
            await using var db = new BotDbContext(_conn);
            var owner = await EnsureProfileAsync(db, ownerDiscordId);
            var target = await EnsureProfileAsync(db, targetDiscordId);

            var exists = await db.FavoriteLinks.AnyAsync(x => x.OwnerId == owner.Id && x.TargetId == target.Id);
            if (!exists)
            {
                db.FavoriteLinks.Add(new FavoriteLink { OwnerId = owner.Id, TargetId = target.Id });
                await db.SaveChangesAsync();
            }
        }

        public async Task RemoveFavoriteAsync(ulong ownerDiscordId, ulong targetDiscordId)
        {
            await using var db = new BotDbContext(_conn);
            var owner = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == ToLong(ownerDiscordId));
            var target = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == ToLong(targetDiscordId));
            if (owner == null || target == null) return;

            var link = await db.FavoriteLinks.FindAsync(owner.Id, target.Id);
            if (link != null)
            {
                db.FavoriteLinks.Remove(link);
                await db.SaveChangesAsync();
            }
        }

        public async Task AddBlacklistAsync(ulong ownerDiscordId, ulong targetDiscordId)
        {
            if (ownerDiscordId == targetDiscordId) return;
            await using var db = new BotDbContext(_conn);
            var owner = await EnsureProfileAsync(db, ownerDiscordId);
            var target = await EnsureProfileAsync(db, targetDiscordId);

            var exists = await db.BlacklistLinks.AnyAsync(x => x.OwnerId == owner.Id && x.TargetId == target.Id);
            if (!exists)
            {
                db.BlacklistLinks.Add(new BlacklistLink { OwnerId = owner.Id, TargetId = target.Id });
                await db.SaveChangesAsync();
            }
        }

        public async Task RemoveBlacklistAsync(ulong ownerDiscordId, ulong targetDiscordId)
        {
            await using var db = new BotDbContext(_conn);
            var owner = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == ToLong(ownerDiscordId));
            var target = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == ToLong(targetDiscordId));
            if (owner == null || target == null) return;

            var link = await db.BlacklistLinks.FindAsync(owner.Id, target.Id);
            if (link != null)
            {
                db.BlacklistLinks.Remove(link);
                await db.SaveChangesAsync();
            }
        }

        private static async Task<PlayerProfile> EnsureProfileAsync(BotDbContext db, ulong discordUserId)
        {
            var lid = ToLong(discordUserId);
            var p = await db.PlayerProfiles.FirstOrDefaultAsync(x => x.DiscordUserId == lid);
            if (p != null) return p;

            p = new PlayerProfile { DiscordUserId = lid, Username = $"user-{discordUserId}" };
            db.PlayerProfiles.Add(p);
            await db.SaveChangesAsync();
            return p;
        }
    }
}
