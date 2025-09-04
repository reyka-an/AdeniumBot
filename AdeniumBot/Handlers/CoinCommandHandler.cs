using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Data;

namespace Adenium.Handlers
{
    public class CoinCommandHandler
    {
        private readonly BotDbContextFactory _dbFactory = new();

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "coin") return;

            await command.DeferAsync(ephemeral: true);
            
            if (command.GuildId is null || command.User is not SocketGuildUser guildUser)
            {
                await command.FollowupAsync("Эта команда доступна только на сервере.", ephemeral: true);
                return;
            }
            
            var roleIdStr = Environment.GetEnvironmentVariable("COIN_ROLE_ID");
            if (!ulong.TryParse(roleIdStr, out var requiredRoleId))
            {
                await command.FollowupAsync("Роль для доступа к команде не настроена. Админ, проверь COIN_ROLE_ID.", ephemeral: true);
                return;
            }

            var hasRole = guildUser.Roles.Any(r => r.Id == requiredRoleId);
            if (!hasRole)
            {
                await command.FollowupAsync("У тебя нет прав использовать эту команду.", ephemeral: true);
                return;
            }

            var allowNegative = string.Equals(
                Environment.GetEnvironmentVariable("COIN_ALLOW_NEGATIVE"),
                "true", StringComparison.OrdinalIgnoreCase);
            
            var sub = command.Data.Options.FirstOrDefault();
            if (sub is null)
            {
                await command.FollowupAsync("Используй: `/coin add|remove|set ...`", ephemeral: true);
                return;
            }

            var subName = sub.Name;
            var targetUser = sub.Options?.FirstOrDefault(o => o.Name == "user")?.Value as IUser;
            var amountOpt  = sub.Options?.FirstOrDefault(o => o.Name == "amount")?.Value;
            var valueOpt   = sub.Options?.FirstOrDefault(o => o.Name == "value")?.Value;

            if (targetUser is null)
            {
                await command.FollowupAsync("Нужно указать пользователя.", ephemeral: true);
                return;
            }

            await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());
            var targetDiscordId = unchecked((long)targetUser.Id);

            var profile = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == targetDiscordId);
            if (profile is null)
            {
                profile = new Adenium.Models.PlayerProfile
                {
                    DiscordUserId = targetDiscordId,
                    Username = targetUser.Username,
                    Coin = 0,
                    CreatedAt = DateTime.UtcNow
                };
                db.PlayerProfiles.Add(profile);
                await db.SaveChangesAsync();
            }
            else
            {
                if (!string.Equals(profile.Username, targetUser.Username, StringComparison.Ordinal))
                {
                    profile.Username = targetUser.Username;
                    await db.SaveChangesAsync();
                }
            }

            switch (subName)
            {
                case "add":
                {
                    if (amountOpt is null || !int.TryParse(amountOpt.ToString(), out var amount) || amount <= 0)
                    {
                        await command.FollowupAsync("Укажи положительное `amount`.", ephemeral: true);
                        return;
                    }

                    profile.Coin += amount;
                    await db.SaveChangesAsync();

                    await command.FollowupAsync(
                        $"💰 Начислено **{amount}** монет для {targetUser.Mention}. Теперь у него **{profile.Coin}**.",
                        ephemeral: true);
                    break;
                }
                case "remove":
                {
                    if (amountOpt is null || !int.TryParse(amountOpt.ToString(), out var amount) || amount <= 0)
                    {
                        await command.FollowupAsync("Укажи положительное `amount`.", ephemeral: true);
                        return;
                    }

                    var newValue = profile.Coin - amount;
                    if (!allowNegative && newValue < 0) newValue = 0;

                    var diff = profile.Coin - newValue;
                    profile.Coin = newValue;
                    await db.SaveChangesAsync();

                    await command.FollowupAsync(
                        $"💸 Списано **{diff}** монет у {targetUser.Mention}. Теперь у него **{profile.Coin}**.",
                        ephemeral: true);
                    break;
                }
                case "set":
                {
                    if (valueOpt is null || !int.TryParse(valueOpt.ToString(), out var value))
                    {
                        await command.FollowupAsync("Укажи целое `value`.", ephemeral: true);
                        return;
                    }

                    if (!allowNegative && value < 0) value = 0;

                    profile.Coin = value;
                    await db.SaveChangesAsync();

                    await command.FollowupAsync(
                        $"🧮 Баланс {targetUser.Mention} установлен на **{profile.Coin}**.",
                        ephemeral: true);
                    break;
                }
                default:
                    await command.FollowupAsync("Я тебя не понимаю", ephemeral: true);
                    break;
            }
        }
    }
}
