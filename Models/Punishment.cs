using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace AwalarBot.Models
{
    public class Punishment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public required string Reason { get; set; }

        public required ulong AdminId { get; set; }

        public required PunishmentType Type { get; set; }

        public required DateTime Expire {  get; set; }

        [DefaultValue(false)]
        public bool Expired { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Executed {  get; set; }


        public required ulong UserId { get; set; }
        
        public GuildSettings GuildSettings { get; set; }
    }
    public enum PunishmentType
    {
        Ban,
        Kick,
        Mute,
        Warn
    }
}
