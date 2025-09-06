using System.Text;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Adenium.Handlers
{

    public sealed class TopCommandHandler
    {
        private readonly DiscordSocketClient _client;

        public TopCommandHandler(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (!string.Equals(command.Data.Name, "top", StringComparison.OrdinalIgnoreCase))
                return;
            
            await command.DeferAsync(ephemeral: false);
            
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
            if (string.IsNullOrWhiteSpace(conn))
            {
                await command.FollowupAsync("⚠️ База данных недоступна");
                return;
            }
            
            using var db = new Adenium.Data.BotDbContext(conn);
            
            var top = await db.PlayerProfiles
                .AsNoTracking()
                .OrderByDescending(p => p.Exp) 
                .ThenBy(p => p.Id)              
                .Take(10)
                .Select(p => new
                {
                    UserId = p.DiscordUserId,   
                    Exp    = p.Exp,             
                    p.Username           
                })
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
