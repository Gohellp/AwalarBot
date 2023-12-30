using AwalarBot.Models;
using AwalarBot.Contexts;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Discord.WebSocket;

namespace AwalarBot.Modules.SlashCommands
{
	[RequireContext(ContextType.Guild)]
	public class GuildSettingsModule : InteractionModuleBase
	{
		[Group("set","Set something in guild settings")]
		public class GuildSettingsSetters : InteractionModuleBase
		{
			private readonly GuildsSettingsContext guildsSettingsContext_;

			public GuildSettingsSetters(GuildsSettingsContext guildsSettingsContext) =>
				guildsSettingsContext_ = guildsSettingsContext;


			[SlashCommand("fight-channels","Set channels for fights")]
			[RequireUserPermission(GuildPermission.ManageChannels)]
			public async Task SetFightChannel([ChannelTypes(ChannelType.Text)] ITextChannel startChannel, [ChannelTypes(ChannelType.Text)] ITextChannel acceptedChannel)
			{

				var guildSettings = guildsSettingsContext_.GuildsSettings.Include(gs => gs.FightChannels).FirstOrDefault(gs => gs.GuildId == Context.Guild.Id);
				if(guildSettings == null)
				{
					guildSettings = new GuildSettings(Context.Guild.Id);
					guildsSettingsContext_.Add(guildSettings);
					guildsSettingsContext_.SaveChanges();
				}

				var fightChannel = guildSettings.FightChannels.FirstOrDefault(fc => fc.StartChannelId == startChannel.Id && fc.AcceptedChannelId == acceptedChannel.Id);
				if (fightChannel != null)
				{
					EmbedBuilder embedError = new()
					{
						Title = "Ошибка",
						Color = Color.Red,
						Description= "Данная запись уже существует"
					};
					await Context.Interaction.RespondAsync(embed: embedError.Build(), ephemeral: true);
					return;
				}

				fightChannel = new FightChannel()
				{
					StartChannelId = startChannel.Id,
					AcceptedChannelId = acceptedChannel.Id,
				};
				guildSettings.FightChannels.Add(fightChannel);
				guildsSettingsContext_.SaveChanges();

				EmbedBuilder embedSucces = new()
				{
					Title = "Успешно!",
					Color = Color.Green,
					Description = "Вы создали связь чатов для битв"
				};

				await Context.Interaction.RespondAsync(embed:embedSucces.Build(), ephemeral:true);
			}

