namespace AdeniumBot.Models
{
    public class RoleExpRule
    {
        public int Id { get; set; }
        public long GuildId { get; set; }
        public long RoleId { get; set; }
        public int ExpAmount { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}