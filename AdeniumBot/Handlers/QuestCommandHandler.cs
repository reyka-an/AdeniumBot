using Discord;
using Discord.WebSocket;
using Adenium.Services;

namespace Adenium.Handlers
{
    public class QuestCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly QuestService _quests;

        public QuestCommandHandler(DiscordSocketClient client, QuestService quests)
        {
            _client = client;
            _quests = quests;
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "quest") return;

            var sub = command.Data.Options.FirstOrDefault();
            if (sub?.Name != "done") return;

            // Проверка роли-прав на отметку
            var roleIdStr = Environment.GetEnvironmentVariable("QUEST_MARKER_ROLE_ID");
            if (!ulong.TryParse(roleIdStr, out var roleId))
            {
                await command.RespondAsync("Не настроена переменная окружения **QUEST_MARKER_ROLE_ID**.", ephemeral: true);
                return;
            }

            var guildUser = command.User as SocketGuildUser;
            if (guildUser == null || !guildUser.Roles.Any(r => r.Id == roleId))
            {
                await command.RespondAsync("У вас нет роли для отметки выполнения квестов.", ephemeral: true);
                return;
            }

            // Параметры
            var number = (long)(sub.Options.First(o => o.Name == "number").Value);
            var user = (IUser)(sub.Options.First(o => o.Name == "user").Value);

            var (ok, msg) = await _quests.MarkQuestDoneAsync(user.Id, (int)number, command.User.Id);
            await command.RespondAsync(msg, ephemeral: !ok ? true : false);
        }
    }
}