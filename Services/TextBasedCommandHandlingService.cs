using Discord;
using Discord.Commands;
using Discord.WebSocket;
using AwalarBot.Contexts;
using AwalarBot.Models;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace AwalarBot.Services
{
    public class TextBasedCommandHandlingService : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        private readonly GuildsSettingsContext _guildsSettingsContext;

        public TextBasedCommandHandlingService(
            DiscordSocketClient discord,
            CommandService commands,
            IServiceProvider services,
            GuildsSettingsContext guildsSettingsContext)
        {
            _discord = discord;
            _services = services;
            _commands = commands;
            _guildsSettingsContext = guildsSettingsContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_discord, message);

            GuildSettings? guildSettings = _guildsSettingsContext.GuildsSettings.FirstOrDefault(gs => gs.GuildId == context.Guild.Id);
            if (guildSettings == null)
                guildSettings = new GuildSettings(context.Guild.Id) 
                {
                    GuildId = context.Guild.Id,
                };

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasStringPrefix(guildSettings.TextCommandPrefix, ref argPos) ||
                message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            if (guildSettings.OnlyInBotChannel && context.Channel.Id != guildSettings.BotChannelId)
                return;


            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);
        }
    }
}