			[SlashCommand("max-warns-count","Set max number of warns")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task SetMaxWarnCount(uint count)
			{

				var guildSettings = await guildsSettingsContext_.GuildsSettings.FirstOrDefaultAsync(gs=>gs.GuildId == Context.Guild.Id);
				if (guildSettings == null)
				{
					guildSettings = new GuildSettings(Context.Guild.Id);
					guildsSettingsContext_.Add(guildSettings);
					guildsSettingsContext_.SaveChanges();
				}

				guildSettings.MaxWarnsCount = count;
				guildsSettingsContext_.SaveChanges();

				EmbedBuilder embed = new()
				{
					Title = "Успешно",
					Color = new Color(0,255,0),
					Description = $"Вы установили значение {nameof(guildSettings.MaxWarnsCount)} на {count}"
				};

				await Context.Interaction.RespondAsync(embed:embed.Build());

			}

			[SlashCommand("role-message","Set role select message")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task SetRoleSelectMessage([ChannelTypes(ChannelType.Text)] ITextChannel channel, string description)
			{

				var guildSettings = await guildsSettingsContext_.GuildsSettings.Include(gs => gs.RolesToSelect).FirstOrDefaultAsync(gs=>gs.GuildId == Context.Guild.Id);
				if (guildSettings == null)
				{
					guildSettings = new GuildSettings(Context.Guild.Id);
					guildsSettingsContext_.Add(guildSettings);
					guildsSettingsContext_.SaveChanges();
				}

				var rolesToSelect = guildSettings.RolesToSelect.ToList();
				if(rolesToSelect.Count==0)
				{
					EmbedBuilder embed = new()
					{
						Title = "Ошибка",
						Color = new Color(255,0,0),
						Description = "В базе данных не указана ни одна роль"
					};

					await Context.Interaction.RespondAsync(embed: embed.Build(),ephemeral:true);
					return;
				}

				if(guildSettings.AcceptRoleChannelId==0)
				{
					EmbedBuilder embed = new()
					{
						Title = "Ошибка",
						Color = new Color(255, 0, 0),
						Description = "Не установлен чат для получения заявок на получение роли.\nСначала установите этот чат с помощью команды `/set leaders-channel`"
					};

					await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
					return;
				}

				SelectMenuBuilder menu = new()
				{
					CustomId = "role-select",
					Placeholder = "Выбор роли",
					MaxValues = 1
				};

				for(int i = 0; i<rolesToSelect.Count && i<20; i++)
				{
					var role = rolesToSelect[i];
					menu.AddOption(new SelectMenuOptionBuilder()
					{
						Label = role.Name,
						Value = role.Id.ToString(),
						Description = role.Description,
					});
				}

				ComponentBuilder component = new ComponentBuilder()
					.WithSelectMenu(menu);

				EmbedBuilder messageEmbed = new()
				{
					Color = new Color(0, 255,255),
					Author = new EmbedAuthorBuilder()
					{
						Name = Context.Client.CurrentUser.GlobalName,
						IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
					},
					Fields =
					{
						new EmbedFieldBuilder()
						{
							Name = "Меню запроса роли",
							Value = description
						}
					}
				};

				IUserMessage msg = await channel.SendMessageAsync(embed: messageEmbed.Build(), components:component.Build());

				guildSettings.RoleSelectMessageId = msg.Id;
				guildSettings.RoleSelectChannelId = msg.Channel.Id;
				guildsSettingsContext_.SaveChanges();

				EmbedBuilder respondEmbed = new()
				{
					Title = "Успешно",
					Color = new Color(0, 255, 0),
					Description = "Сообщение с выбором ролей было успешно доставлено!"
				};

				await Context.Interaction.RespondAsync(embed:respondEmbed.Build(), ephemeral:true);

			}

			[SlashCommand("leaders-channel", "Set channel for accept/reject role requests")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task SetLeadersChannel([ChannelTypes(ChannelType.Text)] ITextChannel channel)
			{

				var guildSettings = guildsSettingsContext_.GuildsSettings.FirstOrDefault(gs=>gs.GuildId == Context.Guild.Id);
				if (guildSettings == null)
				{
					guildSettings = new GuildSettings(Context.Guild.Id);
					guildsSettingsContext_.Add(guildSettings);
					guildsSettingsContext_.SaveChanges();
				}

				guildSettings.AcceptRoleChannelId = channel.Id;
				guildsSettingsContext_.SaveChanges();

				EmbedBuilder embed = new()
				{
					Title = "Успешно",
					Color = new Color(0,255,0),
					Description = "Чат для принятия/отказа в получении ролей был назначен."
				};

				await Context.Interaction.RespondAsync(embed:embed.Build(), ephemeral:true);

			}

			[SlashCommand("duration-warn", "Set duration for warn")]
			public async Task SetWarnDuration(string duration)
			{

                var guildSettings = await guildsSettingsContext_.GuildsSettings.FirstOrDefaultAsync(gs => gs.GuildId == Context.Guild.Id);
                if (guildSettings == null)
                {
                    guildSettings = new GuildSettings(Context.Guild.Id);
                    guildsSettingsContext_.Add(guildSettings);
                    guildsSettingsContext_.SaveChanges();
                }

				guildSettings.WarnDuration = duration;
                guildsSettingsContext_.SaveChanges();

				EmbedBuilder embed = new()
				{
					Title = "Успешно",
					Color = new Color(0,255,0),
					Description = $"Вы установили значение {nameof(guildSettings.WarnDuration)} на {duration}"
				};

				await Context.Interaction.RespondAsync(embed: embed.Build());

            }

			[SlashCommand("duration-mute", "Set duration for mute")]
			public async Task SetMuteDuration(string duration)
			{

                var guildSettings = await guildsSettingsContext_.GuildsSettings.FirstOrDefaultAsync(gs => gs.GuildId == Context.Guild.Id);
                if (guildSettings == null)
                {
                    guildSettings = new GuildSettings(Context.Guild.Id);
                    guildsSettingsContext_.Add(guildSettings);
                    guildsSettingsContext_.SaveChanges();
                }

				guildSettings.MuteDuration = duration;
				guildsSettingsContext_.SaveChanges();

                EmbedBuilder embed = new()
                {
                    Title = "Успешно",
                    Color = new Color(0, 255, 0),
                    Description = $"Вы установили значение {nameof(guildSettings.MuteDuration)} на {duration}"
                };

                await Context.Interaction.RespondAsync(embed: embed.Build());

            }

			[SlashCommand("duration-ban", "Set duration for ban")]
			public async Task SetBanDuration(string duration)
			{

                var guildSettings = await guildsSettingsContext_.GuildsSettings.FirstOrDefaultAsync(gs => gs.GuildId == Context.Guild.Id);
                if (guildSettings == null)
                {
                    guildSettings = new GuildSettings(Context.Guild.Id);
                    guildsSettingsContext_.Add(guildSettings);
                    guildsSettingsContext_.SaveChanges();
                }

				guildSettings.BanDuration = duration;
                guildsSettingsContext_.SaveChanges();

                EmbedBuilder embed = new()
                {
                    Title = "Успешно",
                    Color = new Color(0, 255, 0),
                    Description = $"Вы установили значение {nameof(guildSettings.BanDuration)} на {duration}"
                };

                await Context.Interaction.RespondAsync(embed: embed.Build());

            }

		}

		[Group("add","Add something to guild settings")]
		public class GuildSettingsAdders : InteractionModuleBase
		{

			private readonly GuildsSettingsContext guildsSettingsContext_;

			public GuildSettingsAdders(GuildsSettingsContext guildsSettingsContext) =>
				guildsSettingsContext_ = guildsSettingsContext;


			[SlashCommand("role-to-select","Add role for select menu")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task AddRoleToSelectAsync(IRole target, IRole leaderRole, IRole deputyRole, string name, string description)
			{

				var guildSettings = await guildsSettingsContext_.GuildsSettings.Include(gs => gs.RolesToSelect).FirstOrDefaultAsync(gs => gs.GuildId == Context.Guild.Id);
				if (guildSettings == null)
				{
					guildSettings = new GuildSettings(Context.Guild.Id);
					guildsSettingsContext_.Add(guildSettings);
					guildsSettingsContext_.SaveChanges();
				}

				if(guildSettings.RolesToSelect.Any(rts=>rts.Id == target.Id))
				{
					EmbedBuilder embed = new()
					{
						Title = "Ошибка",
						Color = new Color(255,0,0),
						Description = "Вы пытаетесь добавить уже существующую роль"
					};

					await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
					return;
				}

				RoleToSelect roleToSelect = new()
				{
					Name = name,
					Id = target.Id,
					Description = description,
					LeaderRoleId = leaderRole.Id,
					DeputyRoleId = deputyRole.Id,
					GuildSettings = guildSettings,
					
				};

				guildsSettingsContext_.RolesToSelect.Add(roleToSelect);
				guildsSettingsContext_.SaveChanges();

				EmbedBuilder respondEmbed = new()
				{
					Title = "Успешно",
					Color = new Color(0,255,0),
					Description = "Данные для выбора данной роли были успешно добавлены"
				};

				if (guildSettings.RoleSelectChannelId != 0)
				{
					var rolesToSelect = guildSettings.RolesToSelect.ToList();
					ITextChannel channel = await Context.Guild.GetTextChannelAsync(guildSettings.RoleSelectChannelId);
					SocketMessageComponent message = (SocketMessageComponent)await channel.GetMessageAsync(guildSettings.RoleSelectMessageId);

					SelectMenuBuilder menu = new()
					{
						CustomId = "role-select",
						Placeholder = "Выбор роли",
						MaxValues = 1
					};

					for (int i = 0; i < rolesToSelect.Count && i < 20; i++)
					{
						var role = rolesToSelect[i];
						menu.AddOption(new SelectMenuOptionBuilder()
						{
							Label = role.Name,
							Value = role.Id.ToString(),
							Description = role.Description,
						});
					}

					ComponentBuilder component = new ComponentBuilder()
						.WithSelectMenu(menu);

					await message.UpdateAsync(msg =>
					{
						msg.Components = component.Build();
					});

					respondEmbed.AddField("Дополнительно:","Было изменено сообщение с выбором ролей");
				}

				await Context.Interaction.RespondAsync(embed:respondEmbed.Build(), ephemeral: true);

			}
		}

		[Group("remove","Remove something from guild settings")]
		public class GuildSettingsRemovers : InteractionModuleBase
		{

			private readonly GuildsSettingsContext guildsSettingsContext_;

			public GuildSettingsRemovers(GuildsSettingsContext guildsSettingsContext) =>
				guildsSettingsContext_ = guildsSettingsContext;


			[SlashCommand("role-to-select","Remove role for select menu")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task RemoveRoleToSelectAsync(IRole target)
			{

				var guildSettings = await guildsSettingsContext_.GuildsSettings.Include(gs => gs.RolesToSelect).FirstOrDefaultAsync(gs=>gs.GuildId == Context.Guild.Id);
				if (guildSettings == null)
				{
					guildSettings = new GuildSettings(Context.Guild.Id);
					guildsSettingsContext_.Add(guildSettings);
					guildsSettingsContext_.SaveChanges();
				}

				var roleToSelect = guildSettings.RolesToSelect.FirstOrDefault(rts=>rts.Id==target.Id);

				if (roleToSelect == null)
				{
					EmbedBuilder embed = new()
					{
						Title = "Ошибка",
						Color = new Color(255,0,0),
						Description = "Вы пытаетесь удалить несуществующую роль"
					};

					await Context.Interaction.RespondAsync(embed:embed.Build(), ephemeral:true);
					return;
				}

				guildSettings.RolesToSelect.Remove(roleToSelect);
				guildsSettingsContext_.SaveChanges();

				EmbedBuilder respondEmbed = new()
				{
					Title = "Успешно",
					Color = new Color(0, 255, 0),
					Description = "Данная роль была успешно удалена"
				};

				if (guildSettings.RoleSelectChannelId != 0)
				{
					var rolesToSelect = guildSettings.RolesToSelect.ToList();
					ITextChannel channel = await Context.Guild.GetTextChannelAsync(guildSettings.RoleSelectChannelId);
					SocketMessageComponent message = (SocketMessageComponent)await channel.GetMessageAsync(guildSettings.RoleSelectMessageId);

					SelectMenuBuilder menu = new()
					{
						CustomId = "role-select",
						Placeholder = "Выбор роли",
						MaxValues = 1
					};

					for (int i = 0; i < rolesToSelect.Count && i < 20; i++)
					{
						var role = rolesToSelect[i];
						menu.AddOption(new SelectMenuOptionBuilder()
						{
							Label = role.Name,
							Value = role.Id.ToString(),
							Description = role.Description,
						});
					}

					ComponentBuilder component = new ComponentBuilder()
						.WithSelectMenu(menu);

					await message.UpdateAsync(msg =>
					{
						msg.Components = component.Build();
					});

					respondEmbed.AddField("Дополнительно:", "Было изменено сообщение с выбором ролей");
				}

				await Context.Interaction.RespondAsync(embed: respondEmbed.Build(), ephemeral: true);

			}
		}

		[Group("get","Get something from guild settings")]
		public class GuildSettingsGetters : InteractionModuleBase
		{

			private readonly GuildsSettingsContext guildsSettingsContext_;

			public GuildSettingsGetters(GuildsSettingsContext guildsSettingsContext) =>
				guildsSettingsContext_ = guildsSettingsContext;

			[SlashCommand("settings","Get guild's settings")]
			[RequireUserPermission(GuildPermission.ManageChannels)]
			public async Task GetGuildSettingsAsync()
			{

				var guildSettings = guildsSettingsContext_.GuildsSettings.FirstOrDefault(gs=>gs.GuildId == Context.Guild.Id);
				if(guildSettings == null)
				{
					guildSettings = new(Context.Guild.Id);
					guildsSettingsContext_.GuildsSettings.Add(guildSettings);
					guildsSettingsContext_.SaveChanges();
				}

				EmbedBuilder embed = new()
				{
					Title = Context.Guild.Name + " - настройки",
					ThumbnailUrl = Context.Guild.IconUrl,
					Color = new Color(0, 255, 255)
				};

				embed.AddField(nameof(guildSettings.BanDuration), guildSettings.BanDuration, true)
					.AddField(nameof(guildSettings.MuteDuration), guildSettings.MuteDuration, true)
					.AddField(nameof(guildSettings.WarnDuration), guildSettings.WarnDuration, true)
					.AddField(nameof(guildSettings.MaxWarnsCount), guildSettings.MaxWarnsCount, true);

				if(guildSettings.AcceptRoleChannelId!=0)
				{
					var channel = await Context.Guild.GetTextChannelAsync(guildSettings.AcceptRoleChannelId);
					embed.AddField(nameof(guildSettings.AcceptRoleChannelId), channel.Mention, true);
				}
				if(guildSettings.RoleSelectMessageId!=0)
				{
					var channel = await Context.Guild.GetTextChannelAsync(guildSettings.RoleSelectChannelId);
					var message = await channel.GetMessageAsync(guildSettings.RoleSelectMessageId);

					embed.AddField(nameof(guildSettings.RoleSelectChannelId), channel.Mention, true)
						.AddField(nameof(guildSettings.RoleSelectMessageId), message.GetJumpUrl(), true);
				}

				await Context.Interaction.RespondAsync(embed:embed.Build());

			}
		}
	}
}
