namespace TheJournalApp.Data.Entities;

public enum MoodCategory
{
    Positive,
    Neutral,
    Negative
}

public class Mood
{
    public int MoodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MoodCategory Category { get; set; }
    public string Emoji { get; set; } = string.Empty;
    public bool IsSystem { get; set; } = true;

    // Navigation properties
    public ICollection<JournalEntry> PrimaryEntries { get; set; } = new List<JournalEntry>();
    public ICollection<EntrySecondaryMood> SecondaryMoodEntries { get; set; } = new List<EntrySecondaryMood>();
}
