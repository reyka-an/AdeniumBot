// RelationsCommandHandler.cs
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Data;
using Adenium.Models;

namespace Adenium.Handlers
{
    public class RelationsCommandHandler
    {
        private readonly BotDbContextFactory _dbFactory = new();
        private const int MaxPerList = 3;

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            // /rel unfav|unblock user:@...
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

                var ownerDiscordId  = (long)command.User.Id;
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
                        await command.FollowupAsync($"Все? Нашел себе кого то получше?", ephemeral: true);
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
                        await command.FollowupAsync($"Ох уже эти детские игры. Сегодня добавил... завтра убрал", ephemeral: true);
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
                await command.FollowupAsync("Укажи пользователя: `/fav user:@ник` или `/block user:@ник`.", ephemeral: true);
                return;
            }

            var ownerId  = (long)command.User.Id;
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
                        await command.FollowupAsync("Хватит, хватит. Я с первого раза поняла что между вами искры", ephemeral: true);
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
                    await command.FollowupAsync("Думаешь это взаимно?", ephemeral: true);
                }
                else // /block
                {
                    var exists = await db.BlacklistLinks.AnyAsync(x => x.OwnerId == owner.Id && x.TargetId == target.Id);
                    if (exists)
                    {
                        await command.FollowupAsync("Я все понимаю, но дважды его добавить в чс нельзя", ephemeral: true);
                        return;
                    }

                    var blCount = await db.BlacklistLinks.CountAsync(x => x.OwnerId == owner.Id);
                    if (blCount >= MaxPerList)
                    {
                        await command.FollowupAsync("Сколько в тебе негатива, сначала убери кого-то из списка.", ephemeral: true);
                        return;
                    }
                    
                    var fav = await db.FavoriteLinks.FindAsync(owner.Id, target.Id);
                    if (fav != null) db.FavoriteLinks.Remove(fav);

                    db.BlacklistLinks.Add(new BlacklistLink { OwnerId = owner.Id, TargetId = target.Id });
                    await db.SaveChangesAsync();
                    await command.FollowupAsync("Теперь он тебе не попадется", ephemeral: true);
                }
            }
        }
        
        private static async Task<PlayerProfile> EnsureProfileAsync(BotDbContext db, long discordUserId, string usernameFallback)
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
