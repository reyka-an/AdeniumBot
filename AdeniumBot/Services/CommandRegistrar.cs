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
            
            var desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "start","profile","fav","block","rel","top","quest","relations",
                "roleexp"
            };
            
            var cmds = await guild.GetApplicationCommandsAsync();
            foreach (var existing in cmds)
            {
                if (!desired.Contains(existing.Name))
                {
                    await existing.DeleteAsync();
                    Console.WriteLine($"Удалена устаревшая гильдейская команда /{existing.Name}");
                }
            }
            
            try
            {
                var global = await _client.GetGlobalApplicationCommandsAsync();
                foreach (var gc in global)
                {
                    await gc.DeleteAsync();
                    Console.WriteLine($"Удалена глобальная команда /{gc.Name}");
                }
            }
            catch
            {
                // Если метод недоступен в используемой версии Discord.Net — тихо игнорируем.
            }
            
            cmds = await guild.GetApplicationCommandsAsync();

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

            if (!cmds.Any(c => c.Name == "top"))
            {
                var top = new SlashCommandBuilder()
                    .WithName("top")
                    .WithDescription("Показать топ-10 игроков по EXP");
                await guild.CreateApplicationCommandAsync(top.Build());
                Console.WriteLine("Зарегистрирована команда /top");
            }

            if (!cmds.Any(c => c.Name == "quest"))
            {
                var quest = new SlashCommandBuilder()
                    .WithName("quest")
                    .WithDescription("Квесты")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("done")
                        .WithDescription("Отметить выполнение квеста у игрока")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("number")
                            .WithDescription("Номер квеста")
                            .WithType(ApplicationCommandOptionType.Integer)
                            .WithRequired(true))
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("user")
                            .WithDescription("Кто выполнил квест")
                            .WithType(ApplicationCommandOptionType.User)
                            .WithRequired(true)));

                await guild.CreateApplicationCommandAsync(quest.Build());
                Console.WriteLine("Зарегистрирована команда /quest (done)");
            }

            if (!cmds.Any(c => c.Name == "relations"))
            {
                var relations = new SlashCommandBuilder()
                    .WithName("relations")
                    .WithDescription("Показать твои списки: избранные и чёрный список");

                await guild.CreateApplicationCommandAsync(relations.Build());
                Console.WriteLine("Зарегистрирована команда /relations");
            }
            if (!cmds.Any(c => c.Name == "roleexp"))
            {
                var roleexp = new SlashCommandBuilder()
                    .WithName("roleexp")
                    .WithDescription("Управление стоимостью EXP для ролей")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("set")
                        .WithDescription("Задать стоимость EXP для роли")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("role")
                            .WithDescription("Роль")
                            .WithType(ApplicationCommandOptionType.Role)
                            .WithRequired(true))
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("amount")
                            .WithDescription("Стоимость EXP")
                            .WithType(ApplicationCommandOptionType.Integer)
                            .WithRequired(true)));

                await guild.CreateApplicationCommandAsync(roleexp.Build());
                Console.WriteLine("Зарегистрирована команда /roleexp (set)");
            }
        }
    }
}
