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

            await command.FollowupAsync("1234567", ephemeral: true);
            return;
            try
            {
                await command.DeferAsync(ephemeral: true);

                var sub = command.Data.Options.FirstOrDefault();
                if (sub?.Name != "done")
                {
                    await command.FollowupAsync("Неизвестная подкоманда.", ephemeral: true);
                    return;
                }
                
                var roleIdStr = Environment.GetEnvironmentVariable("QUEST_MARKER_ROLE_ID");
                if (!ulong.TryParse(roleIdStr, out var roleId))
                {
                    await command.FollowupAsync("Не настроена переменная окружения QUEST_MARKER_ROLE_ID.",
                        ephemeral: true);
                    return;
                }

                var gu = command.User as SocketGuildUser;
                
                if (gu == null || !gu.Roles.Any(r => r.Id == roleId))
                {
                    await command.FollowupAsync("У вас нет роли для отметки выполнения квестов.", ephemeral: true);
                    return;
                }
                
                int? number = null;
                IUser? targetUser = null;
                foreach (var opt in sub.Options)
                {
                    if (opt.Name == "number" && opt.Value is long n) number = (int)n;
                    else if (opt.Name == "user" && opt.Value is IUser u) targetUser = u;
                }

                if (number is null || targetUser is null)
                {
                    await command.FollowupAsync("Нужно указать /quest done number:<число> user:<пользователь>.",
                        ephemeral: true);
                    return;
                }
                
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
                catch
                {
                    /* уже поздно отвечать — ок */
                }
            }
        }
    }
}