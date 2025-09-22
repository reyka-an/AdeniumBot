using AdeniumBot.Data;
using AdeniumBot.Models;
using Microsoft.EntityFrameworkCore;

namespace AdeniumBot.Services
{
    public interface IQuestService
    {
        Task<(bool ok, string message)> MarkQuestDoneAsync(ulong targetDiscordUserId, int questNumber,
            ulong markedByDiscordUserId);
    }
    public class QuestService : IQuestService
    {

        private readonly string _conn;
        public QuestService(string connectionString) => _conn = connectionString;


        private static long ToLong(ulong x) => unchecked((long)x);

        public async Task<(bool ok, string message)> MarkQuestDoneAsync(ulong targetDiscordUserId, int questNumber,
            ulong markedByDiscordUserId)
        {
            await using var db = new BotDbContext(_conn);

            var quest = await db.Quests.FirstOrDefaultAsync(q => q.Number == questNumber && q.IsActive);
            if (quest == null)
                return (false, $"Квест с номером **{questNumber}** не найден или отключён.");

            var player = await EnsureProfileAsync(db, targetDiscordUserId);

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

            if (quest.MaxCompletionsPerPlayer.HasValue && pq.CompletedCount >= quest.MaxCompletionsPerPlayer.Value)
                return (false,
                    $"Лимит выполнений для квеста **{quest.Number}** уже достигнут ({pq.CompletedCount}/{quest.MaxCompletionsPerPlayer}).");

            pq.CompletedCount += 1;
            pq.LastCompletedAt = DateTime.UtcNow;

            player.Exp += quest.ExpReward;

            await db.SaveChangesAsync();
            var limitText = quest.MaxCompletionsPerPlayer.HasValue
                ? $"{pq.CompletedCount}/{quest.MaxCompletionsPerPlayer}"
                : $"{pq.CompletedCount}";
            return (true,
                $"Отмечено выполнение квеста **{quest.Number}**. Прогресс: **{limitText}**. Начислено **{quest.ExpReward} EXP**.");
        }

        private static async Task<PlayerProfile> EnsureProfileAsync(BotDbContext db, ulong discordUserId)
        {
            var lid = ToLong(discordUserId);
            var p = await db.PlayerProfiles.FirstOrDefaultAsync(x => x.DiscordUserId == lid);
            if (p != null) return p;

            p = new PlayerProfile { DiscordUserId = lid, Username = $"user-{discordUserId}" };
            db.PlayerProfiles.Add(p);
            await db.SaveChangesAsync();
            return p;
        }
    }
}