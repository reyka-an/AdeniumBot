using Discord;
using Discord.WebSocket;
using Adenium.Services;

namespace Adenium.Handlers
{
    public class StartCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly SessionStore _store;
        private readonly SessionLifecycle _lifecycle;

        public StartCommandHandler(DiscordSocketClient client, SessionStore store, SessionLifecycle lifecycle)
        {
            _client = client;
            _store = store;
            _lifecycle = lifecycle;
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "start") return;

            var channelId = (command.Channel as ISocketMessageChannel)!.Id;

            if (_store.TryGetSessionByChannel(channelId, out _))
            {
                await command.RespondAsync("В этом канале уже идёт набор.", ephemeral: true);
                return;
            }

            var ownerId = command.User.Id;
            var sessionId = _store.CreateSession(channelId, ownerId, out var session);

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
            _store.TryBindMessage(sessionId, channelId, orig.Id);
            
            _lifecycle.StartAutoTimeout(sessionId, session, TimeSpan.FromMinutes(20));

            await command.FollowupAsync("Голосование запущено", ephemeral: true);
        }
    }
    
}
