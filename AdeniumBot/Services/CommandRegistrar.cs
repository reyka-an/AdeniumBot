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
                Console.WriteLine(
                    "Задай переменную окружения GUILD_ID с ID сервера, чтобы быстро регистрировать команды.");
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

            if (!cmds.Any(c => c.Name == "profile"))
            {
                var profile = new SlashCommandBuilder()
                    .WithName("profile")
                    .WithDescription("Открыть ваш профиль");
                await guild.CreateApplicationCommandAsync(profile.Build());
                Console.WriteLine("Зарегистрирована команда /profile");
            }

            if (!cmds.Any(c => c.Name == "fav"))
            {
                var fav = new SlashCommandBuilder()
                    .WithName("fav")
                    .WithDescription("Добавить игрока в избранное")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("user")
                        .WithDescription("Думаешь это взаимно?")
                        .WithType(ApplicationCommandOptionType.User)
                        .WithRequired(true));
                await guild.CreateApplicationCommandAsync(fav.Build());
                Console.WriteLine("Зарегистрирована команда /fav");
            }

            if (!cmds.Any(c => c.Name == "block"))
            {
                var block = new SlashCommandBuilder()
                    .WithName("block")
                    .WithDescription("Добавить игрока в черный список")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("user")
                        .WithDescription("Больше он тебя не побеспокоит")
                        .WithType(ApplicationCommandOptionType.User)
                        .WithRequired(true));
                await guild.CreateApplicationCommandAsync(block.Build());
                Console.WriteLine("Зарегистрирована команда /block");
            }

            if (!cmds.Any(c => c.Name == "rel"))
            {
                var rel = new SlashCommandBuilder()
                    .WithName("rel")
                    .WithDescription("Manage relations (remove)")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("unfav")
                        .WithDescription("Удалить пользователя из избранного")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("user")
                            .WithDescription("Любовь прошла?")
                            .WithType(ApplicationCommandOptionType.User)
                            .WithRequired(true)))
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("unblock")
                        .WithDescription("Удалить пользователя из черного списка")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("user")
                            .WithDescription("Надеюсь ты уверен в своем решении")
                            .WithType(ApplicationCommandOptionType.User)
                            .WithRequired(true)));

                await guild.CreateApplicationCommandAsync(rel.Build());
                Console.WriteLine("Зарегистрирована команда /rel (unfav, unblock)");
            }

            if (!cmds.Any(c => c.Name == "exp"))
            {
                var exp = new SlashCommandBuilder()
                    .WithName("exp")
                    .WithDescription("Управление монетами игроков (только для роли)")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("add")
                        .WithDescription("Начислить монеты игроку")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("user")
                            .WithDescription("Кому начислить")
                            .WithType(ApplicationCommandOptionType.User)
                            .WithRequired(true))
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("amount")
                            .WithDescription("Сколько монет добавить")
                            .WithType(ApplicationCommandOptionType.Integer)
                            .WithRequired(true)))
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("remove")
                        .WithDescription("Забрать монеты у игрока")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("user")
                            .WithDescription("У кого забрать")
                            .WithType(ApplicationCommandOptionType.User)
                            .WithRequired(true))
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("amount")
                            .WithDescription("Сколько монет забрать")
                            .WithType(ApplicationCommandOptionType.Integer)
                            .WithRequired(true)))
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("set")
                        .WithDescription("Установить точный баланс игрока")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("user")
                            .WithDescription("Кому установить баланс")
                            .WithType(ApplicationCommandOptionType.User)
                            .WithRequired(true))
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("value")
                            .WithDescription("Новое значение баланса")
                            .WithType(ApplicationCommandOptionType.Integer)
                            .WithRequired(true)));

                await guild.CreateApplicationCommandAsync(exp.Build());
                Console.WriteLine("Зарегистрирована команда /exp (add, remove, set)");
            }
        }
    }
}