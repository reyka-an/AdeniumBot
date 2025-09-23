using Discord;
using Discord.WebSocket;
using AdeniumBot.Data;
using AdeniumBot.Models;
using Microsoft.EntityFrameworkCore;
using AdeniumBot.Services; 

namespace AdeniumBot.Handlers
{
    public class QuestCommandHandler
    {
        private readonly BotDbContextFactory _dbFactory = new();
        private readonly DiscordSocketClient _client;

        // ID канала для уведомлений
        private const ulong NotifyChannelId = 1419672109575311441;

        public QuestCommandHandler(DiscordSocketClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "quest") return;

            try
            {
                if (command.GuildId is null || command.User is not SocketGuildUser guildUser)
                {
                    await command.RespondAsync("Эта команда доступна только на сервере.", ephemeral: true);
                    return;
                }

                var guild = (command.Channel as SocketGuildChannel)?.Guild;
                if (guild == null)
                {
                    await command.RespondAsync("Гильдия не найдена.", ephemeral: true);
                    return;
                }

                await command.DeferAsync(ephemeral: true);

                // Разрешённая роль
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

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var ct = cts.Token;

                await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());
                
                var helper = new HelperService(_client, db);
                var profiles = await helper.GetOrCreateProfilesAsync(guild, ct);

                var targetIdLong = unchecked((long)targetUser.Id);
                var player = profiles.FirstOrDefault(p => p.DiscordUserId == targetIdLong);
                if (player == null)
                {
                    await command.FollowupAsync("Не удалось найти или создать профиль игрока.", ephemeral: true);
                    return;
                }
                
                var (ok, msg, expReward) = await MarkQuestDoneAsync(db, player, targetUser, number.Value, ct);

                // личный ответ инициатору
                await command.FollowupAsync(msg, ephemeral: true);

                // публичное уведомление
                if (ok && expReward > 0)
                {
                    var ch = guild.GetTextChannel(NotifyChannelId);
                    if (ch != null)
                    {
                        await ch.SendMessageAsync(
                            $"🎯 {targetUser.Mention} выполнил квест **#{number.Value}**. " +
                            $"Начислено **{expReward} EXP**. (отметил {guildUser.Mention})");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                try { await command.FollowupAsync("⏱️ Операция отменена по таймауту.", ephemeral: true); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Quest command error: " + ex);
                try
                {
                    await command.FollowupAsync("Произошла ошибка при обработке команды.", ephemeral: true);
                }
                catch { }
            }
        }

        /// <summary>
        /// Отмечает выполнение квеста для уже существующего профиля игрока.
        /// Возвращает: успех, сообщение и начисленный опыт
        /// </summary>
        private static async Task<(bool ok, string message, int expReward)> MarkQuestDoneAsync(
            BotDbContext db,
            PlayerProfile player,
            IUser targetUser,
            int questNumber,
            CancellationToken ct = default)
        {
            var quest = await db.Quests
                .FirstOrDefaultAsync(q => q.Number == questNumber && q.IsActive, ct);

            if (quest == null)
                return (false, $"Квест с номером **{questNumber}** не найден или отключён.", 0);

            var pq = await db.PlayerQuests
                .FirstOrDefaultAsync(x => x.PlayerId == player.Id && x.QuestId == quest.Id, ct);

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

            if (quest.MaxCompletionsPerPlayer.HasValue && pq.CompletedCount >= quest.MaxCompletionsPerPlayer.Value)
            {
                return (false,
                    $"Лимит выполнений для квеста **{quest.Number}** уже достигнут " +
                    $"({pq.CompletedCount}/{quest.MaxCompletionsPerPlayer}).", 0);
            }

            pq.CompletedCount += 1;
            pq.LastCompletedAt = DateTime.UtcNow;

            player.Exp += quest.ExpReward;

            await db.SaveChangesAsync(ct);

            var limitText = quest.MaxCompletionsPerPlayer.HasValue
                ? $"{pq.CompletedCount}/{quest.MaxCompletionsPerPlayer}"
                : $"{pq.CompletedCount}";

            var message =
                $"Отмечено выполнение квеста **{quest.Number}** у {targetUser.Mention}. " +
                $"Прогресс: **{limitText}**. Начислено **{quest.ExpReward} EXP**.";

            return (true, message, quest.ExpReward);
        }
    }
}
