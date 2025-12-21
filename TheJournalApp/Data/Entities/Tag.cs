namespace TheJournalApp.Data.Entities;

public class Tag
{
    public int TagId { get; set; }
    public int? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsBuiltin { get; set; } = false;

    // Navigation properties
    public User? User { get; set; }
    public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
}
