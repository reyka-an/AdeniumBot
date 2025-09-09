using System.Text;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Data;
using Adenium.Services;

namespace Adenium.Handlers
{
    public sealed class TopCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly BotDbContextFactory _dbFactory = new();

        public TopCommandHandler(DiscordSocketClient client) => _client = client;

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (!string.Equals(command.Data.Name, "top", StringComparison.OrdinalIgnoreCase))
                return;

            await command.DeferAsync(ephemeral: false);
            
            var guild = (command.Channel as SocketGuildChannel)?.Guild;
            ulong? guildId = guild?.Id;
            
            IReadOnlyDictionary<long, IReadOnlyCollection<long>> userRolesMap =
                new Dictionary<long, IReadOnlyCollection<long>>();

            if (guild != null)
            {
                var map = new Dictionary<long, IReadOnlyCollection<long>>(capacity: guild.Users.Count);

                foreach (var u in guild.Users)
                {
                    var discordUserId = unchecked((long)u.Id);
                    var roleIds = u.Roles.Select(r => unchecked((long)r.Id)).ToArray();
                    map[discordUserId] = roleIds;
                }

                userRolesMap = map;
            }

            await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());
            
            if (userRolesMap.Count > 0)
            {
                await HelperService.RecalculateAllAsync(db, unchecked((long)guildId!.Value), userRolesMap);
            }
            
            var top = await db.PlayerProfiles
                .AsNoTracking()
                .OrderByDescending(p => p.Exp)
                .ThenBy(p => p.Id)
                .Take(10)
                .Select(p => new { UserId = p.DiscordUserId, p.Exp, p.Username })
                .ToListAsync();

            var eb = new EmbedBuilder()
                .WithTitle("🏆 Топ-10")
                .WithColor(Color.Gold)
                .WithCurrentTimestamp();

            if (top.Count == 0)
            {
                eb.WithDescription("_Пока пусто._");
            }
            else
            {
                var sb = new StringBuilder();
                int rank = 1;
                foreach (var p in top)
                {
                    var mention = $"<@{p.UserId}>";
                    var name = string.IsNullOrWhiteSpace(p.Username) ? mention : $"{p.Username} ({mention})";
                    sb.AppendLine($"**{rank,2}.** {name} — `{p.Exp}` EXP");
                    rank++;
                }

                eb.WithDescription(sb.ToString());
            }

            await command.FollowupAsync(embed: eb.Build(), ephemeral: false);
        }
    }
}
