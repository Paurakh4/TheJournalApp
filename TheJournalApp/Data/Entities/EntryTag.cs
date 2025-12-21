namespace TheJournalApp.Data.Entities;

public class EntryTag
{
    public int EntryId { get; set; }
    public int TagId { get; set; }

    // Navigation properties
    public JournalEntry Entry { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
