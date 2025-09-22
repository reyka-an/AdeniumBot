using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using AdeniumBot.Data;
using AdeniumBot.Services;

namespace AdeniumBot.Handlers
{
    public class RecalcAllCommandHandler
    {
        private readonly DiscordSocketClient _client;

        public RecalcAllCommandHandler(DiscordSocketClient client)
        {
            _client = client;
        }

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
            var helper = new HelperService(_client, db);
            
            var profiles = await db.PlayerProfiles.ToListAsync();
            
            var changed = await helper.RecalculateAllProfilesWhereAsync(guild.Id, profiles);
            
            foreach (var profile in profiles)
            {
                var user = guild.GetUser((ulong)profile.DiscordUserId);
                if (user != null)
                {
                    await helper.UpdateRankRoleAsync(guild);
                }
            }

            await command.FollowupAsync(
                $"✅ Профили обновлены.",
                ephemeral: true);
        }
    }
}