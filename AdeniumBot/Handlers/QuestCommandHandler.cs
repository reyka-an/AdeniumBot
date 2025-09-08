using Discord;
using Discord.WebSocket;
using Adenium.Services;

namespace Adenium.Handlers
{
    public class QuestCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly IQuestService _quests;

        public QuestCommandHandler(DiscordSocketClient client, IQuestService quests)
        {
            _client = client;
            _quests = quests;
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "quest") return;

            try
            {
                // Команда должна работать только на сервере
                if (command.GuildId is null || command.User is not SocketGuildUser guildUser)
                {
                    await command.RespondAsync("Эта команда доступна только на сервере.", ephemeral: true);
                    return;
                }

                await command.DeferAsync(ephemeral: true);

                // Проверяем роль, допускающую отметку квестов
                var roleIdStr = Environment.GetEnvironmentVariable("QUEST_MARKER_ROLE_ID");
                if (!ulong.TryParse(roleIdStr, out var requiredRoleId))
                {
                    await command.FollowupAsync("Не настроена переменная окружения **QUEST_MARKER_ROLE_ID**.", ephemeral: true);
                    return;
                }

                var hasRole = guildUser.Roles.Any(r => r.Id == requiredRoleId);
                if (!hasRole)
                {
                    await command.FollowupAsync("У тебя нет прав использовать эту команду.", ephemeral: true);
                    return;
                }

                // Ожидаем подкоманду /quest done number:<int> user:<@user>
                var sub = command.Data.Options.FirstOrDefault();
                if (sub is null || sub.Name != "done")
                {
                    await command.FollowupAsync("Используй: `/quest done number:<число> user:<пользователь>`.", ephemeral: true);
                    return;
                }

                int? number = null;
                IUser? targetUser = null;

                foreach (var opt in sub.Options)
                {
                    if (opt.Name == "number" && opt.Value is long n) number = (int)n;
                    if (opt.Name == "user" && opt.Value is IUser u) targetUser = u;
                }

                if (number is null || targetUser is null)
                {
                    await command.FollowupAsync("Нужно указать и `number`, и `user`.", ephemeral: true);
                    return;
                }

                // Отмечаем выполнение квеста через сервис
                var (ok, msg) = await _quests.MarkQuestDoneAsync(targetUser.Id, number.Value, command.User.Id);
                await command.FollowupAsync(msg, ephemeral: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Quest command error: " + ex);
                try
                {
                    await command.FollowupAsync("Произошла ошибка при обработке команды.", ephemeral: true);
                }
                catch { /* уже отвечали/поздно — игнор */ }
            }
        }
    }
}
