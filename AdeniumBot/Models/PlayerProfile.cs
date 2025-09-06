namespace Adenium.Models
{
    public class PlayerProfile
    {
        public int Id { get; set; }
        public long DiscordUserId { get; set; }   
        public string Username { get; set; } = "";
        public int Exp { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<PlayerQuest> Quests { get; set; } = new List<PlayerQuest>();
        public ICollection<FavoriteLink> Favorites { get; set; } = new List<FavoriteLink>(); 
        public ICollection<FavoriteLink> FavoritedBy { get; set; } = new List<FavoriteLink>(); 
        public ICollection<BlacklistLink> Blacklist { get; set; } = new List<BlacklistLink>(); 
        public ICollection<BlacklistLink> BlacklistedBy { get; set; } = new List<BlacklistLink>(); 
    }
}