using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Data;
using Adenium.Models;
using Adenium.Services;

namespace Adenium.Handlers
{
    public class RoleExpRulesCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private const ulong AllowedRoleId = 1412519904229327062;

        public RoleExpRulesCommandHandler(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            try
            {
                if (!string.Equals(command.Data.Name, "roleexp", StringComparison.OrdinalIgnoreCase))
                    return;

                if (command.Data.Options.FirstOrDefault()?.Name != "set")
                {
                    await command.RespondAsync("Неизвестная операция.", ephemeral: true);
                    return;
                }

                var invoker = command.User as SocketGuildUser;
                if (invoker == null || invoker.Roles.All(r => r.Id != AllowedRoleId))
                {
                    await command.RespondAsync("❌ У тебя нет прав использовать эту команду.", ephemeral: true);
                    return;
                }

                var opts = command.Data.Options.First().Options.ToDictionary(o => o.Name, o => o.Value);
                var role = (SocketRole)opts["role"];
                var amount = Convert.ToInt32(opts["amount"]);

                var guild = (command.Channel as SocketGuildChannel)?.Guild
                            ?? _client.GetGuild(role.Guild.Id);
                if (guild == null)
                {
                    await command.RespondAsync("Гильдия не найдена.", ephemeral: true);
                    return;
                }

                await command.DeferAsync(ephemeral: true);

                await using var db = new BotDbContextFactory().CreateDbContext(Array.Empty<string>());
                var helper = new HelperService(_client, db);

                var guildId = unchecked((long)guild.Id);
                var roleId = unchecked((long)role.Id);

                var rule = await db.RoleExpRules.FirstOrDefaultAsync(r => r.GuildId == guildId && r.RoleId == roleId);
                if (rule == null)
                {
                    rule = new RoleExpRule
                    {
                        GuildId = guildId,
                        RoleId = roleId,
                        RoleName = role.Name,
                        ExpAmount = amount
                    };
                    db.RoleExpRules.Add(rule);
                }
                else
                {
                    rule.ExpAmount = amount;
                    rule.RoleName = role.Name;
                }

                await db.SaveChangesAsync();

                await guild.DownloadUsersAsync();

                var userIdsWithRole = guild.Users
                    .Where(u => u.Roles.Any(r => r.Id == role.Id))
                    .Select(u => u.Id)
                    .ToArray();

                var profiles = await helper.GetOrCreateProfilesAsync(guild, userIdsWithRole);

                var changed = await helper.RecalculateAllProfilesWhereAsync(guild.Id, profiles);

                await command.FollowupAsync(
                    $"Правило для роли <@&{role.Id}> установлено: **{amount}** EXP. " +
                    $"Обновлено профилей: **{changed}**.",
                    ephemeral: true);
            }
            catch (Exception e)
            {
                await command.FollowupAsync("Ошибка сервера");
                throw;
            }
        }
    }
}