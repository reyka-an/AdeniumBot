using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Data;
using Adenium.Models;

namespace Adenium.Handlers
{
    public class ProfileCommandHandler
    {
        private readonly BotDbContextFactory _dbFactory = new();
        
        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            
            if (command.CommandName != "profile") return;

            await command.DeferAsync(ephemeral: true);

            try
            {
                var userId = (long)command.User.Id;
                var username = command.User.Username;

                await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                if (!await db.Database.CanConnectAsync(cts.Token))
                {
                    await command.FollowupAsync("У меня дела, мне некогда. Попробуй позже", ephemeral: true);
                    return;
                }

                var profile = await db.PlayerProfiles
                    .FirstOrDefaultAsync(p => p.DiscordUserId == userId);

                if (profile is null)
                {
                    profile = new PlayerProfile
                    {
                        DiscordUserId = userId,
                        Username = username,
                        Coin = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.PlayerProfiles.Add(profile);
                    await db.SaveChangesAsync();
                }

                var embed = new EmbedBuilder()
                    .WithTitle($"Профиль {command.User.Username}")
                    .WithDescription("Ага, окей, допустим")
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
