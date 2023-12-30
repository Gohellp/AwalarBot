using AwalarBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AwalarBot.Contexts
{
    public class GuildsSettingsContext : DbContext
    {
        public DbSet<GuildSettings> GuildsSettings { get; set; }
        public DbSet<FightChannel> FightChannels { get; set; }
        public DbSet<RoleToSelect> RolesToSelect { get; set; }
        public DbSet<Punishment> Punishments { get; set; }


        public GuildsSettingsContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=localhost;user=alawar_bot;password=Make$41aw4rGreat;database=awalar;",
            new MySqlServerVersion(new Version(8, 1, 0)));
        }
    }
}
