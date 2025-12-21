namespace TheJournalApp.Data.Entities;

public class JournalEntry
{
    public int EntryId { get; set; }
    public int UserId { get; set; }
    public int PrimaryMoodId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int WordCount { get; set; } = 0;
    public DateOnly EntryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Mood PrimaryMood { get; set; } = null!;
    public ICollection<EntrySecondaryMood> SecondaryMoods { get; set; } = new List<EntrySecondaryMood>();
    public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
}
