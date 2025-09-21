using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Data;

namespace Adenium.Handlers
{
    public class ProfileCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly BotDbContextFactory _dbFactory = new();

        public ProfileCommandHandler(DiscordSocketClient client)
        {
            _client = client;
        }
        
        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "profile") return;

            await command.DeferAsync(ephemeral: true);

            try
            {
                var userId = command.User.Id;

                await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                if (!await db.Database.CanConnectAsync(cts.Token))
                {
                    await command.FollowupAsync("У меня дела, мне некогда. Попробуй позже", ephemeral: true);
                    return;
                }
                
                var guildId = command.GuildId ?? (command.Channel as SocketGuildChannel)?.Guild.Id;
                if (guildId is null)
                {
                    await command.FollowupAsync("Эта команда доступна только на сервере.", ephemeral: true);
                    return;
                }
                
                var profile = await db.PlayerProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.DiscordUserId == (long)userId, cts.Token);

                if (profile is null)
                {
                    await command.FollowupAsync("Не удалось получить профиль. Попробуй позже.", ephemeral: true);
                    return;
                }

                var favCount = await db.FavoriteLinks.CountAsync(x => x.TargetId == profile.Id, cts.Token);
                var blCount  = await db.BlacklistLinks.CountAsync(x => x.TargetId == profile.Id, cts.Token);

                var embed = new EmbedBuilder()
                    .WithAuthor(command.User)
                    .WithDescription($"⭐ {profile.Exp}    ❤️  {favCount}    ❌  {blCount}")
                    .WithColor(Color.DarkGrey)
                    .WithCurrentTimestamp()
                    .Build();

                await command.FollowupAsync(embed: embed, ephemeral: true);
            }
            catch (OperationCanceledException)
            {
                await command.FollowupAsync("Все так долго что я устала, давай потом, а? ⏳", ephemeral: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PROFILE] Error: {ex}");
                await command.FollowupAsync("Посмотрела я на твой профиль и все сломалось, надеюсь ты доволен ❌", ephemeral: true);
            }
        }
    }
}
