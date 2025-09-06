namespace Adenium.Models
{
    public class Quest
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Description { get; set; } = "";
        public int ExpReward { get; set; } = 0;
        public int? MaxCompletionsPerPlayer { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<PlayerQuest> PlayerQuests { get; set; } = new List<PlayerQuest>();
    }
}