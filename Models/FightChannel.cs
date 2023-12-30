namespace AwalarBot.Models
{
    public class FightChannel
    {
        public int Id { get; set; }

        public ulong? StartChannelId { get; set; }

        public ulong? AcceptedChannelId { get; set; }


        public ulong GuildId { get; set; }
        public GuildSettings? GuildSettings { get; set; }
    }
}
