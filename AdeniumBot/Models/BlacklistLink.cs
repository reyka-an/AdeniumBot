namespace AdeniumBot.Models
{
    public class BlacklistLink
    {
        public int OwnerId { get; set; }
        public PlayerProfile Owner { get; set; } = default!;

        public int TargetId { get; set; }
        public PlayerProfile Target { get; set; } = default!;
    }
}