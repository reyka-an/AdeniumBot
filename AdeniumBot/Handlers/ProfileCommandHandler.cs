using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Data;
using Adenium.Models;
using Adenium.Services; // <-- не забудь пространство имён для HelperService

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
                var userId = unchecked((long)command.User.Id);
                var username = command.User.Username;

                await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                if (!await db.Database.CanConnectAsync(cts.Token))
                {
                    await command.FollowupAsync("У меня дела, мне некогда. Попробуй позже", ephemeral: true);
                    return;
                }

                var profile = await db.PlayerProfiles
                    .FirstOrDefaultAsync(p => p.DiscordUserId == userId, cts.Token);

                if (profile is null)
                {
                    profile = new PlayerProfile
                    {
                        DiscordUserId = userId,
                        Username = username,
                        Exp = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.PlayerProfiles.Add(profile);
                    await db.SaveChangesAsync(cts.Token);
                }

                if (profile.Username != username)
                {
                    profile.Username = username;
                    await db.SaveChangesAsync(cts.Token);
                }
                
                var guild = (command.Channel as SocketGuildChannel)?.Guild;
                var gUser = command.User as SocketGuildUser;

                if (guild != null && gUser != null)
                {
                    var guildId = unchecked((long)guild.Id);
                    var rolesNow = gUser.Roles.Select(r => unchecked((long)r.Id)).ToArray();
                    
                    await HelperService.RecalculateOneAsync(
                        db,
                        guildId,
                        userId,
                        rolesNow,
                        cts.Token
                    );
                    
                    await db.Entry(profile).ReloadAsync(cts.Token);
                }

                var favCount = await db.FavoriteLinks.CountAsync(x => x.TargetId == profile.Id, cts.Token);
                var blCount  = await db.BlacklistLinks.CountAsync(x => x.TargetId == profile.Id, cts.Token);
                
                var exp = profile.Exp;         
                
                var embed = new EmbedBuilder()
                    .WithAuthor(command.User)
                    .WithDescription($"⭐ {exp}    ❤️  {favCount}    ❌  {blCount}")
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
