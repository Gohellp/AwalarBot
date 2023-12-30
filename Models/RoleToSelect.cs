

namespace AwalarBot.Models
{
    public class RoleToSelect
    {
        public required ulong Id { get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }

        public required ulong LeaderRoleId { get; set; }

        public required ulong DeputyRoleId { get; set; }


        public ulong GuildId { get; set; }
        public GuildSettings? GuildSettings { get; set; }
    }
}
