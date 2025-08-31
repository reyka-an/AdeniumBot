using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using YourBot.Services;

namespace YourBot.Handlers
{
    public class StartCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly SessionStore _store;
        private readonly SessionLifecycle _lifecycle;

        public StartCommandHandler(DiscordSocketClient client, SessionStore store)
        {
            _client = client;
            _store = store;
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "start") return;

            var channelId = (command.Channel as ISocketMessageChannel)!.Id;

            if (_store.TryGetSessionByChannel(channelId, out var existingSid))
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

            string BuildLobbyText()
            {
                int count;
                lock (session.SyncRoot) count = session.Participants.Count;
                return "Нажмите на кнопку, чтобы принять участие в распределении на команды.\n" +
                       $"Участников: **{count}**";
            }

            try
            {
                await command.RespondAsync(BuildLobbyText(), components: components);
                var orig = await command.GetOriginalResponseAsync();

                _store.TryBindMessage(sessionId, channelId, orig.Id);

                await command.FollowupAsync("Голосование запущено", ephemeral: true);
            }
            catch
            {
                _store.RemoveByChannel(channelId);
                throw;
            }
        }
    }
}
