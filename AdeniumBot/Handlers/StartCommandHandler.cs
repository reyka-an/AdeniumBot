using Discord;
using Discord.WebSocket;
using AdeniumBot.Services;

namespace AdeniumBot.Handlers
{
    /// <summary>
    /// Обработчик слэш-команды <c>/start</c>.
    /// Создаёт сессию набора в текущем канале, добавляет автора команды в участники
    /// и отправляет сообщение с кнопками «Участвовать» и «Старт».
    /// </summary>
    public class StartCommandHandler(DiscordSocketClient client, SessionStore store, SessionLifecycle lifecycle)
    {
        /// <summary>
        /// Таймаут авто-завершения сессии набора.
        /// </summary>
        private static readonly TimeSpan AutoTimeout = TimeSpan.FromMinutes(20);

        /// <summary>
        /// Обработчик выполнения слэш-команды.
        /// </summary>
        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName != "start")
                return;

            try
            {
                // Безопасная проверка типа канала
                if (command.Channel is not ISocketMessageChannel msgChannel)
                {
                    await command.RespondAsync("Этот тип канала не поддерживается.", ephemeral: true);
                    return;
                }

                var channelId = msgChannel.Id;

                // Не допускаем параллельных наборов в одном канале
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

                lifecycle.StartAutoTimeout(sessionId, session, AutoTimeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StartCommandHandler] Ошибка: {ex}");

                // Если мы ещё не отвечали — отвечаем, иначе отправляем фоллоу-ап
                try
                {
                    if (!command.HasResponded)
                        await command.RespondAsync("Произошла ошибка при запуске набора. Попробуйте ещё раз позже.", ephemeral: true);
                    else
                        await command.FollowupAsync("Произошла ошибка при запуске набора. Попробуйте ещё раз позже.", ephemeral: true);
                }
                catch
                {
                    // скрываем вторичную ошибку отправки сообщения
                }
            }
        }
    }
}
