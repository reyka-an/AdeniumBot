using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Adenium.Models;

namespace Adenium.Handlers
{
    public class RoleExpHandler
    {
        public async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
        {
            var beforeRoles = before.HasValue ? before.Value.Roles.Select(r => r.Id).ToHashSet() : new HashSet<ulong>();
            var afterRoles = after.Roles.Select(r => r.Id).ToHashSet();

            var addedRoles = afterRoles.Except(beforeRoles);
            var removedRoles = beforeRoles.Except(afterRoles);

            if (!addedRoles.Any() && !removedRoles.Any())
                return;

            await using var db = new Adenium.Data.BotDbContextFactory().CreateDbContext(Array.Empty<string>());

            var guildId = unchecked((long)after.Guild.Id);
            var userId = unchecked((long)after.Id);

            var profile = await db.PlayerProfiles.FirstOrDefaultAsync(p => p.DiscordUserId == userId);
            if (profile == null)
            {
                profile = new PlayerProfile { DiscordUserId = userId, Username = after.Username, Exp = 0 };
                db.PlayerProfiles.Add(profile);
                await db.SaveChangesAsync();
            }

            foreach (var roleIdUlong in addedRoles)
            {
                var roleId = unchecked((long)roleIdUlong);

                var rule = await db.RoleExpRules.FirstOrDefaultAsync(r => r.GuildId == guildId && r.RoleId == roleId);
                if (rule != null)
                {
                    profile.Exp += rule.ExpAmount;
                    await db.SaveChangesAsync();

                    var notifyChannelId = Environment.GetEnvironmentVariable("EXP_NOTIFY_CHANNEL_ID");
                    if (ulong.TryParse(notifyChannelId, out var channelId))
                    {
                        var ch = after.Guild.GetTextChannel(channelId);
                        if (ch != null)
                        {
                            await ch.SendMessageAsync(
                                $"⭐ {after.Mention} получил **{rule.ExpAmount}** опыта за роль <@&{roleId}>.");
                        }
                    }
                }
            }

            foreach (var roleIdUlong in removedRoles)
            {
                var roleId = unchecked((long)roleIdUlong);

                var rule = await db.RoleExpRules.FirstOrDefaultAsync(r => r.GuildId == guildId && r.RoleId == roleId);
                if (rule != null)
                {
                    profile.Exp -= rule.ExpAmount;
                    if (profile.Exp < 0) profile.Exp = 0;

                    await db.SaveChangesAsync();

                    var notifyChannelId = Environment.GetEnvironmentVariable("EXP_NOTIFY_CHANNEL_ID");
                    if (ulong.TryParse(notifyChannelId, out var channelId))
                    {
                        var ch = after.Guild.GetTextChannel(channelId);
                        if (ch != null)
                        {
                            await ch.SendMessageAsync(
                                $"➖ {after.Mention} потерял **{rule.ExpAmount}** опыта за снятие роли <@&{roleId}>.");
                        }
                    }
                }
            }
        }
    }
}