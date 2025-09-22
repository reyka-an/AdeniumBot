namespace AdeniumBot.Models
{
    public class PlayerQuest
    {
        public int PlayerId { get; set; }
        public PlayerProfile Player { get; set; } = default!;

        public int QuestId { get; set; }
        public Quest Quest { get; set; } = default!;

        public int CompletedCount { get; set; } = 0;
        public DateTime? LastCompletedAt { get; set; }
    }
}