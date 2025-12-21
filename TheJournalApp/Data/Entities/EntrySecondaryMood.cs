namespace TheJournalApp.Data.Entities;

public class EntrySecondaryMood
{
    public int EntryId { get; set; }
    public int MoodId { get; set; }

    // Navigation properties
    public JournalEntry Entry { get; set; } = null!;
    public Mood Mood { get; set; } = null!;
}
