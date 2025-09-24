using Discord.WebSocket;
using AdeniumBot.Data;
using AdeniumBot.Services;

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
                await command.FollowupAsync("Сервер не найден", ephemeral: true);
                return;
            }


            await using var db = new BotDbContextFactory().CreateDbContext(Array.Empty<string>());
            var helper = new HelperService(client, db);
            int userCount = guild.Users.Count;
            int dbCount = db.PlayerProfiles.Count();        
            
            await helper.GetOrCreateProfilesAsync(guild);
            int changed = await helper.RecalculateAllProfilesAsync(guild);
            await helper.UpdateRankRoleAsync(guild);
            
            await command.FollowupAsync(
                $"✅ Роли обновлены.\n" +
                $"👥 Пользователей на сервере: **{userCount}**\n" +
                $"📊 Профилей в БД: **{dbCount}**\n" +
                $"🔄 Пересчитано профилей: **{changed}**",
                ephemeral: true);
        }
    }
}