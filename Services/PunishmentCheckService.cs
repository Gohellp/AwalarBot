using AwalarBot.Models;
using AwalarBot.Contexts;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace AwalarBot.Services
{
	internal class PunishmentCheckService : IHostedService
	{
		private readonly DiscordSocketClient discord_;
		private readonly GuildsSettingsContext guildsSettingsContext_;

		private Timer? timer_;

		public PunishmentCheckService(GuildsSettingsContext guildsSettingsContext,
									  DiscordSocketClient discord)
		{
			discord_ = discord;
			guildsSettingsContext_ = guildsSettingsContext;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			timer_ = new Timer(async state => await CheckPunishments(), null, 0, 60_000);
			await Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			timer_?.Dispose();
			return Task.CompletedTask;
		}

		private async Task CheckPunishments()
		{

			var punishments = await guildsSettingsContext_.Punishments.Where(p => !p.Expired && p.Expire<=DateTime.Now).ToListAsync();

			if (punishments.Count < 1)
				return;

			foreach(var punishment in punishments)
			{
				punishment.Expired = true;
				guildsSettingsContext_.SaveChanges();

                var guild = discord_.GetGuild(punishment.GuildSettings.GuildId);
				if (guild == null)
					continue;

                var user = guild.GetUser(punishment.UserId);
				if (user == null)
					continue;

                EmbedBuilder embed = new EmbedBuilder()
				{
					Title = "Срок наказания истёк",
					Fields =
					{
						new EmbedFieldBuilder()
						{
							Name = punishment.Type.ToString(),
							Value = "Reason:\n"+punishment.Reason
						},
						new EmbedFieldBuilder()
						{
							Name = "Дата получения наказания:",
							Value = $"<t:{new DateTimeOffset(punishment.Executed).ToUnixTimeSeconds()}>"
						}
					}
				}.WithCurrentTimestamp();

				RequestOptions options = new()
				{
					AuditLogReason = "Наказание истекло"
				};

				switch (punishment.Type)
				{
					case PunishmentType.Ban:

						await guild.RemoveBanAsync(punishment.UserId, options);

					break;
					case PunishmentType.Mute:

						//await user.RemoveRoleAsync(punishment.GuildSettings.MuteRoleId, options);

                        await user.RemoveTimeOutAsync();

                    break;
				}

                await user.SendMessageAsync(embed: embed.Build());
            }
		}
	}
}
