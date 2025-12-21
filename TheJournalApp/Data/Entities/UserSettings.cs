namespace TheJournalApp.Data.Entities;

public class UserSettings
{
    public int SettingId { get; set; }
    public int UserId { get; set; }
    public bool IsDarkMode { get; set; } = false;
    public string? AppPin { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
