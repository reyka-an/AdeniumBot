using Discord;
using Discord.WebSocket;
using Adenium.Models;
using Adenium.Services;
using System.Security.Cryptography;

namespace Adenium.Handlers
{
    public class ButtonHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly SessionStore _store;
        private readonly SessionLifecycle _lifecycle;
        private readonly PairingService _pairing;

        private static readonly string[] JoinPhrases = new[]
        {
            "Ты в игре!",
            "Ура! Ты смог... наконец-то",
            "Ты справился! Твоя мама может тобой гордится",
            "Записался? Отлично, теперь назад дороги нет",
            "Поздравляю, твой IQ официально выше нуля — кнопку нашёл.",
            "Считай, что подписал себе приговор.",
            "Ты в игре! Осталось только научиться играть",
            "Записался? Отлично. Теперь проверим твоё досье на плохие поступки — прежде чем доверить тебе напарника.",
            "Распределяющая шляпа уже у тебя на голове… тсс, она думает.",
            "Добро пожаловать, теперь ты часть легенды… или статистики.",
            "Ну всё, теперь назад только через окно.",
            "Поздравляем! Ещё один герой в нашей трагикомедии.",
            "Ты в игре! Выйти уже нельзя, только с концами.",
            "Распределяющая шляпа уже перебирает варианты… надеюсь, она выспалась.",
            "Ты в игре! Осталось убедить систему, что тебя можно выпускать к другим игрокам.",
            "Ты в игре! Если что — мы всё свалим на тебя.",
            "Ты в игре! Осталось найти героя, готового разделить с тобой все ошибки.",
            "Теперь молись чтобы твоим напарником был не Аста.",
            "Поздравляем! Теперь кому-то придётся терпеть тебя весь матч.",
            "Ты подписал контракт, теперь тебе до конца жизни нельзя банить Зайру.",
            "Вступил? Отлично, теперь у нас меньше шансов на победу."
        };

        private static readonly string[] AlreadyJoinedPhrases = new[]
        {
            "У тебя деменция? Ты уже нажимал эту кнопку",
            "Ты уже жмал ><",
            "Серьёзно? Ещё раз?",
            "Думаешь, два клика дадут двойной шанс? Не-а.",
            "У тебя деменция? Ты уже нажимал эту кнопку",
            "Эта кнопка не банкомат — деньги не выдаст.",
            "Не переживай, мы всё ещё помним, что ты здесь.",
            "Тебе нравится щёлкать? Оставь это для игры",
            "Ты уже тут. Расслабь палец, герой.",
            "Да, мы видели. Да, мы записали. Да, хватит.",
            "Кнопка работает один раз. Не жми, а то сломаешь.",
            "Дважды участвовать нельзя, даже если очень хочется.",
            "Ты уже в игре. Хочешь быть сразу за двух?"
        };

        public ButtonHandler(DiscordSocketClient client, SessionStore store, SessionLifecycle lifecycle, PairingService pairing)
        {
            _client = client;
            _store = store;
            _lifecycle = lifecycle;
            _pairing = pairing;
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

        private static string GetRandomPhrase(string[] pool, int count)
        {
            var index = RandomNumberGenerator.GetInt32(pool.Length);
            return string.Format(pool[index], count);
        }

        private async Task HandleJoin(string[] parts, SocketMessageComponent component)
        {
            if (parts.Length < 2) { await component.RespondAsync("Некорректная кнопка.", ephemeral: true); return; }
            var sid = parts[1];

            if (!_store.TryGetSession(sid, out var s))
            {
                await component.RespondAsync("Что то тут не так... знать бы что", ephemeral: true);
                return;
            }

            var userId = component.User.Id;
            bool added;
            lock (s.SyncRoot)
                added = s.Participants.Add(userId);

            if (added)
                await component.RespondAsync(GetRandomPhrase(JoinPhrases, s.Participants.Count), ephemeral: true);
            else
                await component.RespondAsync(GetRandomPhrase(AlreadyJoinedPhrases, s.Participants.Count), ephemeral: true);

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
                await component.RespondAsync("Тут уже как бы все, поезд ушел", ephemeral: true);
                return;
            }

            if (component.User.Id != ownerId)
            {
                await component.RespondAsync("Сиди жди молча и не жми лишних кнопочек", ephemeral: true);
                return;
            }

            ulong[] participants;
            lock (s.SyncRoot)
                participants = s.Participants.ToArray();

            if (participants.Length < 2)
            {
                await component.RespondAsync("Недостаточно участников для пар.", ephemeral: true);
                return;
            }

            await component.DeferAsync(); 
            
            var result = await _pairing.MakePairsAsync(participants);
            
            var lines = new List<string>();
            foreach (var (a, b) in result.Pairs)
                lines.Add($"• <@{a}> × <@{b}>");

            if (result.Leftover is ulong left)
                lines.Add($"\nОдинокий волк: <@{left}>");

            var resultText =
                $"**Итоговые пары ({result.Pairs.Count}):**\n" +
                string.Join("\n", lines);

            await component.Channel.SendMessageAsync($"**Результат распределения:**\n{resultText}");
            
            var disabled = new ComponentBuilder()
                .WithButton(label: "Участвовать", customId: $"join:{sid}", style: ButtonStyle.Success, disabled: true)
                .WithButton(label: "Старт", customId: $"begin:{sid}:{ownerId}", style: ButtonStyle.Primary, disabled: true)
                .Build();

            await component.Message.ModifyAsync(m =>
            {
                m.Content = $"{component.Message.Content}\n\n_Голосование завершено. Пары сформированы._";
                m.Components = disabled;
            });

            s.Cts.Cancel();
            _store.RemoveSession(sid);

            await component.FollowupAsync("Распределение завершено ✅", ephemeral: true);
        }

        private async Task UpdateLobbyMessageAsync(string sessionId, VoteSession s)
        {
            int count;
            lock (s.SyncRoot) count = s.Participants.Count;

            var newText = "Нажмите на кнопку, чтобы принять участие в распределении на команды.\n" +
                          $"Участников: **{count}**\n";

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
