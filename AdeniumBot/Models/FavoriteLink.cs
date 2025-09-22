namespace AdeniumBot.Models
{
    public class FavoriteLink
    {
        public int OwnerId { get; set; }
        public PlayerProfile Owner { get; set; } = default!;

        public int TargetId { get; set; }
        public PlayerProfile Target { get; set; } = default!;
    }
}