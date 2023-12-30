using AwalarBot.Contexts;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace AwalarBot.Modules.Buttons
{
    [RequireContext(ContextType.Guild)]
    public class FightModule : InteractionModuleBase
	{

		[ComponentInteraction("fight-accept")]
		public async Task AcceptFight()
		{

			SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

			using var guildSettingsContext = new GuildsSettingsContext();
			var fightChannels = guildSettingsContext.FightChannels.FirstOrDefault(fc => fc.StartChannelId == Context.Channel.Id);
			if (fightChannels == null)
				return;

			var channel = await Context.Guild.GetTextChannelAsync(fightChannels.AcceptedChannelId??0);
			if (channel == null)
				return;

			EmbedBuilder embed = component.Message.Embeds.First().ToEmbedBuilder();
			embed.AddField("Статус:", $"{Context.User.Mention} - Принято");
			embed.Color = new Color(0, 255, 0);

			await channel.SendMessageAsync(embed:embed.Build());

			await component.UpdateAsync(msg =>
			{
				msg.Embed = embed.Build();
				msg.Components = null;
			});

		}

		[ComponentInteraction("fight-decline")]
		public async Task DeclineFight()
		{

			SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

			await component.UpdateAsync(msg =>
			{
				EmbedBuilder embed = component.Message.Embeds.First().ToEmbedBuilder().AddField("Статус:", $"{Context.User.Mention} - Отказ");
				embed.Color = new Color(255, 0, 0);

				msg.Embed = embed.Build();
				msg.Components = null;
			});

			await RespondAsync("Отказано!", ephemeral: true);

		}

	}
}
