using Discord;
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
        
        private static readonly (int Exp, ulong RoleId)[] _rankRules =
        {
            (550, 1412853590988423178),
            (350, 1413715537489297520),
            (200, 1412567641943703804),
            (100, 1403503913037991988),
        };

        public HelperService(DiscordSocketClient client, BotDbContext db)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _db     = db     ?? throw new ArgumentNullException(nameof(db));
        }
        
        private SocketRole? GetTargetRankRole(SocketGuild guild, int exp)
        {
            foreach (var (needExp, roleId) in _rankRules.OrderByDescending(r => r.Exp))
            {
                if (exp >= needExp)
                    return guild.GetRole(roleId);
            }
            return null;
        }
        
        private async Task EnsureRankRolesAsync(SocketGuild guild, SocketGuildUser user, int exp, CancellationToken ct)
        {
            var targetRole = GetTargetRankRole(guild, exp);
            var rankRoleIds = _rankRules.Select(r => r.RoleId).ToHashSet();

            var currentRankRoles = user.Roles.Where(r => rankRoleIds.Contains(r.Id)).ToList();
            
            bool hasTarget = targetRole != null && currentRankRoles.Any(r => r.Id == targetRole.Id);
            bool extraRolesExist = currentRankRoles.Any(r => targetRole == null || r.Id != targetRole.Id);

            if (!hasTarget && targetRole != null)
            {
                if (guild.CurrentUser.GuildPermissions.ManageRoles &&
                    guild.CurrentUser.Hierarchy > targetRole.Position)
                {
                    await user.AddRoleAsync(targetRole, new RequestOptions { CancelToken = ct });
                }
            }

            if (extraRolesExist)
            {

                var toRemove = currentRankRoles.Where(r => targetRole == null || r.Id != targetRole.Id).ToArray();

                toRemove = toRemove
                    .Where(r => guild.CurrentUser.Hierarchy > r.Position)
                    .ToArray();

                if (toRemove.Length > 0 && guild.CurrentUser.GuildPermissions.ManageRoles)
                {
                    await user.RemoveRolesAsync(toRemove, new RequestOptions { CancelToken = ct });
                }
            }
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
                
                await EnsureRankRolesAsync(guild, user, newTotalExp, ct);
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
            
            await EnsureRankRolesAsync(guild, user, profile.Exp, ct);

            await _db.SaveChangesAsync(ct);
        }
    }
}
