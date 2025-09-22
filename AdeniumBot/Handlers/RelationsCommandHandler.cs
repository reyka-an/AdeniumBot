using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using AdeniumBot.Data;
using AdeniumBot.Models;
using System.Security.Cryptography;

namespace AdeniumBot.Handlers
{
    public class RelationsCommandHandler
    {
        private readonly BotDbContextFactory _dbFactory = new();
        private const int MaxPerList = 3;
        
        private static readonly string[] FavAddPhrases = new[]
        {
            "Готово, но думаешь это взаимно?",
            "Готово. Пусть судьба сведёт вас… или алгоритм.",
            "Добавила. Теперь система думает, что у тебя есть друзья.",
            "Готово. Ты только что подписался на разочарование.",
            "Добавила. Теперь у вас отношения уровня ‘неловкая симпатия’.",
            "Отметила. Теперь в статистике ты выглядишь ещё отчаяннее.",
            "Записала. Между вами уже что-то искрит.",
            "Готово. Осталось пригласить его на свечи и вино.",
            "Готово. Ты смелый — не каждый так открыто признаётся."
        };

        private static readonly string[] FavRemovePhrases = new[]
        {
            "Убрала из избранного. Новая любовь не за горами?",
            "Готово. Сердце свободно, список — тоже.",
            "Убрала. Он ещё долго будет вспоминать твой нежный клик.",
            "Убрала. Так рождаются обиженные бывшие.",
            "Готово. Твоя любовь была недолгой, но яркой… наверное.",
            "Удалила. Расставания всегда тяжёлые.",
            "Убрала. Надоело тащить за двоих?",
            "Удалила. Видимо, тимплей оказался слишком травматичным.",
            "Удалила. Напарник, наверное, сейчас где-то плачет… или радуется.",
            "Готово. Теперь искать виноватого придётся в другом месте.",
            "Готово. Теперь вы просто два незнакомца в одном лобби."
        };

        private static readonly string[] BlockAddPhrases = new[]
        {
            "Готово. Теперь ты его видишь, но никогда не тащишь.",
            "Записала. Теперь ты будешь смотреть как страдают другие, доволен?",
            "Добавила. Теперь он будет рядом, но на другом конце лобби.",
            "Готово. Совместные катки отменяются, токсик-вайб остаётся.",
            "Чёрный список пополнился новым талантом.",
            "Готово. Ставлю галочку ‘никогда больше’.",
            "Записано. Очередная легенда упала в пропасть.",
            "Добавила. Поздравляю, теперь у вас отношения на уровне ‘заблокирован’."
        };

        private static readonly string[] BlockRemovePhrases = new[]
        {
            "Второй шанс выдан.",
            "Готово. Мир-дружба-жвачка?",
            "Получилось. Любовь победила ненависть.",
            "Снял блок. Ты мазохист или просто соскучился?",
            "Готово. Штош второй шанс — звучит как ошибка.",
            "Всё, посмотрим, кто первым пожалеет.",
            "Удалила. Решил дать второй шанс… зря.",
            "Готово. Слабину дал, да?",
            "Убрала. Держи своего любимого тролля обратно.",
            "Готово. Ждём нового повода вернуть его обратно.",
            "Готово. Система шокирована твоей наивностью.",
            "Удалила. Теперь у тебя снова есть шанс пожалеть об этом.",

        };

        private static string GetRandomPhrase(string[] pool)
        {
            var idx = RandomNumberGenerator.GetInt32(pool.Length);
            return pool[idx];
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName == "relations")
            {
                await command.DeferAsync(ephemeral: true);

                var ownerDiscordId = (long)command.User.Id;

                await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());

                var owner = await EnsureProfileAsync(db, ownerDiscordId, command.User.Username);

                var me = await db.PlayerProfiles
                    .Include(p => p.Favorites).ThenInclude(l => l.Target)
                    .Include(p => p.Blacklist).ThenInclude(l => l.Target)
                    .FirstOrDefaultAsync(p => p.Id == owner.Id);

                if (me is null)
                {
                    await command.FollowupAsync("Профиль не найден.", ephemeral: true);
                    return;
                }
                
                string FormatList<TLink>(IEnumerable<TLink> links, Func<TLink, PlayerProfile> pick)
                {
                    var items = links
                        .Select(l => pick(l))
                        .Select(t => $"<@{t.DiscordUserId}>")
                        .ToList();

                    return items.Count == 0 ? "— пусто —" : string.Join("\n", items);
                }

                var favText = FormatList(me.Favorites, l => l.Target);
                var blText  = FormatList(me.Blacklist, l => l.Target);

                var msg =
                    $"**Избранные ({me.Favorites.Count}):**\n{favText}\n\n" +
                    $"**Чёрный список ({me.Blacklist.Count}):**\n{blText}";

                await command.FollowupAsync(msg, ephemeral: true);
                return;
            }

            if (command.CommandName == "rel")
            {
                await command.DeferAsync(ephemeral: true);

                var sub = command.Data.Options.FirstOrDefault();
                if (sub is null)
                {
                    await command.FollowupAsync("Не указана сабкоманда.", ephemeral: true);
                    return;
                }

                var subName = sub.Name;
                var targetUser = sub.Options?.FirstOrDefault(o => o.Name == "user")?.Value as IUser;
                if (targetUser is null)
                {
                    await command.FollowupAsync("Укажи пользователя.", ephemeral: true);
                    return;
                }

                var ownerDiscordId = (long)command.User.Id;
                var targetDiscordId = (long)targetUser.Id;

                if (ownerDiscordId == targetDiscordId)
                {
                    await command.FollowupAsync("Ты вообще сам понял что сделал?", ephemeral: true);
                    return;
                }

                await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());

