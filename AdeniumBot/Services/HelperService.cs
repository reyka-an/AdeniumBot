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
            _db = db ?? throw new ArgumentNullException(nameof(db));
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

        public async Task UpdateRankRoleAsync(SocketGuild guild, SocketGuildUser user, int exp, CancellationToken ct)
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

        public async Task<int> RecalculateAllProfilesWhereAsync(
            ulong guildId,
            IEnumerable<PlayerProfile> profiles,
            CancellationToken ct = default)
        
        {
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));

            var guild = _client.GetGuild(guildId);
            if (guild == null) throw new InvalidOperationException($"Guild {guildId} not found/accessible.");

            var expByRoleId = await _db.RoleExpRules
                .Where(r => r.GuildId == (long)guildId)
                .ToDictionaryAsync(r => (ulong)r.RoleId, r => r.ExpAmount, ct);

            var profileIds = profiles.Select(p => p.Id).ToArray();

            var profilesToUpdate = await _db.PlayerProfiles
                .Include(p => p.Quests)
                .ThenInclude(pq => pq.Quest)
                .Where(p => profileIds.Contains(p.Id))
                .ToListAsync(ct);

            int changed = 0;

            foreach (var profile in profilesToUpdate)
            {
                var user = guild.GetUser((ulong)profile.DiscordUserId);
                if (user == null)
                    continue;

                int rolesExp = 0;
                foreach (var role in user.Roles)
                {
                    if (expByRoleId.TryGetValue(role.Id, out var add))
                        rolesExp += add;
                }

                int questsExp = 0;
                if (profile.Quests is { Count: > 0 })
                    questsExp = profile.Quests.Sum(pq => (pq.Quest?.ExpReward ?? 0) * pq.CompletedCount);

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
        public async Task<List<PlayerProfile>> GetOrCreateProfilesAsync(
            SocketGuild guild,
            IEnumerable<ulong> userIds,
            CancellationToken ct = default)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (userIds == null) throw new ArgumentNullException(nameof(userIds));

            var ids = userIds.Select(u => (long)u).Distinct().ToArray();
            
            var profiles = await _db.PlayerProfiles
                .Where(p => ids.Contains(p.DiscordUserId))
                .ToListAsync(ct);

            var have = profiles.Select(p => p.DiscordUserId).ToHashSet();
            var missing = ids.Where(id => !have.Contains(id)).ToList();
            
            foreach (var id in missing)
            {
                var user = guild.GetUser((ulong)id);
                profiles.Add(new PlayerProfile
                {
                    DiscordUserId = id,
                    Username = user?.Username ?? string.Empty,
                    Exp = 0
                });
                _db.PlayerProfiles.Add(profiles[^1]);
            }

            if (missing.Count > 0)
                await _db.SaveChangesAsync(ct);

            return profiles;
        }
    }
}