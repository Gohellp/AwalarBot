using AwalarBot.Models;
using AwalarBot.Contexts;
using AwalarBot.Utilities;
using Discord;
using Discord.Interactions;
using System;
using Microsoft.EntityFrameworkCore;

namespace AwalarBot.Modules.SlashCommands
{

	[EnabledInDm(false)]
	[RequireContext(ContextType.Guild)]
	public class ModerationModule : InteractionModuleBase
	{

		private readonly GuildsSettingsContext guildsSettingsContext_;

		public ModerationModule(GuildsSettingsContext guildsSettingsContext) =>
			guildsSettingsContext_ = guildsSettingsContext;


		[SlashCommand("ban", "Забанить пользователя в гильдии")]
		[RequireBotPermission(GuildPermission.BanMembers)]
		[RequireUserPermission(GuildPermission.BanMembers)]
		public async Task BanUserAsync(IGuildUser user, string reason, string duration = "")
		{

			var guildSettings = GetGuildSettings(Context.Guild.Id);

			if (duration == "")
				duration = guildSettings.BanDuration;

			EmbedBuilder embed = new()
			{
				Title = "Вы были забанены",
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.User.Username,
					IconUrl = Context.User.GetAvatarUrl()
				},
				Fields =
				{
					new EmbedFieldBuilder()
					{
						Name = "Причина:",
						Value = reason
					},
					new EmbedFieldBuilder()
					{
						Name = "Администратор:",
						Value = $"{Context.User.Username} ({Context.User.Mention})",
						IsInline = true
					},
					new EmbedFieldBuilder()
					{
						Name = "Продолжительность:",
						Value = duration,
						IsInline = true
					}
				}
			};
			EmbedBuilder contextRespondEmbed = new()
			{
				Title = "Выполнено",
				Color = Color.Green,
				Description = $"Вы забанили {user.DisplayName} / {user.Username} ({user.Id})"
			};

			await user.SendMessageAsync(embed: embed.Build());
			try
			{

				await user.BanAsync(reason: reason);

			}
			catch (Exception)
			{
				EmbedBuilder errorEmbed = new()
				{
					Title = "Ошибка",
					Color = new(255,0,0),
					Description = "Не получилось забанить пользователя"
				};
				await Context.Interaction.RespondAsync(embed:errorEmbed.Build(),ephemeral:true);
				return;
			}

			var punishment = new Punishment()
			{
				UserId = user.Id,
				Reason = reason,
				Type = PunishmentType.Ban,
				AdminId = Context.User.Id,
				Expire = Converters.DurationStringToDateTime(duration),
			};
			guildSettings.Punishments.Add(punishment);
			guildsSettingsContext_.SaveChanges();

			await Context.Interaction.RespondAsync(embed: contextRespondEmbed.Build(), ephemeral: true);

		}

		[SlashCommand("kick", "Выгнать пользователя из гильдии")]
		[RequireBotPermission(GuildPermission.KickMembers)]
		[RequireUserPermission(GuildPermission.KickMembers)]
		public async Task KickUserAsync(IGuildUser user, string reason)
		{

			var guildSettings = GetGuildSettings(Context.Guild.Id);

			EmbedBuilder embed = new()
			{
				Title = "Вас выгнали",
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.User.Username,
					IconUrl = Context.User.GetAvatarUrl()
				},
				Fields =
				{
					new EmbedFieldBuilder()
					{
						Name = "Причина:",
						Value = reason
					},
					new EmbedFieldBuilder()
					{
						Name = "Администратор:",
						Value = $"{Context.User.Username} ({Context.User.Mention})"
					}
				}
			};
			EmbedBuilder contextRespondEmbed = new()
			{
				Title = "Выполнено",
				Color = Color.Green,
				Description = $"Вы выгнали {user.DisplayName} / {user.Username} ({user.Id})"
			};

			await user.SendMessageAsync(embed: embed.Build());

