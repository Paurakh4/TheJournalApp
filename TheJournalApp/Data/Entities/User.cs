namespace TheJournalApp.Data.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public UserSettings? Settings { get; set; }
    public Streak? Streak { get; set; }
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<ExportLog> ExportLogs { get; set; } = new List<ExportLog>();
}
