namespace TheJournalApp.Data.Entities;

public class ExportLog
{
    public int ExportId { get; set; }
    public int UserId { get; set; }
    public DateOnly RangeStart { get; set; }
    public DateOnly RangeEnd { get; set; }
    public string Format { get; set; } = "PDF";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
