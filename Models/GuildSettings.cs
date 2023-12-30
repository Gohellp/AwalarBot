using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AwalarBot.Models
{
    public class GuildSettings
    {

        public GuildSettings(ulong guildId)
        {
            GuildId = guildId;
            TextCommandPrefix = "+";
            OnlyInBotChannel = false;
            BotChannelId = 0;
            BanDuration = "3h";
            MuteDuration = "3h";
            MaxWarnsCount = 3;
            WarnDuration = "7d";
            AcceptRoleChannelId = 0;
            RoleSelectMessageId = 0;
            RoleSelectChannelId = 0;
        }

        [Key]
        public ulong GuildId { get; set; }

        public string TextCommandPrefix { get; set; }

        public bool OnlyInBotChannel {  get; set; }

        public ulong BotChannelId { get; set; }

        public string BanDuration { get; set; }

        public string MuteDuration { get; set; }

        public string WarnDuration { get; set; }

        public uint MaxWarnsCount { get; set; }

        public ulong AcceptRoleChannelId { get; set; }

        public ulong RoleSelectMessageId { get; set; }

        public ulong RoleSelectChannelId { get; set; }


        public ICollection<Punishment> Punishments { get; set; } = new List<Punishment>();

        public ICollection<RoleToSelect> RolesToSelect { get; set; } = new List<RoleToSelect>();

        public ICollection<FightChannel> FightChannels { get; set; } = new List<FightChannel>();
    }
}
