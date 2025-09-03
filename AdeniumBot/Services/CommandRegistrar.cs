using Discord;
using Discord.WebSocket;

namespace Adenium.Services
{
    public class CommandRegistrar
    {
        private readonly DiscordSocketClient _client;

        public CommandRegistrar(DiscordSocketClient client) => _client = client;

        public async Task OnReadyAsync()
        {
            var guildIdStr = Environment.GetEnvironmentVariable("GUILD_ID");
            if (!ulong.TryParse(guildIdStr, out var guildId))
            {
                Console.WriteLine("Задай переменную окружения GUILD_ID с ID сервера, чтобы быстро регистрировать команды.");
                return;
            }

            var guild = _client.GetGuild(guildId);
            if (guild == null)
            {
                Console.WriteLine("Бот не видит указанный сервер. Убедись, что он добавлен на сервер.");
                return;
            }

            var cmds = await guild.GetApplicationCommandsAsync();

            if (!cmds.Any(c => c.Name == "start"))
            {
                var start = new SlashCommandBuilder()
                    .WithName("start")
                    .WithDescription("Запустить набор участников и распределить на команды");
                await guild.CreateApplicationCommandAsync(start.Build());
                Console.WriteLine("Зарегистрирована команда /start");
            }
            if (!cmds.Any(c => c.Name == "профиль"))
            {
                var profile = new SlashCommandBuilder()
                    .WithName("profile")
                    .WithDescription("Открыть ваш профиль (создастся при первом вызове)");
                await guild.CreateApplicationCommandAsync(profile.Build());
                Console.WriteLine("Зарегистрирована команда /profile");
            }
        }
        
        
    }
}