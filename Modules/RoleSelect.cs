using AwalarBot.Contexts;
using AwalarBot.Models;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AwalarBot.Modules
{
	[RequireContext(ContextType.Guild)]
	public class RoleSelectModule : InteractionModuleBase
	{

		private readonly GuildsSettingsContext guildsSettingsContext_;

		public RoleSelectModule(GuildsSettingsContext guildsSettingsContext) =>
			guildsSettingsContext_ = guildsSettingsContext;


		[ComponentInteraction("role-select")]
		public async Task OnRoleSelected(string roleId)
		{

			var guildSettings = await guildsSettingsContext_.GuildsSettings.Include(gs => gs.RolesToSelect).FirstOrDefaultAsync(gs => gs.GuildId == Context.Guild.Id);
			if (guildSettings == null)
			{
				EmbedBuilder embed = new()
				{
					Title = "Ошибка",
					Color = new Color(255, 0, 0),
					Description = "Данной гильдии нет в базе данных, а следовательно невозможно выдать Вам роль"
				};

				await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
				return;
			}

			ulong? roleToSelectId = ulong.Parse(roleId);

			var roleToSelect = guildSettings.RolesToSelect.FirstOrDefault(rts => rts.Id == (roleToSelectId ?? 0));
			if (roleToSelect == null)
			{
				EmbedBuilder embed = new()
				{
					Title = "Ошибка",
					Color = new(255, 0, 0),
					Description = "Невозможно найти выбранную Вами роль в базе данных"
				};

				await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
				return;
			}

			if(Context.User is SocketGuildUser guildUser && guildUser.Roles.Any(r=>r.Id.ToString() == roleId))
			{
				EmbedBuilder embed = new()
				{
					Title = "Ошибка",
					Color = new Color(255,0,0),
					Description = "Я не могу добавить уже имеющуюся роль:D"
				};

				await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral:true);
			}

			if (guildSettings.RoleSelectChannelId != 0)
			{
				var rolesToSelect = guildSettings.RolesToSelect.ToList();
				ITextChannel channel = await Context.Guild.GetTextChannelAsync(guildSettings.RoleSelectChannelId);
                RestUserMessage message = (RestUserMessage)await channel.GetMessageAsync(guildSettings.RoleSelectMessageId);

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

				await message.ModifyAsync(msg =>
				{
					msg.Components = component.Build();
				});
			}

			await Context.Interaction.RespondWithModalAsync<RoleSelectModal>("modal-role-select:" + roleId);
		}

		[ModalInteraction("modal-role-select:*")]
		public async Task ModalResponse(string roleId, RoleSelectModal modal)
		{

			var guildSettings = await guildsSettingsContext_.GuildsSettings.Include(gs => gs.RolesToSelect).FirstOrDefaultAsync(gs => gs.GuildId == Context.Guild.Id);
			if (guildSettings == null)
			{
				EmbedBuilder embed = new()
				{
					Title = "Ошибка",
					Color = new Color(255, 0, 0),
					Description = "Данной гильдии нет в базе данных, а следовательно невозможно выдать Вам роль"
				};

				await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
				return;
			}

			ulong? roleToSelectId = ulong.Parse(roleId);

			var roleToSelect = guildSettings.RolesToSelect.FirstOrDefault(rts => rts.Id == (roleToSelectId ?? 0));
			if (roleToSelect == null)
			{
				EmbedBuilder embed = new()
				{
					Title = "Ошибка",
					Color = new(255, 0, 0),
					Description = "Не получилось найти выбранную Вами роль в базе данных"
				};

				await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
				return;
			}

			ITextChannel? channel = (ITextChannel)await Context.Guild.GetChannelAsync(guildSettings.AcceptRoleChannelId);
			if (channel == null)
            {
                EmbedBuilder embed = new()
                {
                    Title = "Ошибка",
                    Color = new(255, 0, 0),
                    Description = "Невозможно найти канал лидеров"
                };

                await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

			EmbedBuilder messageEmbed = new()
			{
				Title = "Запрос роли",
				Color = new Color(0,255,255),
				Fields =
				{
					new EmbedFieldBuilder()
					{
						Name = "Пользователь:",
						Value = Context.User.Mention,
						IsInline = true,
					},
					new EmbedFieldBuilder()
					{
						Name = "Никнейм:",
						Value = modal.Nickname,
						IsInline = true,
					},
					new EmbedFieldBuilder()
					{
						Name = "Роль для выдачи",
						Value = Context.Guild.Roles.FirstOrDefault(r=>r.Id.ToString() == roleId)!.Mention,
						IsInline = true,
					},
					new EmbedFieldBuilder()
					{
						Name = "Описание кнопок:",
						Value = "*[**Выдать**]* - выдаёт пользователю запрашиваемую роль и отправляет ему уведомление об этом\n*[**Удалить**]* - удаляет это сообщение без уведомления пользователя\n*[**Отказать**]* - отказывает опользователю в получении роли и отправляет ему уведомление об этом"
					}
				}
			};
			ComponentBuilder component = new ComponentBuilder()
				.WithButton("Выдать",$"role-req-accept:{Context.User.Id},{roleId}",ButtonStyle.Success)
				.WithButton("Удалить", $"role-req-delete:{roleId}", ButtonStyle.Danger)
				.WithButton("Отказать",$"role-req-deny:{Context.User.Id},{roleId}",ButtonStyle.Danger);

			EmbedBuilder respondEmbed = new()
			{
				Title = "Успешно",
				Color = new Color(0,255,0),
				Description = "Ваш запрос на получение роли был отправлен. Ожидайте"
			};
			

			await channel.SendMessageAsync(embed: messageEmbed.Build(), components:component.Build());

			await Context.Interaction.RespondAsync(embed: respondEmbed.Build(),ephemeral:true);

		}

		[ComponentInteraction("role-req-accept:*,*")]
		public async Task AcceptRoleRquest(ulong userId, ulong roleId)
		{
			
			if (Context.User is not SocketGuildUser user)
				return;

			var role = guildsSettingsContext_.RolesToSelect.FirstOrDefault(rts=>rts.Id == roleId);
			if(role == null)
				return;

			if(!await CheckUserPermissions(user, role))
                return;

			IGuildUser target = await Context.Guild.GetUserAsync(userId);

			await target.AddRoleAsync(roleId);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

			await component.UpdateAsync(msg =>
			{
				var embed = component.Message.Embeds.First().ToEmbedBuilder();
				embed.Color = new Color(0,255,0);
				embed.Description = $"{Context.User.Mention} - Принято";

				msg.Embed = embed.Build();
                msg.Components = null;
			});

            
			EmbedBuilder userMessageEmbed = new()
			{
				Title = "Информация",
				Color = new Color(0,255,255),
				Description = "Ваш запрос на получение роли был одобрен"
			};

			await target.SendMessageAsync(embed: userMessageEmbed.Build());

        }

		[ComponentInteraction("role-req-delete:*")]
		public async Task DeleteRoleRequest(ulong roleId)
        {

            if (Context.User is not SocketGuildUser user)
                return;

            var role = guildsSettingsContext_.RolesToSelect.FirstOrDefault(rts => rts.Id == roleId);
            if (role == null)
                return;

            if (!await CheckUserPermissions(user, role))
                return;

            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            await component.Message.DeleteAsync();

        }

		[ComponentInteraction("role-req-deny:*,*")]
		public async Task DenyRoleRquest(ulong userId, ulong roleId)
		{

            if (Context.User is not SocketGuildUser user)
                return;

            var role = guildsSettingsContext_.RolesToSelect.FirstOrDefault(rts => rts.Id == roleId);
            if (role == null)
                return;

			if (!await CheckUserPermissions(user, role))
				return;

            IGuildUser target = await Context.Guild.GetUserAsync(userId);

            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            await component.UpdateAsync(msg =>
            {
                var embed = component.Message.Embeds.First().ToEmbedBuilder();
                embed.Color = new Color(255, 0, 0);
				embed.Description = $"{Context.User.Mention} - Отклонено";

                msg.Embed = embed.Build();
                msg.Components = null;
            });

            EmbedBuilder userMessageEmbed = new()
            {
                Title = "Информация",
                Color = new Color(0, 255, 255),
                Description = "Ваш запрос на получение роли был отклонён"
            };

            await target.SendMessageAsync(embed: userMessageEmbed.Build());

        }

		private async Task<bool> CheckUserPermissions(SocketGuildUser user, RoleToSelect role)
		{
			
			bool fractionBool = user.Roles.Any(r=>r.Id == role.LeaderRoleId || r.Id == role.DeputyRoleId);
			bool guildBool = user.GuildPermissions.ManageRoles;

			if(!fractionBool && !guildBool)
			{
                EmbedBuilder embed = new()
                {
                    Title = "Ошибка",
                    Color = new Color(255, 0, 0),
                    Description = $"НЕСАНКЦИОНИРОВАННЫЙ ДОСТУП\nУ пользователя {user.Mention} нет ни роли админа, ни роли Лидера фракции, ни роли заместителя лидера фракции"
                };

                await Context.Interaction.RespondAsync(embed: embed.Build());

                return false;
            }
            return true;
		}

	}

	public class RoleSelectModal : IModal
	{
		public string? Title => "Получение роли";

		[InputLabel("Ваш никнейм:")]
		[ModalTextInput("nickname", TextInputStyle.Short, "Введите сюда свой никнейм")]
		[RequiredInput(true)]
		public string Nickname { get; set; }
	}
}
