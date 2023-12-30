using AwalarBot.Contexts;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace AwalarBot.Services
{
    public class FightHandlingService : IHostedService
    {

        private readonly DiscordSocketClient discord_;
        private readonly GuildsSettingsContext guildsSettingsContext_;

        public FightHandlingService(
            DiscordSocketClient discord,
            GuildsSettingsContext guildsSettingsContext)
        {
            discord_ = discord;
            guildsSettingsContext_ = guildsSettingsContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            discord_.MessageReceived += FightHandler;

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        private async Task FightHandler(SocketMessage message)
        {
            if (message.Content.Length < 10)
                return;

            if (message.Channel is not SocketGuildChannel guildChannel)
                return;

            var guildSettings = guildsSettingsContext_.GuildsSettings.Include(gs => gs.FightChannels).FirstOrDefault(gs => gs.GuildId == guildChannel.Guild.Id);
            if (guildSettings == null)
                return;

            var fightChannel = guildSettings.FightChannels.FirstOrDefault(fc => fc.StartChannelId == message.Channel.Id);
            if (fightChannel == null)
                return;

            EmbedBuilder embed = new()
            {
                Color = new Color(0, 255, 255),
                Fields =
                {
                    new EmbedFieldBuilder()
                    {
                        Name = "Забил",
                        Value = message.Author.Mention
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Информация",
                        Value = $"```{message.Content}```"
                    }
                },
                Author = new EmbedAuthorBuilder()
                {
                    Name = message.Author.Username,
                    IconUrl = message.Author.GetAvatarUrl()
                },
                ThumbnailUrl = message.Author.GetAvatarUrl()
            };
            ComponentBuilder components = new ComponentBuilder()
                .WithButton("Принять", "fight-accept", ButtonStyle.Success)
                .WithButton("Отклонить", "fight-decline", ButtonStyle.Danger);

            await message.Channel.SendMessageAsync(embed: embed.Build(), components: components.Build());

        }
    }
}
