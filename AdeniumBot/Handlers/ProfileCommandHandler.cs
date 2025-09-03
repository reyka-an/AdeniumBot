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
            if (command.CommandName != "профиль") return;

            await command.DeferAsync(ephemeral: true);

            var userId = (long)command.User.Id;
            var username = command.User.Username;

            await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());

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
                .WithDescription("Профиль найден/создан. На следующем шаге выведем монеты и счётчики 🪙❤️🚫.")
                .WithColor(Color.DarkGrey)
                .WithCurrentTimestamp()
                .Build();

            await command.FollowupAsync(embed: embed, ephemeral: true);
        }
    }
}