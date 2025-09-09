using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Data;
using Adenium.Models;

namespace Adenium.Services
{
    public class HelperService
    {
        private readonly DiscordSocketClient _client;
        private readonly BotDbContext _db;

        public HelperService(DiscordSocketClient client, BotDbContext db)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _db     = db     ?? throw new ArgumentNullException(nameof(db));
        }
        public async Task<int> RecalculateAllProfilesAsync(ulong guildId, CancellationToken ct = default)
        {
            var guild = _client.GetGuild(guildId);
            if (guild == null) throw new InvalidOperationException($"Guild {guildId} not found/accessible.");
            
            var roleRules = await _db.RoleExpRules
                .Where(r => r.GuildId == (long)guildId)
                .ToListAsync(ct);

            var expByRoleId = roleRules.ToDictionary(r => (ulong)r.RoleId, r => r.ExpAmount);
            
            var allProfiles = await _db.PlayerProfiles
                .Include(p => p.Quests)
                    .ThenInclude(pq => pq.Quest)
                .ToDictionaryAsync(p => (ulong)p.DiscordUserId, ct);

            int changed = 0;

            foreach (var user in guild.Users)
            {
                int rolesExp = 0;
                foreach (var role in user.Roles)
                {
                    if (expByRoleId.TryGetValue(role.Id, out var add))
                        rolesExp += add;
                }
                
                if (!allProfiles.TryGetValue(user.Id, out var profile))
                {
                    profile = new PlayerProfile
                    {
                        DiscordUserId = (long)user.Id,
                        Username      = user.Username + (string.IsNullOrEmpty(user.Discriminator) || user.Discriminator == "0000"
                                            ? "" : $"#{user.Discriminator}"),
                        CreatedAt     = DateTime.UtcNow
                    };
                    _db.PlayerProfiles.Add(profile);
                    allProfiles[user.Id] = profile;
                }
                else
                {
                    var newUsername = user.Username + (string.IsNullOrEmpty(user.Discriminator) || user.Discriminator == "0000"
                                        ? "" : $"#{user.Discriminator}");
                    if (!string.Equals(profile.Username, newUsername, StringComparison.Ordinal))
                        profile.Username = newUsername;
                }

                int questsExp = 0;
                if (profile.Quests != null && profile.Quests.Count > 0)
                {
                    questsExp = profile.Quests.Sum(pq => (pq.Quest?.ExpReward ?? 0) * pq.CompletedCount);
                }

                var newTotalExp = rolesExp + questsExp;
                if (profile.Exp != newTotalExp)
                {
                    profile.Exp = newTotalExp;
                    changed++;
                }
            }

            await _db.SaveChangesAsync(ct);
            return changed;
        }
        
        public async Task RecalculateProfileAsync(ulong guildId, ulong discordUserId, CancellationToken ct = default)
        {
            var guild = _client.GetGuild(guildId);
            if (guild == null) throw new InvalidOperationException($"Guild {guildId} not found/accessible.");

            var user = guild.GetUser(discordUserId);
            if (user == null) throw new InvalidOperationException($"User {discordUserId} not found in guild {guildId}.");
            
            var expByRoleId = await _db.RoleExpRules
                .Where(r => r.GuildId == (long)guildId)
                .ToDictionaryAsync(r => (ulong)r.RoleId, r => r.ExpAmount, ct);

            int rolesExp = 0;
            foreach (var role in user.Roles)
            {
                if (expByRoleId.TryGetValue(role.Id, out var add))
                    rolesExp += add;
            }
            
            var profile = await _db.PlayerProfiles
                .Include(p => p.Quests)
                    .ThenInclude(pq => pq.Quest)
                .FirstOrDefaultAsync(p => p.DiscordUserId == (long)discordUserId, ct);

            if (profile == null)
            {
                profile = new PlayerProfile
                {
                    DiscordUserId = (long)discordUserId,
                    Username      = user.Username + (string.IsNullOrEmpty(user.Discriminator) || user.Discriminator == "0000"
                                        ? "" : $"#{user.Discriminator}"),
                    CreatedAt     = DateTime.UtcNow
                };
                _db.PlayerProfiles.Add(profile);
            }
            else
            {
                var newUsername = user.Username + (string.IsNullOrEmpty(user.Discriminator) || user.Discriminator == "0000"
                                    ? "" : $"#{user.Discriminator}");
                if (!string.Equals(profile.Username, newUsername, StringComparison.Ordinal))
                    profile.Username = newUsername;
            }

            int questsExp = 0;
            if (profile.Quests != null && profile.Quests.Count > 0)
            {
                questsExp = profile.Quests.Sum(pq => (pq.Quest?.ExpReward ?? 0) * pq.CompletedCount);
            }

            profile.Exp = rolesExp + questsExp;

            await _db.SaveChangesAsync(ct);
        }
    }
}