			try
			{
				await user.KickAsync(reason);
			}
			catch (Exception)
			{
				EmbedBuilder errorEmbed = new()
				{
					Title = "Ошибка",
					Color = new(255, 0, 0),
					Description = "Не получилось выгнать пользователя"
				};
				await Context.Interaction.RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
				return;
			}

			var punishment = new Punishment()
			{
				Reason = reason,
				Type = PunishmentType.Kick,
				AdminId = Context.User.Id,
				Expire = DateTime.UtcNow,
				UserId = user.Id
			};
			guildSettings.Punishments.Add(punishment);
			guildsSettingsContext_.SaveChanges();

			await Context.Interaction.RespondAsync(embed: contextRespondEmbed.Build(), ephemeral: true);

		}

		[SlashCommand("mute", "Замьютить пользователя в гильдии")]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task MuteUserAsync(IGuildUser user, string reason, string duration = "")
		{

			var guildSettings = GetGuildSettings(Context.Guild.Id);

			if (duration == "")
				duration = guildSettings.MuteDuration;

			EmbedBuilder userMessageEmbed = new()
			{
				Title = "Вы получили мут",
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.User.Username,
					IconUrl = Context.User.GetAvatarUrl()
				},
				Fields =
				{
					new EmbedFieldBuilder()
					{
						Name = "Причина:",
						Value = reason
					},
					new EmbedFieldBuilder()
					{
						Name = "Администратор:",
						Value = $"{Context.User.Username} ({Context.User.Mention})",
						IsInline = true
					},
					new EmbedFieldBuilder()
					{
						Name = "Продолжительность:",
						Value = duration,
						IsInline = true
					}
				}
			};
			EmbedBuilder contextRespondEmbed = new()
			{
				Title = "Выполнено",
				Color = Color.Green,
				Description = "Вы замутили " + user.Mention
			};

			await user.SendMessageAsync(embed: userMessageEmbed.Build());

			try
			{
				/*await user.AddRoleAsync(guildSettings.MuteRoleId, new RequestOptions()
				{
					AuditLogReason = reason,
				});*/

				TimeSpan timeOutDuration = Converters.DurationStringToTimeSpan(duration);
				if (timeOutDuration.Days > 28)
				{
					timeOutDuration = new TimeSpan(28, 0, 0, 0);
					contextRespondEmbed.Color = new Color(255, 165, 0);
					contextRespondEmbed.AddField("Ошибка", "Максимальная продолжительность мута может быть 28 дней.\nПользователю выдан мут длительностью 28 дней.");
				}

				await user.SetTimeOutAsync(timeOutDuration, new RequestOptions()
				{
					AuditLogReason = reason,
				});
			}
			catch (Exception)
			{
				EmbedBuilder errorEmbed = new()
				{
					Title = "Ошибка",
					Color = new(255, 0, 0),
					Description = "Не получилось замьютить пользователя"
				};
				await Context.Interaction.RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
				return;
			}



			var punishment = new Punishment()
			{
				Reason = reason,
				Type = PunishmentType.Mute,
				AdminId = Context.User.Id,
				Expire = Converters.DurationStringToDateTime(duration),
				UserId = user.Id,
			};
			guildSettings.Punishments.Add(punishment);
			guildsSettingsContext_.SaveChanges();

			await Context.Interaction.RespondAsync(embed: contextRespondEmbed.Build(), ephemeral: true);

		}

		[SlashCommand("warn", "Предупредить пользователя о нарушении в гильдии")]
		[RequireBotPermission(GuildPermission.ModerateMembers)]
		[RequireUserPermission(GuildPermission.ModerateMembers)]
		public async Task WarnUserAsync(IGuildUser user, string reason)
		{

			var guildSettings = GetGuildSettings(Context.Guild.Id);

			EmbedBuilder embed = new()
			{
				Title = "Вы получили предупреждение",
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.User.Username,
					IconUrl = Context.User.GetAvatarUrl()
				},
				Fields =
				{
					new EmbedFieldBuilder()
					{
						Name = "Причина:",
						Value = reason
					},
					new EmbedFieldBuilder()
					{
						Name = "Администратор:",
						Value = $"{Context.User.Username} ({Context.User.Mention})",
						IsInline = true
					},
					new EmbedFieldBuilder()
					{
						Name = "Продолжительность:",
						Value = guildSettings.WarnDuration,
						IsInline = true
					}
				}

			};
			EmbedBuilder contextRespondEmbed = new()
			{
				Title = "Выполнено",
				Color = Color.Green,
				Description = "Вы предупредили " + user.Mention
			};

			await user.SendMessageAsync(embed: embed.Build());

			var punishment = new Punishment()
			{
				UserId = user.Id,
				Reason = reason,
				Type = PunishmentType.Warn,
				AdminId = Context.User.Id,
				Expire = Converters.DurationStringToDateTime(guildSettings.WarnDuration),
			};
			guildSettings.Punishments.Add(punishment);

			var userPunishments = guildSettings.Punishments.Where(p=>p.UserId == user.Id && p.Type == PunishmentType.Warn && (!p.Expired || p.Expire>DateTime.Now)).ToList();

			if (userPunishments.Count>=3)
            {
                contextRespondEmbed.AddField("Дополнительно", $"Количество предупреждений у пользователя больше {guildSettings.MaxWarnsCount}. Пользователю выдан мут на {guildSettings.MuteDuration}");

                try
				{
					TimeSpan timeOutDuration = Converters.DurationStringToTimeSpan(guildSettings.MuteDuration);
					if (timeOutDuration.Days > 28)
					{
						timeOutDuration = new TimeSpan(28, 0, 0, 0);
						contextRespondEmbed.Color = new Color(255, 165, 0);
						contextRespondEmbed.AddField("Ошибка значений в базе данных", $"Максимальная продолжительность мута может быть 28 дней. Проверьте значение поля {nameof(guildSettings.MuteDuration)}\nПользователю был выдан мут на 28 дней");
					}

					await user.SetTimeOutAsync(timeOutDuration);

				}
				catch (Exception)
				{
					contextRespondEmbed.Color = new Color(255,0,0);
					contextRespondEmbed.AddField("Ошибка", "Не удалось замьютить пользователя. Операция прервана");

                    await Context.Interaction.RespondAsync(embed: contextRespondEmbed.Build(), ephemeral: true);
					return;
                }

                guildsSettingsContext_.SaveChanges();

				EmbedBuilder userMuteEmbed = new()
				{
					Title = "Вы получили мут",
                    Fields =
                {
                    new EmbedFieldBuilder()
                    {
                        Name = "Причина:",
                        Value = "Превышено количество предупреждений"
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Администратор:",
                        Value = $"{Context.Client.CurrentUser.Username} ({Context.Client.CurrentUser.Mention})",
                        IsInline = true
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Продолжительность:",
                        Value = guildSettings.MuteDuration,
                        IsInline = true
                    }
                }

                };
                await user.SendMessageAsync(embed:userMuteEmbed.Build());
			}

			await Context.Interaction.RespondAsync(embed: contextRespondEmbed.Build(), ephemeral: true);

		}



		private GuildSettings GetGuildSettings(ulong guildId)
		{
			var guildSettings = guildsSettingsContext_.GuildsSettings.Include(gs=>gs.Punishments).FirstOrDefault(gs => gs.GuildId == guildId);
			if (guildSettings == null)
			{
				guildSettings = new GuildSettings(guildId: Context.Guild.Id) { GuildId = Context.Guild.Id };
				guildsSettingsContext_.GuildsSettings.Add(guildSettings);
				guildsSettingsContext_.SaveChanges();
			}

			return guildSettings;
		}

	}
}
