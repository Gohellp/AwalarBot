using AwalarBot.Models;
using AwalarBot.Contexts;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AwalarBot.Services
{
	public class DiscordStartupService : IHostedService
	{
		private readonly IConfiguration _config;
		private readonly DiscordSocketClient _discord;
		private readonly ILogger<DiscordSocketClient> _logger;
		private readonly GuildsSettingsContext _guildsSettingsContext;

		public DiscordStartupService(
			IConfiguration config,
			DiscordSocketClient discord,
			ILogger<DiscordSocketClient> logger,
			GuildsSettingsContext guildsSettingsContext)
		{
			_discord = discord;
			_config = config;
			_logger = logger;
			_guildsSettingsContext = guildsSettingsContext;

			_discord.Log += msg => LogHelper.OnLogAsync(_logger, msg);
			_discord.JoinedGuild += OnJoinedGuild;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await _discord.LoginAsync(TokenType.Bot, _config["awalarToken"]);
			await _discord.StartAsync();
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await _discord.LogoutAsync();
			await _discord.StopAsync();
		}

		public async Task OnJoinedGuild(SocketGuild guild)
		{
			GuildSettings? newGuild = await _guildsSettingsContext.GuildsSettings.FindAsync(guild.Id.ToString());

			if (newGuild == null)
			{
				newGuild = new GuildSettings(guildId: guild.Id)
				{
					GuildId = guild.Id
                };
				_guildsSettingsContext.GuildsSettings.Add(newGuild);
				_guildsSettingsContext.SaveChanges();
            }
		}

	}
}
