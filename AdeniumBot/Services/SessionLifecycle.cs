using Discord;
using Discord.WebSocket;
using AdeniumBot.Models;

namespace AdeniumBot.Services
{
    public class SessionLifecycle
    {
        private readonly DiscordSocketClient _client;
        private readonly SessionStore _store;

        public SessionLifecycle(DiscordSocketClient client, SessionStore store)
        {
            _client = client;
            _store = store;
        }
        
        public void StartAutoTimeout(string sessionId, VoteSession session, TimeSpan duration)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(duration, session.Cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (!_store.TryGetSession(sessionId, out var still) || still != session)
                    return;

                await CloseByTimeoutAsync(sessionId, session);
            });
        }

        private async Task CloseByTimeoutAsync(string sessionId, VoteSession s)
        {
            var ch = _client.GetChannel(s.ChannelId) as IMessageChannel;
            
            var disabled = new ComponentBuilder()
                .WithButton(label: "Участвовать", customId: $"join:{sessionId}", style: ButtonStyle.Success, disabled: true)
                .WithButton(label: "Старт", customId: $"begin:{sessionId}:{s.OwnerId}", style: ButtonStyle.Primary, disabled: true)
                .Build();
            
            if (ch != null)
            {
                var msg = await ch.GetMessageAsync(s.MessageId);
                if (msg is IUserMessage um)
                {
                    await um.ModifyAsync(m =>
                    {
                        m.Content = $"{um.Content}\n\n_Время вышло. Сессия закрыта._";
                        m.Components = disabled;
                    });

                    await ch.SendMessageAsync("⏰ **Время вышло:** набор закрыт автоматически");
                }
            }
            _store.RemoveSession(sessionId);
        }
    }
}
