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
            if (command.CommandName != "fav" && command.CommandName != "block")
                return;

            await command.DeferAsync(ephemeral: true);

            var targetUser = command.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value as IUser;
            if (targetUser is null)
            {
                await command.FollowupAsync("Укажи пользователя: `/fav user:@ник` или `/block user:@ник`.", ephemeral: true);
                return;
            }

            var ownerDiscordId  = (long)command.User.Id;
            var targetDiscordId = (long)targetUser.Id;

            if (ownerDiscordId == targetDiscordId)
            {
                await command.FollowupAsync("Нельзя выбрать себя, кринж", ephemeral: true);
                return;
            }

            await using var db = _dbFactory.CreateDbContext(Array.Empty<string>());
            
            var owner = await db.PlayerProfiles.SingleOrDefaultAsync(p => p.DiscordUserId == ownerDiscordId);
            if (owner is null)
            {
                owner = new PlayerProfile { DiscordUserId = ownerDiscordId, Username = command.User.Username };
                db.PlayerProfiles.Add(owner);
            }

            var target = await db.PlayerProfiles.SingleOrDefaultAsync(p => p.DiscordUserId == targetDiscordId);
            if (target is null)
            {
                target = new PlayerProfile { DiscordUserId = targetDiscordId, Username = targetUser.Username };
                db.PlayerProfiles.Add(target);
            }

            await db.SaveChangesAsync();

            if (command.CommandName == "fav")
            {
                var exists = await db.FavoriteLinks.AnyAsync(x => x.OwnerId == owner.Id && x.TargetId == target.Id);
                if (exists)
                {
                    await command.FollowupAsync($"Хватит, хватит. Я с первого раза поняла что между вами искры", ephemeral: true);
                    return;
                }
                
                var favCount = await db.FavoriteLinks.CountAsync(x => x.OwnerId == owner.Id);
                if (favCount >= MaxPerList)
                {
                    await command.FollowupAsync($"Не будь ты такой шлюхой, сначала кого то убери ", ephemeral: true);
                    return;
                }
                
                var bl = await db.BlacklistLinks.FindAsync(owner.Id, target.Id);
                if (bl != null) db.BlacklistLinks.Remove(bl);

                db.FavoriteLinks.Add(new FavoriteLink { OwnerId = owner.Id, TargetId = target.Id });
                await db.SaveChangesAsync();
                await command.FollowupAsync($"Думаешь это взаимно?", ephemeral: true);
            }
            else 
            {

                var exists = await db.BlacklistLinks.AnyAsync(x => x.OwnerId == owner.Id && x.TargetId == target.Id);
                if (exists)
                {
                    await command.FollowupAsync($"Я все понимаю, но дважды его добавить в чс нельзя", ephemeral: true);
                    return;
                }
                
                var blCount = await db.BlacklistLinks.CountAsync(x => x.OwnerId == owner.Id);
                if (blCount >= MaxPerList)
                {
                    await command.FollowupAsync($"Сколько в тебе негатива, сначала убери кого-то из списка.", ephemeral: true);
                    return;
                }
                
                var fav = await db.FavoriteLinks.FindAsync(owner.Id, target.Id);
                if (fav != null) db.FavoriteLinks.Remove(fav);

                db.BlacklistLinks.Add(new BlacklistLink { OwnerId = owner.Id, TargetId = target.Id });
                await db.SaveChangesAsync();
                await command.FollowupAsync($"Теперь он тебе не попадется", ephemeral: true);
            }
        }
    }
}
