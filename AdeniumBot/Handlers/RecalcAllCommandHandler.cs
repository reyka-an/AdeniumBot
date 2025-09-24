using Discord.WebSocket;
using AdeniumBot.Data;
using AdeniumBot.Services;
using Microsoft.EntityFrameworkCore;

namespace AdeniumBot.Handlers
{
    public class RecalcAllCommandHandler(DiscordSocketClient client)
    {
        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (!string.Equals(command.Data.Name, "recalc_all", StringComparison.OrdinalIgnoreCase))
                return;

            await command.DeferAsync(ephemeral: true);

            var guild = (command.Channel as SocketGuildChannel)?.Guild;
            if (guild == null)
            {
                await command.FollowupAsync("Гильдия не найдена.", ephemeral: true);
                return;
            }

            await using var db = new BotDbContextFactory().CreateDbContext(Array.Empty<string>());
            var helper = new HelperService(client, db);

            var profiles = await db.PlayerProfiles.ToListAsync();
            var changed = await helper.RecalculateAllProfilesAsync(guild);

            foreach (var profile in profiles)
            {
                var user = guild.GetUser((ulong)profile.DiscordUserId);
                if (user != null)
                {
                    await helper.UpdateRankRoleAsync(guild);
                }
            }

            var guildCount = guild.Users.Count;
            var dbCount = await db.PlayerProfiles.CountAsync();

            await command.FollowupAsync(
                $"✅ Роли обновлены.\n" +
                $"👥 Пользователей на сервере: **{guildCount}**\n" +
                $"📊 Профилей в БД: **{dbCount}**\n" +
                $"🔄 Пересчитано профилей: **{changed}**",
                ephemeral: true);
        }

    }
}