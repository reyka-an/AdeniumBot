using Discord;
using Discord.WebSocket;
using Adenium.Handlers;
using Adenium.Services;
using Microsoft.EntityFrameworkCore;

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
        private ProfileCommandHandler _profileHandler = default!;
        private RelationsCommandHandler _relationsHandler = default!;
        private Adenium.Handlers.RoleExpHandler _roleExpHandler = default!;
        private Adenium.Handlers.TopCommandHandler _topHandler = default!;
        private PairingService _pairing = default!;
        private Adenium.Handlers.RoleExpRulesCommandHandler _roleExpRulesHandler = default!;
        private Adenium.Handlers.RecalcAllCommandHandler _recalcAllHandler = default!;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers
            };

            _client = new DiscordSocketClient(config);
            _client.Log += msg => { Console.WriteLine(msg.ToString()); return Task.CompletedTask; };
            
            _pairing        = new PairingService();  
            _sessions   = new SessionStore();
            _registrar  = new CommandRegistrar(_client);
            _lifecycle  = new SessionLifecycle(_client, _sessions);
            _startHandler     = new StartCommandHandler(_client, _sessions, _lifecycle);
            _buttonHandler  = new ButtonHandler(_client, _sessions, _lifecycle, _pairing);
            _profileHandler   = new ProfileCommandHandler(_client);
            _relationsHandler = new RelationsCommandHandler();
            _roleExpHandler   = new Adenium.Handlers.RoleExpHandler();
            _topHandler       = new Adenium.Handlers.TopCommandHandler(_client);
            _roleExpRulesHandler = new Adenium.Handlers.RoleExpRulesCommandHandler(_client);
            _recalcAllHandler = new Adenium.Handlers.RecalcAllCommandHandler(_client);

            var questHandler = new QuestCommandHandler();

            _client.Ready                += _registrar.OnReadyAsync;
            _client.ButtonExecuted       += _buttonHandler.OnButtonAsync;
            _client.SlashCommandExecuted += _startHandler.OnSlashCommandAsync;
            _client.SlashCommandExecuted += _relationsHandler.OnSlashCommandAsync;
            _client.SlashCommandExecuted += _profileHandler.OnSlashCommandAsync;
            _client.GuildMemberUpdated   += _roleExpHandler.OnGuildMemberUpdated;
            _client.SlashCommandExecuted += _topHandler.OnSlashCommandAsync;
            _client.SlashCommandExecuted += questHandler.OnSlashCommandAsync;
            _client.SlashCommandExecuted += _roleExpRulesHandler.OnSlashCommandAsync;
            _client.SlashCommandExecuted += _recalcAllHandler.OnSlashCommandAsync;

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
