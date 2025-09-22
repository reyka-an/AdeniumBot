using Discord;
using Discord.WebSocket;
using AdeniumBot.Data;
using AdeniumBot.Models;
using Microsoft.EntityFrameworkCore;

namespace AdeniumBot.Handlers
{
    public class QuestCommandHandler
    {
        // Делаем так же, как в ExpCommandHandler
        private readonly BotDbContextFactory _dbFactory = new();

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "quest") return;

            try
            {
                // Только на сервере
                if (command.GuildId is null || command.User is not SocketGuildUser guildUser)
                {
                    await command.RespondAsync("Эта команда доступна только на сервере.", ephemeral: true);
                    return;
                }

                await command.DeferAsync(ephemeral: true);

                // Проверка роли-маркера квестов
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

                // Ожидаем /quest done number:<int> user:<@user>
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

                // Логика отметки выполнения
                var (ok, msg) = await MarkQuestDoneAsync(targetUser, number.Value);
                await command.FollowupAsync(msg, ephemeral: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Quest command error: " + ex);
                try
                {
                    await command.FollowupAsync("Произошла ошибка при обработке команды.", ephemeral: true);
                }
                catch { /* уже отвечали — игнор */ }
            }
        }

        private static long ToLong(ulong x) => unchecked((long)x);

        private async Task<(bool ok, string message)> MarkQuestDoneAsync(IUser targetUser, int questNumber)
        {
            await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());

            // 1) Находим активный квест по номеру
            var quest = await db.Quests.FirstOrDefaultAsync(q => q.Number == questNumber && q.IsActive);
            if (quest == null)
                return (false, $"Квест с номером **{questNumber}** не найден или отключён.");

            // 2) Обеспечиваем профиль игрока
            var targetDiscordId = ToLong(targetUser.Id);
            var player = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == targetDiscordId);
            if (player is null)
            {
                player = new PlayerProfile
                {
                    DiscordUserId = targetDiscordId,
                    Username = targetUser.Username,
                    Exp = 0,
                    CreatedAt = DateTime.UtcNow
                };
                db.PlayerProfiles.Add(player);
                await db.SaveChangesAsync();
            }
            else
            {
                // Актуализируем username, если сменился
                if (!string.Equals(player.Username, targetUser.Username, StringComparison.Ordinal))
                {
                    player.Username = targetUser.Username;
                    await db.SaveChangesAsync();
                }
            }

            // 3) Получаем/создаём прогресс по этому квесту
            var pq = await db.PlayerQuests.FirstOrDefaultAsync(x => x.PlayerId == player.Id && x.QuestId == quest.Id);
            if (pq == null)
            {
                pq = new PlayerQuest
                {
                    PlayerId = player.Id,
                    QuestId = quest.Id,
                    CompletedCount = 0
                };
                db.PlayerQuests.Add(pq);
            }

            // 4) Проверяем лимит выполнений
            if (quest.MaxCompletionsPerPlayer.HasValue && pq.CompletedCount >= quest.MaxCompletionsPerPlayer.Value)
            {
                return (false,
                    $"Лимит выполнений для квеста **{quest.Number}** уже достигнут " +
                    $"({pq.CompletedCount}/{quest.MaxCompletionsPerPlayer}).");
            }

            // 5) Обновляем прогресс и выдаём EXP
            pq.CompletedCount += 1;
            pq.LastCompletedAt = DateTime.UtcNow;

            player.Exp += quest.ExpReward;

            await db.SaveChangesAsync();

            var limitText = quest.MaxCompletionsPerPlayer.HasValue
                ? $"{pq.CompletedCount}/{quest.MaxCompletionsPerPlayer}"
                : $"{pq.CompletedCount}";

            return (true,
                $"Отмечено выполнение квеста **{quest.Number}** у {targetUser.Mention}. " +
                $"Прогресс: **{limitText}**. Начислено **{quest.ExpReward} EXP**.");
        }
    }
}