                var owner = await EnsureProfileAsync(db, ownerDiscordId, command.User.Username);

                var target = await db.PlayerProfiles
                    .FirstOrDefaultAsync(p => p.DiscordUserId == targetDiscordId);

                if (subName == "unfav")
                {
                    if (target is null)
                    {
                        await command.FollowupAsync($"Сначала добавь, потом удаляй", ephemeral: true);
                        return;
                    }

                    var link = await db.FavoriteLinks.FindAsync(owner.Id, target.Id);
                    if (link is null)
                    {
                        await command.FollowupAsync($"Я такого тут не вижу", ephemeral: true);
                    }
                    else
                    {
                        db.FavoriteLinks.Remove(link);
                        await db.SaveChangesAsync();
                        await command.FollowupAsync(GetRandomPhrase(FavRemovePhrases), ephemeral: true);
                    }
                }
                else if (subName == "unblock")
                {
                    if (target is null)
                    {
                        await command.FollowupAsync($"Ты точно его сюда добавлял?", ephemeral: true);
                        return;
                    }

                    var link = await db.BlacklistLinks.FindAsync(owner.Id, target.Id);
                    if (link is null)
                    {
                        await command.FollowupAsync($"Ничего такого нет, не придумывай", ephemeral: true);
                    }
                    else
                    {
                        db.BlacklistLinks.Remove(link);
                        await db.SaveChangesAsync();
                        await command.FollowupAsync(GetRandomPhrase(BlockRemovePhrases), ephemeral: true); 
                    }
                }
                else
                {
                    await command.FollowupAsync("Не понимаю о чем ты", ephemeral: true);
                }

                return;
            }

            if (command.CommandName != "fav" && command.CommandName != "block")
                return;

            await command.DeferAsync(ephemeral: true);

            var userOpt = command.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value as IUser;
            if (userOpt is null)
            {
                await command.FollowupAsync("Укажи пользователя: `/fav user:@ник` или `/block user:@ник`.",
                    ephemeral: true);
                return;
            }

            var ownerId = (long)command.User.Id;
            var targetId = (long)userOpt.Id;

            if (ownerId == targetId)
            {
                await command.FollowupAsync("Нельзя выбрать себя, кринж", ephemeral: true);
                return;
            }

            await using (var db = _dbFactory.CreateDbContext(Array.Empty<string>()))
            {
                var owner = await EnsureProfileAsync(db, ownerId, command.User.Username);
                var target = await EnsureProfileAsync(db, targetId, userOpt.Username);

                if (command.CommandName == "fav")
                {
                    var exists = await db.FavoriteLinks.AnyAsync(x => x.OwnerId == owner.Id && x.TargetId == target.Id);
                    if (exists)
                    {
                        await command.FollowupAsync("Хватит, хватит. Я с первого раза поняла что между вами искры",
                            ephemeral: true);
                        return;
                    }

                    var favCount = await db.FavoriteLinks.CountAsync(x => x.OwnerId == owner.Id);
                    if (favCount >= MaxPerList)
                    {
                        await command.FollowupAsync("Не будь ты такой шлюхой, сначала кого то убери ", ephemeral: true);
                        return;
                    }

                    var bl = await db.BlacklistLinks.FindAsync(owner.Id, target.Id);
                    if (bl != null) db.BlacklistLinks.Remove(bl);

                    db.FavoriteLinks.Add(new FavoriteLink { OwnerId = owner.Id, TargetId = target.Id });
                    await db.SaveChangesAsync();
                    await command.FollowupAsync(GetRandomPhrase(FavAddPhrases), ephemeral: true);
                }
                else 
                {
                    var exists =
                        await db.BlacklistLinks.AnyAsync(x => x.OwnerId == owner.Id && x.TargetId == target.Id);
                    if (exists)
                    {
                        await command.FollowupAsync("Я все понимаю, но дважды его добавить в чс нельзя",
                            ephemeral: true);
                        return;
                    }

                    var blCount = await db.BlacklistLinks.CountAsync(x => x.OwnerId == owner.Id);
                    if (blCount >= MaxPerList)
                    {
                        await command.FollowupAsync("Сколько в тебе негатива, сначала убери кого-то из списка.",
                            ephemeral: true);
                        return;
                    }

                    var fav = await db.FavoriteLinks.FindAsync(owner.Id, target.Id);
                    if (fav != null) db.FavoriteLinks.Remove(fav);

                    db.BlacklistLinks.Add(new BlacklistLink { OwnerId = owner.Id, TargetId = target.Id });
                    await db.SaveChangesAsync();
                    await command.FollowupAsync(GetRandomPhrase(BlockAddPhrases), ephemeral: true);
                }
            }
        }

        private static async Task<PlayerProfile> EnsureProfileAsync(BotDbContext db, long discordUserId,
            string usernameFallback)
        {
            var existing = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == discordUserId);
            if (existing != null)
            {
                if (!string.Equals(existing.Username, usernameFallback, StringComparison.Ordinal))
                {
                    existing.Username = usernameFallback;
                    await db.SaveChangesAsync();
                }

                return existing;
            }

            var created = new PlayerProfile { DiscordUserId = discordUserId, Username = usernameFallback };
            db.PlayerProfiles.Add(created);
            try
            {
                await db.SaveChangesAsync();
                return created;
            }
            catch (DbUpdateException)
            {
                return await db.PlayerProfiles.FirstAsync(p => p.DiscordUserId == discordUserId);
            }
        }
    }
}
