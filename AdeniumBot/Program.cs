using Discord;
using Discord.WebSocket;
using Adenium.Handlers;
using Adenium.Services;

namespace Adenium
{
    class Program
    {
        private DiscordSocketClient _client = default!;
        private SessionStore _sessions = default!;
        private CommandRegistrar _registrar = default!;
        private StartCommandHandler _startHandler = default!;
        private ButtonHandler _buttonHandler = default!;
        private SessionLifecycle _lifecycle = default!;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
            };

            _client = new DiscordSocketClient(config);
            _client.Log += msg => { Console.WriteLine(msg.ToString()); return Task.CompletedTask; };
            
            _sessions = new SessionStore();
            _registrar = new CommandRegistrar(_client);
            _lifecycle = new SessionLifecycle(_client, _sessions);
            _startHandler = new StartCommandHandler(_client, _sessions, _lifecycle);
            _buttonHandler = new ButtonHandler(_client, _sessions, _lifecycle);

            _client.Ready += _registrar.OnReadyAsync;
            _client.SlashCommandExecuted += _startHandler.OnSlashCommandAsync;
            _client.ButtonExecuted += _buttonHandler.OnButtonAsync;

            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Задай переменную окружения DISCORD_TOKEN с токеном бота.");
                return;
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }
    }
}