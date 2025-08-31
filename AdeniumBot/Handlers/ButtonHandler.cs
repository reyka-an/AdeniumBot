using Discord;
using Discord.WebSocket;
using Adenium.Models;
using Adenium.Services;
using Adenium.Utils;

namespace Adenium.Handlers
{
    public class ButtonHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly SessionStore _store;
        private readonly SessionLifecycle _lifecycle;

        public ButtonHandler(DiscordSocketClient client, SessionStore store, SessionLifecycle lifecycle)
        {
            _client = client;
            _store = store;
            _lifecycle = lifecycle;
        }

        public async Task OnButtonAsync(SocketMessageComponent component)
        {
            var parts = component.Data.CustomId.Split(':');
            var kind = parts[0];

            switch (kind)
            {
                case "join":
                    await HandleJoin(parts, component);
                    break;
                case "begin":
                    await HandleBegin(parts, component);
                    break;
            }
        }

        private async Task HandleJoin(string[] parts, SocketMessageComponent component)
        {
            if (parts.Length < 2) { await component.RespondAsync("Некорректная кнопка.", ephemeral: true); return; }
            var sid = parts[1];

            if (!_store.TryGetSession(sid, out var s))
            {
                await component.RespondAsync("Эта сессия уже завершена или не найдена.", ephemeral: true);
                return;
            }

            var userId = component.User.Id;
            bool added;
            lock (s.SyncRoot)
                added = s.Participants.Add(userId);

            var nowCount = s.Participants.Count;
            await component.RespondAsync(added
                ? $"Готово! Сейчас участников: {nowCount}"
                : $"Ты уже жмакал. Сейчас участников: {nowCount}", ephemeral: true);

            try { await UpdateLobbyMessageAsync(sid, s); }
            catch (Exception ex) { Console.WriteLine($"UpdateLobbyMessage error: {ex}"); }
        }

        private async Task HandleBegin(string[] parts, SocketMessageComponent component)
        {
            if (parts.Length < 3) { await component.RespondAsync("Некорректная кнопка.", ephemeral: true); return; }
            var sid = parts[1];
            if (!ulong.TryParse(parts[2], out var ownerId))
            {
                await component.RespondAsync("Некорректная кнопка (owner).", ephemeral: true);
                return;
            }

            if (!_store.TryGetSession(sid, out var s))
            {
                await component.RespondAsync("Эта сессия уже завершена или не найдена.", ephemeral: true);
                return;
            }

            if (component.User.Id != ownerId)
            {
                await component.RespondAsync("Только создатель голосования может нажать «Старт».", ephemeral: true);
                return;
            }

            var current = s.Participants.ToList();
            if (current.Count == 0)
            {
                await component.RespondAsync("Некого распределять.", ephemeral: true);
                return;
            }

            var resultText = Pairing.BuildPairsText(current);
            await component.Channel.SendMessageAsync($"**Результат распределения:**\n{resultText}");

            var disabled = new ComponentBuilder()
                .WithButton(label: "Участвовать", customId: $"join:{sid}", style: ButtonStyle.Success, disabled: true)
                .WithButton(label: "Старт", customId: $"begin:{sid}:{ownerId}", style: ButtonStyle.Primary, disabled: true)
                .Build();

            await component.Message.ModifyAsync(m =>
            {
                m.Content = $"{component.Message.Content}\n\n_Голосование завершено._";
                m.Components = disabled;
            });

            s.Cts.Cancel();
            
            _store.RemoveSession(sid);
        }

        private async Task UpdateLobbyMessageAsync(string sessionId, VoteSession s)
        {
            int count;
            lock (s.SyncRoot) count = s.Participants.Count;

            var newText = "Нажмите на кнопку, чтобы принять участие в распределении на команды.\n" +
                          $"Участников: **{count}**";

            var components = new ComponentBuilder()
                .WithButton(label: "Участвовать", customId: $"join:{sessionId}", style: ButtonStyle.Success)
                .WithButton(label: "Старт", customId: $"begin:{sessionId}:{s.OwnerId}", style: ButtonStyle.Primary)
                .Build();

            var ch = _client.GetChannel(s.ChannelId) as IMessageChannel;
            if (ch == null) return;

            var msg = await ch.GetMessageAsync(s.MessageId);
            if (msg is not IUserMessage um) return;

            await um.ModifyAsync(m =>
            {
                m.Content = newText;
                m.Components = components;
            });
        }
    }
}
