using Adenium.Data;
using Adenium.Models;
using Microsoft.EntityFrameworkCore;


namespace Adenium.Services
{
    public static class HelperService
    {
        public static async Task<int> RecalculateAllAsync(
            BotDbContext db,
            long guildId,
            IReadOnlyDictionary<long, IReadOnlyCollection<long>> userRoles,
            CancellationToken ct = default)
        {
            var profiles = await db.PlayerProfiles.AsNoTracking().ToListAsync(ct);

            var rules = await db.RoleExpRules
                .Where(r => r.GuildId == guildId)
                .AsNoTracking()
                .ToListAsync(ct);
            
            var allPlayerQuests = await db.PlayerQuests
                .AsNoTracking()
                .Include(pq => pq.Quest)
                .ToListAsync(ct);
            
            var questExpByPlayerId = allPlayerQuests
                .GroupBy(pq => pq.PlayerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(pq => pq.CompletedCount * (pq.Quest?.ExpReward ?? 0))
                );
            
            var profileByDiscordId = profiles.ToDictionary(p => p.DiscordUserId);

            int updated = 0;

            foreach (var kv in userRoles)
            {
                var discordUserId = kv.Key;
                var rolesNow = kv.Value;

                if (!profileByDiscordId.TryGetValue(discordUserId, out var profile))
                {
                    continue;
                }
                
                var roleExp = rules
                    .Where(r => rolesNow.Contains(r.RoleId))
                    .Sum(r => r.ExpAmount);
                
                questExpByPlayerId.TryGetValue(profile.Id, out var questExp);

                var expectedExp = roleExp + questExp;

                if (profile.Exp != expectedExp)
                {
                    var stub = new PlayerProfile { Id = profile.Id, Exp = expectedExp };
                    db.PlayerProfiles.Attach(stub);
                    db.Entry(stub).Property(p => p.Exp).IsModified = true;
                    updated++;
                }
            }

            if (updated > 0)
                await db.SaveChangesAsync(ct);

            return updated;
        }
        

        public static async Task<bool> RecalculateOneAsync(
            BotDbContext db,
            long guildId,
            long discordUserId,
            IReadOnlyCollection<long> rolesNow,
            CancellationToken ct = default)
        {
            var updated = await RecalculateAllAsync(
                db,
                guildId,
                new Dictionary<long, IReadOnlyCollection<long>>
                {
                    [discordUserId] = rolesNow
                },
                ct);

            return updated > 0;
        }
    }
}