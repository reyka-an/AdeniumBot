using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Data;
using Adenium.Models;

namespace Adenium.Handlers
{
    public class ExpCommandHandler
    {
        private readonly BotDbContextFactory _dbFactory = new();

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "exp") return;

            await command.DeferAsync(ephemeral: true);
            if (command.GuildId is null || command.User is not SocketGuildUser guildUser)
            {
                await command.FollowupAsync("Эта команда доступна только на сервере.", ephemeral: true);
                return;
            }
            
            var roleIdStr = Environment.GetEnvironmentVariable("EXP_ROLE_ID");
            if (!ulong.TryParse(roleIdStr, out var requiredRoleId))
            {
                await command.FollowupAsync("Роль для доступа к команде не настроена. Админ, проверь EXP_ROLE_ID.", ephemeral: true);
                return;
            }

            var hasRole = guildUser.Roles.Any(r => r.Id == requiredRoleId);
            if (!hasRole)
            {
                await command.FollowupAsync("У тебя нет прав использовать эту команду.", ephemeral: true);
                return;
            }

            var allowNegative = string.Equals(
                Environment.GetEnvironmentVariable("EXP_ALLOW_NEGATIVE"),
                "true", StringComparison.OrdinalIgnoreCase);
            
            var sub = command.Data.Options.FirstOrDefault();
            if (sub is null)
            {
                await command.FollowupAsync("Используй: `/exp add|remove|set ...`", ephemeral: true);
                return;
            }

            var subName   = sub.Name;
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
                profile = new PlayerProfile
                {
                    DiscordUserId = targetDiscordId,
                    Username = targetUser.Username,
                    Exp = 0,                        
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

                    profile.Exp += amount;         
                    await db.SaveChangesAsync();

                    await command.FollowupAsync(
                        $"⭐ Начислено **{amount}** опыта для {targetUser.Mention}. Теперь у него **{profile.Exp}**.",
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

                    var newValue = profile.Exp - amount;  
                    if (!allowNegative && newValue < 0) newValue = 0;

                    var diff = profile.Exp - newValue;    
                    profile.Exp = newValue;              
                    await db.SaveChangesAsync();

                    await command.FollowupAsync(
                        $"➖ Списано **{diff}** опыта у {targetUser.Mention}. Теперь у него **{profile.Exp}**.",
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

                    profile.Exp = value;                
                    await db.SaveChangesAsync();

                    await command.FollowupAsync(
                        $"🧮 Опыт {targetUser.Mention} установлен на **{profile.Exp}**.",
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
