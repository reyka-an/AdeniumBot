using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using AdeniumBot.Data;
using AdeniumBot.Models;

namespace AdeniumBot.Services
{
    public class HelperService
    {
        private readonly DiscordSocketClient _client;
        private readonly BotDbContext _db;

        private static readonly (int Exp, ulong RoleId)[] RankRules =
        {
            (550, 1412853590988423178), //Призматическая ауга
            (350, 1413715537489297520), //Алмазная ауга
            (200, 1412567641943703804), //Изумрудная ауга
            (100, 1403503913037991988), //Золотая ауга
        };

        public HelperService(DiscordSocketClient client, BotDbContext db)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }
        
        /// <summary>
        /// Возвращает ранговую роль в зависимости от переданного опыта
        /// </summary>
        private SocketRole? GetTargetRankRole(SocketGuild guild, int exp)
        {
            foreach (var (needExp, roleId) in RankRules.OrderByDescending(r => r.Exp))
            {
                if (exp >= needExp)
                    return guild.GetRole(roleId);
            }

            return null;
        }

        /// <summary>
        /// Заменяет ранговую роль (Призматическая ауга, Алмазная ауга...) у всех пользователей в зависимости их опыта
        /// </summary>
        public async Task UpdateRankRoleAsync(SocketGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));

            // Роли рангов для быстрого фильтра
            var rankRoleIds = RankRules.Select(r => r.RoleId).ToHashSet();

            // Забираем EXP из БД только для пользователей гильдии
            var guildUserIds = guild.Users.Select(u => (long)u.Id).ToArray();
            
            //TODO Добавить метод создания профилей пользователей для всех участников сервера которых нет в БД
            
            var profiles = await _db.PlayerProfiles
                .Where(p => guildUserIds.Contains(p.DiscordUserId))
                .Select(p => new { p.DiscordUserId, p.Exp })
                .ToListAsync();

            var expByUserId = profiles.ToDictionary(p => (ulong)p.DiscordUserId, p => p.Exp);

            // Пройдём по всем участникам гильдии и обновим ранговые роли
            foreach (var user in guild.Users)
            {
                if (!expByUserId.TryGetValue(user.Id, out var exp))
                    continue; // нет профиля — пропускаем

                var targetRole = GetTargetRankRole(guild, exp);
                var currentRankRoles = user.Roles.Where(r => rankRoleIds.Contains(r.Id)).ToList();

                bool hasTarget = targetRole != null && currentRankRoles.Any(r => r.Id == targetRole.Id);
                bool extraRolesExist = currentRankRoles.Any(r => targetRole == null || r.Id != targetRole.Id);

                // Добавляем нужную ранговую роль, если её ещё нет
                if (!hasTarget && targetRole != null)
                {
                    if (guild.CurrentUser.GuildPermissions.ManageRoles &&
                        guild.CurrentUser.Hierarchy > targetRole.Position)
                    {
                        await user.AddRoleAsync(targetRole);
                    }
                }

                // Удаляем лишние ранговые роли (оставляем только целевую)
                if (extraRolesExist)
                {
                    var toRemove = currentRankRoles
                        .Where(r => targetRole == null || r.Id != targetRole.Id)
                        .Where(r => guild.CurrentUser.Hierarchy > r.Position)
                        .ToArray();

                    if (toRemove.Length > 0 && guild.CurrentUser.GuildPermissions.ManageRoles)
                    {
                        await user.RemoveRolesAsync(toRemove);
                    }
                }
            }
        }


        public async Task<int> RecalculateAllProfilesWhereAsync(ulong guildId, IEnumerable<PlayerProfile> profiles,
            CancellationToken ct = default)
        {
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));

            var guild = _client.GetGuild(guildId);
            if (guild == null) throw new InvalidOperationException($"Сервер не найден");

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

        public async Task<List<PlayerProfile>> GetOrCreateProfilesAsync(SocketGuild guild, IEnumerable<ulong> userIds,
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