using Discord;
using Discord.WebSocket;
using AdeniumBot.Services;

namespace AdeniumBot.Handlers
{
    public class StartCommandHandler(DiscordSocketClient client, SessionStore store, SessionLifecycle lifecycle)
    {
        private readonly DiscordSocketClient _client = client;

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "start") return;

            var channelId = (command.Channel as ISocketMessageChannel)!.Id;

            if (store.TryGetSessionByChannel(channelId, out _))
            {
                await command.RespondAsync("В этом канале уже идёт набор.", ephemeral: true);
                return;
            }

            var ownerId = command.User.Id;
            var sessionId = store.CreateSession(channelId, ownerId, out var session);

            lock (session.SyncRoot)
                session.Participants.Add(ownerId);

            var components = new ComponentBuilder()
                .WithButton(label: "Участвовать", customId: $"join:{sessionId}", style: ButtonStyle.Success)
                .WithButton(label: "Старт", customId: $"begin:{sessionId}:{ownerId}", style: ButtonStyle.Primary)
                .Build();

            await command.RespondAsync(
                "Нажмите на кнопку, чтобы принять участие в распределении на команды.\nУчастников: **1**",
                components: components
            );

            var orig = await command.GetOriginalResponseAsync();
            store.TryBindMessage(sessionId, channelId, orig.Id);
            
            lifecycle.StartAutoTimeout(sessionId, session, TimeSpan.FromMinutes(20));
        }
    }
    
}
