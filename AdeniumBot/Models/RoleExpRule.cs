using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adenium.Models
{
    [Table("role_exp_rules")]
    public class RoleExpRule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("guild_id", TypeName = "bigint")]
        public long GuildId { get; set; }

        [Column("role_id", TypeName = "bigint")]
        public long RoleId { get; set; }
        
        [Column("exp_amount")]
        public int ExpAmount { get; set; }
        [Column("role_name")]
        [MaxLength(200)]
        public string RoleName { get; set; } = string.Empty;
    }
}
