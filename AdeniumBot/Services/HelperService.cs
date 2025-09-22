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

        /// <summary>
        /// Пересчитывает опыт у всех пользователей, с учетом опыта за квесты и роли
        /// </summary>
        public async Task<int> RecalculateAllProfilesAsync(SocketGuild guild, CancellationToken ct = default)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));

            var guildIdLong = unchecked((long)guild.Id);
            
            var expByRoleId = await _db.RoleExpRules
                .Where(r => r.GuildId == guildIdLong)
                .ToDictionaryAsync(r => (ulong)r.RoleId, r => r.ExpAmount, ct);
            
            var guildUserIds = guild.Users
                .Select(u => (long)u.Id)
                .Distinct()
                .ToArray();

            if (guildUserIds.Length == 0)
                return 0;

            // Подтягиваем существующие профили и квесты только для пользователей этой гильдии
            var profiles = await _db.PlayerProfiles
                .Include(p => p.Quests)
                .ThenInclude(pq => pq.Quest)
                .Where(p => guildUserIds.Contains(p.DiscordUserId))
                .ToListAsync(ct);

            // Пересчёт EXP для всех профилей роли + квесты
            int changed = 0;

            foreach (var profile in profiles)
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

                var newTotal = rolesExp + questsExp;
                if (profile.Exp != newTotal)
                {
                    profile.Exp = newTotal;
                    changed++;
                }
            }
            await _db.SaveChangesAsync(ct);
            return changed;
        }

        /// <summary>
        /// Возвращает профили всех пользователей сервера, и создает их, если их нет в БД
        /// </summary>
        public async Task<List<PlayerProfile>> GetOrCreateProfilesAsync(SocketGuild guild,
            CancellationToken ct = default)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));

            var guildUserIds = guild.Users
                .Select(u => (long)u.Id)
                .Distinct()
                .ToArray();

            if (guildUserIds.Length == 0)
                return new List<PlayerProfile>();

            var existing = await _db.PlayerProfiles
                .Where(p => guildUserIds.Contains(p.DiscordUserId))
                .ToListAsync(ct);

            var existingById = existing.ToDictionary(p => p.DiscordUserId, p => p);

            // Создаём недостающие профили
            var toCreate = new List<PlayerProfile>(capacity: Math.Max(0, guildUserIds.Length - existing.Count));

            foreach (var user in guild.Users)
            {
                var id = (long)user.Id;

                if (!existingById.TryGetValue(id, out var profile))
                {
                    profile = new PlayerProfile
                    {
                        DiscordUserId = id,
                        Username = user.Username,
                        Exp = 0
                    };
                    toCreate.Add(profile);
                    existingById[id] = profile;
                }
                else
                {
                    // Для существующих пользователей синхронизируем Username
                    if (!string.Equals(profile.Username, user.Username, StringComparison.Ordinal))
                        profile.Username = user.Username;
                }
            }

            if (toCreate.Count > 0)
                await _db.PlayerProfiles.AddRangeAsync(toCreate, ct);

            if (toCreate.Count > 0 || existing.Any(p =>
                    p.Username != (guild.GetUser((ulong)p.DiscordUserId)?.Username ?? p.Username)))
                await _db.SaveChangesAsync(ct);

            // Возвращаем профили для всех пользователей сервера
            return existingById.Values.ToList();
        }
    }
}