using System.Text;
using System.Globalization;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using AdeniumBot.Data;
using AdeniumBot.Services;

namespace AdeniumBot.Handlers
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

            await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());

            var top = await db.PlayerProfiles
                .AsNoTracking()
                .OrderByDescending(p => p.Exp)
                .ThenBy(p => p.Id)
                .Take(10)
                .Select(p => new { UserId = p.DiscordUserId, p.Exp })
                .ToListAsync();
            
            var eb = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithCurrentTimestamp()
                .WithAuthor(a =>
                {
                    a.Name = "🏆 Топ-10 игроков";
                    a.IconUrl = _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl();
                });

            if (top.Count == 0)
            {
                eb.WithDescription("_Пока пусто._");
            }
            else
            {
                var medals = new[] { "🥇", "🥈", "🥉" };
                var sb = new StringBuilder();

                int rank = 1;
                foreach (var p in top)
                {
                    var mention = $"<@{p.UserId}>";

                    string place = rank <= 3
                        ? $"{medals[rank - 1]}"
                        : $"**{rank,2}.**";

                    string expPretty = p.Exp.ToString("N0", CultureInfo.GetCultureInfo("ru-RU"));

                    string line;
                    if (rank <= 3)
                        line = $"{place} **{mention}** — `{expPretty} EXP`";
                    else
                        line = $"{place} {mention} — `{expPretty} EXP`";
                    
                    sb.AppendLine(line);
                    sb.AppendLine();

                    rank++;
                }

                eb.WithDescription(sb.ToString());
                
            }

            await command.FollowupAsync(embed: eb.Build(), ephemeral: false);
        }
    }
}
